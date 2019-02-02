using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using static Modding.Logger;

namespace HollowPoint
{
    class HP_BulletPrefab : MonoBehaviour
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

            //Prefab instantiation
            bulletPrefab = new GameObject("bulletPrefabObject", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(HP_Behaviour_Bullet));
            bulletPrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(LoadAssets.bulletSprite,
                new Rect(0, 0, LoadAssets.bulletSprite.width, LoadAssets.bulletSprite.height),
                new Vector2(0.5f, 0.5f), 42);

            bulletPrefab.GetComponent<Rigidbody2D>().isKinematic = true;
            bulletPrefab.transform.localScale = new Vector3(1.2f, 1.2f, 0);

            //Collider Changes
            bulletPrefab.GetComponent<BoxCollider2D>().enabled = false;
            bulletPrefab.GetComponent<BoxCollider2D>().isTrigger = true;
            bulletPrefab.GetComponent<BoxCollider2D>().size = bulletPrefab.GetComponent<SpriteRenderer>().size - new Vector2(0.10f, 0.10f);
            bulletPrefab.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0);

            DontDestroyOnLoad(bulletPrefab);

            Log("[HOLLOW POINT] Initalized BulletObject");

        }
    }

    /*
     *  ### BULLET BEHAVIOUR ###
     */

    public class HP_Behaviour_Bullet : MonoBehaviour
    {
        System.Random rand = new System.Random();
        HealthManager hm;

        private Rigidbody2D bulletRB2D;
        private SpriteRenderer bulletSprite;

        public float xSpeed;
        public float ySpeed;

        private HitInstance damage = new HitInstance
        {
            AttackType = AttackTypes.NailBeam,
            DamageDealt = 4 + (PlayerData.instance.nailSmithUpgrades * 2),
            Multiplier = 1,
            IgnoreInvulnerable = true,

            //CircleDirection = false,
            //IsExtraDamage = false,
            //Direction = 1,
            //MoveAngle = 1,
            //MoveDirection = false,
            //MagnitudeMultiplier = 1,
            //SpecialType = SpecialTypes.None,

        };

        public void Start()
        {
            On.HealthManager.Hit += BulletDamage;

            //Gets the components this HP_Behaviour_Bullet Monobehaviour has attached with
            bulletRB2D = GetComponent<Rigidbody2D>();
            bulletSprite = GetComponent<SpriteRenderer>();

            //If the bullet sprite rotation should be directed left or right (180 or 0)
            float spriteRotation = (HeroController.instance.cState.facingRight) ? 0f : 180f;
            float xDir = (spriteRotation > 0) ? -1 : 1;

            //If the bullet sprite rotation should be directed upwards or downwards (90 or 270)
            spriteRotation = (HP_DirectionHandler.upPressed && !HP_DirectionHandler.forwardPressed) ? 90 : (HP_DirectionHandler.downPressed && !HP_DirectionHandler.forwardPressed) ? 270 : spriteRotation;

            //If the bullet should be aiming diagonally
            spriteRotation += (HP_DirectionHandler.diagonalUpwardsPressed) ? 45f*xDir : (HP_DirectionHandler.diagonalDownwardsPressed) ? -45f*xDir : 0f;

            bulletSprite.transform.Rotate(0, 0, spriteRotation, 0);
        }

        public void FixedUpdate()
        {
            bulletRB2D.velocity = new Vector2(xSpeed, ySpeed);
        }

        //Handles the colliders
        void OnTriggerEnter2D(Collider2D col)
        {
            hm = col.GetComponentInChildren<HealthManager>();
            damage.Source = gameObject;        

            //Destroy GO when it comes in contact with the terrain
            //Log(col.gameObject.layer);
            if (hm == null && col.gameObject.layer.Equals(8))
            {
                HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.surfaceHitSFX[rand.Next(5)]);
                //Log("Destroyed by " + col.name);
                Destroy(gameObject);
            }
            //Damages the enemy and destroys the bullet
            else if (hm != null)
            {             
                hm.Hit(damage);
                Destroy(gameObject);
                //Log(col.name + " has destroyed the GO");
            }
            /*
            else if (!col.name.Contains("Knight") && !col.name.Contains("Particle") && CloseToKnight(gameObject))
            {
                //Destroy the bullet because its a wall
                Log("GO destroyed at " + gameObject.transform.position);
                Log("Player was at " + HeroController.instance.transform.position);

                //Destroy(gameObject);
                Log(col.name + " has destroyed the GO");
            }
            */

        }    

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
