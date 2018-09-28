using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using static Modding.Logger;

namespace HollowPoint
{
    public class BulletObject : MonoBehaviour
    {
        public GameObject bulletPrefab;
        public Transform bulletSpawn;
        public float bulletForce;

        public void Start()
        {
            /*
            bulletPrefab.AddComponent<BoxCollider2D>();
            bulletPrefab.AddComponent<Rigidbody2D>().isKinematic = true;
            bulletPrefab.AddComponent<SpriteRenderer>().sprite = Sprite.Create(LoadAssets.bulletSprite,
                new Rect(0, 0, LoadAssets.bulletSprite.width, LoadAssets.bulletSprite.height),
                new Vector2(0.5f, 0.5f), 1);
                */
            StartCoroutine(CreateBulletPrefab());

        }

        IEnumerator CreateBulletPrefab()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);


            bulletPrefab = new GameObject("bulletPrefabObject", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Bullet),
            typeof(DamageEnemies));
            bulletPrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(LoadAssets.bulletSprite,
                new Rect(0, 0, LoadAssets.bulletSprite.width, LoadAssets.bulletSprite.height),
                new Vector2(0.5f, 0.5f), 10);

            bulletPrefab.GetComponent<Rigidbody2D>().isKinematic = true;

            //Collider Changes
            bulletPrefab.GetComponent<BoxCollider2D>().enabled = true;
            bulletPrefab.GetComponent<BoxCollider2D>().isTrigger = true;
            bulletPrefab.GetComponent<BoxCollider2D>().size = bulletPrefab.GetComponent<SpriteRenderer>().size;

            Log(bulletPrefab.GetComponent<SpriteRenderer>().size);
            bulletPrefab.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0);


            DontDestroyOnLoad(bulletPrefab);

            Log("[HOLLOW POINT] Initalized BulletObject");
        }

        public void Update()
        {
            if (AmmunitionControl.firing)
            {
                AmmunitionControl.firing = false;
                FireBullet();
            }
        }

        public void OnCollisionEnter2D()
        {
            Log("Collision Enter");
        }

        public void OnTriggerEnter2D()
        {
            Log("Trigger Enter");
        }

        public void FireBullet()
        {
            Log("Firing");
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");

            GameObject bulletClone = Instantiate(bulletPrefab, HeroController.instance.spellControl.gameObject.transform.position + new Vector3(1 * Direction(), -0.8f, 0), new Quaternion(0, DirectionRotation(), 0, 0));
            Rigidbody2D rd = bulletClone.GetComponent<Rigidbody2D>();

            rd.velocity = Direction() * new Vector2(45, 0);

            Destroy(bulletClone, 5f);
        }

        int Direction()
        {
            if (HeroController.instance.cState.facingRight)
            {
                return 1;
            }
            else if (!HeroController.instance.cState.facingRight)
            {
                return -1;
            }

            return 1;
        }

        float DirectionRotation()
        {
            if (HeroController.instance.cState.facingRight)
            {
                return 0;
            }
            else if (!HeroController.instance.cState.facingRight)
            {
                return 180;
            }

            return 1;
        }

        class Bullet : MonoBehaviour
        {
            public Vector3 Speed;

            public void Update()
            {
                
            }

            public void OnCollisionEnter2D(Collision2D col)
            {
                Modding.Logger.Log("Hitbox Hit");
            }

        }



    }
}











/*
public static GameObject BulletPrefab;
public static Sprite BulletSprite;


public static GameObject Create(Vector3 origin, Vector3 Direction, float Speed)
{
    if (BulletPrefab == null)
    {
        BulletPrefab = new GameObject("BulletObject", typeof(GunSpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Bullet));
        BulletPrefab.SetActive(false);

        Rigidbody2D rb2d = BulletPrefab.GetComponent<Rigidbody2D>();
        rb2d.isKinematic = true;
        rb2d.freezeRotation = true;
        GameObject.DontDestroyOnLoad(BulletPrefab);
    }

    GameObject bullet = GameObject.Instantiate(BulletPrefab);
    bullet.transform.position = origin;
    bullet.GetComponent<Rigidbody2D>().velocity = Direction * Speed;
    bullet.GetComponent<Bullet>().speed = Direction * Speed;
    return bullet;
}
}

class Bullet : MonoBehaviour
{
public Vector3 speed;
public void OnCollisionEnter2D(Collision2D col)
{

}
}
*/
