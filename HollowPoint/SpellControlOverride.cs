using System;
using UnityEngine;
using System.Collections;
using ModCommon.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using static UnityEngine.Random ;
using static Modding.Logger;
using static HollowPoint.HollowPointEnums;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;


namespace HollowPoint
{
    class SpellControlOverride : MonoBehaviour
    {
        public static bool isUsingGun = false;
        //static UnityEngine.Random rand = new UnityEngine.Random();
        PlayMakerFSM nailArtFSM = null;
        PlayMakerFSM glowingWombFSM = null;
        public static bool canSwap = true;
        public float swapTimer = 30f;

        float grenadeCooldown = 30f;
        float airstrikeCooldown = 300f;
        float typhoonTimer = 20f;

        static PlayMakerFSM soulOrbFSM;
        PlayMakerFSM spellControlFSM;
        public static GameObject sharpFlash;
        public static GameObject focusBurstAnim;


        static GameObject infusionSoundGO;
        public static GameObject grimmFireballGO;

        private static ILHook removeFocusCost = null;

        void Awake()
        {
            StartCoroutine(InitSpellControl());
            StartCoroutine(ModifySoulOrbFSM());

            try
            {
                if (removeFocusCost == null)
                {
                    var method = typeof(HeroController).GetMethod("orig_Update", BindingFlags.NonPublic | BindingFlags.Instance);
                    removeFocusCost = new ILHook(method, RemoveFocusSoulCost);
                }
            }
            catch (Exception e)
            {
                Log("SPELLCONTROLOVERRIDE EXCEPTION " + e);
            }
        }

        public IEnumerator ModifySoulOrbFSM()
        {
            while (GameManager.instance.soulOrb_fsm == null)
            {
                yield return null;
            }
            soulOrbFSM = null;
            soulOrbFSM = GameManager.instance.soulOrb_fsm;
            //Array.ForEach<FsmState>(soulOrb.FsmStates, x => Log("FSM Soul Orb : " + x.Name));
            //Array.ForEach<NamedVariable>(soulOrbFSM.FsmVariables.GetAllNamedVariables(), x => Log("FSM Soul Orb Vars : " + x.Name));
            soulOrbFSM.RemoveAction("Can Heal 2", 4);
            soulOrbFSM.RemoveAction("Can Heal 2", 3);
            //PlayerData.instance.focusMP_amount = 15;
        }

