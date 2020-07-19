using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
            bulletTR.time = 0.04f;

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
            bullet.GetComponent<BulletBehaviour>().heatOnHit = HeatHandler.currentHeat;
            bullet.SetActive(true);

            return bullet;
        }

        public static GameObject SpawnBulletAtCoordinate(float bulletDegreeDirection, Vector3 spawnPoint)
        {
            bulletDegreeDirection = bulletDegreeDirection % 360;

            float xOffSet = (float)(Math.Cos(bulletDegreeDirection) * 4);
            float yOffSet = (float)(Math.Sin(bulletDegreeDirection) * 4);

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
   
}
