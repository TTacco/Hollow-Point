﻿using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MonoMod.Utils;
using MonoMod;
using static Modding.Logger;
using Modding;
using Object = UnityEngine.Object;
using static HollowPoint.HollowPointEnums;
using Vasi;

namespace HollowPoint
{
    class AttackHandler : MonoBehaviour
    { 
        public static Rigidbody2D knight;
        public static GameObject damageNumberTestGO;

        public static bool isFiring = false;
        public static bool slowWalk = false;

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
            Destroy(go.GetComponent<WeaponSwapAndStatHandler>());
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
            knight = HeroController.instance.GetComponent<Rigidbody2D>();
            damageNumberTestGO = new GameObject("damageNumberTESTCLONE", typeof(Text), typeof(CanvasRenderer), typeof(RectTransform));
            DontDestroyOnLoad(damageNumberTestGO);
        }

        public void Update()
        {
            //Melee attack with the gun out 
            if (WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Ranged && !isFiring && hc_instance.CanCast())
            {
                /*
                if (InputHandler.Instance.inputActions.dreamNail.WasPressed)
                {
                    Log("[AttackHandler] Changing Firemode from : " + Stats.instance.cardinalFiringMode + " to : " + !Stats.instance.cardinalFiringMode );
                    Stats.instance.ToggleFireMode();
                }
                */
                if(OrientationHandler.heldAttack && Stats.instance.canFire)
                {

                    if(PlayerData.instance.MPCharge >= Stats.instance.SoulCostPerShot())
                    {
                        FireGun(FireModes.Single);
                    }
                    else if(clickTimer <= 0)
                    {
                        AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.ClickSFXGO, alteredPitch: false);
                        clickTimer = 1f;
                    }

                    //FireGun(FireModes.Burst);
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
                clickTimer -= Time.deltaTime * 1;
            }
        }