        static void RemoveFocusSoulCost(ILContext il)
        {
            Log("Removing Focus Cost");
            var cursor = new ILCursor(il).Goto(0);
            MethodInfo mi = typeof(HeroController).GetMethod("TakeMP");
            cursor.GotoNext(moveType: MoveType.Before, x => x.MatchLdarg(0), x => x.MatchLdcI4(1), x => x.MatchCallvirt(mi));
            for (int x = 0; x < 3; x++) cursor.Remove();
        }

   
        public IEnumerator InitSpellControl()
        {
            while (HeroController.instance == null)
            {
                yield return null;
            }

            try
            {         
                //Get the spellControl and Nail Art FSMs 
                spellControlFSM = HeroController.instance.spellControl;
                nailArtFSM = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");

                //Grimmchild
                //PlayMakerFSM spawnGrimmChild = GameObject.Find("Charm Effects").LocateMyFSM("Spawn Grimmchild");
                //GameObject grimmChild = spawnGrimmChild.GetAction<SpawnObjectFromGlobalPool>("Spawn", 2).gameObject.Value;
                //PlayMakerFSM grimmChildControl = grimmChild.LocateMyFSM("Control");
                //GameObject grimmchildFireball = grimmChildControl.GetAction<SpawnObjectFromGlobalPool>("Shoot", 4).gameObject.Value;
                //grimmchildFireball.AddComponent<TestBehaviour>();

                //grimmChildControl.GetAction<RandomFloat>("Antic", 3).min.Value = 0.1f;
                //grimmChildControl.GetAction<RandomFloat>("Antic", 3).max.Value = 0.1f;

                //Makes hatchlings free
                glowingWombFSM = GameObject.Find("Charm Effects").LocateMyFSM("Hatchling Spawn");
                glowingWombFSM.GetAction<IntCompare>("Can Hatch?", 2).integer2.Value = 0;
                glowingWombFSM.GetAction<Wait>("Equipped", 0).time.Value = 2.5f;
                glowingWombFSM.RemoveAction("Hatch", 0); //Removes the soul consume on spawn
           
                //Modifies Heal Amount, Heal Cost and Heal Speed
                spellControlFSM.GetAction<SetIntValue>("Set HP Amount", 0).intValue = 0; //Heal Amt
                spellControlFSM.GetAction<SetIntValue>("Set HP Amount", 2).intValue = 0; //Heal Amt w/ Shape of Unn
                spellControlFSM.GetAction<GetPlayerDataInt>("Can Focus?", 1).storeValue = 0; //Heal Soul Cost Requirement
                spellControlFSM.FsmVariables.GetFsmFloat("Time Per MP Drain UnCH").Value = 0.01f; //default: 0.0325
                //spellControlFSM.FsmVariables.GetFsmFloat("Time Per MP Drain CH").Value = 0.01f;

                //TODO: Get rid of the infusion stuff
                infusionSoundGO = new GameObject("infusionSoundGO", typeof(AudioSource));
                DontDestroyOnLoad(infusionSoundGO);

                //Get focus burst and shade dash flash game objects to spawn later
                focusBurstAnim = HeroController.instance.spellControl.FsmVariables.GetFsmGameObject("Focus Burst Anim").Value;
                sharpFlash = HeroController.instance.spellControl.FsmVariables.GetFsmGameObject("SD Sharp Flash").Value;
                //Instantiate(qTrail.Value, HeroController.instance.transform).SetActive(true);

                //lowers the scream effects
                FsmGameObject screamFsmGO = spellControlFSM.GetAction<CreateObject>("Scream Burst 1", 2).gameObject;
                screamFsmGO.Value.gameObject.transform.position = new Vector3(0, 0, 0);
                screamFsmGO.Value.gameObject.transform.localPosition = new Vector3(0, -3, 0);
                spellControlFSM.GetAction<CreateObject>("Scream Burst 1", 2).gameObject = screamFsmGO;

                //Note some of these repeats because after removing an action, their index is pushed backwards to fill in the missing parts
                spellControlFSM.RemoveAction("Scream Burst 1", 6);  // Removes both Scream 1 "skulls"
                spellControlFSM.RemoveAction("Scream Burst 1", 6);  // ditto

                spellControlFSM.RemoveAction("Scream Burst 2", 7); //ditto but for Scream 2 (Abyss Shriek)
                spellControlFSM.RemoveAction("Scream Burst 2", 7); //ditto

                spellControlFSM.RemoveAction("Level Check 2", 0); //removes the action that takes your soul when you slam

                spellControlFSM.RemoveAction("Quake1 Land", 9); // Removes slam effect
                spellControlFSM.RemoveAction("Quake1 Land", 11); // removes pillars

                spellControlFSM.RemoveAction("Q2 Land", 11); //slam effects

                spellControlFSM.RemoveAction("Q2 Pillar", 2); //pillars 
                spellControlFSM.RemoveAction("Q2 Pillar", 2); // "Q mega" no idea but removing it otherwise

                spellControlFSM.InsertAction("Can Cast?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "SwapWeapon",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.InsertAction("Can Cast? QC", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "CanCastQC",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.InsertAction("Can Cast? QC", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "CanCastQC_SkipSpellReq",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 3);

                //Removes soul requirement
                //HeroController.instance.spellControl.RemoveAction("Can Cast? QC", 2);


                spellControlFSM.AddAction("Quake Antic", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "StartQuake",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                spellControlFSM.AddAction("Quake1 Land", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "StartTyphoon",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                spellControlFSM.AddAction("Q2 Land", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "StartTyphoon",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                spellControlFSM.InsertAction("Has Fireball?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "SpawnFireball",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.InsertAction("Has Scream?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "HasScream_HasFireSupportAmmo",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.InsertAction("Has Quake?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "HasQuake_CanCastQuake",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.InsertAction("Scream End", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "ScreamEnd",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.InsertAction("Scream End 2", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "ScreamEnd",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.RemoveAction("Scream Burst 1", 3);
                spellControlFSM.RemoveAction("Scream Burst 2", 4);

                spellControlFSM.InsertAction("Focus Heal", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "KnightHasHealed",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.InsertAction("Focus Heal 2", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "KnightHasHealed",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                nailArtFSM.GetAction<ActivateGameObject>("G Slash", 2).activate = false;

                nailArtFSM.AddAction("G Slash", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "State_GSlash",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

            }
            catch (Exception e)
            {
                Modding.Logger.Log(e);
            }

        }

        public void StartQuake()
        {
            //LoadAssets.sfxDictionary.TryGetValue("divetrigger.wav", out AudioClip ac);
            //AudioSource audios = HollowPointSprites.gunSpriteGO.GetComponent<AudioSource>();
            //audios.PlayOneShot(ac);
        }

        public void StartTyphoon()
        {
        }

        IEnumerator SpawnGasPulse(Vector3 spawnPos, float explosionAmount)
        {
            Log("Spawning Gas Pulse");
            float addedDegree = 180 / (explosionAmount + 1);
            AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.DiveDetonateSFXGO);
            GameObject dungCloud;
            for (int pulse = 0; pulse < 1; pulse++)
            {
                dungCloud = HollowPointPrefabs.SpawnObjectFromDictionary("Knight Spore Cloud", HeroController.instance.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                dungCloud.transform.localScale = new Vector3(2f, 2f, 0);
                dungCloud.GetComponent<DamageEffectTicker>().SetAttr<float>("damageInterval", 0.10f); //DEFAULT: 0.15
            }
            yield break;
        }

        public void SwapWeapon()
        {
 
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

            AudioSource audios = HollowPointSprites.gunSpriteGO.GetComponent<AudioSource>();
            if (WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Ranged)
            {
                //Holster gun
                LoadAssets.sfxDictionary.TryGetValue("weapon_holster.wav", out AudioClip ac);
                audios.PlayOneShot(ac);

                /*the ACTUAL attack cool down variable, i did this to ensure the player wont have micro stutters 
                 * on animation because even at 0 animation time, sometimes they play for a quarter of a milisecond
                 * thus giving that weird head jerk anim playing on the knight
                */
                HeroController.instance.SetAttr<float>("attack_cooldown", 0.1f);
                WeaponSwapAndStatHandler.instance.SwapBetweenNail();
            }
            else
            {             
                //Equip gun
                LoadAssets.sfxDictionary.TryGetValue("weapon_draw.wav", out AudioClip ac);
                audios.PlayOneShot(ac);
                WeaponSwapAndStatHandler.instance.SwapBetweenNail();
            }
            isUsingGun = !isUsingGun;

            HeroController.instance.spellControl.SetState("Inactive");
        }

        public void CanCastQC_SkipSpellReq()
        {
            //HeroController.instance.spellControl.SetState("QC");
            //this stuff allows using spells on times youre NOT supposed to use it which is pretty busted
            //forgot why i wrote this, i think it was for preventing soul consumption but i already manually removed that
        }

        public void CanCastQC()
        {
            //Modding.Logger.Log("Forcing Fireball");
            //Modding.Logger.Log("[SpellControlOverride] Can Cast?  " + HeroController.instance.CanCast());
            if (!HeroController.instance.CanCast() || (PlayerData.instance.fireballLevel == 0))
            {
                spellControlFSM.SetState("Inactive");
                return;
            }

            if ((WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Ranged) && !(grenadeCooldown > 0))
            {
                grenadeCooldown = 30f;

                //StartCoroutine(SpreadShot(1));
                spellControlFSM.SetState("Has Fireball?"); 
                spellControlFSM.SetState("Inactive");

                //WeaponSwapHandler.SwapBetweenGun();
            }
            else if (WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Ranged)
            {
                spellControlFSM.SetState("Inactive");
            }
        }

        public void HasQuake_CanCastQuake()
        {
            if (!HeroController.instance.CanCast() || (PlayerData.instance.quakeLevel == 0)) return;

            int soulCost = (PlayerData.instance.equippedCharm_33) ? 24 : 33;

            if(PlayerData.instance.MPCharge < 33 || WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Ranged)
            {       
                HeroController.instance.spellControl.SetState("Inactive");
            }
            else
            {
                typhoonTimer = 20f;
                HeroController.instance.TakeMP(soulCost);
            }
        }

        public void SpawnFireball()
        {
            HeroController.instance.spellControl.SetState("Inactive");


            if (PlayerData.instance.fireballLevel == 0 || Stats.instance.heal_Charges <= 0)
            {
                HeroController.instance.spellControl.SetState("Inactive");
                return;
            }
            //HeroController.instance.TakeMP(soulCost);
            //HeroController.instance.TakeMP(33);

            Stats.instance.ActivateInfusionBuff(true);
            Stats.instance.ConsumeBloodRushCharges();

            HeroController.instance.spellControl.SetState("Spell End");
        }

        public void State_GSlash()
        {
            float startingDegree = HeroController.instance.cState.facingRight ? -25 : 155; 


            for(int k = 0; k < 5; k++)
            {
                GameObject knife = HollowPointPrefabs.SpawnBulletFromKnight(120, DirectionalOrientation.Horizontal);
                BulletBehaviour hpbb = knife.GetComponent<BulletBehaviour>();
                hpbb.weaponUsed = Stats.instance.currentWeapon.gunName;
                hpbb.noDeviation = true;
                hpbb.pierce = true;
                hpbb.bulletOriginPosition = knife.transform.position;
                hpbb.bulletSpeed = 40f;
                hpbb.bulletDegreeDirection = startingDegree;
                hpbb.appliesDamageOvertime = true;
                startingDegree += 10;
                hpbb.size = new Vector3(1, 0.6f, 1);
                Destroy(knife, 1f);

            }
        }

        void Update()
        {
            if (OrientationHandler.pressingAttack && typhoonTimer > 0)
            {
                typhoonTimer = -1;

                int pelletAmnt = (PlayerData.instance.quakeLevel == 2) ? 8 : 6;
                StartCoroutine(SpawnGasPulse(HeroController.instance.transform.position, pelletAmnt));
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

            if (grenadeCooldown > 0)
            {
                grenadeCooldown -= Time.deltaTime * 30f;
            }

            if (airstrikeCooldown > 0)
            {
                airstrikeCooldown -= Time.deltaTime * 30f;
            }

            if (typhoonTimer > 0)
            {
                typhoonTimer -= Time.deltaTime * 30f;
            }
        }


        //========================================SECONDARY FIRE METHODS====================================

        public IEnumerator FireGAU(int rounds)
        {
            AttackHandler.isFiring = true;
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            //float direction = (hc_instance.cState.facingRight) ? 315 : 225;
            //DirectionalOrientation orientation = DirectionalOrientation.Diagonal;
            float direction = OrientationHandler.finalDegreeDirection;
            DirectionalOrientation orientation = OrientationHandler.directionOrientation;

            AudioHandler.instance.PlayGunSoundEffect("gatlinggun");
            for (int b = 0; b < rounds; b++)
            {
                GameObject bullet = HollowPointPrefabs.SpawnBulletFromKnight(direction, orientation);
                HeatHandler.IncreaseHeat(15f);
                BulletBehaviour hpbb = bullet.GetComponent<BulletBehaviour>();
                bullet.AddComponent<BulletIsExplosive>().explosionType = BulletIsExplosive.ExplosionType.DungExplosionSmall;
                hpbb.bulletOriginPosition = bullet.transform.position; //set the origin position of where the bullet was spawned
                //hpbb.specialAttrib = "DungExplosionSmall";
                hpbb.bulletSpeed = 40;

                HollowPointSprites.StartGunAnims();
                HollowPointSprites.StartFlash();
                HollowPointSprites.StartMuzzleFlash(OrientationHandler.finalDegreeDirection, 1);

                Destroy(bullet, 2f);
                yield return new WaitForSeconds(0.03f); //0.12f This yield will determine the time inbetween shots   
            }

            yield return new WaitForSeconds(0.02f);
            AttackHandler.isFiring = false;
        }

        public void CheckNailArt()
        {
            //Modding.Logger.Log("Passing Through Nail Art");
            //nailArtFSM.SetState("Regain Control");
        }

        public void HasScream_HasFireSupportAmmo()
        {

            //if (HP_Stats.artifactPower <= 0 || HP_WeaponHandler.currentGun.gunName != "Nail")
            if (PlayerData.instance.MPCharge < 99 || WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Ranged)
            {
                spellControlFSM.SetState("Inactive");
            }
        }

        public void ScreamEnd()
        {
            //Prepare the airstrike by taking 99 MP
            HeroController.instance.TakeMP(99);
            StartCoroutine(StartSteelRainNoTrack(HeroController.instance.transform.position, 8));

            //artifactActivatedEffect = Instantiate(HeroController.instance.artChargeEffect, HeroController.instance.transform);
            //artifactActivatedEffect.SetActive(true);
            //AttackHandler.airStrikeActive = true;
            //infuseTimer = 500f;
        }

        public void KnightHasHealed()
        {
            Log("Knight Has Focused");
            HeroController.instance.AddHealth(2);//TODO: change this depending on fury
            Stats.instance.ConsumeBloodRushCharges(true);
        }

        //========================================FIRE SUPPORT SPAWN METHODS====================================

        //Regular steel rain (non tracking)
        public static IEnumerator StartSteelRainNoTrack(Vector3 targetCoordinates, int totalShells)
        {
            int artyDirection = (HeroController.instance.cState.facingRight) ? 1 : -1;
            Modding.Logger.Log("SPELL CONTROL STEEL RAIN NO TRACKING");
            float shellAimPosition = 5 * artyDirection; //Allows the shell to "walk" slowly infront of the player
            for (int shells = 0; shells < totalShells; shells++)
            {
                AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.MortarWhistleSFXGO);
                yield return new WaitForSeconds(0.65f);
                //GameObject shell = Instantiate(HollowPointPrefabs.bulletPrefab, targetCoordinates + new Vector3(Range(-5, 5), Range(25, 50), -0.1f), new Quaternion(0, 0, 0, 0));
                GameObject shell = HollowPointPrefabs.SpawnBulletAtCoordinate(270, HeroController.instance.transform.position + new Vector3(shellAimPosition, 60, -0.1f), 0);
                shellAimPosition += 3 * artyDirection;
                BulletBehaviour hpbb = shell.GetComponent<BulletBehaviour>();
                hpbb.fuseTimerXAxis = true;
                hpbb.ignoreCollisions = true;
                hpbb.targetDestination = targetCoordinates + new Vector3(0, Range(6f,8f), -0.1f);
                shell.AddComponent<BulletIsExplosive>().explosionType = BulletIsExplosive.ExplosionType.ArtilleryShell;
                shell.SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }
        }

        public static IEnumerator StartInfusion()
        {
            
            LoadAssets.sfxDictionary.TryGetValue("infusionsound.wav", out AudioClip ac);
            AudioSource aud = infusionSoundGO.GetComponent<AudioSource>();
            aud.PlayOneShot(ac);

            GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");

            //Gives fancy effects to when you infuse yourself, should add a sound soon
            Instantiate(sharpFlash, HeroController.instance.transform).SetActive(true);
            Instantiate(focusBurstAnim, HeroController.instance.transform).SetActive(true);

            //SpriteFlash knightFlash = HeroController.instance.GetAttr<SpriteFlash>("spriteFlash");
            //knightFlash.flashBenchRest();

            GameObject artChargeEffect = Instantiate(HeroController.instance.artChargedEffect, HeroController.instance.transform.position, Quaternion.identity);
            artChargeEffect.SetActive(true);
            artChargeEffect.transform.SetParent(HeroController.instance.transform);

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

        void OnDestroy()
        {
            Destroy(gameObject.GetComponent<SpellControlOverride>());
            Modding.Logger.Log("SpellControl Destroyed");
        }
    }

}
