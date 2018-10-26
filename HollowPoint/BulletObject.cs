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
        public static GameObject bulletPrefab;
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


            bulletPrefab = new GameObject("bulletPrefabObject", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(BulletBehavior));

            //
            bulletPrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(LoadAssets.bulletSprite,
                new Rect(0, 0, LoadAssets.bulletSprite.width, LoadAssets.bulletSprite.height),
                new Vector2(0.5f, 0.5f), 22);

            bulletPrefab.GetComponent<Rigidbody2D>().isKinematic = true;

            bulletPrefab.transform.localScale = new Vector3(1.2f,1.2f,0);


            //Collider Changes
            bulletPrefab.GetComponent<BoxCollider2D>().enabled = true;
            bulletPrefab.GetComponent<BoxCollider2D>().isTrigger = true;
            bulletPrefab.GetComponent<BoxCollider2D>().size = bulletPrefab.GetComponent<SpriteRenderer>().size - new Vector2(0.15f, 0.15f);

            Log(bulletPrefab.GetComponent<SpriteRenderer>().size);
            bulletPrefab.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0);

            DontDestroyOnLoad(bulletPrefab);

            Log("[HOLLOW POINT] Initalized BulletObject");
        }

        public void Update()
        {
        }

        //Creates the bullet object whenever this method is called
        public static void FireBullet()
        {
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");

            //HeroController.instance.spellControl.gameObject.transform.position
            GameObject bulletClone = Instantiate(bulletPrefab, GunSpriteController.gunSpriteGO.transform.position + new Vector3(0.01f * DirectionX(), 0, -1), new Quaternion(0, ObjectSpriteRotation(), 0, 0));
            Rigidbody2D rd = bulletClone.GetComponent<Rigidbody2D>();


            rd.velocity = new Vector2(DirectionX() * XVelocity(), DirectionY() * YVelocity());
            Log("Returns " + DirectionY());


            //Set bullet sprite rotation, so it looks normal when fired diagonally or upwards
            bulletClone.transform.Rotate(new Vector3(0, 0, DirectionY() * SpriteRotation()));

            //Destroys the fucking object after 1.5f what else do you expect?
            Destroy(bulletClone, 1.5f);
        }

        //Returns on what direction the object should be going, -1 will inverse it, which is where the player would look at this left
        static int DirectionX()
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

        static int DirectionY()
        {
            if (InputHandler.Instance.inputActions.down.IsPressed)
            {
                return -1;
            }

            return 1;
        }

        //Changes the rotation of the sprite (not the direction, just the sprite) depending on where the player is looking
        static float ObjectSpriteRotation()
        {
            if (HeroController.instance.cState.facingRight)
            {
                return 0;
            }
            else if (!HeroController.instance.cState.facingRight)
            {
                return 180;
            }

            return 0;
        }

        //Changes on the sprite rotation on the bullet, for firing diagonally
        static float SpriteRotation()
        {
            if ((InputHandler.Instance.inputActions.up.IsPressed || InputHandler.Instance.inputActions.down.IsPressed) && !(InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed))
            {
                return 90;
            }

            if (InputHandler.Instance.inputActions.up.IsPressed || InputHandler.Instance.inputActions.down.IsPressed)
            {
                if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                {
                    return 45;
                }
            }


            return 0;
        }

        #region BulletDirection

        static float YVelocity()
        {
            if (InputHandler.Instance.inputActions.up.IsPressed && (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed))
            {
                return 45;
            }
            else if (InputHandler.Instance.inputActions.up.IsPressed || InputHandler.Instance.inputActions.down.IsPressed)
            {
                return 45;
            }

            return 0;
        }

        static float XVelocity()
        {
            //If the player is holding up or down ONLY, do not give X velocity so the bullet will only go straight up or down
            if ((InputHandler.Instance.inputActions.up.IsPressed || InputHandler.Instance.inputActions.down.IsPressed) && !((InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed))) 
            {
                return 0;
            }
            else
            {
                return 45;
            }
        }

        #endregion

        class BulletOnHit : MonoBehaviour
        {
            //HealthManager hm;

            //private HitInstance damage = new HitInstance
            //{
            //    AttackType = AttackTypes.NailBeam,
            //    DamageDealt = 3,
            //    Multiplier = 1,
            //    IgnoreInvulnerable = true,

            //    //CircleDirection = false,
            //    //IsExtraDamage = false,
            //    //Direction = 1,
            //    //MoveAngle = 1,
            //    //MoveDirection = false,
            //    //MagnitudeMultiplier = 1,
            //    //SpecialType = SpecialTypes.None,

            //};

            //public void Start()
            //{
            //    On.HealthManager.Hit += BulletDamage;
            //}

            //public void Update()
            //{
            //}

            //void OnTriggerEnter2D(Collider2D col)
            //{
            //    hm = col.GetComponentInChildren<HealthManager>();
            //    damage.Source = gameObject;

            //    if (col.gameObject.GetComponentInChildren<HealthManager>() != null)
            //    {
            //        //Log("HEALTH MANAGER =  TRUE and it was " + col.name);
            //        hm.Hit(damage);
            //        Destroy(gameObject);
            //        Log(col.name + " has destroyed the GO");
            //    }
            //    else if (!col.name.Contains("Knight") && CloseToKnight(gameObject))
            //    {
            //        //=null && !col.name.Contains("Knight")
            //        //Log("HEALTH MANAGER = FALSE and it was " + col.name);
            //        Log("GO destroyed at " + gameObject.transform.position);
            //        Log("Player was at " + HeroController.instance.transform.position);

            //        Destroy(gameObject);
            //        Log(col.name + " has destroyed the GO");
            //        //Log("From the Knight");

            //        // StartCoroutine(ParentRecursion(col));
            //    }
            //}

            ////Returns false if the bullet is close enough to the knight, thus making sure the bullet is NOT destroyed by the Knight's own hitbox
            //bool CloseToKnight(GameObject bulletGO)
            //{
            //    Vector3 tempKnightP = HeroController.instance.transform.position;
            //    Vector3 tempBulletP = bulletGO.transform.position;

            //    double distance = Math.Pow(tempKnightP.x - tempBulletP.x, 2) + Math.Pow(tempKnightP.y - tempBulletP.y, 2);

            //    //Log("DISTANCE IS " + distance);

            //    if (distance < 2.5)
            //        return false;

            //    return true;
            //}

            //public void BulletDamage(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
            //{
            //    if (hitInstance.Source.name.Contains("bullet"))
            //    {
            //        DamageEnemies.HitEnemy(self, hitInstance.DamageDealt, hitInstance, 0);
            //    }
            //    else
            //    {
            //        orig(self, hitInstance);
            //    }
            //}

            //public void OnDestroy()
            //{
            //    On.HealthManager.Hit -= BulletDamage;
            //}

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
