using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using ModCommon.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine.SceneManagement;
using static UnityEngine.Random ;
using USceneManager = UnityEngine.SceneManagement.SceneManager;


namespace HollowPoint
{
    class HP_SpellControl : MonoBehaviour
    {
        public static bool isUsingGun = false;
        //static UnityEngine.Random rand = new UnityEngine.Random();
        PlayMakerFSM nailArtFSM = null;
        public static bool canSwap = true;
        public float swapTimer = 30f;

        float grenadeCooldown = 30f;
        float airstrikeCooldown = 300f;
        float typhoonTimer = 20f;
        float infuseTimer = 20f;

        PlayMakerFSM spellControl;
        static GameObject sharpFlash;
        public static GameObject focusBurstAnim;

        public static float buff_duration = 0;
        public static bool buffActive = false;
        float buff_constantSoul_timer = 0;

        public static GameObject artifactActivatedEffect;
        static GameObject infusionSoundGO;

        public void Awake()
        {
            StartCoroutine(InitSpellControl());
        }

        void Update()
        {
            if (HP_DirectionHandler.pressingAttack && typhoonTimer > 0)
            {
                typhoonTimer = -1;

                int pelletAmnt = (PlayerData.instance.quakeLevel == 2) ? 12 : 8; 
                StartCoroutine(SpawnTyphoon(HeroController.instance.transform.position, pelletAmnt));
            }

            if (HP_DirectionHandler.pressingAttack && infuseTimer > 0)
            {
                if (HP_WeaponHandler.currentGun.gunName == "Nail")
                {
                    infuseTimer = -1;
                    HP_AttackHandler.airStrikeActive = false;
                    StartCoroutine(StartInfusion());
                }
               
            }
        }

        void FixedUpdate()
        {
            if (swapTimer > 0)
            {
                swapTimer -= Time.deltaTime * 30f;
                canSwap = false;
            }
            else
            {
                canSwap = true;
            }

            if(grenadeCooldown > 0)
            {
                grenadeCooldown -= Time.deltaTime * 30f;
            }

            if(airstrikeCooldown > 0)
            {
                airstrikeCooldown -= Time.deltaTime * 30f;
            }

            if (typhoonTimer > 0)
            {
                typhoonTimer -= Time.deltaTime * 30f;
            }

            if (infuseTimer > 0)
            {
                infuseTimer -= Time.deltaTime * 30f;
            }

            //BUFFS TIMER HANDLERS

            if(buff_duration > 0)
            {
                buff_duration -= Time.deltaTime * 10f;
            }
            else
            {
                buffActive = false;
                return;
            }

            if (buff_constantSoul_timer < 0)
            {
                buff_constantSoul_timer = 5f;
                HeroController.instance.AddMPChargeSpa(3);
            }
            else
            {
                buff_constantSoul_timer -= Time.deltaTime * 10f;
            }

        }

