using System;
using UnityEngine;
using System.Collections;
using static Modding.Logger;
using static HollowPoint.HollowPointEnums;
using ModCommon.Util;

namespace HollowPoint
{
    public class BulletBehaviour : MonoBehaviour
    {
        Rigidbody2D rb2d;
        BoxCollider2D bc2d;
        SpriteRenderer bulletSpriteRenderer;
        HealthManager hm;
        double xDeg, yDeg;
        public float bulletSpeed = 25;

        public string specialAttrib;
        public bool piercesEnemy = false;

        public int bulletDamage = 5;
        public int bulletDamageScale = 5;
        public float bulletSpeedMult = 1;
        public float bulletDegreeDirection = 0;
        public float bulletSizeOverride = 1.2f;

        public bool piercesWalls = false;
        public bool ignoreAllCollisions = false;
        public bool hasSporeCloud = true;

        public bool isDagger;

        //Fire Support Attribs
        //This means that the bullet will detonate with the afformentioned destination in the X plane, this is done for artillery strikes
        public bool fuseTimerXAxis = false;
        public Vector3 targetDestination;

        public bool noDeviation = false;
        public bool useDefaultParticles = true;
        public bool noHeat = false;
        public bool perfectAccuracy = false;
        public bool appliesDamageOvertime = false;
        static float bulletPivot = 0;

        public Vector3 bulletOriginPosition;
        public Vector3 gameObjectScale = new Vector3(1.2f, 1.2f, 0);
        public Vector3 size = new Vector3(1.2f, 1.2f, 0.90f);

        //TODO: Clean this up, and the bullet types too with structs instead
        public Gun gunUsed;
        public BulletSpriteType bulletSprite = BulletSpriteType.soul;

        static System.Random rand = new System.Random();

        public static HitInstance bulletDummyHitInstance = new HitInstance
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

        public enum BulletSpriteType
        {
            soul,
            dung,
            artillery,
            dagger,
        }

        public void Start()
        {

            rb2d = GetComponent<Rigidbody2D>();
            bc2d = GetComponent<BoxCollider2D>();
            bulletSpriteRenderer = GetComponent<SpriteRenderer>();
            bc2d.enabled = !ignoreAllCollisions;

            gameObject.tag = "Wall Breaker";

            // +---| Bullet Origin for Distance Calculation |---+
            bulletOriginPosition = gameObject.transform.position;

            //float size = bulletSizeOverride;
            gameObject.transform.localScale = size;
            bc2d.size = size;//Collider size

            // +---| Air Strike Bullet Modifier |---+
            if (fuseTimerXAxis)
            {
                HollowPointPrefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite fireSupportBulletSprite);
                bulletSpriteRenderer.sprite = fireSupportBulletSprite;
                bulletSpeed = 120;
            }
            /*
            else if (isDagger)
            { 
                //HollowPointPrefabs.projectileSprites.TryGetValue("daggerSprite.png", out Sprite fireSupportBulletSprite);
                //bulletSprite.sprite = fireSupportBulletSprite;
                bulletSpriteRenderer.sprite = HollowPointPrefabs.projectileSprites["bullet_daggerSprite.png"];
            }
            */
            if (!bulletSprite.Equals(BulletSpriteType.soul))
            {
                bulletSpriteRenderer.sprite = HollowPointPrefabs.projectileSprites["sprite_bullet_" + bulletSprite.ToString() + ".png"];
            }

            //Particle Effects

            string particlePrefabName = (useDefaultParticles) ? "SpellParticlePrefab" : "GrimmParticlePrefab"; 
            GameObject fireballParticles = HollowPointPrefabs.SpawnObjectFromDictionary(particlePrefabName, gameObject.transform.position, Quaternion.identity);
            fireballParticles.AddComponent<ParticlesController>().parent = gameObject;
            //fireballParticles.GetComponent<ParticlesController>().size = bullet.size;
            fireballParticles.SetActive(true);


