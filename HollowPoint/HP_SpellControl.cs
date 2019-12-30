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

        public void Awake()
        {
            StartCoroutine(InitSpellControl());
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
                if (HP_DirectionHandler.pressingAttack)
                {
                    typhoonTimer = -1;
                    StartCoroutine(SpawnTyphoon(HeroController.instance.transform.position,5));
                }
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

                PlayMakerFSM dive = HeroController.instance.spellControl;
                nailArtFSM = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");
                //dive.RemoveAction("Quake1 Land", 14);

                /*
                FsmOwnerDefault fsmgo = dive.GetAction<ActivateGameObject>("Quake1 Land", 4).gameObject;
                Modding.Logger.Log(fsmgo.GameObject.Name);
                prefab1 = fsmgo.GameObject.Value;
                */

                FsmGameObject fsmgo = dive.GetAction<CreateObject>("Scream Burst 1", 2).gameObject;
                fsmgo.Value.gameObject.transform.position = new Vector3(0, 0, 0);
                fsmgo.Value.gameObject.transform.localPosition = new Vector3(0, -3, 0);
                dive.GetAction<CreateObject>("Scream Burst 1", 2).gameObject = fsmgo;


                //Note some of these repeats because after removing an action, their index is pushed backwards to fill in the missing parts
                HeroController.instance.spellControl.RemoveAction("Scream Burst 1", 6);  // Removes both Scream 1 "skulls"
                HeroController.instance.spellControl.RemoveAction("Scream Burst 1", 6);  // same

                HeroController.instance.spellControl.RemoveAction("Scream Burst 2", 7); //Same but for Scream 2
                HeroController.instance.spellControl.RemoveAction("Scream Burst 2", 7); //Same

                HeroController.instance.spellControl.RemoveAction("Quake1 Land", 9); // Removes slam effect
                HeroController.instance.spellControl.RemoveAction("Quake1 Land", 11); // removes pillars

                HeroController.instance.spellControl.RemoveAction("Q2 Land", 11); //slam effects

                HeroController.instance.spellControl.RemoveAction("Q2 Pillar", 2); //pillars 
                HeroController.instance.spellControl.RemoveAction("Q2 Pillar", 2); // "Q mega" no idea but removing it otherwise




                HeroController.instance.spellControl.InsertAction("Can Cast?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "SwapWeapon",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);      

                HeroController.instance.spellControl.AddAction("Quake Antic", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "StartQuake",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                HeroController.instance.spellControl.AddAction("Quake1 Land", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "StartTyphoon",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                HeroController.instance.spellControl.AddAction("Q2 Land", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "StartTyphoon",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                HeroController.instance.spellControl.InsertAction("Has Fireball?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "StartFireball",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                HeroController.instance.spellControl.InsertAction("Can Cast? QC", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "ForceFireball",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 3);

                HeroController.instance.spellControl.InsertAction("Scream End", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "ScreamEndOne",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                HeroController.instance.spellControl.InsertAction("Scream End 2", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                    methodName = "ScreamEndOne",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

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
            //audios.clip = ac;

            audios.PlayOneShot(ac);
        }

        public void StartTyphoon()
        {
            //LoadAssets.sfxDictionary.TryGetValue("divedetonate.wav", out AudioClip ac);
            //AudioSource audios = HP_Sprites.gunSpriteGO.GetComponent<AudioSource>();
            //audios.clip = ac;
            //audios.PlayOneShot(ac);

            //HP_Prefabs.prefabDictionary.TryGetValue("Knight Dung Cloud", out GameObject cloud);
            //GameObject cloudGO = Instantiate(cloud, HeroController.instance.transform.position + new Vector3(0, 0, -1), Quaternion.identity);
            //cloudGO.SetActive(true);

            //Activate this if SOUL EATER is equipped

            typhoonTimer = 40f;

            //StartCoroutine(SpawnTyphoon(HeroController.instance.transform.position, 5));
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
                Destroy(typhoon_ball, 0.125f);
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
                HeroController.instance.spellControl.SetState("Spell End");
                return;
            }

            swapTimer = (PlayerData.instance.equippedCharm_26)? 2f : 45f;

            HeroController.instance.spellControl.SetState("Spell End");
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

            HeroController.instance.spellControl.SetState("Spell End");
        }

        public void ForceFireball()
        {
            //Modding.Logger.Log("Forcing Fireball");
            if (!HeroController.instance.CanCast() || (PlayerData.instance.fireballLevel == 0)) return;

            int soulCost = (PlayerData.instance.equippedCharm_33) ? 24 : 33;
            if ((!(HP_WeaponHandler.currentGun.gunName == "Nail")) && (PlayerData.instance.MPCharge >= soulCost) && HP_Stats.grenadeAmnt > 0 && !(grenadeCooldown > 0))
            {
                grenadeCooldown = 30f;
                HeroController.instance.TakeMP(soulCost);
                HeroController.instance.spellControl.SetState("Has Fireball?");
                HP_Stats.grenadeAmnt -= 1;
                HP_UIHandler.UpdateDisplay();
            }
            else if (HP_WeaponHandler.currentGun.gunName != "Nail")
            {
                HeroController.instance.spellControl.SetState("Spell End");
            }
        }


        public void StartFireball()
        {
            if (HP_WeaponHandler.currentGun.gunName == "Nail")
            {
                HeroController.instance.spellControl.SetState("Spell End");
                return;
            }

            try
            {

                HeroController.instance.spellControl.SetState("Spell End");

                float directionMultiplier = (HeroController.instance.cState.facingRight) ? 1f : -1f;
                float wallClimbMultiplier = (HeroController.instance.cState.wallSliding) ? -1f : 1f;
                directionMultiplier *= wallClimbMultiplier;
                // GameObject bullet = Instantiate(HP_Prefabs.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.2f * directionMultiplier, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                GameObject bullet = HP_Prefabs.SpawnBullet(HP_DirectionHandler.finalDegreeDirection);
                bullet.SetActive(true);
                HP_BulletBehaviour hpbb = bullet.GetComponent<HP_BulletBehaviour>();
                bullet.GetComponent<BoxCollider2D>().size *= 1.5f;
                hpbb.bulletDegreeDirection = HP_DirectionHandler.finalDegreeDirection;
                hpbb.specialAttrib = "Explosion";
                hpbb.bulletSpeedMult = 2;

                HP_Prefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite specialBulletTexture);
                bullet.GetComponent<SpriteRenderer>().sprite = specialBulletTexture;

                //HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

                PlayAudio("firerocket", true);

            }
            catch (Exception e)
            {
                Modding.Logger.Log("[SpellControl] StartFireball() " + e);
            }

            HP_Sprites.StartGunAnims();
        }

        public void CheckNailArt()
        {
            Modding.Logger.Log("Passing Through Nail Art");
            //nailArtFSM.SetState("Regain Control");
        }

        public void ScreamEndOne()
        {
            Modding.Logger.Log("AIR STRIKE AVAILABLE - WAITING FOR COORDINATES");

            if (HP_AttackHandler.flareRound)
            {
                StartCoroutine(CallSupplies());
                HP_AttackHandler.flareRound = false;
                return;
            }


            if (airstrikeCooldown > 0 || HP_Stats.fireSupportAmnt <= 0)
            {
                int soulCost = (PlayerData.instance.equippedCharm_33) ? 24 : 33; //refunds soul when airstrike unavailable
                HeroController.instance.AddMPCharge(soulCost);
                return;
            }

            HP_Stats.fireSupportAmnt -= 1;
            HP_UIHandler.UpdateDisplay();
            HP_AttackHandler.flareRound = true;

            //AirStrike in progress
            StartCoroutine(AirStrikeRequestDelay());
        }

        //Delays the airstrike sound file so doesnt sound like quirrel is receiving your request immediately
        IEnumerator AirStrikeRequestDelay()
        {
            yield return new WaitForSeconds(0.5f);
            PlayAudio("airstrikerequest", false);
        }


        //========================================FIRE SUPPORT SPAWN METHODS====================================

        public static IEnumerator CallMortar(Vector3 targetCoordinates)
        {
            //GameObject bullet = Instantiate(HP_Prefabs.bulletPrefab, targetCoordinates + new Vector3(0, 300, -0.1f), new Quaternion(0, 0, 0, 0));
            //HP_BulletBehaviour hpbb = bullet.GetComponent<HP_BulletBehaviour>();
            yield return new WaitForSeconds(1f);
            PlayAudio("airstrikeinbound", false);
            Modding.Logger.Log("STEEL RAIN IS INBOUND");
            yield return new WaitForSeconds(4f);
            
            for (int ammo = 0; ammo < 8; ammo++)
            {
                //Modding.Logger.Log("ROUND " + ammo);
                PlayAudio("mortarclose", true);
                yield return new WaitForSeconds(0.45f);

                //for (int cannister = 0; cannister < 3; cannister++)
               // {
                    GameObject shell = Instantiate(HP_Prefabs.bulletPrefab, targetCoordinates + new Vector3(Range(-5, 5), Range(25,50), -0.1f), new Quaternion(0, 0, 0, 0));
                    HP_BulletBehaviour hpbb = shell.GetComponent<HP_BulletBehaviour>();
                    hpbb.isFireSupportBullet = true;
                    hpbb.ignoreCollisions = true;
                    hpbb.targetDestination = targetCoordinates + new Vector3(0, Range(2, 8), -0.1f); 
                    shell.SetActive(true);
                //}
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(4.5f);
            PlayAudio("airstrikeend", false);

        }

        public static IEnumerator CallSupplies()
        {
            //yield return new WaitForSeconds(1f);
            //PlayAudio("airstrikeinbound", false);
            //Modding.Logger.Log("SUPPLY DROP IS INBOUND");
            //yield return new WaitForSeconds(1f);

            HeroController.instance.AddMPCharge(99);

            //Gives fancy effects to when you infuse yourself, should add a sound soon
            SpriteFlash knightFlash = HeroController.instance.GetAttr<SpriteFlash>("spriteFlash");
            knightFlash.flashBenchRest();

            GameObject artChargeEffect = Instantiate(HeroController.instance.artChargedEffect, HeroController.instance.transform.position, Quaternion.identity);
            artChargeEffect.SetActive(true);
            artChargeEffect.transform.SetParent(HeroController.instance.transform);
            Destroy(artChargeEffect, 0.5f);

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

    }

}
