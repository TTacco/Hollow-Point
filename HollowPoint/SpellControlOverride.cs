using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
        //static UnityEngine.Random rand = new UnityEngine.Random();
        PlayMakerFSM nailArtFSM = null;
        PlayMakerFSM glowingWombFSM = null;

        float grenadeCooldown = 30f;
        float airstrikeCooldown = 300f;
        float typhoonTimer = 20f;

        static PlayMakerFSM soulOrbFSM;
        PlayMakerFSM spellControlFSM;
        public static GameObject sharpFlash;
        public static GameObject focusBurstAnim;

        static GameObject infusionSoundGO;

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


                spellControlFSM.RemoveAction("Quake1 Land", 9);
                spellControlFSM.RemoveAction("Quake1 Land", 11); // removes pillars

                spellControlFSM.RemoveAction("Q2 Land", 11); //slam effects

                spellControlFSM.RemoveAction("Q2 Pillar", 2); //pillars 
                spellControlFSM.RemoveAction("Q2 Pillar", 2); // "Q mega" no idea but removing it otherwise

                spellControlFSM.InsertAction("Can Cast?", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "State_CanCast",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.InsertAction("Can Cast? QC", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "State_CanCastQC",
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

                spellControlFSM.AddAction("Quake1 Land", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "State_QuakeLand",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                spellControlFSM.AddAction("Q2 Land", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "State_QuakeLand",
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
                    methodName = "State_FocusHeal",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                spellControlFSM.InsertAction("Focus Heal 2", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "State_FocusHeal",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                , 0);

                nailArtFSM.GetAction<ActivateGameObject>("G Slash", 2).activate = false;

                //For Fan of Knives
                nailArtFSM.AddAction("G Slash", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "State_GSlash",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                //Dagger Rain
                nailArtFSM.AddAction("Cyclone Spin", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "State_CycloneSpin",
                    parameters = new FsmVar[0],
                    everyFrame = false
                }
                );

                //Assassinate

                nailArtFSM.GetAction<ActivateGameObject>("Dash Slash", 0).activate = false; //Disable DSlash
                nailArtFSM.InsertAction("Dash Slash", new CallMethod
                {
                    behaviour = GameManager.instance.GetComponent<SpellControlOverride>(),
                    methodName = "State_DashSlash",
                    parameters = new FsmVar[0],
                    everyFrame = false
                },
                0);
            }
            catch (Exception e)
            {
                Modding.Logger.Log(e);
            }

        }

        //STATES 

        public void State_CanCast()
        {
            string animName = HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name;
            if (animName.Contains("Sit") || animName.Contains("Get Off") || !HeroController.instance.CanCast()) return;

            if (Stats.instance.canSwap)
            {
                WeaponSwapAndStatHandler.instance.SwapWeapons();
                spellControlFSM.SetState("Inactive");
            }

            spellControlFSM.SetState("Inactive");
            return;
        }

        public void CanCastQC_SkipSpellReq()
        {

        }

        //This state handles the checking whether a can cast during Quick Cast or not 
        public void State_CanCastQC()
        {
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


            if (PlayerData.instance.fireballLevel == 0 || Stats.instance.adrenalineCharges <= 0)
            {
                HeroController.instance.spellControl.SetState("Inactive");
                return;
            }
            //HeroController.instance.TakeMP(soulCost);
            //HeroController.instance.TakeMP(33);

            Stats.instance.ActivateInfusionBuff(true);
            Stats.instance.ConsumeAdrenalineCharges();

            HeroController.instance.spellControl.SetState("Spell End");
        }

        //State GSlash is called whenever the player is about to perform a great slash, use this to spawn the knives
        public void State_GSlash()
        {
            float startingDegree = HeroController.instance.cState.facingRight ? -25 : 155;
            AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.ThrowDaggerSFXGO);
            for (int k = 0; k < 5; k++)
            {
                GameObject knife = HollowPointPrefabs.SpawnBulletFromKnight(120, DirectionalOrientation.Horizontal);
                BulletBehaviour hpbb = knife.GetComponent<BulletBehaviour>();
                hpbb.gunUsed = Stats.instance.currentEquippedGun;
                hpbb.bulletDamage = 2;
                hpbb.bulletDamageScale = 2;
                hpbb.noDeviation = true;
                hpbb.bulletOriginPosition = knife.transform.position;
                hpbb.bulletSpeed = 30f;
                hpbb.bulletDegreeDirection = startingDegree;
                //hpbb.appliesDamageOvertime = true;
                hpbb.isDagger = true;
                hpbb.piercesEnemy = true;
                startingDegree += 10;
                hpbb.size = new Vector3(1.2f, 0.65f, 1);
                Destroy(knife, 0.45f);
            }

            Stats.instance.DisableNailArts(6f);
        }

        public void State_CycloneSpin()
        {
            Stats.instance.enemyList.RemoveAll(enemy => enemy == null);
            Log("Enemy Count " + Stats.instance.enemyList.Count);
            //Array.ForEach<GameObject>(Stats.instance.enemyList.ToArray(), g => Log("GameObject Name Is " + g.name));

            foreach(GameObject enemy in Stats.instance.enemyList)
            {
                Vector2 knightPos = HeroController.instance.transform.position;
                Vector2 enemyPos = enemy.transform.position;
                double angle = Math.Atan2(enemyPos.y - knightPos.y, enemyPos.x - knightPos.x) * 180 / Math.PI;
                Log("Throwing at " + angle);
                GameObject knife = HollowPointPrefabs.SpawnBulletFromKnight(120, DirectionalOrientation.Vertical);
                BulletBehaviour hpbb = knife.GetComponent<BulletBehaviour>();
                hpbb.gunUsed = Stats.instance.currentEquippedGun;
                hpbb.bulletDamage = 3;
                hpbb.bulletDamageScale = 4;
                hpbb.noDeviation = true;
                hpbb.bulletOriginPosition = knife.transform.position;
                hpbb.bulletSpeed = 45f;
                hpbb.bulletDegreeDirection = (float)angle;//Range(0, HeroController.instance.cState.onGround ? 180 : 360);
                //hpbb.appliesDamageOvertime = true;
                hpbb.isDagger = true;
                hpbb.bulletSprite = BulletBehaviour.BulletSpriteType.dagger;
                hpbb.size = new Vector3(1.2f, 1f, 1);


                TrailRenderer knifeTR = knife.AddComponent<TrailRenderer>();
                knifeTR.material = new Material(Shader.Find("Diffuse"));
                //knifeTR.material = new Material(Shader.Find("Particles/Additive"));	
                //knifeTR.widthMultiplier = 0.05f;	
                knifeTR.startWidth = 0.2f;
                knifeTR.endWidth = 0.04f;
                knifeTR.numCornerVertices = 50;
                knifeTR.numCapVertices = 30;
                knifeTR.enabled = true;
                knifeTR.time = 0.10f;
                knifeTR.startColor = new Color(102, 178, 255);
                knifeTR.endColor = new Color(204, 229, 255);

                Destroy(knife, 60f);

            }

            Stats.instance.DisableNailArts(18f);
        }

        public void State_DashSlash()
        {
            AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.ThrowDaggerSFXGO);
            float startingDegree = HeroController.instance.cState.facingRight ? 0 : 180;
            GameObject knife = HollowPointPrefabs.SpawnBulletFromKnight(120, DirectionalOrientation.Horizontal);
            BulletBehaviour hpbb = knife.GetComponent<BulletBehaviour>();
            hpbb.gunUsed = Stats.instance.currentEquippedGun;
            hpbb.bulletDamage = 25;
            hpbb.bulletDamageScale = 5;
            hpbb.noDeviation = true;
            hpbb.bulletOriginPosition = knife.transform.position;
            hpbb.bulletSpeed = 45f;
            hpbb.bulletDegreeDirection = startingDegree;
            //hpbb.appliesDamageOvertime = true;
            hpbb.isDagger = true;
            hpbb.size = new Vector3(1.6f, 1f, 1);
            Destroy(knife, 1f);


            Stats.instance.DisableNailArts(12f);
        }

        //Called once the Scream/Shriek has ended, used to call in an airstrike
        public void ScreamEnd()
        {
            //HeroController.instance.TakeMP(99);
            //StartCoroutine(StartSteelRainNoTrack(HeroController.instance.transform.position, 8));
            StartCoroutine(ScreamAbility_CreepingAirburst());
            //StartCoroutine(ScreamAbility_GasArtilleryAlt());
            //StartCoroutine(SparkingBullets());
        }

        public void State_QuakeLand()
        {
            //GameObject explosionClone = HollowPointPrefabs.SpawnObjectFromDictionary("Gas Explosion Recycle M", HeroController.instance.transform.position, Quaternion.identity);
            //explosionClone.transform.localScale = new Vector3(1.25f, 1.25f, -1f);
            //StartCoroutine(QuakeAbility_GasPulse());
            StartCoroutine(QuakeAbility_DangerClose());
        }

        //Triggers from both Heal 1 and Heal 2 states, this state is accessed when the knight successfully heals
        public void State_FocusHeal()
        {
            Log("Knight Has Focused");
            HeroController.instance.AddHealth(2);//TODO: change this depending on fury
            Stats.instance.ConsumeAdrenalineCharges(true);
        }


        public void HasScream_HasFireSupportAmmo()
        {
            //if (HP_Stats.artifactPower <= 0 || HP_WeaponHandler.currentGun.gunName != "Nail")
            if (PlayerData.instance.MPCharge < 99 || WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Ranged)
            {
                spellControlFSM.SetState("Inactive");
            }
        }

        void Update()
        {
            if (OrientationHandler.pressingAttack && typhoonTimer > 0)
            {
                typhoonTimer = -1;
                int pelletAmnt = (PlayerData.instance.quakeLevel == 2) ? 8 : 6;
                GameObject explosionClone = HollowPointPrefabs.SpawnObjectFromDictionary("Gas Explosion Recycle M", HeroController.instance.transform.position, Quaternion.identity);
                explosionClone.transform.localScale = new Vector3(1.5f, 1.5f, 1);
                //StartCoroutine(SpawnGasPulse(HeroController.instance.transform.position, pelletAmnt));
            }
        }

        void FixedUpdate()
        {
            if (grenadeCooldown > 0) grenadeCooldown -= Time.deltaTime * 30f;

            if (airstrikeCooldown > 0) airstrikeCooldown -= Time.deltaTime * 30f;

            if (typhoonTimer > 0) typhoonTimer -= Time.deltaTime * 30f;
        }


        //========================================SECONDARY FIRE METHODS====================================

        public IEnumerator SparkingBullets()
        {
            int sparkAmount = 60;
            GameObject closestEnemy = null;
            while (sparkAmount > 0)
            {
                yield return new WaitForSeconds(0.12f);
                Stats.instance.enemyList.RemoveAll(enemy => enemy == null);

                //Create a list of valid targets from the enemy list, meaning anyone closer than 10 units
                List<GameObject> validTargets = new List<GameObject>();
                while (Stats.instance.enemyList.Count <= 0 || HeroController.instance.cState.transitioning) yield return null;
                foreach (GameObject enemy in Stats.instance.enemyList)
                {
                    Vector2 knightPos = HeroController.instance.transform.position;
                    Vector2 enemyPos = enemy.transform.position;
                    float enemyDistanceFromKnight = Vector3.Distance(knightPos, enemyPos);
                    if (enemyDistanceFromKnight < 10) validTargets.Add(enemy);   
                }

                if (validTargets.Count <= 0) continue; //If there is no valid tagets, start all over again;

                //For each of the valid enemies, find the closest one to the knight
                closestEnemy = null;
                float closetEnemyDistance = float.MaxValue;
                foreach (GameObject validTarget in validTargets)
                {
                    Vector2 knightPos = HeroController.instance.transform.position;
                    Vector2 enemyPos = validTarget.transform.position;
                    float enemyDistance = Vector3.Distance(knightPos, enemyPos);
                    if (Vector3.Distance(knightPos, enemyPos) < closetEnemyDistance)
                    {
                        closestEnemy = validTarget;
                        closetEnemyDistance = enemyDistance;
                    }
                }

                Vector2 knightPos2 = HeroController.instance.transform.position;
                Vector2 enemyPos2 = closestEnemy.transform.position;
                double fireBulletAtAngle = Math.Atan2(enemyPos2.y - knightPos2.y, enemyPos2.x - knightPos2.x) * 180 / Math.PI;

                AudioHandler.instance.PlayGunSoundEffect("lmg");
                GameObject spark = HollowPointPrefabs.SpawnBulletFromKnight((float)fireBulletAtAngle, DirectionalOrientation.Center);
                BulletBehaviour hpbb = spark.GetComponent<BulletBehaviour>();
                hpbb.gunUsed = Stats.instance.currentEquippedGun;
                hpbb.bulletDamage = 2;
                hpbb.bulletDamageScale = 2;
                hpbb.noDeviation = true;
                hpbb.bulletOriginPosition = spark.transform.position;
                hpbb.bulletSpeed = 40f;
                hpbb.bulletDegreeDirection = (float)fireBulletAtAngle + Range(-3,3);
                hpbb.size = new Vector3(0.8f, 0.8f, 1);
                Destroy(spark, 1);
                sparkAmount -= 1;
            }
            yield return null;
        }
        

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

                Destroy(bullet, 4f);
                yield return new WaitForSeconds(0.03f); //0.12f This yield will determine the time inbetween shots   
            }

            yield return new WaitForSeconds(0.02f);
            AttackHandler.isFiring = false;
        }

        //========================================SPELL METHODS====================================

        public static IEnumerator ScreamAbility_GasArtilleryAlt()
        {
            int burstAmount = 3;
            int shellsPerBurst = 6;
            float shellLifeTime = 0.07f;

            for (int burst = 0; burst < burstAmount; burst++)
            {
                //AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.MortarWhistleSFXGO, alteredPitch: false);
                float waveRotation = 60;
                for (int shells = 0; shells < shellsPerBurst; shells++)
                {
                    Vector3 knightPos = HeroController.instance.transform.position;
                    GameObject shell = HollowPointPrefabs.SpawnBulletAtCoordinate(waveRotation + Range(-10f, 10f), knightPos, 0);
                    BulletBehaviour hpbb = shell.GetComponent<BulletBehaviour>();
                    hpbb.ignoreAllCollisions = true;
                    hpbb.bulletSpeed = 70f;
                    hpbb.size = new Vector2(0.6f, 0.7f);
                    hpbb.bulletSprite = BulletBehaviour.BulletSpriteType.dung;
                    shell.AddComponent<BulletIsExplosive>().explosionType = BulletIsExplosive.ExplosionType.DungGas;
                    shell.SetActive(true);
                    Destroy(shell, shellLifeTime);
                    shellLifeTime += 0.01f;
                    waveRotation += 60;

                    yield return new WaitForSeconds(0.08f);
                }
                //yield return new WaitForSeconds(1f);
            }
        }

        //Regular steel rain (non tracking)
        public static IEnumerator ScreamAbility_GasArtillery()
        {
            int burstAmount = 6;
            int shellsPerBurst = 6;
            Vector3 knightPos = HeroController.instance.transform.position;
            for (int burst = 0; burst < burstAmount; burst++)
            {
                //AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.MortarWhistleSFXGO, alteredPitch: false);
                for (int shells = 0; shells < shellsPerBurst; shells++)
                {
                    //GameObject shell = Instantiate(HollowPointPrefabs.bulletPrefab, targetCoordinates + new Vector3(Range(-5, 5), Range(25, 50), -0.1f), new Quaternion(0, 0, 0, 0));
                    GameObject shell = HollowPointPrefabs.SpawnBulletAtCoordinate(270, knightPos + new Vector3(Range(-15, 15), 120, 1f), 0);
                    BulletBehaviour hpbb = shell.GetComponent<BulletBehaviour>();
                    hpbb.fuseTimerXAxis = true;
                    hpbb.ignoreAllCollisions = true;
                    hpbb.targetDestination = knightPos + new Vector3(0, Range(-1f, 12f), -0.1f);
                    //shell.AddComponent<BulletIsExplosive>().explosionType = BulletIsExplosive.ExplosionType.ArtilleryShell;
                    shell.AddComponent<BulletIsExplosive>().explosionType = BulletIsExplosive.ExplosionType.DungGas;
                    shell.SetActive(true);
                    yield return new WaitForSeconds(0.14f);
                }

                //yield return new WaitForSeconds(0.7f);
            }
        }

        public static IEnumerator ScreamAbility_CreepingAirburst()
        {
            Vector3 targetCoordinates = HeroController.instance.transform.position;
            int totalShells = 5;
            int artyDirection = (HeroController.instance.cState.facingRight) ? 1 : -1;
            Modding.Logger.Log("SPELL CONTROL STEEL RAIN NO TRACKING");
            float shellAimPosition = 5 * artyDirection; //Allows the shell to "walk" slowly infront of the player
            AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.MortarWhistleSFXGO, alteredPitch: false);
            yield return new WaitForSeconds(0.65f);
            for (int shells = 0; shells < totalShells; shells++)
            {
                //GameObject shell = Instantiate(HollowPointPrefabs.bulletPrefab, targetCoordinates + new Vector3(Range(-5, 5), Range(25, 50), -0.1f), new Quaternion(0, 0, 0, 0));
                GameObject shell = HollowPointPrefabs.SpawnBulletAtCoordinate(270, HeroController.instance.transform.position + new Vector3(shellAimPosition, 80, -1f), 0);
                shellAimPosition += 2 * artyDirection;
                BulletBehaviour hpbb = shell.GetComponent<BulletBehaviour>();
                hpbb.fuseTimerXAxis = true;
                hpbb.ignoreAllCollisions = true;
                hpbb.targetDestination = targetCoordinates + new Vector3(0, Range(5f, 6f), -0.1f);
                //shell.AddComponent<BulletIsExplosive>().explosionType = BulletIsExplosive.ExplosionType.ArtilleryShell;
                shell.AddComponent<BulletIsExplosive>().explosionType = BulletIsExplosive.ExplosionType.ArtilleryShell;
                shell.SetActive(true);
                yield return new WaitForSeconds(0.30f);
            }
        }

        IEnumerator QuakeAbility_GasPulse()
        {
            Vector3 spawnPos = HeroController.instance.transform.position;
            float pulseAmount = 5;

            Log("Spawning Gas Pulse");

            AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.DiveDetonateSFXGO);
            GameObject dungCloud;
            for (int pulse = 0; pulse < pulseAmount; pulse++)
            {
                dungCloud = HollowPointPrefabs.SpawnObjectFromDictionary("Knight Spore Cloud", HeroController.instance.transform.position + new Vector3(0, 0, -.001f), Quaternion.identity);
                dungCloud.transform.localScale = new Vector3(2f, 2f, 0);
                dungCloud.GetComponent<DamageEffectTicker>().SetAttr<float>("damageInterval", 0.10f); //DEFAULT: 0.15

                yield return new WaitForSeconds(0.80f);
            }
            yield break;
        }

        public IEnumerator QuakeAbility_BulletSpray()
        {
            for (int i = 0; i < 30; i++)
            {
                float degreeDeviation = Range(0, 180);
                GameObject bullet = HollowPointPrefabs.SpawnBulletFromKnight(Range(0, 180), DirectionalOrientation.Center);
                BulletBehaviour hpbb = bullet.GetComponent<BulletBehaviour>();
                hpbb.gunUsed = Stats.instance.currentEquippedGun;
                hpbb.bulletDamage = 4;
                hpbb.bulletDamageScale = 3;
                hpbb.noDeviation = true;
                hpbb.bulletOriginPosition = bullet.transform.position;
                hpbb.bulletSpeed = 50f;
                hpbb.bulletDegreeDirection = degreeDeviation;
                hpbb.piercesEnemy = true;
                hpbb.piercesWalls = true;
                hpbb.size = new Vector3(1f, 0.65f, 1);
                Destroy(bullet, 0.25f);

                yield return new WaitForSeconds(0.01f);
            }
        }

        public static IEnumerator QuakeAbility_DangerClose()
        {
            int totalShells = 10;
            Vector3 knightPos = HeroController.instance.transform.position;
            AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.MortarWhistleSFXGO, alteredPitch: false);
            yield return new WaitForSeconds(0.65f);
            for (int shells = 0; shells < totalShells; shells++)
            {
                //GameObject shell = Instantiate(HollowPointPrefabs.bulletPrefab, targetCoordinates + new Vector3(Range(-5, 5), Range(25, 50), -0.1f), new Quaternion(0, 0, 0, 0));
                GameObject shell = HollowPointPrefabs.SpawnBulletAtCoordinate(270, knightPos + new Vector3(Range(-6,6), 80, -1f), 0);
                BulletBehaviour hpbb = shell.GetComponent<BulletBehaviour>();
                hpbb.fuseTimerXAxis = true;
                hpbb.targetDestination = knightPos + new Vector3(0, Range(2f, 8f), -0.1f);
                hpbb.ignoreAllCollisions = true;
                shell.AddComponent<BulletIsExplosive>().explosionType = BulletIsExplosive.ExplosionType.DungExplosion;
                shell.SetActive(true);
                yield return new WaitForSeconds(0.05f);
            }
        }

        public IEnumerator StrafingRun(GameObject enemyTarget)
        {
            yield return null;


        }

        void OnDestroy()
        {
            Destroy(gameObject.GetComponent<SpellControlOverride>());
            Modding.Logger.Log("SpellControl Destroyed");
        }
    }

}
