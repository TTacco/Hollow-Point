using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Modding;
using static Modding.Logger;
using static HollowPoint.HollowPointEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;


namespace HollowPoint
{

    class HollowPointPrefabs : MonoBehaviour
    {

        public static GameObject bulletPrefab;

        //TODO: Clean this up too and the dictionary checking
        //public static GameObject greenscreen;
        public static GameObject blood = null;
        public static GameObject dungexplosion = null;
        public static GameObject explosion = null;
        public static GameObject knight_spore = null;
        public static GameObject takeDamage = null;
        public static GameObject fireballImpactPrefab = null;
        public static RandomFloat grimmChildAttackSpeed = null; //TODO: transfer this method to the Stats class instead

        public static Dictionary<String, Sprite> projectileSprites = new Dictionary<String, Sprite>();
        public static Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();


        public void Start()
        {
            StartCoroutine(CreateBulletPrefab());
            StartCoroutine(GetFSMPrefabsAndParticles());
            ModHooks.Instance.ObjectPoolSpawnHook += Instance_ObjectPoolSpawnHook;
        }

        //On objects spawning on the world


        private GameObject Instance_ObjectPoolSpawnHook(GameObject go)
        {
            //Log(go.name);
            /*
            if (go.name.Contains("Weaverling")) Destroy(go);
            else if (go.name.Contains("Orbit Shield") && !prefabDictionary.ContainsKey("Orbit Shield"))
            {
                prefabDictionary.Add("Orbit Shield", go);
            }
            if(grimmChildAttackSpeed == null && go.name.Contains("Grimmchild"))
            {
                //StartCoroutine(ChangeGrimmChildFSM(go));
            }
            */

            if (!prefabDictionary.ContainsKey("Hatchling") && go.name.Contains("Hatchling"))
            {
                prefabDictionary.Add("Hatchling", go);
            }

            if (go.name.Contains("Grubberfly"))
            {
                Destroy(go);
            }

            if (go.name.Contains("Weaverling") && !prefabDictionary.ContainsKey("Weaverling"))
            {
                prefabDictionary.Add("Weaverling", go);
            }


            return go;
        }

        IEnumerator ChangeGrimmChildFSM(GameObject grimmChild)
        {
            //PlayMakerFSM grimmChildFSM = grimmChild.LocateMyFSM("Control");
            //grimmChildFSM.GetAction<SetFsmInt>("Shoot", 6).setValue.Value = 30;
            //grimmChildFSM.GetAction<FireAtTarget>("Shoot", 7).speed.Value = 60f;
            //grimmChildAttackSpeed = grimmChildFSM.GetAction<RandomFloat>("Antic", 3);
            //grimmChildAttackSpeed.min.Value = 0.01f;
            //grimmChildAttackSpeed.max.Value = 0.01f;

            yield return null;
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
                    //Log(go.name);
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

            LoadAssets.spriteDictionary.TryGetValue("sprite_bullet_soul.png", out Texture2D bulletTexture);
            LoadAssets.spriteDictionary.TryGetValue("bulletSpriteFade.png", out Texture2D fadeTexture);

            //Prefab instantiation
            bulletPrefab = new GameObject("bulletPrefabObject", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(BulletBehaviour), typeof(AudioSource));
            bulletPrefab.GetComponent<SpriteRenderer>().sprite = Sprite.Create(bulletTexture,
                new Rect(0, 0, bulletTexture.width, bulletTexture.height),
                new Vector2(0.5f, 0.5f), 42);

            string[] textureNames = {"specialbullet.png", "furybullet.png", "shadebullet.png", "sprite_bullet_dagger.png", "sprite_bullet_dung.png", "sprite_bullet_voids.png" };
            //Special bullet sprite
            /*
            LoadAssets.spriteDictionary.TryGetValue("specialbullet.png", out Texture2D specialBulletTexture);
            projectileSprites.Add("specialbullet.png", Sprite.Create(specialBulletTexture,
                new Rect(0, 0, specialBulletTexture.width, specialBulletTexture.height),
                new Vector2(0.5f, 0.5f), 42));
            */

            foreach (string tn in textureNames)
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
                    Log(e);
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

            bulletPrefab.SetActive(false);
            DontDestroyOnLoad(bulletPrefab);

            Modding.Logger.Log("[HOLLOW POINT] Initalized BulletObject");
        }

