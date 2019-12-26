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
        public static GameObject dungexplosion = null;
        public static GameObject explosion = null;
        public static GameObject knight_spore = null;
        public static GameObject takeDamage = null;

        public static Dictionary<String, Sprite> projectileSprites = new Dictionary<String, Sprite>();
        public static Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();
        public void Start()
        {
            StartCoroutine(CreateBulletPrefab());
            ModHooks.Instance.ObjectPoolSpawnHook += Instance_ObjectPoolSpawnHook;
        }

        private GameObject Instance_ObjectPoolSpawnHook(GameObject go)
        {
            //Modding.Logger.Log(go.name);
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
                //Modding.Logger.Log(go.name);
                //if (go.name.Equals("shadow burst") && blood == null)
                if (go.name.Equals("particle_orange blood") && blood == null)
                {
                    //globalPrefabDict.Add("blood", Instantiate(go));
                    blood = go;
                    //blood.SetActive(false);
                    Modding.Logger.Log(go.name);
                }

                else if (go.name.Equals("Gas Explosion Recycle M") && explosion == null)
                {
                    //globalPrefabDict.Add("explosion medium", Instantiate(go));
                    explosion = go;
                    //explosion.SetActive(false);
                    Modding.Logger.Log(go.name);
                }

                else if (go.name.Equals("Dung Explosion") && dungexplosion == null)
                {                   
                    dungexplosion = go;
                    Modding.Logger.Log(go.name);
                }

                else if (go.name.Equals("Knight Spore Cloud") && knight_spore == null)
                {
                    prefabDictionary.Add("Knight Spore Cloud", go);
                    knight_spore = go;
                    Modding.Logger.Log(go.name);
                }
                else if (go.name.Equals("Knight Dung Cloud") && knight_spore == null)
                {
                    prefabDictionary.Add("Knight Dung Cloud", go);
                    knight_spore = go;
                    Modding.Logger.Log(go.name);
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

            //Get the cool af white particles from fireball and add it to the bullets
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
        public string specialAttrib;
        public bool pierce = false;
        public bool isSingleFire = false; //determines if the bullet is shot from regular mode


        public int bulletDamage;
        public float bulletSpeedMult = 1; 
        public float bulletDegreeDirection = 0;
        public Vector3 bulletOriginPosition;

        public bool ignoreCollisions = false;
        public bool hasSporeCloud = true;

        //Fire Support Attribs
        public bool special = false;
        public bool isFireSupportBullet = false;
        public Vector3 targetDestination;

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
            bc2d.enabled = !ignoreCollisions;

            gameObject.tag = "Wall Breaker";

            //Trail    
            bulletTrailObjectClone = Instantiate(HP_Prefabs.bulletTrailPrefab, gameObject.transform);


            //Override this entire code if its from fire support and give the bullet its own special properties aka because making new GOs with code is effort
            if (isFireSupportBullet)
            {
                bulletSprite.transform.Rotate(0, 0, 270);
                bulletTrailObjectClone.GetComponent<TrailRenderer>().time = 0.9f;
                HP_Prefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite specialBulletTexture);

                gameObject.transform.localScale = new Vector3(1.2f, 1.2f, 0.90f);
                bulletSprite.sprite = specialBulletTexture;

                rb2d.velocity = new Vector2(0, -120);


                return;
            }

            //===================================REGULAR BULLET ATTRIBUTES=========================================

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
                size *= 1.25f;
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

        //For tracking fire support target
        void FixedUpdate()
        {
            if(isFireSupportBullet)
            {
                if(gameObject.transform.position.y < targetDestination.y)
                {
                    Destroy(gameObject);
                }
            }
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

                //Mark target for fire support
                if (special)
                {
                    FireSupportTarget(gameObject);
                    return;
                }


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
                //Mark target for fire support
                if (special)
                {
                    FireSupportTarget(gameObject);
                    return;
                }

                if (!pierce) Destroy(gameObject);
                
                hm.Hit(bulletHitInstance);
            }
        }

        /* NOTE:
            Heres the thing with this method
                
            Turns out if the game object that gets deleted, whatever coroutine they do also gets deleted
            Which is why the coroutine only fires 1 round before destroying itself
            This method just ensures that theres a long enough lifespan on the bullet once it hits that it'll be able to
            deplete all the rounds
        */
        public void FireSupportTarget(GameObject fireSupportGO)
        {
            Vector3 pos = gameObject.transform.position;
            Modding.Logger.Log("CALL AN AIR STRIKE AT THIS POSITION " + pos);
            
            fireSupportGO.GetComponent<BoxCollider2D>().enabled = false; //If i dont disable the collider, itll keep colliding and keep calling FireSupportTarget
            fireSupportGO.GetComponent<SpriteRenderer>().enabled = false; //Just to make sure it stops showing up
            fireSupportGO.GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0); //Same for line render
            StartCoroutine(HP_SpellControl.CallMortar(pos));

            Destroy(fireSupportGO, 25f);
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

            
            if (specialAttrib.Contains("DungExplosion"))
            {
                GameObject explosionClone = Instantiate(HP_Prefabs.dungexplosion, gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                explosionClone.SetActive(true);
                explosionClone.name += " KnightMadeDungExplosion";
            }

            else if (gameObject.GetComponent<HP_BulletBehaviour>().specialAttrib.Contains("Explosion") || isFireSupportBullet)
            {
                GameObject explosionClone = Instantiate(HP_Prefabs.explosion, gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                explosionClone.SetActive(true);
                explosionClone.name += " KnightMadeExplosion";


                if (isFireSupportBullet)
                {
                    HP_SpellControl.PlayAudio("mortarexplosion", true);
                }

                HP_Prefabs.prefabDictionary.TryGetValue("Knight Spore Cloud", out GameObject knightgascloud);
                GameObject knightspore = Instantiate(knightgascloud, gameObject.transform.position + new Vector3(0, 0, -1), Quaternion.identity);
                knightspore.SetActive(true);
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
