using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HollowPoint
{
    class HP_BulletHandler : MonoBehaviour
    {

        public static GameObject bulletPrefab;
        //public static GameObject greenscreen;

        public void Start()
        {
            StartCoroutine(CreateBulletPrefab());
        }

        IEnumerator CreateBulletPrefab()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);

            /*
            Texture2D greenscreentex;
            LoadAssets.spriteDictionary.TryGetValue("anim.png", out greenscreentex);
            greenscreen = new GameObject("greenscreenPrefabObject", typeof(SpriteRenderer));
            greenscreen.GetComponent<SpriteRenderer>().sprite = Sprite.Create(greenscreentex,
                new Rect(0, 0, greenscreentex.width, greenscreentex.height),
                new Vector2(0.5f, 0.5f), 1);

            greenscreen.transform.SetPositionZ(0.01f);
            DontDestroyOnLoad(greenscreen);
            */

            Texture2D bulletTexture;
           

            LoadAssets.spriteDictionary.TryGetValue("bulletSprite.png", out bulletTexture);

            //Prefab instantiation
            bulletPrefab = new GameObject("bulletPrefabObject", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(HP_BulletBehaviour));
            bulletPrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(bulletTexture,
                new Rect(0, 0, bulletTexture.width, bulletTexture.height),
                new Vector2(0.5f, 0.5f), 42);

            bulletPrefab.GetComponent<Rigidbody2D>().isKinematic = true;
            bulletPrefab.transform.localScale = new Vector3(1.2f, 1.2f, 0);

            //Collider Changes
            bulletPrefab.GetComponent<BoxCollider2D>().enabled = false;
            bulletPrefab.GetComponent<BoxCollider2D>().isTrigger = true;
            bulletPrefab.GetComponent<BoxCollider2D>().size = bulletPrefab.GetComponent<SpriteRenderer>().size - new Vector2(0.10f, 0.10f);
            bulletPrefab.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0);

            DontDestroyOnLoad(bulletPrefab);

            Modding.Logger.Log("[HOLLOW POINT] Initalized BulletObject");

        }
    }

    class HP_BulletBehaviour : MonoBehaviour
    {
        GameObject parentGo;
        Rigidbody2D rb2d;
        BoxCollider2D bc2d;
        SpriteRenderer bulletSprite;
        HealthManager hm;
        double bulletSpeed = 35;
        double xDeg, yDeg;
        System.Random rand = new System.Random();

        private HitInstance damage = new HitInstance
        {
            AttackType = AttackTypes.Spell,
            DamageDealt = 4 + (PlayerData.instance.nailSmithUpgrades * 2),
            Multiplier = 1,
            IgnoreInvulnerable = false,

            //CircleDirection = false,
            //IsExtraDamage = false,
            Direction = 0,
            MoveAngle = 180,
            //MoveDirection = false,
            //MagnitudeMultiplier = 1,
            //SpecialType = SpecialTypes.None,
        };

        public void Start()
        {
            On.HealthManager.Hit += BulletDamage;

            rb2d = GetComponent<Rigidbody2D>();
            bc2d = GetComponent<BoxCollider2D>();
            bulletSprite = GetComponent<SpriteRenderer>();

            bc2d.enabled = true;

            //Bullet Direction
            float deviation = HP_WeaponHandler.currentGun.gunDeviation;

            rand.NextDouble();

            float degree = HP_DirectionHandler.finalDegreeDirection + (rand.Next((int)-deviation,(int)deviation+1)) - (float)rand.NextDouble();
            float radian = (float) (degree * Math.PI / 180);

            xDeg = bulletSpeed * Math.Cos(radian);
            yDeg = bulletSpeed * Math.Sin(radian);

            //Bullet Rotation
            bulletSprite.transform.Rotate(0, 0, degree, 0);
        }

        public void FixedUpdate()
        {
            rb2d.velocity = new Vector2((float)xDeg, (float)yDeg); 
        }


        //Handles the colliders
        void OnTriggerEnter2D(Collider2D col)
        {
            hm = col.GetComponentInChildren<HealthManager>();
            damage.Source = gameObject;
            if (hm == null && col.gameObject.layer.Equals(8))
            {
                LoadAssets.sfxDictionary.TryGetValue("impact_0" + rand.Next(1,6) + ".wav", out AudioClip ac);
                HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(ac);
                Destroy(gameObject);
            }
            //Damages the enemy and destroys the bullet
            else if (hm != null)
            {
                hm.Hit(damage);
                Destroy(gameObject);
            }
        }

        //Handles the damage
        public void BulletDamage(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (hitInstance.Source.name.Contains("bullet"))
            {
                DamageEnemies.HitEnemy(self, hitInstance.DamageDealt, hitInstance, 0);
            }
            else
            {
                orig(self, hitInstance);
            }
        }

        public void OnDestroy()
        {
            On.HealthManager.Hit -= BulletDamage;
        }
    }
}