        IEnumerator GetFSMPrefabsAndParticles()
        {
            while (HeroController.instance == null) yield return null;

            try
            {
                //Fireball/Vengeful Spirit Wall Impact Prefab
                PlayMakerFSM spellControlFSM = HeroController.instance.spellControl;
                PlayMakerFSM fireball_FireballTopFSM = spellControlFSM.GetAction<SpawnObjectFromGlobalPool>("Fireball 1", 3).gameObject.Value.LocateMyFSM("Fireball Cast");
                GameObject go = Instantiate(fireball_FireballTopFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).gameObject.Value);
                go.SetActive(false);
                PlayMakerFSM fireballControl = go.LocateMyFSM("Fireball Control");
                GameObject fireballImpactClone = Instantiate(fireballControl.GetAction<ActivateGameObject>("Wall Impact", 5).gameObject.GameObject.Value);
                GameObject.DontDestroyOnLoad(fireballImpactClone);
                fireballImpactClone.SetActive(false);
                //Destroy(fireballImpact);
                fireballImpactPrefab = fireballImpactClone;
                prefabDictionary.Add("FireballImpact", fireballImpactClone);

                //Fireball/Vengeful Spirit Particles
                GameObject spellParticlesClone = Instantiate(fireballControl.GetAction<StopParticleEmitter>("Wall Impact", 1).gameObject.GameObject.Value);
                GameObject.DontDestroyOnLoad(spellParticlesClone);
                prefabDictionary.Add("SpellParticlePrefab", spellParticlesClone);
                spellParticlesClone.SetActive(false);

                //Grimmchild Particle
                //TODO: Clean this up to reduce object clutter
                PlayMakerFSM spawnGrimmChild = GameObject.Find("Charm Effects").LocateMyFSM("Spawn Grimmchild");
                GameObject grimmChild = spawnGrimmChild.GetAction<SpawnObjectFromGlobalPool>("Spawn", 2).gameObject.Value;
                PlayMakerFSM grimmChildControl = grimmChild.LocateMyFSM("Control");
                GameObject grimmball = grimmChildControl.GetAction<SpawnObjectFromGlobalPool>("Shoot", 4).gameObject.Value;

                GameObject grimmballClone = Instantiate(grimmball);
                grimmballClone.SetActive(false);
                PlayMakerFSM grimmballControl = FSMUtility.GetFSM(grimmballClone); //grimmball.LocateMyFSM("Control");
                GameObject grimmParticle = grimmballControl.GetAction<StopParticleEmitter>("Impact", 4).gameObject.GameObject.Value;

                GameObject grimmParticleClone = Instantiate(grimmParticle);
                GameObject.DontDestroyOnLoad(grimmParticleClone);
                prefabDictionary.Add("GrimmParticlePrefab", grimmParticleClone);

                PlayMakerFSM furyFSM = GameObject.Find("Charm Effects").LocateMyFSM("Fury");
                GameObject furyParticles = Instantiate(furyFSM.GetAction<PlayParticleEmitter>("Activate", 2).gameObject.GameObject.Value);
                GameObject.DontDestroyOnLoad(furyParticles);
                furyParticles.SetActive(false);
                prefabDictionary.Add("FuryParticlePrefab",furyParticles);

                GameObject furyBurst = Instantiate(furyFSM.GetAction<ActivateGameObject>("Activate", 20).gameObject.GameObject.Value);
                GameObject.DontDestroyOnLoad(furyBurst);
                furyBurst.SetActive(false);
                prefabDictionary.Add("FuryBurstPrefab", furyBurst);
            }
            catch(Exception e)
            {
                Log("Getting the FSM prefabs was fucked my dude" + e);
            }

        }

