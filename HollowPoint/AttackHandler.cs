using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MonoMod.Utils;
using MonoMod;
using static Modding.Logger;
using Modding;
using ModCommon;
using ModCommon.Util;
using Object = UnityEngine.Object;
using static HollowPoint.HollowPointEnums;

namespace HollowPoint
{
    class AttackHandler : MonoBehaviour
    { 
        public static Rigidbody2D knight;
        public static GameObject damageNumberTestGO;

        public static bool isFiring = false;
        public static bool isBursting = false;
        public static bool slowWalk = false;
        public static bool airStrikeActive = false; //Dictates this round will send determine if the bullet is an airstrike marker

        static float slowWalkDisableTimer = 0;
        float clickTimer = 0;

        public HeroControllerStates h_state;
        public HeroController hc_instance;

        public void Awake()
        {
            On.NailSlash.StartSlash += OnSlash;
            On.GameManager.OnDisable += GameManager_OnDisable;
            StartCoroutine(AttackHandlerInit());        
        }

        private void GameManager_OnDisable(On.GameManager.orig_OnDisable orig, GameManager self)
        {
            GameObject go = self.gameObject;

            Destroy(go.GetComponent<HollowPointPrefabs>());
            Destroy(go.GetComponent<OrientationHandler>());
            Destroy(go.GetComponent<WeaponHandler>());
            Destroy(go.GetComponent<WeaponSwapHandler>());
            Destroy(go.GetComponent<UIHandler>());
            Destroy(go.GetComponent<DamageOverride>());
            Destroy(go.GetComponent<HollowPointSprites>());
            Destroy(go.GetComponent<HeatHandler>());
            Destroy(go.GetComponent<SpellControlOverride>());
            Destroy(go.GetComponent<Stats>());
            Destroy(go.GetComponent<AttackHandler>());

            orig(self);
        }

        public IEnumerator AttackHandlerInit()
        {
            while (HeroController.instance == null)
            {
                yield return null;
            }

            hc_instance = HeroController.instance;
            h_state = HeroController.instance.cState;
            knight = HeroController.instance.GetAttr<Rigidbody2D>("rb2d");
            damageNumberTestGO = new GameObject("damageNumberTESTCLONE", typeof(Text), typeof(CanvasRenderer), typeof(RectTransform));
            DontDestroyOnLoad(damageNumberTestGO);
        }

        public void Update()
        {
            //TODO: Replace weapon handler to accomodate "isUsingGun" bool instead of checking what theyre using
            if (WeaponSwapHandler.currentWeapon == WeaponType.Ranged && !isFiring && hc_instance.CanCast())
            {
                if(OrientationHandler.heldAttack && Stats.canFire)
                {
                    Stats.StartBothCooldown();
                    FireGun(FireModes.Single);
                    //FireGun(FireModes.Burst);
                }
                else if (OrientationHandler.pressingAttack && Stats.canFire && false)
                {
                    if (Stats.currentPrimaryAmmo > 0)
                    {
                        //HP_Stats.ReduceAmmunition();
                        Stats.StartBothCooldown();
                        FireGun(FireModes.Spread);
                        //FireGun((PlayerData.instance.equippedCharm_11) ? FireModes.Spread : FireModes.Single);
                    }
                    else if(clickTimer <= 0)
                    {
                        clickTimer = 3f;
                        AudioHandler.PlaySoundsMisc("cantfire");
                    }
                }
            }
            else if (!isFiring)
            {
                hc_instance.WALK_SPEED = 2.5f;
            }

            //TODO: Slow down the player while firing MOVE TO HP STATS LATER
            if (slowWalk)
            {
                h_state.inWalkZone = true;
            }
            else
            {
                h_state.inWalkZone = false;
            }

        }

        void FixedUpdate()
        {
            if (slowWalkDisableTimer > 0 && slowWalk)
            {
                slowWalkDisableTimer -= Time.deltaTime * 30f;
                if (slowWalkDisableTimer < 0)
                {
                    slowWalk = false;
                }
            }

            if (clickTimer > 0)
            {
                clickTimer -= Time.deltaTime * 10;
            }
        }

        public void OnSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            if (WeaponSwapHandler.currentWeapon == WeaponType.Melee)
            {
                orig(self);
                return;
            }
        }

