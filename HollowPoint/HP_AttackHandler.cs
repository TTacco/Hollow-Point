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
using static HollowPoint.HP_Enums;

namespace HollowPoint
{
    class HP_AttackHandler : MonoBehaviour
    { 
        public static Rigidbody2D knight;
        public static GameObject damageNumberTestGO;

        public static bool isFiring = false;
        public static bool isBursting = false;
        public static bool slowWalk = false;
        public static bool artifactActive = false; //Dictates this round will send determine if the bullet is an airstrike marker

        static float slowWalkDisableTimer = 0;
        float clickTimer = 0;

        public HeroControllerStates h_state;

        GameObject clickAudioGO;
        GameObject fireAudioGO;

        public void Awake()
        {
            On.NailSlash.StartSlash += OnSlash;
            //On.HeroController.CanDash += HeroController_CanDash;
            On.GameManager.OnDisable += GameManager_OnDisable;
            StartCoroutine(InitRoutine());        
        }

        private void GameManager_OnDisable(On.GameManager.orig_OnDisable orig, GameManager self)
        {
            GameObject go = self.gameObject;

            Destroy(go.GetComponent<HP_Prefabs>());
            Destroy(go.GetComponent<HP_DirectionHandler>());
            Destroy(go.GetComponent<HP_WeaponHandler>());
            Destroy(go.GetComponent<HP_WeaponSwapHandler>());
            Destroy(go.GetComponent<HP_UIHandler>());
            Destroy(go.GetComponent<HP_DamageCalculator>());
            Destroy(go.GetComponent<HP_Sprites>());
            Destroy(go.GetComponent<HP_HeatHandler>());
            Destroy(go.GetComponent<HP_SpellControl>());
            Destroy(go.GetComponent<HP_Stats>());
            Destroy(go.GetComponent<HP_AttackHandler>());

            orig(self);
        }

        private bool HeroController_CanDash(On.HeroController.orig_CanDash orig, HeroController self)
        {
            if (slowWalk)
            {
                if (PlayerData.instance.equippedCharm_31) return orig(self);

                else return false;
            }

            return orig(self);
        }

        public IEnumerator InitRoutine()
        {
            while (HeroController.instance == null)
            {
                yield return null;
            }
            clickAudioGO = new GameObject("GunEmptyGO", typeof(AudioSource));
            fireAudioGO = new GameObject("GunFireGO", typeof(AudioSource));

            DontDestroyOnLoad(clickAudioGO);
            DontDestroyOnLoad(fireAudioGO);

            h_state = HeroController.instance.cState;
            knight = HeroController.instance.GetAttr<Rigidbody2D>("rb2d");
            damageNumberTestGO = new GameObject("damageNumberTESTCLONE", typeof(Text), typeof(CanvasRenderer), typeof(RectTransform));
            DontDestroyOnLoad(damageNumberTestGO);
        }