        public IEnumerator InitSpellControl()
        {
            while (HeroController.instance == null)
            {
                yield return null;
            }

            try
            {
                infusionSoundGO = new GameObject("infusionSoundGO", typeof(AudioSource));
                DontDestroyOnLoad(infusionSoundGO);

                focusBurstAnim = HeroController.instance.spellControl.FsmVariables.GetFsmGameObject("Focus Burst Anim").Value;
                sharpFlash = HeroController.instance.spellControl.FsmVariables.GetFsmGameObject("SD Sharp Flash").Value;

                //Instantiate(qTrail.Value, HeroController.instance.transform).SetActive(true);

                spellControl = HeroController.instance.spellControl;
                PlayMakerFSM dive = HeroController.instance.spellControl;
                nailArtFSM = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");

                FsmGameObject fsmgo = dive.GetAction<CreateObject>("Scream Burst 1", 2).gameObject;
                fsmgo.Value.gameObject.transform.position = new Vector3(0, 0, 0);
                fsmgo.Value.gameObject.transform.localPosition = new Vector3(0, -3, 0);
                dive.GetAction<CreateObject>("Scream Burst 1", 2).gameObject = fsmgo;

                //Note some of these repeats because after removing an action, their index is pushed backwards to fill in the missing parts
                spellControl.RemoveAction("Scream Burst 1", 6);  // Removes both Scream 1 "skulls"
                spellControl.RemoveAction("Scream Burst 1", 6);  // same

                spellControl.RemoveAction("Scream Burst 2", 7); //Same but for Scream 2
                spellControl.RemoveAction("Scream Burst 2", 7); //Same

                spellControl.RemoveAction("Level Check 2", 0); //removes the action that takes your soul when you slam

                spellControl.RemoveAction("Quake1 Land", 9); // Removes slam effect
                spellControl.RemoveAction("Quake1 Land", 11); // removes pillars

                spellControl.RemoveAction("Q2 Land", 11); //slam effects

                spellControl.RemoveAction("Q2 Pillar", 2); //pillars 
                spellControl.RemoveAction("Q2 Pillar", 2); // "Q mega" no idea but removing it otherwise

                spellControl.InsertAction("Can Cast?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "SwapWeapon",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControl.InsertAction("Can Cast? QC", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "CanCastQC",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControl.InsertAction("Can Cast? QC", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "CanCastQC_SkipSpellReq",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 3);

                //Removes soul requirement
                //HeroController.instance.spellControl.RemoveAction("Can Cast? QC", 2);


                spellControl.AddAction("Quake Antic", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "StartQuake",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                spellControl.AddAction("Quake1 Land", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "StartTyphoon",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                spellControl.AddAction("Q2 Land", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "StartTyphoon",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                spellControl.InsertAction("Has Fireball?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "SpawnFireball",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControl.InsertAction("Has Scream?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "HasScream_HasFireSupportAmmo",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControl.InsertAction("Has Quake?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "HasQuake_CanCastQuake",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControl.InsertAction("Scream End", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "ScreamEnd",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControl.InsertAction("Scream End 2", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "ScreamEnd",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControl.RemoveAction("Scream Burst 1", 3);
                spellControl.RemoveAction("Scream Burst 2", 4);

                DontDestroyOnLoad(artifactActivatedEffect);

            }
            catch (Exception e)
            {
                Modding.Logger.Log(e);
            }

        }

        public void StartQuake()
        {
            LoadAssets.sfxDictionary.TryGetValue("divetrigger.wav", out AudioClip ac);
            AudioSource audios = HP_Sprites.gunSpriteGO.GetComponent<AudioSource>();
            audios.PlayOneShot(ac);
        }

        public void StartTyphoon()
        {
            //Dung Crest cloud on slam
            if (PlayerData.instance.equippedCharm_10)
            {
                HP_Prefabs.SpawnObjectFromDictionary("Knight Dung Cloud", HeroController.instance.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
            }
            typhoonTimer = 25f;
        }

        IEnumerator SpawnTyphoon(Vector3 spawnPos, float explosionAmount)
        {
            Modding.Logger.Log("Spawning Typhoon");
            float degreeTotal = 0;
            float addedDegree = 180 / (explosionAmount + 1);
            for(; explosionAmount > 0; explosionAmount--)
            {
                yield return new WaitForEndOfFrame();
                degreeTotal += addedDegree;
                GameObject typhoon_ball = Instantiate(HP_Prefabs.bulletPrefab, spawnPos, new Quaternion(0, 0, 0, 0));
                HP_BulletBehaviour hpbb = typhoon_ball.GetComponent<HP_BulletBehaviour>();
                hpbb.bulletDegreeDirection = degreeTotal;
                hpbb.specialAttrib = "DungExplosionSmall";            
                typhoon_ball.SetActive(true);

                //Destroy(typhoon_ball, Range(0.115f, 0.315f));
                Destroy(typhoon_ball, Range(0.115f, 0.315f));
            }
            yield return null;

        }

        public void SwapWeapon()
        {
            //Maybe transfer all of this to weapon control???

            string animName = HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name;
            if (animName.Contains("Sit") || animName.Contains("Get Off") || !HeroController.instance.CanCast()) return;

            if (!canSwap)
            {
                HeroController.instance.spellControl.SetState("Inactive");
                return;
            }

            swapTimer = (PlayerData.instance.equippedCharm_26)? 1.5f : 7f;

            HeroController.instance.spellControl.SetState("Inactive");
            Modding.Logger.Log("Swaping weapons");

            AudioSource audios = HP_Sprites.gunSpriteGO.GetComponent<AudioSource>();
            if (isUsingGun)
            {
                //Holster gun
                LoadAssets.sfxDictionary.TryGetValue("weapon_holster.wav", out AudioClip ac);
                audios.PlayOneShot(ac);

                /*the ACTUAL attack cool down variable, i did this to ensure the player wont have micro stutters 
                 * on animation because even at 0 animation time, sometimes they play for a quarter of a milisecond
                 * thus giving that weird head jerk anim playing on the knight
                */
                HeroController.instance.SetAttr<float>("attack_cooldown", 0.1f); 
                HP_WeaponHandler.currentGun = HP_WeaponHandler.allGuns[0];
            }
            else
            {
                //Equip gun
                LoadAssets.sfxDictionary.TryGetValue("weapon_draw.wav", out AudioClip ac);
                audios.PlayOneShot(ac);
                HP_WeaponHandler.currentGun = HP_WeaponHandler.allGuns[1];
            }
            isUsingGun = !isUsingGun;

            HeroController.instance.spellControl.SetState("Inactive");
        }

        public void CanCastQC_SkipSpellReq()
        {
            HeroController.instance.spellControl.SetState("QC");
        }

        public void CanCastQC()
        {
            //Modding.Logger.Log("Forcing Fireball");
            if (!HeroController.instance.CanCast() || (PlayerData.instance.fireballLevel == 0))
            {
                spellControl.SetState("Inactive");
                return;
            }

            int soulCost = (PlayerData.instance.equippedCharm_33) ? 24 : 33;

            //Prevents casting if under the required soul cost
            //Note, if this method ends, it will transition normally with regards to whatever avaiable spells the player has right now 
            if (PlayerData.instance.MPCharge < soulCost)
            {
                //Modding.Logger.Log("Cancelling fire ball");
                spellControl.SetState("Inactive");
                return;
            }

            if ((!(HP_WeaponHandler.currentGun.gunName == "Nail")) && !(grenadeCooldown > 0))
            {
                grenadeCooldown = 50f;
                spellControl.SetState("Has Fireball?");

            }
            else if (HP_WeaponHandler.currentGun.gunName != "Nail")
            {
                spellControl.SetState("Inactive");
            }
        }

        public void HasQuake_CanCastQuake()
        {
            if (!HeroController.instance.CanCast() || (PlayerData.instance.quakeLevel == 0)) return;

            int soulCost = (PlayerData.instance.equippedCharm_33) ? 24 : 33;

            if(PlayerData.instance.MPCharge < soulCost || HP_WeaponHandler.currentGun.gunName != "Nail")
            {
                HeroController.instance.spellControl.SetState("Inactive");
            }
        }

        public void SpawnFireball()
        {
            int soulCost = (PlayerData.instance.equippedCharm_33) ? 33 : 50;
            if (HP_WeaponHandler.currentGun.gunName == "Nail" || PlayerData.instance.fireballLevel == 0 || PlayerData.instance.MPCharge < soulCost)
            {
                HeroController.instance.spellControl.SetState("Inactive");
                return;
            }
            HeroController.instance.TakeMP(soulCost);
            HeroController.instance.spellControl.SetState("Spell End");

            float directionMultiplier = (HeroController.instance.cState.facingRight) ? 1f : -1f;
            float wallClimbMultiplier = (HeroController.instance.cState.wallSliding) ? -1f : 1f;
            directionMultiplier *= wallClimbMultiplier;
            GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
            bullet.SetActive(true);
            HP_BulletBehaviour hpbb = bullet.GetComponent<HP_BulletBehaviour>();
            hpbb.perfectAccuracy = true;
            bullet.GetComponent<BoxCollider2D>().size *= 1.5f;
            hpbb.bulletDegreeDirection = HP_DirectionHandler.finalDegreeDirection;
            hpbb.specialAttrib = "Explosion";
            hpbb.bulletSpeedMult = 2;

            HP_Prefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite specialBulletTexture);
            bullet.GetComponent<SpriteRenderer>().sprite = specialBulletTexture;

            //HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

            PlayAudio("firerocket", true);

            HP_Sprites.StartGunAnims();
        }

        public void CheckNailArt()
        {
            //Modding.Logger.Log("Passing Through Nail Art");
            //nailArtFSM.SetState("Regain Control");
        }

        public void HasScream_HasFireSupportAmmo()
        {

            if (HP_AttackHandler.airStrikeActive)
            {
                if(artifactActivatedEffect != null) spellControl.SetState("Inactive");
                artifactActivatedEffect.SetActive(false);
                HP_AttackHandler.airStrikeActive = false;
                return;
            }

            //if (HP_Stats.artifactPower <= 0 || HP_WeaponHandler.currentGun.gunName != "Nail")
            if (PlayerData.instance.MPCharge < 99 || HP_WeaponHandler.currentGun.gunName != "Nail")
            {
                spellControl.SetState("Inactive");
            }
        }

        public void ScreamEnd()
        {
            //Prepare the airstrike by taking 99 MP
            HeroController.instance.TakeMP(99);
            artifactActivatedEffect = Instantiate(HeroController.instance.artChargeEffect, HeroController.instance.transform);
            artifactActivatedEffect.SetActive(true);
            HP_AttackHandler.airStrikeActive = true;
            //infuseTimer = 500f;
        }

        //========================================FIRE SUPPORT SPAWN METHODS====================================

        //Regular steel rain (non tracking)
        public static IEnumerator StartSteelRainNoTrack(Vector3 targetCoordinates, int totalShells)
        {
            HP_UIHandler.UpdateDisplay();
            Modding.Logger.Log("SPELL CONTROL STEEL RAIN NO TRACKING");
            for (int shells = 0; shells < totalShells; shells++)
            {
                yield return new WaitForSeconds(0.45f);
                GameObject shell = Instantiate(HP_Prefabs.bulletPrefab, targetCoordinates + new Vector3(Range(-5, 5), Range(25, 50), -0.1f), new Quaternion(0, 0, 0, 0));
                HP_BulletBehaviour hpbb = shell.GetComponent<HP_BulletBehaviour>();
                hpbb.isFireSupportBullet = true;
                hpbb.ignoreCollisions = true;
                hpbb.targetDestination = targetCoordinates + new Vector3(0, Range(2, 8), -0.1f);
                shell.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }
        }

        //For steel rains that tracks targets
        public static IEnumerator StartSteelRain(GameObject enemyGO, int totalShells)
        {
            HP_UIHandler.UpdateDisplay();
            //Modding.Logger.Log("SPELL CONTROL STEEL RAIN TRACK");
            Transform targetCoordinates = enemyGO.transform;
            Vector3 enemyPos = targetCoordinates.position;


            for (int shells = 0; shells < totalShells; shells++)
            {
                yield return new WaitForSeconds(0.45f);
                if ((enemyGO != null) || (targetCoordinates != null)) enemyPos = targetCoordinates.position;

                GameObject shell = Instantiate(HP_Prefabs.bulletPrefab, enemyPos + new Vector3(Range(-5, 5), Range(25, 50), -0.1f), new Quaternion(0, 0, 0, 0));
                HP_BulletBehaviour hpbb = shell.GetComponent<HP_BulletBehaviour>();
                hpbb.isFireSupportBullet = true;
                hpbb.ignoreCollisions = true;
                hpbb.targetDestination = enemyPos + new Vector3(0, Range(2, 8), -0.1f);
                shell.SetActive(true);

                yield return new WaitForSeconds(0.5f);
            }
        }

        public static IEnumerator StartInfusion()
        {
            
            artifactActivatedEffect.SetActive(false);
            LoadAssets.sfxDictionary.TryGetValue("infusionsound.wav", out AudioClip ac);
            AudioSource aud = infusionSoundGO.GetComponent<AudioSource>();
            aud.PlayOneShot(ac);

            buff_duration = (PlayerData.instance.screamLevel > 1) ? 150f : 80f;

            //Charm 9 lifeblood core
            buff_duration += (PlayerData.instance.equippedCharm_9) ? 200f : 0;

            //Joni's Blessing
            /*
            if (PlayerData.instance.equippedCharm_27)
            {
                buff_duration = -40f;
                int mpCharge = PlayerData.instance.MPCharge;
                int grenadeAmount = (int)(mpCharge/15f);

                HP_Stats.grenadeAmnt += grenadeAmount;
                HeroController.instance.TakeMP(mpCharge);
            }
            */
            buffActive = true;

            HP_Stats.ReduceAmmunition();
            
            if (PlayerData.instance.equippedCharm_34)
            {
                buff_duration = -40f;
                HeroController.instance.AddHealth(4);
            }

            //To make sure the minimum buff duration is always at 30f
            buff_duration = (buff_duration < 30f) ? 30f : buff_duration;

            GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");

            HP_UIHandler.UpdateDisplay();

            //Gives fancy effects to when you infuse yourself, should add a sound soon
            Instantiate(sharpFlash, HeroController.instance.transform).SetActive(true);
            Instantiate(focusBurstAnim, HeroController.instance.transform).SetActive(true);

            SpriteFlash knightFlash = HeroController.instance.GetAttr<SpriteFlash>("spriteFlash");
            knightFlash.flashBenchRest();

            GameObject artChargeEffect = Instantiate(HeroController.instance.artChargedEffect, HeroController.instance.transform.position, Quaternion.identity);
            artChargeEffect.SetActive(true);
            artChargeEffect.transform.SetParent(HeroController.instance.transform);
            Destroy(artChargeEffect, buff_duration/10f);

            GameObject artChargeFlash = Instantiate(HeroController.instance.artChargedFlash, HeroController.instance.transform.position, Quaternion.identity);
            artChargeFlash.SetActive(true);
            artChargeFlash.transform.SetParent(HeroController.instance.transform);
            Destroy(artChargeFlash, 0.5f);

            GameObject dJumpFlash = Instantiate(HeroController.instance.dJumpFlashPrefab, HeroController.instance.transform.position, Quaternion.identity);
            dJumpFlash.SetActive(true);
            dJumpFlash.transform.SetParent(HeroController.instance.transform);
            Destroy(dJumpFlash, 0.5f);

            yield return null;
        }



        public static void PlayAudio(string audioName, bool addPitch)
        {
            LoadAssets.sfxDictionary.TryGetValue(audioName.ToLower() + ".wav", out AudioClip ac);
            AudioSource audios = HeroController.instance.spellControl.GetComponent<AudioSource>();
            audios.clip = ac;
            audios.pitch = 1;
            //HP_Sprites.gunSpriteGO.GetComponent<AudioSource>().PlayOneShot(ac);
            if (addPitch)
                audios.pitch = Range(0.8f, 1.5f);

            audios.PlayOneShot(audios.clip);
        }

        void OnDestroy()
        {
            Destroy(gameObject.GetComponent<HP_SpellControl>());
            Modding.Logger.Log("SpellControl Destroyed");
        }
    }

}
