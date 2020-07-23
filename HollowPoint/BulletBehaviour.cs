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

            float heatMult = 0.01f + (heatCount * 0.06f);
            //float deviationFromHeat = (noHeat) ? 0 : (HeatHandler.currentHeat * heatMult); //old formula
            float deviationFromHeat = (noHeat) ? 0 : (float)Math.Pow(HeatHandler.currentHeat, 2f)/500; //exponential
            deviationFromHeat *= (PlayerData.instance.equippedCharm_37) ? 1.25f : 1.15f; //Increase movement penalty when equipping sprint master
            deviationFromHeat -= (PlayerData.instance.equippedCharm_14 && HeroController.instance.cState.onGround) ? 18 : 0; //Decrease innacuracy when on ground and steady body is equipped

            float deviation = (perfectAccuracy) ? 0 : (deviationFromHeat + deviationFromMovement);
            deviation = Mathf.Clamp(deviation, 0, 14); //just set up the minimum value, bullets starts acting weird when deviation is negative
            //deviation = (deviation < 0) ? 0 : deviation; 

            bulletSpeed = Stats.bulletVelocity;

            //Bullet Sprite Size
            float size = bulletSizeOverride;

            //===================================FIRE SUPPORT=========================================
            //Override this entire code if its from fire support and give the bullet its own special properties aka because making new GOs with code is effort
            if (isFireSupportBullet)
            {
                //bulletSprite.transform.Rotate(0, 0, 270);
                //bulletTrailObjectClone.GetComponent<TrailRenderer>().time = 0.9f;
                HollowPointPrefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite fireSupportBulletSprite);

                //gameObject.transform.localScale = new Vector3(1.2f, 1.2f, 0.90f);
                bulletSprite.sprite = fireSupportBulletSprite;
                bulletSpeed = 120;
                //rb2d.velocity = new Vector2(0, -120);

                //return;
            }

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
            //a moving pivot for where the bullet can spread, with its maximum and minimum deviation, this allows the bullet to
            //smoothly spread instead of being just random

            //bulletPivot = (bulletPivot > deviation) ? deviation : (bulletPivot < deviation * -1)? deviation * -1 : bulletPivot; //Clamps the value
            
            bulletPivot = Mathf.Clamp(bulletPivot, deviation * -1, deviation); //Clamps the value
            float bulletPivotDelta = rand.Next(0, 2) * 2 - 1; //gives either -1 or 1
            bulletPivotDelta = (bulletPivot >= deviation || bulletPivot <= (deviation * -1)) ? bulletPivotDelta * -1 : bulletPivotDelta; 
            bulletPivot += bulletPivotDelta * 6; //1 can be changed by the amount of distance each bullet deviation should have
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

            return;

            if (col.gameObject.name.Contains("Idle"))
            {
                //Modding.Logger.Log("PV IS HIT");
                HitTaker.Hit(col.gameObject, bulletDummyHitInstance);


                if (pureVesselHM != null)
                {
                    hm = pureVesselHM;
                }

                Component[] pvc = col.gameObject.GetComponents<Component>();
                Log("Components" + pvc);

                foreach (Component c in pvc)
                {
                    Type type = c.GetType();

                    //Transform BoxCollider2D DamageHero
                    if (type.Name.Contains("Transform"))
                    {
                        Transform pvt = (Transform)c;
                        Component[] parent_pvt = pvt.GetComponentsInParent(typeof(Component));

                        foreach (Component cp in parent_pvt)
                        {
                            Type type_2 = cp.GetType();
                            if (type_2.Name.Contains("HealthManager"))
                            {
                                hm = (HealthManager)cp;
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
                    bulletDummyHitInstance.Direction = 270f;
                    br.Hit(bulletDummyHitInstance);
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
                HitTaker.Hit(col.gameObject, bulletDummyHitInstance);
                HeroController.instance.ResetAirMoves();
                if (flareRound)
                {
                    OffensiveFireSupport_Target(gameObject, col.gameObject, true);
                    hm.Hit(bulletDummyHitInstance);
                    return;
                }
                if (!pierce) Destroy(gameObject);
                //hm.Hit(bulletDummyHitInstance);
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

                if (false)
                {
                    for (int shrap = 0; shrap <= 5; shrap++)
                    {
                        GameObject bul = HollowPointPrefabs.SpawnBulletAtCoordinate(rand.Next(0, 360), gameObject.transform.position, 6);
                        bul.GetComponent<BulletBehaviour>().specialAttrib = "DungExplosion";
                    }
                }

                //Shrinks the explosion when its not a fire support bullet or its not an upgraded vengeful, as a nerf/downgrade
                if (isFireSupportBullet) SpellControlOverride.PlayAudio("mortarexplosion", true);
                else if (PlayerData.instance.fireballLevel > 1) explosionClone.transform.localScale = new Vector3(1.3f, 1.3f, 0);
                else explosionClone.transform.localScale = new Vector3(0.7f, 0.7f, 0);
            }
        }

    }
}
