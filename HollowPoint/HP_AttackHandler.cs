using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MonoMod.Utils;
using MonoMod;
using HutongGames;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using ModCommon;
using ModCommon.Util;
using Object = UnityEngine.Object;

namespace HollowPoint
{
    class HP_AttackHandler : MonoBehaviour
    {
        float defaultAttackSpeed = 0.41f;
        float defaultAttackSpeed_CH = 0.25f;

        float defaultAnimationSpeed = 0.35f;
        float defaultAnimationSpeed_CH = 0.28f;

        static float accumulativeForceTimer = 0f;
        static float accumulativeForce = 0;
        static bool accumulativeForceIsActive = false;

        public static Rigidbody2D knight;
        public static GameObject damageNumberTestGO;

        public static bool isFiring = false;
        public static bool slowWalk = false;

        static float slowWalkDisableTimer = 0;

        public HeroControllerStates h_state;

        public void Awake()
        {
            On.NailSlash.StartSlash += OnSlash;
            StartCoroutine(InitRoutine());        
        }

        public IEnumerator InitRoutine()
        {
            while(HeroController.instance == null)
            {
                yield return null;
                
            }

            h_state = HeroController.instance.cState;

            knight = HeroController.instance.GetAttr<Rigidbody2D>("rb2d");

            damageNumberTestGO = new GameObject("damageNumberTESTCLONE", typeof(Text), typeof(CanvasRenderer), typeof(RectTransform));
            DontDestroyOnLoad(damageNumberTestGO);

            /*
            enemyBelow = new GameObject("ColliderChecker", typeof(HP_Component_EnemyBelow), typeof(BoxCollider2D), typeof(SpriteRenderer));

            LoadAssets.spriteDictionary.TryGetValue("exampletext.png", out Texture2D rifleTextureInit);
            enemyBelow.GetComponent<SpriteRenderer>().sprite = Sprite.Create(rifleTextureInit,
                new Rect(0, 0, rifleTextureInit.width, rifleTextureInit.height),
                new Vector2(0.5f, 0.5f), 50);

            enemyBelow.GetComponent<BoxCollider2D>().enabled = false;         

            GameObject go = Instantiate(enemyBelow);
            DontDestroyOnLoad(go);
            */
        }

        public void Update()
        {
            //Transfer this into WeaponHandler
            if (HP_WeaponHandler.currentGun.gunName != "Nail") // && !HP_HeatHandler.overheat
            {
                HeroController.instance.ATTACK_DURATION = 0.0f;
                HeroController.instance.ATTACK_DURATION_CH = 0f;

                HeroController.instance.ATTACK_COOLDOWN_TIME = 0.06f;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = 0.06f;
            }
            else
            {
                HeroController.instance.ATTACK_COOLDOWN_TIME = defaultAnimationSpeed;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = defaultAnimationSpeed_CH;

                HeroController.instance.ATTACK_DURATION = defaultAttackSpeed;
                HeroController.instance.ATTACK_DURATION_CH = defaultAttackSpeed_CH;
            }

            //TODO: FORCE WALK
            HeroController.instance.WALK_SPEED = 4.5f;
            HeroController.instance.RUN_SPEED = 9f;

            //HeroController.instance.DASH_SPEED = 160f;
            //HeroController.instance.DASH_TIME = 0.2f;
            //HeroController.instance.DASH_COOLDOWN = 0.35f;

            //TODO: Replace weapon handler to accomodate "isUsingGun" bool instead of checking what theyre using

            if(!(HP_WeaponHandler.currentGun.gunName == "Nail"))
            {
                if (HP_DirectionHandler.holdingAttack && !isFiring && HP_Stats.canFireBurst)
                {
                    //HeroController.instance.Attack(GlobalEnums.AttackDirection.normal);
                    if (PlayerData.instance.MPCharge >= HP_Stats.soulBurstCost)
                        FireGun(true);

                    //HP_Stats.StartBurstCooldown();
                }
                else if (HP_DirectionHandler.pressingAttack && !isFiring && HP_Stats.canFireMain)
                {
                    FireGun(false);
                    //HP_Stats.StartMainCooldown();
                }
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
      
            if(slowWalkDisableTimer > 0 && slowWalk)
            {
                slowWalkDisableTimer -= Time.deltaTime * 30f;
                if(slowWalkDisableTimer < 0 )
                {
                    slowWalk = false;
                }
            }
           
        }

        public void FireGun(bool burst)
        {
            if (isFiring) return;
            isFiring = true;
            HP_Stats.StartBothCooldown();

            float finalDegreeDirectionLocal = HP_DirectionHandler.finalDegreeDirection;
            if (burst)
            {
                HeroController.instance.TakeMP(HP_Stats.soulBurstCost);
                slowWalk = true;
                StartCoroutine(BurstShot(4));
            }
            else
            {
                StartCoroutine(SingleShot());
            }

            //Firing below will push the player up
            if (finalDegreeDirectionLocal == 270) StartCoroutine(KnockbackRecoil(1f, finalDegreeDirectionLocal));
            else if (!HeroController.instance.cState.onGround && (finalDegreeDirectionLocal == 0 || finalDegreeDirectionLocal == 180))
            {
                //float force = (HP_DirectionHandler.right || HP_DirectionHandler.left)? 0.025f : 0.75f;


                StartCoroutine(KnockbackRecoil(0.75f - accumulativeForce, 270));
                accumulativeForce += 0.125f;

                if (accumulativeForce > 0.75f) accumulativeForce = 0.75f;

            }

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
            
            for (int i = 0; i < burst; i++)
            {
                HP_HeatHandler.IncreaseHeat(0.8f);

                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
                PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
                Destroy(bullet, .125f);

                HP_Sprites.StartGunAnims();
                HP_Sprites.StartFlash();
                HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);
                yield return new WaitForSeconds(0.12f); //0.12f This yield will determine the time inbetween shots   

                if (h_state.dashing || h_state.jumping) break;
            }
            isFiring = false;
            slowWalkDisableTimer = 3.75f * burst;

        }

        public IEnumerator SingleShot()
        {
            HP_HeatHandler.IncreaseHeat(1.5f);
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
            PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
            Destroy(bullet, .25f);

            HP_Sprites.StartGunAnims();
            HP_Sprites.StartFlash();
            HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);

            yield return new WaitForSeconds(0.04f);
            isFiring = false;
        }

        public IEnumerator ShotgunShot(int pellets, float directionMultiplier, float directionDegree)
        {
            HP_Sprites.StartGunAnims();
            PlayGunSounds("Shotgun");

            float startAngle = directionDegree - 30;
            for (int i = 0; i < pellets; i++)
            {
                yield return new WaitForEndOfFrame();
                GameObject bullet = Instantiate(HP_Prefabs.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.7f * directionMultiplier, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                bullet.GetComponent<HP_BulletBehaviour>().bulletDegreeDirection = startAngle;
                bullet.GetComponent<HP_BulletBehaviour>().pierce = true; 
                Destroy(bullet, 0.135f);

                startAngle += 6.7f;
            }
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

    }
}