        public void OnSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            if (WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Melee)
            {
                orig(self);
                return;
            }
        }



        public void FireGun(FireModes fm)
        {
            //if (isFiring) return;
            isFiring = true;
            Stats.instance.StartFirerateCooldown();

            if (Stats.instance.currentEquippedGun.gunName == WeaponModifierName.SMG)
            {
                StartCoroutine(BurstShot(4));
            }
            else if (Stats.instance.currentEquippedGun.gunName == WeaponModifierName.SHOTGUN)
            {
                StartCoroutine(SpreadShot());
            }
            else if (fm == FireModes.Single)
            {
                StartCoroutine(SingleShot());
            }

            //Gun Boosting Call Method
            float fireDegree = OrientationHandler.finalDegreeDirection;

            if (hc_instance.cState.wallSliding) StartCoroutine(KnockbackRecoil(2.5f, 270));
            else if (fireDegree == 270) StartCoroutine(KnockbackRecoil(Stats.instance.current_boostMultiplier, 270));
            else if (fireDegree < 350 && fireDegree > 190) StartCoroutine(KnockbackRecoil(0.07f, 270));
        }

        public IEnumerator SingleShot()
        {
            hc_instance.TakeMPQuick(Stats.instance.SoulCostPerShot());
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");           
            //GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            HeatHandler.IncreaseHeat(Stats.instance.current_heatPerShot);      

            float direction = OrientationHandler.finalDegreeDirection;
            DirectionalOrientation orientation = OrientationHandler.directionOrientation;
            GameObject bullet = HollowPointPrefabs.SpawnBulletFromKnight(direction, orientation);

            BulletBehaviour hpbb = bullet.GetComponent<BulletBehaviour>();
            hpbb.bulletDamage = Stats.instance.current_damagePerShot;
            hpbb.bulletDamageScale = Stats.instance.current_damagePerLevel;
            hpbb.noDeviation = (PlayerData.instance.equippedCharm_14 && HeroController.instance.cState.onGround) ? true : false;
            hpbb.bulletOriginPosition = bullet.transform.position;
            hpbb.bulletDegreeDirection = direction;
            hpbb.size = Stats.instance.currentEquippedGun.bulletSize;
            hpbb.gunUsed = Stats.instance.currentEquippedGun;

            bool sapperBuffs = (Stats.instance.current_class == WeaponSubClass.SABOTEUR && Stats.instance.infusionActivated);
            hpbb.appliesDamageOvertime = (Stats.instance.infusionActivated && PlayerData.instance.equippedCharm_35);
            hpbb.bulletSpeed = (sapperBuffs)? Stats.instance.current_bulletVelocity * 1.20f: Stats.instance.current_bulletVelocity;
            hpbb.piercesEnemy = (Stats.instance.current_weapon == WeaponModifierName.SNIPER || PlayerData.instance.equippedCharm_25);

            AudioHandler.instance.PlayGunSoundEffect(Stats.instance.currentEquippedGun.gunName.ToString());
            HollowPointSprites.StartGunAnims();
            HollowPointSprites.StartFlash();
            HollowPointSprites.StartMuzzleFlash(OrientationHandler.finalDegreeDirection, 1);

            Destroy(bullet, Stats.instance.current_bulletLifetime);

            yield return new WaitForSeconds(0.02f);
            isFiring = false;
        }

        public IEnumerator BurstShot(int burst)
        {
            hc_instance.TakeMP(Stats.instance.SoulCostPerShot());
            HeatHandler.IncreaseHeat(15);
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            float direction = OrientationHandler.finalDegreeDirection;
            DirectionalOrientation orientation = OrientationHandler.directionOrientation;

            for (int i = 0; i < burst; i++)
            {
                GameObject bullet = HollowPointPrefabs.SpawnBulletFromKnight(direction, orientation);
                BulletBehaviour hpbb = bullet.GetComponent<BulletBehaviour>();
                hpbb.bulletDamage = Stats.instance.current_damagePerShot;
                hpbb.bulletDamageScale = Stats.instance.current_damagePerLevel;
                hpbb.gunUsed = Stats.instance.currentEquippedGun;
                hpbb.noDeviation = (PlayerData.instance.equippedCharm_14 && HeroController.instance.cState.onGround) ? true : false;
                hpbb.piercesEnemy = PlayerData.instance.equippedCharm_13;
                hpbb.bulletOriginPosition = bullet.transform.position;
                hpbb.bulletSpeed = Stats.instance.current_bulletVelocity;
                hpbb.bulletDegreeDirection = direction;
                hpbb.size = Stats.instance.currentEquippedGun.bulletSize;

                bool sapperBuffs = (Stats.instance.current_class == WeaponSubClass.SABOTEUR && Stats.instance.infusionActivated);
                hpbb.appliesDamageOvertime = (Stats.instance.infusionActivated && PlayerData.instance.equippedCharm_35);
                hpbb.bulletSpeed = (sapperBuffs) ? Stats.instance.current_bulletVelocity * 1.20f : Stats.instance.current_bulletVelocity;
                hpbb.piercesEnemy = (PlayerData.instance.equippedCharm_25);

                AudioHandler.instance.PlayGunSoundEffect(Stats.instance.currentEquippedGun.gunName.ToString());
                HollowPointSprites.StartGunAnims();
                HollowPointSprites.StartFlash();
                HollowPointSprites.StartMuzzleFlash(OrientationHandler.finalDegreeDirection, 1);

                Destroy(bullet, Stats.instance.current_bulletLifetime);

                yield return new WaitForSeconds(0.055f);
                isFiring = false;

                if (h_state.dashing) break;
            }
            HeatHandler.IncreaseHeat(Stats.instance.current_heatPerShot);

            isFiring = false;
        }

        public IEnumerator SpreadShot()
        {
            hc_instance.TakeMP(Stats.instance.SoulCostPerShot());
            //HeatHandler.IncreaseHeat(50);
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
            int pellets = 6;
            float coneSpread = 40;
            float coneSpreadDelta = coneSpread / (pellets + 1);
            float direction = OrientationHandler.finalDegreeDirection;
            float coneStart = direction - (coneSpread / 2);
            float pelletSpawnDegree = coneStart + coneSpreadDelta;

            DirectionalOrientation orientation = OrientationHandler.directionOrientation;

            AudioHandler.instance.PlayGunSoundEffect(Stats.instance.currentEquippedGun.gunName.ToString());
            HollowPointSprites.StartGunAnims();
            HollowPointSprites.StartFlash();
            HollowPointSprites.StartMuzzleFlash(OrientationHandler.finalDegreeDirection, 1);

            for (int i = 0; i < pellets; i++)
            {
                GameObject bullet = HollowPointPrefabs.SpawnBulletFromKnight(direction, orientation);
                BulletBehaviour hpbb = bullet.GetComponent<BulletBehaviour>();
                hpbb.bulletDamage = Stats.instance.current_damagePerShot;
                hpbb.bulletDamageScale = Stats.instance.current_damagePerLevel;
                hpbb.gunUsed = Stats.instance.currentEquippedGun;
                hpbb.bulletOriginPosition = bullet.transform.position;
                hpbb.bulletSpeed = Stats.instance.current_bulletVelocity;
                hpbb.bulletDegreeDirection = pelletSpawnDegree + UnityEngine.Random.Range(-3f, 3f); ; //direction + UnityEngine.Random.Range(-20f, 20f);
                pelletSpawnDegree += coneSpreadDelta;
                hpbb.size = Stats.instance.currentEquippedGun.bulletSize;
                hpbb.piercesEnemy = true;

                bool sapperBuffs = (Stats.instance.current_class == WeaponSubClass.SABOTEUR && Stats.instance.infusionActivated);
                hpbb.appliesDamageOvertime = (Stats.instance.infusionActivated && PlayerData.instance.equippedCharm_35);
                hpbb.bulletSpeed = (sapperBuffs) ? Stats.instance.current_bulletVelocity * 1.20f : Stats.instance.current_bulletVelocity;

                Destroy(bullet, Stats.instance.current_bulletLifetime + UnityEngine.Random.Range(-0.03f, 0.03f));
            }
            //HeatHandler.IncreaseHeat(Stats.instance.current_heatPerShot);

            yield return null;
            isFiring = false;
        }

        public IEnumerator FireGAU()
        {
            isFiring = true;
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            //float direction = (hc_instance.cState.facingRight) ? 315 : 225;
            //DirectionalOrientation orientation = DirectionalOrientation.Diagonal;
            float direction = OrientationHandler.finalDegreeDirection;
            DirectionalOrientation orientation = OrientationHandler.directionOrientation;

            AudioHandler.instance.PlayGunSoundEffect("gatlinggun");
            for (int b = 0; b < 14; b++)
            {
                GameObject bullet = HollowPointPrefabs.SpawnBulletFromKnight(direction, orientation);
                HeatHandler.IncreaseHeat(1.5f);
                BulletBehaviour hpbb = bullet.GetComponent<BulletBehaviour>();
                hpbb.bulletOriginPosition = bullet.transform.position; //set the origin position of where the bullet was spawned
                hpbb.specialAttrib = "DungExplosionSmall";
                hpbb.bulletSpeedMult += 1.5f;

                HollowPointSprites.StartGunAnims();
                HollowPointSprites.StartFlash();
                HollowPointSprites.StartMuzzleFlash(OrientationHandler.finalDegreeDirection, 1);

                Destroy(bullet, 1f);
                yield return new WaitForSeconds(0.03f); //0.12f This yield will determine the time inbetween shots   
            }

            yield return new WaitForSeconds(0.02f);
            isFiring = false;
        }

        public void SpawnVoidSpikes()
        {
            StartCoroutine(VoidSpikes());
        }

        public IEnumerator VoidSpikes()
        {
            isFiring = true;
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            DirectionalOrientation orientation = DirectionalOrientation.Center;

            float rotation = 0;
            for (int b = 0; b < 15; b++)
            {
                GameObject bullet = HollowPointPrefabs.SpawnBulletFromKnight(rotation, orientation);
                BulletBehaviour hpbb = bullet.GetComponent<BulletBehaviour>();
                hpbb.bulletDamage = 1;
                hpbb.bulletDamageScale = 1;
                hpbb.noDeviation = true;
                hpbb.canGainEnergyCharges = false;
                hpbb.bulletOriginPosition = HeroController.instance.transform.position;
                hpbb.bulletSpeed = 10;
                hpbb.bulletDegreeDirection = rotation;
                hpbb.enableSoulParticles = false;
                hpbb.piercesEnemy = true;
                hpbb.playSoundOnSurfaceHit = false;
                hpbb.size = new Vector3(0.65f, 0.65f, 1);
                hpbb.bulletSprite = BulletBehaviour.BulletSpriteType.voids;
 
                rotation += 24;
                Destroy(bullet, 4f);
            }

            yield return new WaitForSeconds(0.02f);
            isFiring = false;
        }

        public IEnumerator KnockbackRecoil(float recoilStrength, float applyForceFromDegree)
        {
            if (recoilStrength < 0.05) yield break;
            float deg = applyForceFromDegree + 180;
            deg = deg % 360;

            float radian = deg * Mathf.Deg2Rad;
            float xDeg = (float) ((1 * recoilStrength) * Math.Cos(radian));
            float yDeg = (float) ((1 * recoilStrength) * Math.Sin(radian));

            xDeg = (xDeg == 0) ? 0 : xDeg;
            yDeg = (yDeg == 0) ? 0 : yDeg;

            HeroController.instance.cState.shroomBouncing = true;
            HeroController.instance.cState.recoiling = true;

            if (deg == 90 || deg == 270)
            {
                knight.velocity = new Vector2(0, yDeg);
                yield break;
            }

            if (HeroController.instance.cState.facingRight)
            {
                //Modding.Logger.Log(HeroController.instance.GetAttr<float>("RECOIL_HOR_VELOCITY"));
                Mirror.SetField<HeroController, int>(HeroController.instance, "recoilSteps", 0);
                HeroController.instance.cState.recoilingLeft = true;
                HeroController.instance.cState.recoilingRight = false;
                Mirror.SetField<HeroController, bool>(HeroController.instance, "recoilLarge", true);

                knight.velocity = new Vector2(-xDeg, yDeg);
            }
            else
            {
                //Modding.Logger.Log(HeroController.instance.GetAttr<float>("RECOIL_HOR_VELOCITY"));
                Mirror.SetField<HeroController, int>(HeroController.instance, "recoilSteps", 0);
                HeroController.instance.cState.recoilingLeft = false;
                HeroController.instance.cState.recoilingRight = true;
                Mirror.SetField<HeroController, bool>(HeroController.instance, "recoilLarge", true);

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