            // +---| Bullet Heat |---+
            float deviationFromHeat = (noHeat) ? 0 : (float)Math.Pow(HeatHandler.currentHeat, 2f) / 500; //exponential
            deviationFromHeat *= (PlayerData.instance.equippedCharm_37) ? 1.25f : 1.15f; //Increase movement penalty when equipping sprint master
            deviationFromHeat -= (PlayerData.instance.equippedCharm_14 && HeroController.instance.cState.onGround) ? 18 : 0; //Decrease innacuracy when on ground and steady body is equipped

            float deviation = (perfectAccuracy) ? 0 : (deviationFromHeat);
            deviation = Mathf.Clamp(deviation, 0, 20); //just set up the minimum value, bullets starts acting weird when deviation is negative
            //deviation = (deviation < 0) ? 0 : deviation; 

            // +---| Bullet Spread and Recoil |---+
            //a moving pivot for where the bullet can spread, with its maximum and minimum deviation, this allows the bullet to smoothly spread instead of being just random
            bulletPivot = Mathf.Clamp(bulletPivot, deviation * -1, deviation); //Clamps the max/min deviation, shrinking the cone of fire
            float bulletPivotDelta = rand.Next(0, 2) * 2 - 1; //gives either -1 or 1
            bulletPivotDelta = (bulletPivot >= deviation || bulletPivot <= (deviation * -1)) ? bulletPivotDelta * -1 : bulletPivotDelta;
            bulletPivot += bulletPivotDelta * rand.Next(gunUsed.minWeaponSpreadFactor, gunUsed.minWeaponSpreadFactor + 4); //1 can be changed by the amount of distance each bullet deviation should have
            float degree = bulletDegreeDirection + Mathf.Clamp(bulletPivot, deviation * -1, deviation); ;
            float radian = (float)(degree * Math.PI / 180);

            xDeg = bulletSpeed * Math.Cos(radian) * bulletSpeedMult;
            yDeg = bulletSpeed * Math.Sin(radian) * bulletSpeedMult;

            //Changes the degree of bullet sprite rotation and the bullet direction when wall sliding
            if (HeroController.instance.cState.wallSliding)
            {
                xDeg *= -1;
                degree = (HeroController.instance.cState.facingRight) ? ((degree <= 90 ? 180 : -180) - degree) : (180 - degree);
            }

            rb2d.velocity = new Vector2((float)xDeg, (float)yDeg); //Bullet speed
            bulletSpriteRenderer.transform.Rotate(0, 0, degree, 0); //Bullet rotation
        }

        //Destroy the artillery shell when it hits the destination
        void FixedUpdate()
        {
            if (fuseTimerXAxis && gameObject.transform.position.y < targetDestination.y) Destroy(gameObject);
        }

        //Handles the colliders
        void OnTriggerEnter2D(Collider2D col)
        {
            hm = col.GetComponentInChildren<HealthManager>();
            bulletDummyHitInstance.Source = gameObject;

            // Log("[BulletBehaviour] Col Name" + col.name);
            if (col.gameObject.name.Contains("Idle") || hm != null)
            {
                HeroController.instance.ResetAirMoves();
                HitTaker.Hit(col.gameObject, bulletDummyHitInstance);
                if (piercesEnemy) return;
                Destroy(gameObject, 0.03f);
                return;
            }

            else if (hm == null && col.gameObject.layer.Equals(8))
            {
                StartCoroutine(WallHitDust());
                if (col.gameObject.GetComponent<Breakable>() != null)
                {
                    Breakable br = col.gameObject.GetComponent<Breakable>();
                    bulletDummyHitInstance.Direction = 270f;
                    br.Hit(bulletDummyHitInstance);
                }
                AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.TerrainHitSFXGO);
                if(!piercesWalls) Destroy(gameObject, 0.04f);
            }
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