        public void FireGun(FireModes fm)
        {
            if (isFiring) return;
            isFiring = true;

            float finalDegreeDirectionLocal = OrientationHandler.finalDegreeDirection;

            if (airStrikeActive)
            {
                StartCoroutine(FireFlare());
                return;
            }

            if(fm == FireModes.Single)
            {
                slowWalk = ((PlayerData.instance.equippedCharm_37 && PlayerData.instance.equippedCharm_32) || !PlayerData.instance.equippedCharm_37);
                HeroController.instance.WALK_SPEED = Stats.walkSpeed;
                StartCoroutine(SingleShot());
            }
            if (fm == FireModes.Burst)
            {
                slowWalk = ((PlayerData.instance.equippedCharm_37 && PlayerData.instance.equippedCharm_32) || !PlayerData.instance.equippedCharm_37);
                HeroController.instance.WALK_SPEED = Stats.walkSpeed;
                StartCoroutine(BurstShot(3));
            }
            if (fm == FireModes.Spread)
            {
                slowWalk = ((PlayerData.instance.equippedCharm_37 && PlayerData.instance.equippedCharm_32) || !PlayerData.instance.equippedCharm_37);
                HeroController.instance.WALK_SPEED = Stats.walkSpeed;
                StartCoroutine(SpreadShot(5));
                //StartCoroutine(KnockbackRecoil(1f, finalDegreeDirectionLocal));
                return;
            }

            //Firing below will push the player up
            float mult = (PlayerData.instance.equippedCharm_31) ? 2 : 1;
            if (finalDegreeDirectionLocal == 270) StartCoroutine(KnockbackRecoil(1.75f * mult, 270));
            else if (finalDegreeDirectionLocal < 350 && finalDegreeDirectionLocal > 190) StartCoroutine(KnockbackRecoil(0.07f*mult, 270));
        }

        public IEnumerator SingleShot()
        {
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            HeatHandler.IncreaseHeat(1f);

            float direction = OrientationHandler.finalDegreeDirection;
            DirectionalOrientation orientation = OrientationHandler.directionOrientation;
            GameObject bullet = HollowPointPrefabs.SpawnBullet(direction, orientation);

            BulletBehaviour hpbb = bullet.GetComponent<BulletBehaviour>();
            hpbb.fm = FireModes.Single;

            //Charm 14 Steady Body
            hpbb.noDeviation = (PlayerData.instance.equippedCharm_14 && HeroController.instance.cState.onGround) ? true : false;
            //Charm 13 Mark of Pride gives perfect accuracy and pierce
            hpbb.perfectAccuracy = (PlayerData.instance.equippedCharm_13 && (HeatHandler.currentHeat < 10)) ? true : false;
            hpbb.pierce = PlayerData.instance.equippedCharm_13;
            
            //set the origin position of where the bullet was spawned
            hpbb.bulletOriginPosition = bullet.transform.position;

            Destroy(bullet, Stats.bulletRange + 3f);

            HollowPointSprites.StartGunAnims();
            HollowPointSprites.StartFlash();
            HollowPointSprites.StartMuzzleFlash(OrientationHandler.finalDegreeDirection);

            slowWalkDisableTimer = 14f;

            string weaponType = PlayerData.instance.equippedCharm_13 ? "sniper" : "rifle";
            //PlayGunSounds("rifle");
            AudioHandler.PlayGunSounds("rifle");
            if(weaponType == "sniper") bullet.transform.localScale = new Vector3(1.8f, 1.8f, 0.1f);

            yield return new WaitForSeconds(0.02f);
            isFiring = false;
        }
        

        public IEnumerator BurstShot(int burst)
        {
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            for (int i = 0; i < burst; i++)
            {
                HeatHandler.IncreaseHeat(0.5f);

                float direction = OrientationHandler.finalDegreeDirection;
                DirectionalOrientation orientation = OrientationHandler.directionOrientation;
                GameObject bullet = HollowPointPrefabs.SpawnBullet(direction, orientation);

                AudioHandler.PlayGunSounds("rifle");
                Destroy(bullet, .4f);

                HollowPointSprites.StartGunAnims();
                HollowPointSprites.StartFlash();
                HollowPointSprites.StartMuzzleFlash(OrientationHandler.finalDegreeDirection);
                yield return new WaitForSeconds(0.1f); //0.12f This yield will determine the time inbetween shots   

                if (h_state.dashing) break;
            }
            HeatHandler.IncreaseHeat(8f);
            isFiring = false;
            slowWalkDisableTimer = 4f * burst;
        }

