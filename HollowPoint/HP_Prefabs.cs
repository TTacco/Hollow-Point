using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ModCommon;
using System.Linq;
using System.Text;
using Modding;
using ModCommon.Util;


namespace HollowPoint
{
    class HP_Prefabs : MonoBehaviour
    {

        public static GameObject bulletPrefab;
        public static GameObject bulletFadePrefab;
        public static GameObject bulletTrailPrefab;


        //public static GameObject greenscreen;
        public static GameObject blood = null;
        public static GameObject explosion = null;
        public static GameObject takeDamage = null;

        public static Dictionary<String, Sprite> projectileSprites = new Dictionary<String, Sprite>();
        public void Start()
        {
            StartCoroutine(CreateBulletPrefab());
            ModHooks.Instance.ObjectPoolSpawnHook += Instance_ObjectPoolSpawnHook;
        }

        private GameObject Instance_ObjectPoolSpawnHook(GameObject go)
        {
            Modding.Logger.Log(go.name);
            return go;
        }

        public void Instantiated()
        {

        }

        IEnumerator CreateBulletPrefab()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);

            Resources.LoadAll<GameObject>("");
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {

                if (go.name.Equals("particle_orange blood") && blood == null)
                {
                    //globalPrefabDict.Add("blood", Instantiate(go));
                    blood = go;
                    //blood.SetActive(false);
                    Modding.Logger.Log("Found blood!");
                }

                else if (go.name.Equals("Gas Explosion Recycle M") && explosion == null)
                {
                    //globalPrefabDict.Add("explosion medium", Instantiate(go));
                    explosion = go;
                    //explosion.SetActive(false);
                    Modding.Logger.Log("Found the explosion!");
                }


            }


            LoadAssets.spriteDictionary.TryGetValue("bulletSprite.png", out Texture2D bulletTexture);
            LoadAssets.spriteDictionary.TryGetValue("bulletSpriteFade.png", out Texture2D fadeTexture);

            //Prefab instantiation
            bulletPrefab = new GameObject("bulletPrefabObject", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(HP_BulletBehaviour), typeof(AudioSource));
            bulletPrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(bulletTexture,
                new Rect(0, 0, bulletTexture.width, bulletTexture.height),
                new Vector2(0.5f, 0.5f), 42);


            //Special bullet sprite
            LoadAssets.spriteDictionary.TryGetValue("specialbullet.png", out Texture2D specialBulletTexture);
            projectileSprites.Add("specialbullet.png", Sprite.Create(specialBulletTexture,
                new Rect(0, 0, specialBulletTexture.width, specialBulletTexture.height),
                new Vector2(0.5f, 0.5f), 42));
            //Rigidbody
            bulletPrefab.GetComponent<Rigidbody2D>().isKinematic = true;
            bulletPrefab.transform.localScale = new Vector3(1.2f, 1.2f, 0);

            //Collider Changes
            BoxCollider2D bulletCol = bulletPrefab.GetComponent<BoxCollider2D>();
            bulletCol.enabled = false;
            bulletCol.isTrigger = true;
            bulletCol.size = bulletPrefab.GetComponent<SpriteRenderer>().size + new Vector2(-0.30f, -0.60f);
            bulletCol.offset = new Vector2(0, 0);

            bulletFadePrefab = new GameObject("bulletFadePrefabObject", typeof(SpriteRenderer));
            bulletFadePrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(fadeTexture,
                new Rect(0, 0, fadeTexture.width, fadeTexture.height),
                new Vector2(0.5f, 0.5f), 42);


            //Trail
            bulletTrailPrefab = new GameObject("bulletTrailPrefab", typeof(TrailRenderer));
            TrailRenderer bulletTR = bulletTrailPrefab.GetComponent<TrailRenderer>();
            //bulletTR.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            bulletTR.material = new Material(Shader.Find("Diffuse")); 
            //bulletTR.material = new Material(Shader.Find("Particles/Additive"));
            //bulletTR.widthMultiplier = 0.05f;
            bulletTR.startWidth = 0.08f;
            bulletTR.endWidth = 0.04f;
            bulletTR.numCornerVertices = 50;
            bulletTR.numCapVertices = 30;
            bulletTR.enabled = true;
            bulletTR.time = 0.045f; //0.075
            bulletTR.startColor = new Color(240, 234, 196);
            bulletTR.endColor = new Color(237, 206, 154);

