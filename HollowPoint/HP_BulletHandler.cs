using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModCommon.Util;

namespace HollowPoint
{
    class HP_BulletHandler : MonoBehaviour
    {

        public static GameObject bulletPrefab;
        public static GameObject bulletFadePrefab;
        public static GameObject bulletTrailPrefab;
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
            LoadAssets.spriteDictionary.TryGetValue("bulletSpriteFade.png", out Texture2D fadeTexture);

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

            bulletFadePrefab = new GameObject("bulletFadePrefabObject", typeof(SpriteRenderer));
            bulletFadePrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(fadeTexture,
                new Rect(0, 0, fadeTexture.width, fadeTexture.height),
                new Vector2(0.5f, 0.5f), 42);


            //Trail
            bulletTrailPrefab = new GameObject("bulletTrailPrefab", typeof(TrailRenderer));
            TrailRenderer bulletTR = bulletTrailPrefab.GetComponent<TrailRenderer>();
            //bulletTR.widthMultiplier = 0.05f;
            bulletTR.startWidth = 0.1f;
            bulletTR.endWidth = 0.050f;
            bulletTR.enabled = true;
            bulletTR.time = 0.075f;
            bulletTR.startColor = new Color(240, 234, 196);
            bulletTR.endColor = new Color(237, 206, 154);
            bulletTR.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));

            DontDestroyOnLoad(bulletPrefab);
            DontDestroyOnLoad(bulletFadePrefab);
            DontDestroyOnLoad(bulletTrailPrefab);

            Modding.Logger.Log("[HOLLOW POINT] Initalized BulletObject");

        }
    }

    class HP_BulletBehaviour : MonoBehaviour
    {
        GameObject parentGo;
        GameObject bulletTrailObjectClone;
        GameObject blockHitPrefab = null;
        Rigidbody2D rb2d;
        BoxCollider2D bc2d;
        SpriteRenderer bulletSprite;
        HealthManager hm;      
        double xDeg, yDeg;
        double bulletSpeed = 10;
        bool noDamage = false;
        

        Vector3 bulletOriginPosition;

        System.Random rand = new System.Random();

        private HitInstance damage = new HitInstance
        {          
            DamageDealt = 4 + (PlayerData.instance.nailSmithUpgrades * 2),
            Multiplier = 1,
            IgnoreInvulnerable = false,
            CircleDirection = true,
            IsExtraDamage = false,
            Direction = 0,
            MoveAngle = 180,
            MoveDirection = false,
            MagnitudeMultiplier = 1,
            SpecialType = SpecialTypes.None,
            
        };

        public void Start()
        {
            On.HealthManager.Hit += BulletDamage;

            rb2d = GetComponent<Rigidbody2D>();
            bc2d = GetComponent<BoxCollider2D>();
            bulletSprite = GetComponent<SpriteRenderer>();
            bc2d.enabled = true;

            //Trail    
            bulletTrailObjectClone = Instantiate(HP_BulletHandler.bulletTrailPrefab, gameObject.transform);

            //Bullet Distance
            bulletOriginPosition = gameObject.transform.position;

            //bulletTR.material = new Material(Shader.Find("Particles/Additive"));
            //Bullet Direction
            float deviation = HP_WeaponHandler.currentGun.gunDeviation + SpreadDeviationControl.ExtraDeviation();
            bulletSpeed = HP_WeaponHandler.currentGun.gunBulletSpeed;
            float size = HP_WeaponHandler.currentGun.gunBulletSize;
            gameObject.transform.localScale = new Vector3(size, size, 0.90f);
            gameObject.transform.localScale = new Vector3(size, size, 0.90f);

            //TODO: Weapon mechanic where spread is greater if the player is moving

            //Handles weapon spread
            float degree = HP_DirectionHandler.finalDegreeDirection + (rand.Next((int)-deviation,(int)deviation+1)) - (float)rand.NextDouble();
            float radian = (float) (degree * Math.PI / 180);

            xDeg = bulletSpeed * Math.Cos(radian);
            yDeg = bulletSpeed * Math.Sin(radian);

            //Changes the degree of bullet sprite rotation and the bullet direction when wall sliding
            if (HeroController.instance.cState.wallSliding)
            {
                xDeg *= -1;
                degree += 180;
            }

            //Bullet Rotation

            bulletSprite.transform.Rotate(0, 0, degree + HP_Sprites.SpriteRotationWallSlide(), 0);

            //MuzzleFlash Rotation and Spawn
            float flashOffsetX = (float) (1.9f * Math.Cos(radian));
            float flashOffsetY = (float) (1.9f * Math.Sin(radian));

            Modding.Logger.Log(flashOffsetX + " " + flashOffsetY);

            Destroy(Instantiate(HP_Sprites.muzzleFlashGO, HP_Sprites.gunSpriteGO.transform.position + new Vector3(flashOffsetX, flashOffsetY + 0.25f, -0.8f), bulletSprite.transform.rotation), 0.07f);

        }

        public void FixedUpdate()
        {
            if (HP_WeaponHandler.currentGun.Equals("Flamethrower")) SlowDownBullet();    

            rb2d.velocity = new Vector2((float)xDeg, (float)yDeg);
        }

        //Slows down bullets the longer they travel, will be used for the flamethrower projectiles
        public void SlowDownBullet()
        {
            bool positivex = (xDeg > 0) ? true : false;
            bool positivey = (yDeg > 0) ? true : false;
            if (positivex && xDeg > 0)
            {
                xDeg -= Time.deltaTime * 50;
            }
            else if (!positivex && xDeg < 0)
            {
                xDeg += Time.deltaTime * 50;
            }

            if (positivey && yDeg > 0)
            {
                yDeg -= Time.deltaTime * 50;
            }
            else if (!positivey && yDeg < 0)
            {
                yDeg += Time.deltaTime * 50;
            }
        }

        //Handles the colliders
        void OnTriggerEnter2D(Collider2D col)
        {
            hm = col.GetComponentInChildren<HealthManager>();
            damage.Source = gameObject;

            if (hm == null && col.gameObject.layer.Equals(8))
            {
                //TODO: change this audio source location
                LoadAssets.sfxDictionary.TryGetValue("impact_0" + rand.Next(1,6) + ".wav", out AudioClip ac);
                HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(ac);
                Destroy(gameObject);

            }
            //Damages the enemy and destroys the bullet
            else if (hm != null)
            {         
                hm.Hit(damage); //Due to the bullet being hooked to the HealthManager's Hit method, this call BulletDamage() method below
                Destroy(gameObject);
            }

        }

        //Handles the damage
        public void BulletDamage(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            // TODO: Put these in the weapon handler section
            //damage.AttackType = AttackTypes.Generic;
            // damage.DamageDealt = HP_WeaponHandler.currentGun.gunDamage;
            HP_Gun gunTheBulletCameFrom = HP_WeaponHandler.currentGun;
            float finalBulletDamage = gunTheBulletCameFrom.gunDamage;
            float travelDistance = Vector3.Distance(bulletOriginPosition, self.transform.position) * 10;
            float damageDropOff = Mathf.Floor(travelDistance / gunTheBulletCameFrom.gunDamMultiplier); //gunDamMult basically shows how much distance does it take before the bullet loses 1/4th of its damage
            float damageColor = 0;
            if (damageDropOff == 0)
            {
                finalBulletDamage = gunTheBulletCameFrom.gunDamage; 
            }
            else
            {
                damageColor = damageDropOff / 4;
                finalBulletDamage = Mathf.CeilToInt(finalBulletDamage - (damageColor * finalBulletDamage));
            }

            //TODO: get all of these into the UI handler instead
            Color c = Color.Lerp(new Color(255,128,0), new Color(255,255,0), damageColor);
            if (damageDropOff > 3) //at losing its damage for the 5th time, it now does no damage
            {
                finalBulletDamage = 0;
                blockHitPrefab = self.GetAttr<GameObject>("blockHitPrefab");
                c = new Color(95,95,95);
            }
            HP_DamageNumber.ShowDamageNumbers("" +  finalBulletDamage, self, c); //OUTPUTS THE DAMAGE NUMBERS ON THE HEAD   


            //if the damage is from a bullet, override the damage causing function because the bullets do not function well
            //with the regular HealthManager.Hit() method, and instead we apply our own damage with the use of our self defined "DamageEnemies" class
            if (hitInstance.Source.name.Contains("bullet"))
            {
                //Modding.Logger.Log(travelDistance);
                DamageEnemies.HitEnemy(self,(int) finalBulletDamage, damage, 0); //note the 2nd "damage" var is a hitinstance, not an integer
            }
            else
            {
                orig(self, hitInstance);
            }
        }

        public float DamageFalloffCalculation()
        {
            return 0f;
        }

        public void OnDestroy()
        {
            On.HealthManager.Hit -= BulletDamage;
            Destroy(Instantiate(HP_BulletHandler.bulletFadePrefab, gameObject.transform.position, gameObject.transform.localRotation), 0.025f); //bullet fade out sprite

            bulletTrailObjectClone.transform.SetParent(null);
            bulletTrailObjectClone.GetComponent<TrailRenderer>().autodestruct = true;

            if(blockHitPrefab != null)
            {
                GameObject bH = blockHitPrefab.Spawn();
                bH.transform.position = gameObject.transform.position;
            }
        }
    }
}