        //TODO: Create an "explosion component" to spawn on destroy instead of creating the object at the Destroy of the bullet
        public void OnDestroy()
        {
            GameObject fireballImpact = HollowPointPrefabs.SpawnObjectFromDictionary("FireballImpact", gameObject.transform.position, Quaternion.identity);
            fireballImpact.transform.Rotate(0, 0, gameObject.transform.eulerAngles.z, 0);
            fireballImpact.transform.localScale = size - new Vector3(0, 0.50f, 0);
            Destroy(fireballImpact, 1.5f);
        }

    }

    //Attaches itself to an enemy with a Health Manager
    public class EnemyDamageOvertime : MonoBehaviour
    {
        int stack;
        int damage;
        float tick;
        float duration;
        HealthManager hm = null;
        GameObject particles = null;
        ParticleSystem particleSystem = null;

        bool playFireAnim = false;

        void Start()
        {
            try
            {
                //Log("[DamageOverride:DamageOvertime] Awakening Damage Overtime");
                stack = 1;
                damage = 1;
                tick = 0f;
                duration = 2f;

                hm = gameObject.GetComponent<HealthManager>();
                Vector2 enemySize = gameObject.GetComponent<BoxCollider2D>().size;
                particles = HollowPointPrefabs.SpawnObjectFromDictionary("SpellParticlePrefab", gameObject.transform.position, Quaternion.identity);

                ParticlesController fpc = particles.AddComponent<ParticlesController>();
                fpc.parent = gameObject;
                fpc.rateOverTimeMultiplier = 100;
                fpc.size = enemySize;

                particles.SetActive(true);
                particleSystem = particles.GetComponent<ParticleSystem>();
                particleSystem.Stop();
                //Log("ENEMY SIZE IS " + enemySize);
                //gameObject.GetComponent<SetParticleScale>().transform.localScale = enemySize - new Vector2(0.3f, 0.3f);
            }
            catch(Exception e)
            {
                Log("DamageOvertime " + e);
            }
        }

        void Update()
        {
            tick -= Time.deltaTime * 1f;
            duration -= Time.deltaTime * 1f;

            if (tick < 0 && stack > 0)
            {
                tick = 0.5f - (stack * 0.12f);
                DamageEnemy();
            }
            if (duration < 0 && stack > 0)
            {
                duration = 3f;
                stack--;
            }
            if(stack <= 0 && playFireAnim)
            {
                particleSystem.Stop();
                playFireAnim = false;
            }
        }

        void DamageEnemy()
        {
            if (hm == null)
            {
                Log("[DamageOverride:DamageOvertime] Game Object/HealthManager null, cannot apply damage overtime");
                stack = 0;             
                return;
            }

            if (!playFireAnim)
            {
                particleSystem.Play();
                playFireAnim = true;
            }
            //TODO: Add sounds whenever they take damage

            hm.hp -= damage * stack;
            SpriteFlash f = hm.gameObject.GetComponent<SpriteFlash>();
            if (f != null) f.flashBenchRest();
            if (hm.hp <= 0) DamageOverride.EnemyDeathEvent(hm, 90, false);       
        }

        public void IncreaseStack()
        {
            Log("[BulletBehaviour:DamageOvertime] Increasing Damage Stack");
            if (stack < 3) stack++;
           
            duration = 2f;
        }

    }
    
    //This behaviour is attached to the particle gameobjects, and from here we control the particles properties like size, emission etc
    public class ParticlesController : MonoBehaviour
    {
        public GameObject parent;
        public float rateOverTimeMultiplier = 30f;
        ParticleSystemRenderer particleSystemRenderer;
        ParticleSystem particleSystem;
        SetParticleScale setParticleScale;
        bool toBeDestroyed = false;
        public Vector2 size = new Vector2(0.2f, 0.2f);

        void Start()
        {
            //Log("BulletBehaviour:FireballParticlesProperties Start()");
            //Array.ForEach<Component>(gameObject.GetComponents<Component>(), x => Log(x));

            particleSystemRenderer = gameObject.GetComponent<ParticleSystemRenderer>();
            particleSystem = gameObject.GetComponent<ParticleSystem>();
            setParticleScale = gameObject.GetComponent<SetParticleScale>();

            ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
            emissionModule.rateOverTimeMultiplier = rateOverTimeMultiplier;
            setParticleScale.transform.localScale = size;
        }

        void Update()
        {
            if (parent == null && !toBeDestroyed)
            {
                toBeDestroyed = true;
                //Log("BulletBehaviour:FireballParticlesProperties Parent gone, behaviour will destroy itself in a bit");
                particleSystem.Stop();
                Destroy(gameObject, 2.7f);
            }
            gameObject.transform.position = parent.transform.position;
        }

        void OnDestroy()
        {
            //Log("BulletBehaviour:FireballParticlesProperties Destroy()");
        }
    }

    public class BulletIsExplosive : MonoBehaviour
    {
        public ExplosionType explosionType = ExplosionType.DungExplosion;
        public bool artilleryShell = false;
        static UnityEngine.Random rand = new UnityEngine.Random();
        public enum ExplosionType
        {
            SporeGas,
            DungGas,
            GasExplosion,
            DungExplosion,
            DungExplosionSmall,
            ArtilleryShell,
        }

        public void OnDestroy()
        {
            if (explosionType == ExplosionType.DungGas)
            {
                HollowPointPrefabs.prefabDictionary.TryGetValue("Dung Explosion", out GameObject dungExplosion);
                GameObject dungExplosionGO = Instantiate(dungExplosion, gameObject.transform.position + new Vector3(0, 0, -1.5f), Quaternion.identity);
                dungExplosionGO.SetActive(true);
                dungExplosionGO.name += " KnightMadeDungExplosion";

                HollowPointPrefabs.prefabDictionary.TryGetValue("Knight Dung Cloud", out GameObject sporeCloud);
                GameObject sporeCloudGO = Instantiate(sporeCloud, gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                sporeCloudGO.SetActive(true);
                AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.DiveDetonateSFXGO);

            }

            if (explosionType == ExplosionType.DungExplosion || explosionType == ExplosionType.DungExplosionSmall)
            {
                HollowPointPrefabs.prefabDictionary.TryGetValue("Dung Explosion", out GameObject dungExplosion);
                GameObject dungExplosionGO = Instantiate(dungExplosion, gameObject.transform.position + new Vector3(0, 0, -1.5f), Quaternion.identity);
                dungExplosionGO.SetActive(true);
                dungExplosionGO.name += " KnightMadeDungExplosion";

                if (explosionType == ExplosionType.DungExplosionSmall)
                {
                    dungExplosionGO.transform.localScale = new Vector3(0.6f, 0.6f, -1);
                }
            }

            //If its from a grenade launch or a offensive fire support projectile, make it explode
            else if (explosionType == ExplosionType.GasExplosion || explosionType == ExplosionType.ArtilleryShell)
            {
                Log("ARTILLERY!");
                GameObject explosionClone = HollowPointPrefabs.SpawnObjectFromDictionary("Gas Explosion Recycle M", gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                explosionClone.name += " KnightMadeExplosion";

                //Shrinks the explosion when its not a fire support bullet or its not an upgraded vengeful, as a nerf/downgrade
                if (explosionType == ExplosionType.ArtilleryShell)
                {
                    AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.MortarExplosionSFXGO);
                    //StartCoroutine(SpawnCluster());
                    for (int shrapnel = 0; shrapnel < 6; shrapnel++)
                    {
                        GameObject s = HollowPointPrefabs.SpawnBulletAtCoordinate(UnityEngine.Random.Range(190, 350), gameObject.transform.position, 1);
                        s.AddComponent<BulletIsExplosive>().explosionType = ExplosionType.DungExplosion;
                        Destroy(s, 1f);
                    }
                }
                else if (PlayerData.instance.fireballLevel > 1) explosionClone.transform.localScale = new Vector3(1.3f, 1.3f, 0);
                else explosionClone.transform.localScale = new Vector3(0.7f, 0.7f, 0);
            }       
        }
    }
}