            bulletPrefab.SetActive(false);

            DontDestroyOnLoad(bulletPrefab);
            DontDestroyOnLoad(bulletFadePrefab);
            DontDestroyOnLoad(bulletTrailPrefab);

            Modding.Logger.Log("[HOLLOW POINT] Initalized BulletObject");

        }

        public static GameObject SpawnBullet(float bulletDegreeDirection)
        {
            float directionOffsetY = 0;

            if(bulletDegreeDirection > 10 && bulletDegreeDirection < 170)
            {
                directionOffsetY = 0.5f;
            }
            else if(bulletDegreeDirection > 190 && bulletDegreeDirection < 350)
            {
                directionOffsetY = -0.5f;
            }

           
            float directionMultiplierX = (HeroController.instance.cState.facingRight) ? 1f : -1f;

            float wallClimbMultiplier = (HeroController.instance.cState.wallSliding) ? -1f : 1f;

            if (bulletDegreeDirection == 90 || bulletDegreeDirection == 270) directionMultiplierX = 0.2f * directionMultiplierX;

            directionMultiplierX *= wallClimbMultiplier;
                
            GameObject bullet = Instantiate(bulletPrefab, HeroController.instance.transform.position + new Vector3(1.5f * directionMultiplierX, -0.7f + directionOffsetY, -0.002f), new Quaternion(0, 0, 0, 0));

            bullet.GetComponent<HP_BulletBehaviour>().bulletDegreeDirection = bulletDegreeDirection;
            bullet.SetActive(true);

            return bullet;
        }
    }

    

   
    class HP_BulletBehaviour : MonoBehaviour
    {
        GameObject bulletTrailObjectClone;
        Rigidbody2D rb2d;
        BoxCollider2D bc2d;
        SpriteRenderer bulletSprite;
        HealthManager hm;
        double xDeg, yDeg;
        double bulletSpeed = 10;
       
        bool noDamage = false;
        public String specialAttrib;
        public bool pierce = false;
        public bool special = false;
        public int bulletDamage;
        public float bulletSpeedMult = 1;

        public float bulletDegreeDirection = 0;

        public Vector3 bulletOriginPosition;

        static System.Random rand = new System.Random();

        public static HitInstance bulletHitInstance = new HitInstance
        {
            DamageDealt = 4 + (PlayerData.instance.nailSmithUpgrades * 3),
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

            gameObject.tag = "Wall Breaker";

            //Trail    
            bulletTrailObjectClone = Instantiate(HP_Prefabs.bulletTrailPrefab, gameObject.transform);

            //Bullet Distance
            bulletOriginPosition = gameObject.transform.position;

            //Bullet Direction
            float deviation = (HP_HeatHandler.currentHeat * 0.2f) + SpreadDeviationControl.ExtraDeviation();
            //bulletSpeed = HP_WeaponHandler.currentGun.gunBulletSpeed;
            bulletSpeed = 45f * bulletSpeedMult;

            //Bullet Sprite Size
            float size = HP_WeaponHandler.currentGun.gunBulletSize;
            if (special)
            {
                HP_Prefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite specialBulletTexture);
                size *= 1.5f;
                bulletSprite.sprite = specialBulletTexture;
            }

            gameObject.transform.localScale = new Vector3(size, size, 0.90f);
            gameObject.transform.localScale = new Vector3(size, size, 0.90f);

            //Handles weapon spread
            //HP_DirectionHandler.finalDegreeDirection

            float degree = bulletDegreeDirection + (rand.Next((int)-deviation, (int)deviation + 1)) - (float)rand.NextDouble();
            float radian = (float)(degree * Math.PI / 180);

            xDeg = bulletSpeed * Math.Cos(radian);
            yDeg = bulletSpeed * Math.Sin(radian);

            //Changes the degree of bullet sprite rotation and the bullet direction when wall sliding
            if (HeroController.instance.cState.wallSliding)
            {
                xDeg *= -1;
                degree += 180;

                if (HeroController.instance.cState.facingRight) degree += 180;
            }

            rb2d.velocity = new Vector2((float)xDeg, (float)yDeg);

            //Bullet Rotation
            bulletSprite.transform.Rotate(0, 0, degree + HP_Sprites.SpriteRotationWallSlide(), 0);
        }

        //Handles the colliders
        void OnTriggerEnter2D(Collider2D col)
        {
            hm = col.GetComponentInChildren<HealthManager>();
            bulletHitInstance.Source = gameObject;

            //PURE VESSEL CHECK
            if (col.gameObject.name.Contains("Idle"))
            {
                Modding.Logger.Log("PV IS HIT");

                Component[] pvc = col.gameObject.GetComponents<Component>();

                foreach(Component c in pvc){
                    Type type = c.GetType();

                    //Transform BoxCollider2D DamageHero
                    if (type.Name.Contains("Transform")){
                        Transform pvt = (Transform) c;
                        Component[] parent_pvt = pvt.GetComponentsInParent(typeof(Component));

                        foreach(Component cp in parent_pvt)
                        {
                            Type type_2 = cp.GetType();
                            if (type_2.Name.Contains("HealthManager"))
                            {
                                hm = (HealthManager) cp;
                                break;
                            }
                        }
                    }
                    break;
                }
            }

            if (hm == null && col.gameObject.layer.Equals(8))
            {
                StartCoroutine(wallHitDust());
                //Modding.Logger.Log("hitted collider of name " + col.gameObject.name);
                //TODO: change this audio source location
                LoadAssets.sfxDictionary.TryGetValue("impact_0" + rand.Next(1, 6) + ".wav", out AudioClip ac);
                //if (gameObject.GetComponent<AudioSource>() == null) Modding.Logger.Log("No Audio Source");

                HeroController.instance.GetComponent<AudioSource>().PlayOneShot(ac);

                Destroy(gameObject);
            }
            //Damages the enemy and destroys the bullet
            else if (hm != null)
            {
                //Displays the player Modding.Logger.Log(col.gameObject.layer);

                /*
                GameObject bloodSplat = Instantiate(HP_Prefabs.blood, gameObject.transform.position, Quaternion.identity);
                bloodSplat.SetActive(true);
                */


                if (!pierce) Destroy(gameObject);
                
                hm.Hit(bulletHitInstance);


            }
        }


        public IEnumerator wallHitDust() //fuck your naming violations
        {
            ParticleSystem wallDust = Instantiate(HeroController.instance.wallslideDustPrefab);

            Destroy(wallDust, 0.5f);
            wallDust.transform.position = gameObject.transform.position;
            wallDust.Emit(200);
            ParticleSystem.VelocityOverLifetimeModule v = wallDust.velocityOverLifetime;
            
            v.enabled = true;
            float rad = Mathf.Deg2Rad * (gameObject.transform.eulerAngles.z + 180);
            v.xMultiplier = 3f * Mathf.Cos(rad);
            v.yMultiplier = 3f * Mathf.Sin(rad);

            yield return new WaitForSeconds(0.3f);
            v.enabled = false;
        }

       
        public float DamageFalloffCalculation()
        {
            return 0f;
        }

        public void OnDestroy()
        {
            Destroy(Instantiate(HP_Prefabs.bulletFadePrefab, gameObject.transform.position, gameObject.transform.localRotation), 0.03f); //bullet fade out sprite

            if (gameObject.GetComponent<HP_BulletBehaviour>().specialAttrib.Contains("Explosion"))
            {
               GameObject explosionClone = Instantiate(HP_Prefabs.explosion, gameObject.transform.position, Quaternion.identity);
                explosionClone.SetActive(true);
                explosionClone.name += " KnightMadeExplosion";
            }
        }
    }


    public class OnFire : MonoBehaviour
    {
        public void Start()
        {
            Destroy(this, 10f);
            StartCoroutine(SetEnemyOnFire(gameObject));
        }

        public void Update()
        {
            Modding.Logger.Log(gameObject.name + " is on fire!");
        }

        public void OnDestroy()
        {
            Modding.Logger.Log(gameObject.name + " is no longer on fire!");
        }


        IEnumerator SetEnemyOnFire(GameObject targetEnemyToBurn)
        {
            HealthManager hm = targetEnemyToBurn.GetComponent<HealthManager>();

            while (true)
            {
                yield return new WaitForSeconds(0.2f);
                hm.Hit(HP_BulletBehaviour.bulletHitInstance);
            }

            yield return null; 
        }
    }

}
