﻿using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MonoMod.Utils;
using MonoMod;
using Modding;
using ModCommon;
using ModCommon.Util;
using Object = UnityEngine.Object;
using static HollowPoint.HP_Enums;

namespace HollowPoint
{
    class HP_AttackHandler : MonoBehaviour
    {
        static float accumulativeForceTimer = 0f;
        static float accumulativeForce = 0;
        static bool accumulativeForceIsActive = false;

        public static Rigidbody2D knight;
        public static GameObject damageNumberTestGO;

        public static bool isFiring = false;
        public static bool isBursting = false;
        public static bool slowWalk = false;
        public static bool flareRound = false; //Dictates this round will send determine if the bullet is an airstrike marker

        static float slowWalkDisableTimer = 0;

        public HeroControllerStates h_state;

        GameObject clickAudioGO;

        public void Awake()
        {
            On.NailSlash.StartSlash += OnSlash;
            StartCoroutine(InitRoutine());        
        }

        private void HeroController_HeroDash(On.HeroController.orig_HeroDash orig, HeroController self)
        {
            if (isBursting)
            {
                Modding.Logger.Log(self.cState.facingRight);
                if (self.cState.facingRight)
                {
                    self.FaceLeft();
                }
                else
                {
                    self.FaceRight();
                }

                //StartCoroutine(BackDash(self));
            } 

            orig(self);
        }

        public IEnumerator InitRoutine()
        {
            while(HeroController.instance == null)
            {
                yield return null;            
            }
            clickAudioGO = new GameObject("GunEmptyGO",typeof(AudioSource));
            DontDestroyOnLoad(clickAudioGO);

            h_state = HeroController.instance.cState;
            knight = HeroController.instance.GetAttr<Rigidbody2D>("rb2d");
            damageNumberTestGO = new GameObject("damageNumberTESTCLONE", typeof(Text), typeof(CanvasRenderer), typeof(RectTransform));
            DontDestroyOnLoad(damageNumberTestGO);
        }

        public void Update()
        {
            //TODO: Replace weapon handler to accomodate "isUsingGun" bool instead of checking what theyre using
            if (!(HP_WeaponHandler.currentGun.gunName == "Nail") && !isFiring)
            {
                if (HP_DirectionHandler.holdingAttack && HP_Stats.canFireBurst)
                {
                    if (PlayerData.instance.MPCharge >= HP_Stats.soulBurstCost)
                    {
                        HP_Stats.StartBothCooldown();
                        FireGun(FireModes.Burst);
                    }
                    else
                    {
                        PlaySoundsMisc("cantfire");
                    }
                }
                else if (HP_DirectionHandler.pressingAttack && HP_Stats.canFireSingle)
                {
                    if (PlayerData.instance.MPCharge >= HP_Stats.soulSingleCost)
                    {
                        HP_Stats.StartBothCooldown();
                        FireGun(FireModes.Single);
                    }
                    else
                    {
                        PlaySoundsMisc("cantfire");
                    }
                }
            }
            else if (!isFiring)
            {
                HeroController.instance.WALK_SPEED = 3f;
            }

            //If the player lands, allow themselves to get lifted from the ground again when firing
            if (accumulativeForce > 0 && (h_state.onGround || h_state.doubleJumping || h_state.wallSliding))
            {
                accumulativeForce = 0;
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

            if (slowWalkDisableTimer > 0 && slowWalk)
            {
                slowWalkDisableTimer -= Time.deltaTime * 30f;
                if (slowWalkDisableTimer < 0)
                {
                    slowWalk = false;
                }
            }

        }


        public void FireGun(FireModes fm)
        {
            if (isFiring) return;
            isFiring = true;

            HP_Stats.StartBothCooldown();

            float finalDegreeDirectionLocal = HP_DirectionHandler.finalDegreeDirection;

            if (flareRound)
            {              
                StartCoroutine(FireFlare());
                return;
            }

            if (fm == FireModes.Burst)
            {
                HeroController.instance.TakeMP(HP_Stats.soulBurstCost);
                slowWalk = true;
                isBursting = true;

                //Change this depending on the Charm
                HeroController.instance.WALK_SPEED = 3f;

                if (PlayerData.instance.equippedCharm_11)
                {
                    StartCoroutine(SpreadShot(5));
                }
                else
                {
                    StartCoroutine(BurstShot(3));
                }
            }
            else if(fm == FireModes.Single)
            {
                HeroController.instance.TakeMP(HP_Stats.soulSingleCost);
                slowWalk = true;

                HeroController.instance.WALK_SPEED = (PlayerData.instance.equippedCharm_14) ? 4f : 5.5f;
                StartCoroutine(SingleShot());
            }

            //Firing below will push the player up
            if (finalDegreeDirectionLocal == 270) StartCoroutine(KnockbackRecoil(1f, finalDegreeDirectionLocal));
            else if (finalDegreeDirectionLocal > 215 && finalDegreeDirectionLocal < 325) StartCoroutine(KnockbackRecoil(0.5f, finalDegreeDirectionLocal));
            /*
            else if (!HeroController.instance.cState.onGround && (finalDegreeDirectionLocal == 0 || finalDegreeDirectionLocal == 180))
            {
                //float force = (HP_DirectionHandler.right || HP_DirectionHandler.left)? 0.025f : 0.75f;
                StartCoroutine(KnockbackRecoil(0.75f - accumulativeForce, 270));
                accumulativeForce += 0.125f;

                if (accumulativeForce > 0.75f) accumulativeForce = 0.75f;
            }
            */
            return;
        }

        public void OnSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            if (HP_WeaponHandler.currentGun.gunName == "Nail")
            {
                orig(self);
                return;
            }
        }