        public void Update()
        {
            //TODO: Replace weapon handler to accomodate "isUsingGun" bool instead of checking what theyre using
            if (!(HP_WeaponHandler.currentGun.gunName == "Nail") && !isFiring && HeroController.instance.CanCast())
            {
                if (HP_DirectionHandler.heldAttack && HP_Stats.canFire)
                {
                    if (PlayerData.instance.MPCharge >= HP_Stats.fireSoulCost)
                    {
                        HP_Stats.StartBothCooldown();
                        FireGun((PlayerData.instance.equippedCharm_11)? FireModes.Spread : FireModes.Single );
                    }
                    else if(clickTimer <= 0)
                    {
                        clickTimer = 3f;
                        PlaySoundsMisc("cantfire");
                    }
                }
            }
            else if (!isFiring)
            {
                HeroController.instance.WALK_SPEED = 2.5f;
            }

            //TODO: Slow down the player while firing MOVE TO HP STATS LATER
            if (slowWalk)
            {
                HeroController.instance.cState.inWalkZone = true;
            }
            else
            {
                HeroController.instance.cState.inWalkZone = false;
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


        public void FireGun(FireModes fm)
        {
            if (isFiring) return;
            isFiring = true;

            HP_Stats.StartBothCooldown();

            float finalDegreeDirectionLocal = HP_DirectionHandler.finalDegreeDirection;

            if (artifactActive)
            {
                StartCoroutine(FireFlare());
                return;
            }

            if(fm == FireModes.Single)
            {
                HeroController.instance.TakeMPQuick(HP_Stats.fireSoulCost);

                slowWalk = !PlayerData.instance.equippedCharm_32;
                slowWalk = PlayerData.instance.equippedCharm_37;
                HeroController.instance.WALK_SPEED = HP_Stats.walkSpeed;
                //StartCoroutine(SingleShot());
                StartCoroutine(SingleShot());
            }
            if (fm == FireModes.Spread)
            {
                HeroController.instance.TakeMPQuick(HP_Stats.fireSoulCost);

                slowWalk = !PlayerData.instance.equippedCharm_32;
                slowWalk = PlayerData.instance.equippedCharm_37;
                HeroController.instance.WALK_SPEED = HP_Stats.walkSpeed;
                StartCoroutine(SpreadShot(5));

                StartCoroutine(KnockbackRecoil(3f, finalDegreeDirectionLocal));
                return;
            }

            //Firing below will push the player up
            float mult = (PlayerData.instance.equippedCharm_31) ? 2 : 1;
            if (finalDegreeDirectionLocal == 270) StartCoroutine(KnockbackRecoil(1.75f * mult, 270));
            else if (finalDegreeDirectionLocal < 350 && finalDegreeDirectionLocal > 190) StartCoroutine(KnockbackRecoil(0.07f*mult, 270));

        }

        public void OnSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            if (HP_WeaponHandler.currentGun.gunName == "Nail" && artifactActive)
            {
                artifactActive = false;
                StartCoroutine(HP_SpellControl.StartInfusion());
                return;
            }

            if (HP_WeaponHandler.currentGun.gunName == "Nail")
            {
                orig(self);
                return;
            }
        }

        public IEnumerator SingleShot()
        {
            HP_HeatHandler.IncreaseHeat(HP_Stats.heatPerShot);
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
            HP_BulletBehaviour hpbb = bullet.GetComponent<HP_BulletBehaviour>();
            hpbb.fm = FireModes.Single;

            //Charm 14 Steady Body
            hpbb.noDeviation = (PlayerData.instance.equippedCharm_14 && HeroController.instance.cState.onGround) ? true : false;
            //Charm 13 Mark of Pride
            hpbb.perfectAccuracy = (PlayerData.instance.equippedCharm_13 && (HP_HeatHandler.currentHeat < 10)) ? true : false;

            Destroy(bullet, HP_Stats.bulletRange);

            HP_Sprites.StartGunAnims();
            HP_Sprites.StartFlash();
            HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);

            slowWalkDisableTimer = 10f;

            string weaponType = PlayerData.instance.equippedCharm_13 ? "sniper" : "rifle";

            if (weaponType.Contains("sniper"))
            {
                bullet.transform.localScale = new Vector3(1.3f, 1.3f, 0.1f);
            }

            PlayGunSounds(weaponType);

            yield return new WaitForSeconds(0.02f);
            isFiring = false;

        }
        

        public IEnumerator BurstShot(int burst)
        {
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            for (int i = 0; i < burst; i++)
            {
                HP_HeatHandler.IncreaseHeat(0.5f);
         
                GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
                PlayGunSounds("rifle");
                Destroy(bullet, .2f);

                HP_Sprites.StartGunAnims();
                HP_Sprites.StartFlash();
                HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);
                yield return new WaitForSeconds(0.12f); //0.12f This yield will determine the time inbetween shots   

                if (h_state.dashing || h_state.jumping) break;
            }
            isFiring = false;
            isBursting = false;
            slowWalkDisableTimer = 4f * burst;
        }

