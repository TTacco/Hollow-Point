using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ModCommon;
using System.Linq;
using System.Text;
using Modding;
using static Modding.Logger;
using static HollowPoint.HollowPointEnums;
using ModCommon.Util;


namespace HollowPoint
{
    class HollowPointPrefabs : MonoBehaviour
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

        //On objects spawning on the world
        private GameObject Instance_ObjectPoolSpawnHook(GameObject go)
        {
            //Modding.Logger.Log(go.name);
            //if (go.name.Contains("Weaverling")) Destroy(go);
            //else if (go.name.Contains("Orbit Shield") && !prefabDictionary.ContainsKey("Orbit Shield"))
            //{
            //    prefabDictionary.Add("Orbit Shield", go);
            //}

            if (go.name.Contains("Grubberfly"))
            {
                Destroy(go);
            }

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

            projectileSprites = new Dictionary<String, Sprite>();
            prefabDictionary = new Dictionary<string, GameObject>();

            Resources.LoadAll<GameObject>("");
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                try
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
                    else if (go.name.Equals("Gas Explosion Recycle M") && !prefabDictionary.ContainsKey("Gas Explosion Recycle M"))
                    {
                        //globalPrefabDict.Add("explosion medium", Instantiate(go));
                        explosion = go;
                        //explosion.SetActive(false);
                        prefabDictionary.Add("Gas Explosion Recycle M", go);
                        Modding.Logger.Log(go.name);

                    }
                    else if (go.name.Equals("Dung Explosion") && !prefabDictionary.ContainsKey("Dung Explosion"))
                    {
                        prefabDictionary.Add("Dung Explosion", go);
                        Modding.Logger.Log(go.name);
                    }
                    else if (go.name.Equals("Knight Spore Cloud") && !prefabDictionary.ContainsKey("Knight Spore Cloud"))
                    {
                        prefabDictionary.Add("Knight Spore Cloud", go);
                        Modding.Logger.Log(go.name);
                    }
                    else if (go.name.Equals("Knight Dung Cloud") && !prefabDictionary.ContainsKey("Knight Dung Cloud"))
                    {
                        prefabDictionary.Add("Knight Dung Cloud", go);
                        Modding.Logger.Log(go.name);
                    }
                    else if (go.name.Equals("soul_particles") && !prefabDictionary.ContainsKey("soul_particles"))
                    {
                        prefabDictionary.Add("soul_particles", go);
                        Modding.Logger.Log(go.name);
                    }
                    else if (go.name.Equals("Focus Effects") && !prefabDictionary.ContainsKey("Focus Effects"))
                    {
                        prefabDictionary.Add("Focus Effects", go);
                        Modding.Logger.Log(go.name);
                    }

                }
                catch (Exception e)
                {
                    Modding.Logger.Log(e);
                }

            }


            LoadAssets.spriteDictionary.TryGetValue("bulletSprite.png", out Texture2D bulletTexture);
            LoadAssets.spriteDictionary.TryGetValue("bulletSpriteFade.png", out Texture2D fadeTexture);

            //Prefab instantiation
            bulletPrefab = new GameObject("bulletPrefabObject", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(BulletBehaviour), typeof(AudioSource));
            bulletPrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(bulletTexture,
                new Rect(0, 0, bulletTexture.width, bulletTexture.height),
                new Vector2(0.5f, 0.5f), 42);



            string[] textureNames = {"specialbullet.png", "furybullet.png", "shadebullet.png"};
            //Special bullet sprite
            /*
            LoadAssets.spriteDictionary.TryGetValue("specialbullet.png", out Texture2D specialBulletTexture);
            projectileSprites.Add("specialbullet.png", Sprite.Create(specialBulletTexture,
                new Rect(0, 0, specialBulletTexture.width, specialBulletTexture.height),
                new Vector2(0.5f, 0.5f), 42));
            */
            
            foreach(string tn in textureNames)
            {
                try
                {
                    LoadAssets.spriteDictionary.TryGetValue(tn, out Texture2D specialBulletTexture);
                    projectileSprites.Add(tn, Sprite.Create(specialBulletTexture,
                        new Rect(0, 0, specialBulletTexture.width, specialBulletTexture.height),
                        new Vector2(0.5f, 0.5f), 42));
                }
                catch(Exception e)
                {
                    Modding.Logger.Log(e);
                }
            }


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
                new Vector2(0.5f, 0.5f), 70);


            //Trail
            bulletTrailPrefab = new GameObject("bulletTrailPrefab", typeof(TrailRenderer));
            TrailRenderer bulletTR = bulletTrailPrefab.GetComponent<TrailRenderer>();
            //bulletTR.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            bulletTR.material = new Material(Shader.Find("Diffuse")); 
            //bulletTR.material = new Material(Shader.Find("Particles/Additive"));
            //bulletTR.widthMultiplier = 0.05f;
            bulletTR.startWidth = 0.2f;
            bulletTR.endWidth = 0.04f;
            bulletTR.numCornerVertices = 50;
            bulletTR.numCapVertices = 30;
            bulletTR.enabled = true;
            bulletTR.time = 0.05f;

            //bulletTR.startColor = new Color(240, 234, 196);
            //bulletTR.endColor = new Color(237, 206, 154);

            bulletTR.startColor = new Color(102, 178, 255);
            bulletTR.endColor = new Color(204, 229, 255);

            bulletPrefab.SetActive(false);

            //Get the cool af white particles from fireball and add it to the bullets
            DontDestroyOnLoad(bulletPrefab);
            DontDestroyOnLoad(bulletFadePrefab);
            DontDestroyOnLoad(bulletTrailPrefab);

            Modding.Logger.Log("[HOLLOW POINT] Initalized BulletObject");

        }

        public static GameObject SpawnBullet(float bulletDegreeDirection, DirectionalOrientation dirOrientation)
        {
            bulletDegreeDirection = bulletDegreeDirection % 360; 

            float directionOffsetY = 0;

            //If the player is aiming upwards, change the bullet offset of where it will spawn
            //Otherwise the bullet will spawn too high or inbetween the knight

            bool dirOffSetBool = (dirOrientation == DirectionalOrientation.Vertical || dirOrientation == DirectionalOrientation.Diagonal);
            bool posYQuadrant = (bulletDegreeDirection > 0 && bulletDegreeDirection < 180);
            if (dirOffSetBool && posYQuadrant)
            {
                directionOffsetY = 0.8f;
            }
            else if(dirOffSetBool && !posYQuadrant)
            {
                directionOffsetY = -1.1f;
            }
   
            float directionMultiplierX = (HeroController.instance.cState.facingRight) ? 1f : -1f;

            float wallClimbMultiplier = (HeroController.instance.cState.wallSliding) ? -1f : 1f;

            //Checks if the player is firing upwards/downwards, and enables the x offset so the bullets spawns directly ontop of the knight
            //from the gun's barrel instead of spawning to the upper right/left of them 

            if (dirOrientation == DirectionalOrientation.Vertical)
            {
                directionMultiplierX = 0.2f * directionMultiplierX;
            }

            directionMultiplierX *= wallClimbMultiplier;
                
            GameObject bullet = Instantiate(bulletPrefab, HeroController.instance.transform.position + new Vector3(1.4f * directionMultiplierX, -0.7f + directionOffsetY, -0.002f), new Quaternion(0, 0, 0, 0));
            bullet.GetComponent<BulletBehaviour>().bulletDegreeDirection = bulletDegreeDirection;
            bullet.SetActive(true);

            return bullet;
        }

        public void OnDestroy()
        {
            ModHooks.Instance.ObjectPoolSpawnHook -= Instance_ObjectPoolSpawnHook;
            Destroy(gameObject.GetComponent<HollowPointPrefabs>());
        }

        public static GameObject SpawnObjectFromDictionary(string key, Vector3 spawnPosition, Quaternion rotation)
        {
            try
            {
                HollowPointPrefabs.prefabDictionary.TryGetValue(key, out GameObject spawnedGO);
                GameObject spawnedGO_Instance = Instantiate(spawnedGO, spawnPosition, rotation);
                spawnedGO_Instance.SetActive(true);
                return spawnedGO_Instance;
            }
            catch (Exception e)
            {
                Log("HP_Prefabs SpawnObjectFromDictionary(): Could not find GameObject with key " + key);
                return null;
            }
        }
    }
   
    class BulletBehaviour : MonoBehaviour
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

        public int bulletDamage;
        public float bulletSpeedMult = 1; 
        public float bulletDegreeDirection = 0;
        public Vector3 bulletOriginPosition;

        public bool ignoreCollisions = false;
        public bool hasSporeCloud = true;

        //Fire Support Attribs
        public bool flareRound = false;
        public bool isFireSupportBullet = false;

        public bool noDeviation = false;
        public bool noHeat = false;
        public bool perfectAccuracy = false;

        public Vector3 targetDestination;

        public HollowPointEnums.FireModes fm = HollowPointEnums.FireModes.Single;
        public HollowPointEnums.BulletType bt = HollowPointEnums.BulletType.Standard;

        public GameObject bulletTrailClone;

        HealthManager pureVesselHM = null;

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
            bulletTrailClone = Instantiate(HollowPointPrefabs.bulletTrailPrefab, gameObject.transform);

            //Increase the bullet size
            bc2d.size = new Vector2(1f, 0.65f);

            //Override this entire code if its from fire support and give the bullet its own special properties aka because making new GOs with code is effort
            if (isFireSupportBullet)
            {
                bulletSprite.transform.Rotate(0, 0, 270);
                bulletTrailObjectClone.GetComponent<TrailRenderer>().time = 0.9f;
                HollowPointPrefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite fireSupportBulletSprite);

                gameObject.transform.localScale = new Vector3(1.2f, 1.2f, 0.90f);
                bulletSprite.sprite = fireSupportBulletSprite;

                rb2d.velocity = new Vector2(0, -120);

                return;
            }

            //===================================REGULAR BULLET ATTRIBUTES=========================================


            //Bullet sprite changer
            string sn = Stats.bulletSprite;
            if (sn != "" && false)
            {
                HollowPointPrefabs.projectileSprites.TryGetValue(Stats.bulletSprite, out Sprite regularBulletSprite);
                bulletSprite.sprite = regularBulletSprite;
            }


            //Bullet Origin for Distance Calculation
            bulletOriginPosition = gameObject.transform.position;

            //Bullet Direction
            float heat = HeatHandler.currentHeat;
            // heat -= (fm == HP_Enums.FireModes.Single && PlayerData.instance.equippedCharm_14 && HeroController.instance.cState.onGround) ? -40 : 0 ;
            // heat = (heat < 0)? 0 :  heat;

            noDeviation = (PlayerData.instance.equippedCharm_14);

            float deviationFromMovement = (noDeviation) ? 0 : SpreadDeviationControl.ExtraDeviation();

            float currentHeat = HeatHandler.currentHeat;

            //Heat add basically dictates how high the multiplier will be depending on your heat level
            //0-32 = 0.05 | 34-65 = 0.15 | 66 - 100 = 0.25  
            int heatCount = (int)(currentHeat / 33);


            float heatMult = 0.01f + (heatCount*0.035f);
            float deviationFromHeat = (noHeat) ? 0 : (HeatHandler.currentHeat * heatMult);
            deviationFromHeat *= (PlayerData.instance.equippedCharm_37)? 1.25f : 1.15f; //Increase movement penalty when equipping sprint master
            deviationFromHeat -= (PlayerData.instance.equippedCharm_14 && HeroController.instance.cState.onGround) ? 18 : 0; //Decrease innacuracy when on ground and steady body is equipped

            float deviation = (perfectAccuracy)? 0 : (deviationFromHeat + deviationFromMovement);
            deviation = (deviation < 0) ? 0 : deviation; //just set up the minimum value, bullets starts acting weird when deviation is negative

            bulletSpeed = Stats.bulletVelocity;

            //Bullet Sprite Size
            float size = 0.90f;

            //===================================FIRE SUPPORT=========================================
            if (flareRound)
            {
                //OFFENSIVE FIRE SUPPORT
                HollowPointPrefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite flareBulletTexture);
                size *= 1.5f;

                bulletSprite.sprite = flareBulletTexture;

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
            bulletSprite.transform.Rotate(0, 0, degree + HollowPointSprites.SpriteRotationWallSlide(), 0);
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

            //Log(col.name);

            //PURE VESSEL CHECK
            if (col.gameObject.name.Contains("Idle"))
            {
                //Modding.Logger.Log("PV IS HIT");
                if(pureVesselHM != null)
                {
                    hm = pureVesselHM;
                }

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
                                pureVesselHM = hm;
                                break;
                            }
                        }
                    }
                    break;
                }
            }

            if (hm == null && col.gameObject.layer.Equals(8))
            {
                StartCoroutine(WallHitDust());

                if (col.gameObject.GetComponent<Breakable>() != null)
                {
                    Breakable br = col.gameObject.GetComponent<Breakable>();
                    bulletHitInstance.Direction = 270f;
                    br.Hit(bulletHitInstance);
                }
                //TODO: change this audio source location
                LoadAssets.sfxDictionary.TryGetValue("impact_0" + rand.Next(1, 6) + ".wav", out AudioClip ac);
                //if (gameObject.GetComponent<AudioSource>() == null) Modding.Logger.Log("No Audio Source");
                HeroController.instance.GetComponent<AudioSource>().PlayOneShot(ac);

                //Mark target for fire support
                if (flareRound)
                {
                    OffensiveFireSupport_Target(gameObject, null, false);
                    return;
                }


                Destroy(gameObject);
            }
            //Damages the enemy and destroys the bullet
            else if (hm != null)
            {
                HeroController.instance.ResetAirMoves();

                if (flareRound)
                {
                    OffensiveFireSupport_Target(gameObject, col.gameObject, true);
                    hm.Hit(bulletHitInstance);
                    return;
                }

                if (!pierce) Destroy(gameObject);
                hm.Hit(bulletHitInstance);
            }
        }

        /* ======================================================FIRE SUPPORT OFFENSIVE TARGET===========================
         * NOTE:
            Heres the thing with this method
                
            Turns out if the game object that gets deleted, whatever coroutine they do also gets deleted
            Which is why the coroutine only fires 1 round before destroying itself
            This method just ensures that theres a long enough lifespan on the bullet once it hits that it'll be able to
            deplete all the rounds
        */
        public void OffensiveFireSupport_Target(GameObject fireSupportGO, GameObject enemyGO, bool trackTarget)
        {

            Vector3 pos = gameObject.transform.position;
            Modding.Logger.Log("CALL AN AIR STRIKE AT THIS POSITION " + pos);

            fireSupportGO.GetComponent<BoxCollider2D>().enabled = false; //If i dont disable the collider, itll keep colliding and keep calling fire support on wtv it collides on
            fireSupportGO.GetComponent<SpriteRenderer>().enabled = false; //Just to make sure it stops showing up visually
            fireSupportGO.GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0); //Stop the bullet movement, so the line renderer wont show up
            int totalShells = (PlayerData.instance.screamLevel > 1) ? 10 : 6;
            //Modding.Logger.Log(enemyGO.transform == null);
            if (trackTarget && enemyGO != null)
            {
                StartCoroutine(SpellControlOverride.StartSteelRain(enemyGO, totalShells));
            }
            else
            {

                StartCoroutine(SpellControlOverride.StartSteelRainNoTrack(pos, totalShells));
            }

            Destroy(fireSupportGO, 25f);
        }

        public IEnumerator WallHitDust() 
        {
            ParticleSystem wallDust = Instantiate(HeroController.instance.wallslideDustPrefab);

            Destroy(wallDust, 0.75f);
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
            bulletTrailClone.transform.parent = null;
            Destroy(bulletTrailClone, 1f); //just to make sure theres no lingering game objects in the background

            //Destroy(Instantiate(HP_Prefabs.bulletFadePrefab, gameObject.transform.position, gameObject.transform.localRotation), 0.03f); //bullet fade out sprite
           
            if(specialAttrib.Contains("Explosion") && PlayerData.instance.equippedCharm_17)
            {
                HollowPointPrefabs.prefabDictionary.TryGetValue("Knight Spore Cloud", out GameObject sporeCloud);
                GameObject sporeCloudGO = Instantiate(sporeCloud, gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                sporeCloudGO.SetActive(true);
            }

            if (specialAttrib.Contains("DungExplosion"))
            {
                HollowPointPrefabs.prefabDictionary.TryGetValue("Dung Explosion", out GameObject dungExplosion);
                GameObject dungExplosionGO = Instantiate(dungExplosion, gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                dungExplosionGO.SetActive(true);
                dungExplosionGO.name += " KnightMadeDungExplosion";

                if (specialAttrib.Contains("Small"))
                {
                    dungExplosionGO.transform.localScale = new Vector3(0.75f, 0.75f, 0);
                }
            }

            //If its from a grenade launch or a offensive fire support projectile, make it explode
            else if (gameObject.GetComponent<BulletBehaviour>().specialAttrib.Contains("Explosion") || isFireSupportBullet)
            {
                GameObject explosionClone = HollowPointPrefabs.SpawnObjectFromDictionary("Gas Explosion Recycle M", gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                explosionClone.name += " KnightMadeExplosion";

                //Shrinks the explosion when its not a fire support bullet or its not an upgraded vengeful, as a nerf/downgrade
                if (isFireSupportBullet) SpellControlOverride.PlayAudio("mortarexplosion", true);             
                else if(PlayerData.instance.fireballLevel > 1) explosionClone.transform.localScale = new Vector3(1.3f, 1.3f, 0);
                else explosionClone.transform.localScale = new Vector3(0.7f, 0.7f, 0);
            }
        }
       
    }
}
