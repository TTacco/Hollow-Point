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
        GameObject bulletTrailObjectClone;
        Rigidbody2D rb2d;
        BoxCollider2D bc2d;
        SpriteRenderer bulletSprite;
        HealthManager hm;
        double xDeg, yDeg;
        double bulletSpeed = 10;

        public string specialAttrib;
        public bool pierce = false;

        public int bulletDamage;
        public float bulletSpeedMult = 1;
        public float bulletDegreeDirection = 0;
        public float bulletSizeOverride = 1.2f;

        public bool ignoreCollisions = false;
        public bool hasSporeCloud = true;

        //Fire Support Attribs
        public bool flareRound = false;
        public bool isFireSupportBullet = false;

        public bool noDeviation = false;
        public bool noHeat = false;
        public bool perfectAccuracy = false;
        public float heatOnHit = 0;
        static float bulletPivot = 0;

        public Vector3 bulletOriginPosition;
        public Vector3 targetDestination;

        public FireModes fm = FireModes.Single;
        public BulletType bt = BulletType.Standard;

        public GameObject bulletTrailClone;
        HealthManager pureVesselHM = null;
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

            // +---| Bullet Sprite Swapper |---+

            //Bullet sprite changer
            /*
            string sn = Stats.bulletSprite;
            if (sn != "" && false)
            {
                HollowPointPrefabs.projectileSprites.TryGetValue(Stats.bulletSprite, out Sprite regularBulletSprite);
                bulletSprite.sprite = regularBulletSprite;
            }
            */

            // +---| Bullet Origin for Distance Calculation |---+
            bulletOriginPosition = gameObject.transform.position;

            // +---| Bullet Degree And Recoil |---+
            float heat = HeatHandler.currentHeat;
            noDeviation = (PlayerData.instance.equippedCharm_14);
            float deviationFromMovement = (noDeviation) ? 0 : SpreadDeviationControl.ExtraDeviation();
            float currentHeat = HeatHandler.currentHeat;
            int heatCount = (int)(currentHeat / 33);
            float heatMult = 0.01f + (heatCount * 0.06f);
            float deviationFromHeat = (noHeat) ? 0 : (float)Math.Pow(HeatHandler.currentHeat, 2f)/500; //exponential
            deviationFromHeat *= (PlayerData.instance.equippedCharm_37) ? 1.25f : 1.15f; //Increase movement penalty when equipping sprint master
            deviationFromHeat -= (PlayerData.instance.equippedCharm_14 && HeroController.instance.cState.onGround) ? 18 : 0; //Decrease innacuracy when on ground and steady body is equipped

            float deviation = (perfectAccuracy) ? 0 : (deviationFromHeat + deviationFromMovement);
            deviation = Mathf.Clamp(deviation, 0, 14); //just set up the minimum value, bullets starts acting weird when deviation is negative
            //deviation = (deviation < 0) ? 0 : deviation; 

            // +---| Bullet Properties |---+
            bulletSpeed = Stats.instance.bulletVelocity;
            float size = bulletSizeOverride;

            // +---| Air Strike Bullet Modifier |---+
            //Override this entire code if its from fire support and give the bullet its own special properties aka because making new GOs with code is effort
            if (isFireSupportBullet)
            {
                //bulletTrailObjectClone.GetComponent<TrailRenderer>().time = 0.9f;
                HollowPointPrefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite fireSupportBulletSprite);
                bulletSprite.sprite = fireSupportBulletSprite;
                bulletSpeed = 120;
            }

            gameObject.transform.localScale = new Vector3(size, size, 0.90f);
            gameObject.transform.localScale = new Vector3(size, size, 0.90f);

            // +---| Bullet Spread and Recoil |---+
            //a moving pivot for where the bullet can spread, with its maximum and minimum deviation, this allows the bullet to
            //smoothly spread instead of being just random
            bulletPivot = Mathf.Clamp(bulletPivot, deviation * -1, deviation); //Clamps the value
            float bulletPivotDelta = rand.Next(0, 2) * 2 - 1; //gives either -1 or 1
            bulletPivotDelta = (bulletPivot >= deviation || bulletPivot <= (deviation * -1)) ? bulletPivotDelta * -1 : bulletPivotDelta; 
            bulletPivot += bulletPivotDelta * rand.Next(5,9); //1 can be changed by the amount of distance each bullet deviation should have
            float degree = bulletDegreeDirection + Mathf.Clamp(bulletPivot, deviation * -1, deviation); ;

            //float degree = bulletDegreeDirection + (rand.Next((int)-deviation, (int)deviation + 1)) - (float)rand.NextDouble();
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
            bulletSprite.transform.Rotate(0, 0, degree, 0); //Bullet rotation
        }

        //Destroy the artillery shell when it hits the destination
        void FixedUpdate()
        {
            if (isFireSupportBullet)
            {
                if (gameObject.transform.position.y < targetDestination.y)
                {
                    Log("[BulletBehaviour] Reached destroy point for artillery shell");
                    Destroy(gameObject);
                }
            }
        }

        //Handles the colliders
        void OnTriggerEnter2D(Collider2D col)
        {
            hm = col.GetComponentInChildren<HealthManager>();
            bulletDummyHitInstance.Source = gameObject;

           // Log("[BulletBehaviour] Col Name" + col.name);
            if(col.gameObject.name.Contains("Idle") || hm != null)
            {
                HeroController.instance.ResetAirMoves();
                HitTaker.Hit(col.gameObject, bulletDummyHitInstance);
                Destroy(gameObject);
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
                //TODO: change this audio source location
                LoadAssets.sfxDictionary.TryGetValue("impact_0" + rand.Next(1, 6) + ".wav", out AudioClip ac);
                //if (gameObject.GetComponent<AudioSource>() == null) Modding.Logger.Log("No Audio Source");
                HeroController.instance.GetComponent<AudioSource>().PlayOneShot(ac);
                //Mark target for fire support
                Destroy(gameObject);
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
            
            bulletTrailClone.transform.parent = null;
            Destroy(bulletTrailClone, 1f); //just to make sure theres no lingering game objects in the background

            Destroy(Instantiate(HollowPointPrefabs.bulletFadePrefab, gameObject.transform.position + new Vector3(0, 0, -0.5f), gameObject.transform.localRotation), 0.03f); //bullet fade out sprite

            if (specialAttrib.Contains("Explosion") && PlayerData.instance.equippedCharm_17)
            {
                //HollowPointPrefabs.prefabDictionary.TryGetValue("Knight Spore Cloud", out GameObject sporeCloud);
                //GameObject sporeCloudGO = Instantiate(sporeCloud, gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                //sporeCloudGO.SetActive(true);
            }

            if (specialAttrib.Contains("DungExplosion"))
            {
                HollowPointPrefabs.prefabDictionary.TryGetValue("Dung Explosion", out GameObject dungExplosion);
                GameObject dungExplosionGO = Instantiate(dungExplosion, gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                dungExplosionGO.SetActive(true);
                dungExplosionGO.name += " KnightMadeDungExplosion";

                if (specialAttrib.Contains("Small"))
                {
                    dungExplosionGO.transform.localScale = new Vector3(0.6f, 0.6f, 0);
                }
            }

            //If its from a grenade launch or a offensive fire support projectile, make it explode
            else if (gameObject.GetComponent<BulletBehaviour>().specialAttrib.Contains("Explosion") || isFireSupportBullet)
            {
                GameObject explosionClone = HollowPointPrefabs.SpawnObjectFromDictionary("Gas Explosion Recycle M", gameObject.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                explosionClone.name += " KnightMadeExplosion";

                //Shrinks the explosion when its not a fire support bullet or its not an upgraded vengeful, as a nerf/downgrade
                if (isFireSupportBullet) SpellControlOverride.PlayAudio("mortarexplosion", true);
                else if (PlayerData.instance.fireballLevel > 1) explosionClone.transform.localScale = new Vector3(1.3f, 1.3f, 0);
                else explosionClone.transform.localScale = new Vector3(0.7f, 0.7f, 0);
            }
        }

    }
}