        public IEnumerator BurstShot(int burst)
        {
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            //HeroController.instance.SetAttr<int>("recoilSteps", 2);
            for (int i = 0; i < burst; i++)
            {
                HeroController.instance.RecoilLeft();
                HP_HeatHandler.IncreaseHeat(0.5f);
         
                GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
                bullet.GetComponent<HP_BulletBehaviour>().fm = FireModes.Burst;
                PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
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

        public IEnumerator SingleShot()
        {
            HP_HeatHandler.IncreaseHeat(0.75f);
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
            bullet.GetComponent<HP_BulletBehaviour>().fm = FireModes.Single;

            float lifeSpan = (PlayerData.instance.equippedCharm_18) ? .325f : .225f;
            Destroy(bullet, lifeSpan);

            HP_Sprites.StartGunAnims();
            HP_Sprites.StartFlash();
            HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);

            slowWalkDisableTimer = 17f;
            PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
            yield return new WaitForSeconds(0.04f);
            isFiring = false;
        }

        public IEnumerator FireFlare()
        {
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
            PlayGunSounds("flare");

            HP_BulletBehaviour bullet_behaviour = bullet.GetComponent<HP_BulletBehaviour>();
            bullet_behaviour.flareRound = true;

            HP_Sprites.StartGunAnims();
            HP_Sprites.StartFlash();
            HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);

            flareRound = false;
            yield return new WaitForSeconds(0.04f);
            isFiring = false;
        }

        public IEnumerator SpreadShot(int pellets)
        {
            slowWalkDisableTimer = 25f;
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            HP_Sprites.StartGunAnims();
            HP_Sprites.StartFlash();
            HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);
            PlayGunSounds("Shotgun");

            float direction = HP_DirectionHandler.finalDegreeDirection;
            for (int i = 0; i < pellets; i++)
            {
                yield return new WaitForEndOfFrame();
                GameObject bullet = HP_Prefabs.SpawnBullet(direction);
                bullet.GetComponent<HP_BulletBehaviour>().bulletDegreeDirection += UnityEngine.Random.Range(-20, 20);
                Destroy(bullet, UnityEngine.Random.Range(0.15f, 0.3f));
            }

            yield return new WaitForSeconds(0.05f);
            isFiring = false;
            isBursting = false;
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
                LoadAssets.sfxDictionary.TryGetValue("shoot_sfx_" + gunName.ToLower() + ".wav", out AudioClip ac);
                AudioSource audios = HP_Sprites.gunSpriteGO.GetComponent<AudioSource>();
                audios.clip = ac;
                //HP_Sprites.gunSpriteGO.GetComponent<AudioSource>().PlayOneShot(ac);
                audios.pitch = UnityEngine.Random.Range(0.8f, 1.2f);

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

    }
}