        public IEnumerator SpreadShot(int pellets)
        {
            slowWalkDisableTimer = 15f;
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake"); //SmallShake
            HollowPointSprites.StartGunAnims();
            HollowPointSprites.StartFlash();
            HollowPointSprites.StartMuzzleFlash(OrientationHandler.finalDegreeDirection);
            AudioHandler.PlayGunSounds("Shotgun");

            float direction = OrientationHandler.finalDegreeDirection; //90 degrees
            DirectionalOrientation orientation = OrientationHandler.directionOrientation; 

            float coneDegree = 40;
            float angleToSpawnBullet = direction - (coneDegree / 2); //90 - (30 / 2) = 75, start at 75 degrees
            float angleIncreasePerPellet = coneDegree / (pellets + 2); // 30 / (5 + 2) = 4.3, move angle to fire for every pellet by 4.3 degrees

            angleToSpawnBullet = angleToSpawnBullet + angleIncreasePerPellet;

            //Checks if the player is firing upwards, and enables the x offset so the bullets spawns directly ontop of the knight
            //from the gun's barrel instead of spawning to the upper right/left of them 
            bool fixYOrientation = (direction == 270 || direction == 90) ? true : false;
            for (int i = 0; i < pellets; i++)
            {
                yield return new WaitForEndOfFrame();

                GameObject bullet = HollowPointPrefabs.SpawnBullet(angleToSpawnBullet, orientation);
                BulletBehaviour hpbb = bullet.GetComponent<BulletBehaviour>();
                hpbb.bulletDegreeDirection += UnityEngine.Random.Range(-3, 3);
                //hpbb.pierce = PlayerData.instance.equippedCharm_13;
                bullet.transform.localScale = new Vector3(0.2f,0.2f,0.1f);

                angleToSpawnBullet += angleIncreasePerPellet;
                Destroy(bullet, 0.7f);
            }

            yield return new WaitForSeconds(0.05f);
            isFiring = false;
        }

        public IEnumerator FireFlare()
        {
            airStrikeActive = false;
            Stats.currentPrimaryAmmo -= 1;
            SpellControlOverride.artifactActivatedEffect.SetActive(false);

            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

            float direction = OrientationHandler.finalDegreeDirection;
            DirectionalOrientation orientation = OrientationHandler.directionOrientation;
            GameObject bullet = HollowPointPrefabs.SpawnBullet(direction, orientation);
            AudioHandler.PlayGunSounds("flare");

            BulletBehaviour bullet_behaviour = bullet.GetComponent<BulletBehaviour>();
            bullet_behaviour.flareRound = true;

            HollowPointSprites.StartGunAnims();
            HollowPointSprites.StartFlash();
            HollowPointSprites.StartMuzzleFlash(OrientationHandler.finalDegreeDirection);

            yield return new WaitForSeconds(0.04f);
            isFiring = false;
        }

        public IEnumerator KnockbackRecoil(float recoilStrength, float applyForceFromDegree)
        {
            //TODO: Develop the direction launch soon 
            if (recoilStrength < 0.05) yield break;
            float deg = applyForceFromDegree + 180;
            deg = deg % 360;

            float radian = deg * Mathf.Deg2Rad;
            float xDeg = (float) ((1 * recoilStrength) * Math.Cos(radian));
            float yDeg = (float) ((1 * recoilStrength) * Math.Sin(radian));

            xDeg = (xDeg == 0) ? 0 : xDeg;
            yDeg = (yDeg == 0) ? 0 : yDeg;

            //HeroController.instance.cState.shroomBouncing = true;
            HeroController.instance.cState.recoiling = true;

            if (deg == 90 || deg == 270)
            {
                knight.velocity = new Vector2(0, yDeg);
                yield break;
            }

            if (HeroController.instance.cState.facingRight)
            {
                //Modding.Logger.Log(HeroController.instance.GetAttr<float>("RECOIL_HOR_VELOCITY"));
                HeroController.instance.SetAttr<int>("recoilSteps", 0);
                HeroController.instance.cState.recoilingLeft = true;
                HeroController.instance.cState.recoilingRight = false;
                HeroController.instance.SetAttr<bool>("recoilLarge", true);

                knight.velocity = new Vector2(-xDeg, yDeg);
            }
            else
            {
                //Modding.Logger.Log(HeroController.instance.GetAttr<float>("RECOIL_HOR_VELOCITY"));
                HeroController.instance.SetAttr<int>("recoilSteps", 0);
                HeroController.instance.cState.recoilingLeft = false;
                HeroController.instance.cState.recoilingRight = true;
                HeroController.instance.SetAttr<bool>("recoilLarge", true);

                knight.velocity = new Vector2(xDeg, yDeg);
            }

            yield return null;
        }

        public void OnDestroy()
        {
            On.NailSlash.StartSlash -= OnSlash;
            Destroy(gameObject.GetComponent<AttackHandler>());
        }
    }
}