        public IEnumerator FireFlare()
        {
            artifactActive = false;
            HP_Stats.artifactPower -= 1;
            HP_SpellControl.artifactActivatedEffect.SetActive(false);

            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
            PlayGunSounds("flare");

            HP_BulletBehaviour bullet_behaviour = bullet.GetComponent<HP_BulletBehaviour>();
            bullet_behaviour.flareRound = true;

            HP_Sprites.StartGunAnims();
            HP_Sprites.StartFlash();
            HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);

            yield return new WaitForSeconds(0.04f);
            isFiring = false;
        }

        public IEnumerator SpreadShot(int pellets)
        {
            slowWalkDisableTimer = 15f;
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
            HP_Sprites.StartGunAnims();
            HP_Sprites.StartFlash();
            HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);
            PlayGunSounds("Shotgun");

            float direction = HP_DirectionHandler.finalDegreeDirection;
            for (int i = 0; i < pellets; i++)
            {
                yield return new WaitForEndOfFrame();
                GameObject bullet = HP_Prefabs.SpawnBullet(direction);
                HP_BulletBehaviour hpbb = bullet.GetComponent<HP_BulletBehaviour>();
                hpbb.bulletDegreeDirection += UnityEngine.Random.Range(-20, 20);
                bullet.transform.localScale = new Vector3(0.3f,0.3f,0.1f);

                Destroy(bullet, HP_Stats.bulletRange);
            }

            yield return new WaitForSeconds(0.05f);
            isFiring = false;
        }

        public IEnumerator KnockbackRecoil(float recoilStrength, float applyForceFromDegree)
        {
            //TODO: Develop the direction launch soon 
            if (recoilStrength < 0.05) yield break;
            float deg = applyForceFromDegree + 180;
            deg = deg % 360;

            float radian = deg * Mathf.Deg2Rad;
            float xDeg = (float) ((4 * recoilStrength) * Math.Cos(radian));
            float yDeg = (float) ((4 * recoilStrength) * Math.Sin(radian));

            xDeg = (xDeg == 0) ? 0 : xDeg;
            yDeg = (yDeg == 0) ? 0 : yDeg;

            HeroController.instance.cState.shroomBouncing = true;

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


        public void PlayGunSounds(string gunName)
        {
            try
            {
                //HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.enemyHurtSFX[soundRandom.Next(0, 2)]);
                //AudioManager print = GameManager.instance.AudioManager;
                //print.GetAttr<float>("volume");
                LoadAssets.sfxDictionary.TryGetValue("shoot_sfx_" + gunName.ToLower() + ".wav", out AudioClip ac);
                //AudioSource audios = HP_Sprites.gunSpriteGO.GetComponent<AudioSource>();
                AudioSource audios = fireAudioGO.GetComponent<AudioSource>();
                audios.clip = ac;
                audios.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                audios.PlayOneShot(audios.clip);

                //Play subsonic WOOOP (lol) whenever you fire
                //LoadAssets.sfxDictionary.TryGetValue("subsonicsfx.wav", out ac);
                //audios.PlayOneShot(ac);

            }
            catch (Exception e)
            {
                Modding.Logger.Log("HP_AttackHandler.cs, cannot find the SFX " + gunName + " " + e);
            }
        }

        public void PlaySoundsMisc(string soundName)
        {
            try
            {
                //HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.enemyHurtSFX[soundRandom.Next(0, 2)]);
                LoadAssets.sfxDictionary.TryGetValue(soundName + ".wav", out AudioClip ac);
                AudioSource audios = clickAudioGO.GetComponent<AudioSource>();
                
                audios.clip = ac;
                audios.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
                audios.PlayOneShot(audios.clip);


            }
            catch (Exception e)
            {
                Modding.Logger.Log("HP_AttackHandler.cs, cannot find the SFX " + soundName + " " + e);
            }
        }

        public void OnDestroy()
        {
            On.NailSlash.StartSlash -= OnSlash;
            On.HeroController.CanDash -= HeroController_CanDash;
            Destroy(gameObject.GetComponent<HP_AttackHandler>());
        }
    }
}
