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

            Texture2D bulletTexture;        
            LoadAssets.spriteDictionary.TryGetValue("bulletSprite.png", out bulletTexture);
            LoadAssets.spriteDictionary.TryGetValue("bulletSpriteFade.png", out Texture2D fadeTexture);

            //Prefab instantiation
            bulletPrefab = new GameObject("bulletPrefabObject", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(HP_BulletBehaviour), typeof(AudioSource));
            bulletPrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(bulletTexture,
                new Rect(0, 0, bulletTexture.width, bulletTexture.height),
                new Vector2(0.5f, 0.5f), 42);

            //Rigidbody
            bulletPrefab.GetComponent<Rigidbody2D>().isKinematic = true;
            bulletPrefab.transform.localScale = new Vector3(1.2f, 1.2f, 0);

            //Collider Changes
            bulletPrefab.GetComponent<BoxCollider2D>().enabled = false;
            bulletPrefab.GetComponent<BoxCollider2D>().isTrigger = true;
            bulletPrefab.GetComponent<BoxCollider2D>().size = bulletPrefab.GetComponent<SpriteRenderer>().size + new Vector2(.30f, -0.30f);
            bulletPrefab.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0);

            bulletFadePrefab = new GameObject("bulletFadePrefabObject", typeof(SpriteRenderer));
            bulletFadePrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(fadeTexture,
                new Rect(0, 0, fadeTexture.width, fadeTexture.height),
                new Vector2(0.5f, 0.5f), 42);


            //Trail
            bulletTrailPrefab = new GameObject("bulletTrailPrefab", typeof(TrailRenderer));
            TrailRenderer bulletTR = bulletTrailPrefab.GetComponent<TrailRenderer>();
            //bulletTR.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            bulletTR.material = new Material(Shader.Find("Diffuse")); //Find("Particles/Additive")
            //bulletTR.widthMultiplier = 0.05f;
            bulletTR.startWidth = 0.1f;
            bulletTR.endWidth = 0.050f;
            bulletTR.enabled = true;
            bulletTR.time = 0.1f;
            bulletTR.startColor = new Color(240, 234, 196);
            bulletTR.endColor = new Color(237, 206, 154);
   
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
        AudioSource audio;
        double xDeg, yDeg;
        double bulletSpeed = 10;
        bool noDamage = false;
        public bool pierce = false;

        public Vector3 bulletOriginPosition;

        System.Random rand = new System.Random();

        public static HitInstance damage = new HitInstance
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
            rb2d = GetComponent<Rigidbody2D>();
            bc2d = GetComponent<BoxCollider2D>();
            bulletSprite = GetComponent<SpriteRenderer>();
            bc2d.enabled = true;

            //Trail    
            bulletTrailObjectClone = Instantiate(HP_BulletHandler.bulletTrailPrefab, gameObject.transform);

            //Bullet Distance
            bulletOriginPosition = gameObject.transform.position;

            //Bullet Direction
            float deviation = HP_WeaponHandler.currentGun.gunDeviation + SpreadDeviationControl.ExtraDeviation();
            bulletSpeed = HP_WeaponHandler.currentGun.gunBulletSpeed;
            float size = HP_WeaponHandler.currentGun.gunBulletSize;
            gameObject.transform.localScale = new Vector3(size, size, 0.90f);
            gameObject.transform.localScale = new Vector3(size, size, 0.90f);

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

             rb2d.velocity = new Vector2((float)xDeg, (float)yDeg);

            //Bullet Rotation
            bulletSprite.transform.Rotate(0, 0, degree + HP_Sprites.SpriteRotationWallSlide(), 0);

            //TODO: Refactor this to SpriteHandler
            //MuzzleFlash Rotation and Spawn
            float wallSlideOffset = (HeroController.instance.cState.wallSliding)? -1 : 1;
            float flashOffsetX = (float) (wallSlideOffset * 1.9f * Math.Cos(radian));
            float flashOffsetY = (float) (1.9f * Math.Sin(radian));
            float muzzleFlashWallSlide = (HeroController.instance.cState.wallSliding) ? 180 : 0;

            //Destroy(Instantiate(HP_Sprites.muzzleFlashGO, HP_Sprites.gunSpriteGO.transform.position + new Vector3(flashOffsetX, flashOffsetY + 0.25f, -0.8f), bulletSprite.transform.rotation), 0.07f);
            GameObject muzzleFlashClone = Instantiate(HP_Sprites.muzzleFlashGO, HP_Sprites.gunSpriteGO.transform.position + new Vector3(flashOffsetX, flashOffsetY + 0.25f, -0.8f), new Quaternion(0, 0, 0, 0));
            muzzleFlashClone.transform.Rotate(0, 0, HP_DirectionHandler.finalDegreeDirection + HP_Sprites.SpriteRotationWallSlide() + muzzleFlashWallSlide , 0);

            Destroy(muzzleFlashClone, 0.06f);

        }

        public void FixedUpdate()
        {
            //if (HP_WeaponHandler.currentGun.Equals("Flamethrower")) SlowDownBullet();

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
                StartCoroutine(wallHitDust());

                //TODO: change this audio source location
                LoadAssets.sfxDictionary.TryGetValue("impact_0" + rand.Next(1, 6) + ".wav", out AudioClip ac);
                //if (gameObject.GetComponent<AudioSource>() == null) Modding.Logger.Log("No Audio Source");

                HeroController.instance.GetComponent<AudioSource>().PlayOneShot(ac);
                Destroy(gameObject);
                
            }
            //Damages the enemy and destroys the bullet
            else if (hm != null)
            {
                Modding.Logger.Log(col.gameObject.layer);
                hm.Hit(damage); 
                if(!pierce) Destroy(gameObject);
            }

        }

        public IEnumerator wallHitDust()
        {

            ParticleSystem wallDust = Instantiate(HeroController.instance.wallslideDustPrefab);
            wallDust.transform.position = gameObject.transform.position;
            wallDust.Emit(200);
            ParticleSystem.VelocityOverLifetimeModule v = wallDust.velocityOverLifetime;
            
            v.enabled = true;
            float rad = Mathf.Deg2Rad * (gameObject.transform.eulerAngles.z + 180);
            v.xMultiplier = 3f * Mathf.Cos(rad);
            v.yMultiplier = 3f * Mathf.Sin(rad);

            yield return new WaitForSeconds(0.3f);
        }

       
        public float DamageFalloffCalculation()
        {
            return 0f;
        }

        public void OnDestroy()
        {
            Destroy(Instantiate(HP_BulletHandler.bulletFadePrefab, gameObject.transform.position, gameObject.transform.localRotation), 0.03f); //bullet fade out sprite

            /* TODO: change all of these to the damage handler
            bulletTrailObjectClone.transform.SetParent(null);
            bulletTrailObjectClone.GetComponent<TrailRenderer>().autodestruct = true;

            if(blockHitPrefab != null)
            {
                GameObject bH = blockHitPrefab.Spawn();
                bH.transform.position = gameObject.transform.position;
            }
            */
        }
    }
}