        public static GameObject SpawnBulletFromKnight(float bulletDegreeDirection, DirectionalOrientation dirOrientation)
        {
            //SpawnObjectFromDictionary("FireballImpact", HeroController.instance.transform.position, Quaternion.identity);
            //Instantiate(fireballImpactPrefab, HeroController.instance.transform.position, Quaternion.identity).SetActive(true);

            bulletDegreeDirection = bulletDegreeDirection % 360;        
            float directionOffsetY = 0;
        
            //If the player is aiming upwards, change the bullet offset of where it will spawn
            //Otherwise the bullet will spawn too high or inbetween the knight
            bool directionalOffSetBool = (dirOrientation == DirectionalOrientation.Vertical || dirOrientation == DirectionalOrientation.Diagonal);
            bool firingUpwards = (bulletDegreeDirection > 0 && bulletDegreeDirection < 180);
            if (directionalOffSetBool && firingUpwards) directionOffsetY = 0.8f;
            else if(directionalOffSetBool && !firingUpwards) directionOffsetY = -1.1f;
  
            float directionMultiplierX = (HeroController.instance.cState.facingRight) ? 1f : -1f;
            float wallClimbMultiplier = (HeroController.instance.cState.wallSliding) ? -1f : 1f;

            //Checks if the player is firing upwards/downwards, and enables the x offset so the bullets spawns directly ontop of the knight
            //from the gun's barrel instead of spawning to the upper right/left of them 
            if (dirOrientation == DirectionalOrientation.Vertical) directionMultiplierX = 0.2f * directionMultiplierX;

            if(dirOrientation == DirectionalOrientation.Center)
            {
                directionMultiplierX = 0f;
                directionOffsetY = 0f;
            } 

            directionMultiplierX *= wallClimbMultiplier;
                
            GameObject bullet = Instantiate(bulletPrefab, HeroController.instance.transform.position + new Vector3(1.4f * directionMultiplierX, -0.7f + directionOffsetY, -0.002f), new Quaternion(0, 0, 0, 0));
           //BulletBehaviour bb = bullet.GetComponent<BulletBehaviour>();
           // bb.bulletDegreeDirection = bulletDegreeDirection;
            //bb.heatOnHit = HeatHandler.currentHeat;
            //bb.size = Stats.instance.currentWeapon.bulletSize;
            bullet.SetActive(true);

            return bullet;
        }

        public static GameObject SpawnBulletAtCoordinate(float bulletDegreeDirection, Vector3 spawnPoint, float offset)
        {
            bulletDegreeDirection = bulletDegreeDirection % 360;

            float xOffSet = (float)(Math.Cos(bulletDegreeDirection) * offset);
            float yOffSet = (float)(Math.Sin(bulletDegreeDirection) * offset);

            GameObject bullet = Instantiate(bulletPrefab, spawnPoint + new Vector3(xOffSet, yOffSet), new Quaternion(0, 0, 0, 0));
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
                prefabDictionary.TryGetValue(key, out GameObject spawnedGO);
                GameObject spawnedGO_Instance = Instantiate(spawnedGO, spawnPosition, rotation);
                spawnedGO_Instance.SetActive(true);
                return spawnedGO_Instance;
            }
            catch (Exception e)
            {
                Log("HP_Prefabs SpawnObjectFromDictionary(): Could not find GameObject with key " + key);
                //Log("only available keys are");

                //foreach (string keys in prefabDictionary.Keys) Log(keys);

                return null;
            }
        }

        public static GameObject SpawnObjectFromDictionary(string key, Transform parent)
        {
            try
            {
                prefabDictionary.TryGetValue(key, out GameObject spawnedGO);
                GameObject spawnedGO_Instance = Instantiate(spawnedGO, parent);
                spawnedGO_Instance.SetActive(true);
                return spawnedGO_Instance;
            }
            catch (Exception e)
            {
                Log("HP_Prefabs SpawnObjectFromDictionary(): Could not find GameObject with key " + key);
                //Log("only available keys are");

                //foreach (string keys in prefabDictionary.Keys) Log(keys);

                return null;
            }
        }
    }
   
}
