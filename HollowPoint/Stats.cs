using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Random;
using static Modding.Logger;
using Modding;
using MonoMod;
using Language;
using System.Xml;
using static HollowPoint.HollowPointEnums;


namespace HollowPoint
{
    class Stats : MonoBehaviour
    {
        public static Stats instance = null;

        public static event Action<string> fireModeIcon;
        public static event Action<string> adrenalineChargeIcons;
        public static event Action<string> nailArtIcon;

        const float DEFAULT_ATTACK_SPEED = 0.41f;
        const float DEFAULT_ATTACK_SPEED_CH = 0.25f;
        const float DEFAULT_ANIMATION_SPEED = 0.35f;
        const float DEFAULT_ANIMATION_SPEED_CH = 0.28f;

        //static ShitStack extraWeavers = new ShitStack();
        //TODO: clean unused variables
        //Setters for values values
        public WeaponModifierName current_weapon;
        public WeaponSubClass current_class;
        public float current_bulletLifetime = 0;
        public float current_bulletVelocity = 0;
        public float current_boostMultiplier = 0;
        public int current_damagePerShot = 0;
        public int current_damagePerLevel = 0;
        public int current_energyGainOnHit = 0;
        public float current_fireRateCooldown = 5f;
        public float current_heatPerShot = 0;
        public int current_soulCostPerShot = 1;
        public float current_soulRegenSpeed = 1;
        public float current_soulRegenTimer = 3f;
        public float current_walkSpeed = 3f;
        public int current_minWeaponSpreadFactor = 1;
        public int current_soulGainedPerHit = 0;
        public int current_soulGainedPerKill = 0;

        int current_energyRequirement = 100;
        float current_energyPenalty = 0.6f;

        public bool canUseNailArts = false;
        public bool canFire = false;
        public bool usingGunMelee = false;
        public bool cardinalFiringMode = false;
        public bool slowWalk = false;
        public bool hasActivatedAdrenaline = false;
        public bool hiveBloodHealActive = false;

        //Update Timers
        public float canUseNailArtsTimer = 1f;
        public float recentlyFiredTimer = 0;
        public float passiveSoulTimer = 3f;
        private float recentlyKilledTimer;
        public float fireRateCooldownTimer = 5f;
        float recentlyTookDamageTimer = 0f;
        float hiveBloodHealTimer = 0f;

        //Weapon Swapping
        public bool canSwap = true;
        public float swapTimer = 30f;

        //Quick Access Instances
        public PlayerData pd_instance;
        public HeroController hc_instance;
        public AudioManager am_instance;

        //Variables for the healing mechanic
        public int adrenalineCharges;
        int adrenalineEnergy;
        float adenalineCooldownTimer;
        float heal_ChargesDecayTimer;
        bool adrenalineOnCooldown;

        //Infusion Stuff
        GameObject furyParticle = null;
        GameObject furyBurst = null;
        float infusionTimer = 0;
        public bool infusionActivated = false;

        public Gun currentEquippedGun;

        public void Awake()
        {
            if (instance == null) instance = this;

            Log("Intializing Stats ");
            StartCoroutine(InitStats());
        }

        public IEnumerator InitStats()
        {
            while (HeroController.instance == null)
            {
                yield return null;
            }

            pd_instance = PlayerData.instance;
            hc_instance = HeroController.instance;
            am_instance = GameManager.instance.AudioManager;

            /*
            Log("Default Dash Cooldown " + hc_instance.DASH_COOLDOWN);
            Log("Default Dash Cooldown Charm " + hc_instance.DASH_COOLDOWN_CH);
            Log("Default Dash Speed " + hc_instance.DASH_SPEED);
            Log("Default Dash Speed Sharp " + hc_instance.DASH_SPEED_SHARP);
            Log("Default Dash Time " + hc_instance.DASH_TIME);
            Log("Default Dash Gravity " + hc_instance.DEFAULT_GRAVITY);
            */
            //Log(am_instance.GetAttr<float>("Volume"));

            ModHooks.CharmUpdateHook += CharmUpdate;
            ModHooks.GetPlayerIntHook += OverrideFocusCost;
            ModHooks.LanguageGetHook += LanguageHook;
            ModHooks.SoulGainHook += Instance_SoulGainHook;
            ModHooks.OnEnableEnemyHook += Instance_OnEnableEnemyHook;
            ModHooks.SceneChanged += Instance_SceneChanged;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
            On.HeroController.CanDreamNail += HeroController_CanDreamNail;
            On.HeroController.CanFocus += HeroController_CanFocus;
        }

        private void Instance_SceneChanged(string targetScene)
        {
            enemyList.RemoveAll(enemy => enemy == null);
        }

        public List<GameObject> enemyList = new List<GameObject>();

        private bool Instance_OnEnableEnemyHook(GameObject enemy, bool isAlreadyDead)
        {
            HealthManager hm = enemy.GetComponent<HealthManager>();
            if (hm == null) return false;


            if (!isAlreadyDead)
            {
                enemyList.Add(enemy);
                Log("[HOLLOW POINT] adding " + enemy.name + " to the list");
            }

            return false;
        }

        private bool HeroController_CanFocus(On.HeroController.orig_CanFocus orig, HeroController self)
        {
            return false;
            /* commented so vs doesn't yell at me
            if (adrenalineCharges < 1) return false; //If your soul charges are less than 1, you cant heal bud

            return orig(self);*/
        }

        private bool HeroController_CanDreamNail(On.HeroController.orig_CanDreamNail orig, HeroController self)
        {
            if (WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Ranged) return false;

            return orig(self); 
        }

        private int Instance_SoulGainHook(int num)
        {
            IncreaseAdrenalineChargeEnergy();
            return 4;
        }

        private void HeroController_AddGeo(On.HeroController.orig_AddGeo orig, HeroController self, int amount)
        {

            orig(self, amount);
        }

        private bool HeroController_CanNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self)
        {
            if (WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Melee && canUseNailArts) return orig(self);

            return false;
        }

        public string LanguageHook(string key, string sheet, string txt)
        {
            //string txt = Language.Language.GetInternal(key, sheet);
            //Modding.Logger.Log("KEY: " + key + " displays this text: " + txt);

            string nodePath = "/TextChanges/Text[@name=\'" + key + "\']";
            XmlNode newText = LoadAssets.textChanges.SelectSingleNode(nodePath);

            if (newText == null)
            {
                return txt;
            }
            //Modding.Logger.Log("NEW TEXT IS " + newText.InnerText);
            string replace = newText.InnerText.Replace("$", "<br><br>");
            replace = replace.Replace("#", "<page>");
            return replace;
        }

        public void CharmUpdate(PlayerData data, HeroController controller)
        {
            if (furyParticle == null)
            {
                furyParticle = HollowPointPrefabs.SpawnObjectFromDictionary("FuryParticlePrefab", HeroController.instance.transform);
                furyParticle.SetActive(false);
            }

            if (furyBurst == null)
            {
                furyBurst = HollowPointPrefabs.SpawnObjectFromDictionary("FuryBurstPrefab", HeroController.instance.transform);
                furyBurst.SetActive(false);
            }

            Log("Charm Update Called");
            //Initialise stats
            currentEquippedGun = WeaponSwapAndStatHandler.instance.EquipWeapon();

            current_weapon = currentEquippedGun.gunName;
            current_class = currentEquippedGun.gunSubClass;
            current_damagePerShot = currentEquippedGun.damageBase;
            current_damagePerLevel = currentEquippedGun.damageScale;
            current_energyGainOnHit = currentEquippedGun.energyGainOnHit;
            current_bulletLifetime = currentEquippedGun.bulletLifetime;
            current_boostMultiplier = currentEquippedGun.boostMultiplier;
            current_bulletVelocity = currentEquippedGun.bulletVelocity; 
            current_fireRateCooldown = currentEquippedGun.fireRate; 
            current_soulCostPerShot = currentEquippedGun.soulCostPerShot;
            current_heatPerShot = currentEquippedGun.heatPerShot;
            current_soulGainedPerHit = currentEquippedGun.soulGainOnHit;
            current_soulGainedPerKill = currentEquippedGun.soulGainOnHit;
            current_soulRegenSpeed = currentEquippedGun.soulRegenSpeed;
            current_minWeaponSpreadFactor = currentEquippedGun.minWeaponSpreadFactor;
            current_walkSpeed = 3f;

            current_class = WeaponSwapAndStatHandler.instance.ChangeClass(current_class);

            if (pd_instance.equippedCharm_6) //Fury of the Fallen
            {
                current_soulGainedPerKill += 10;
                current_soulRegenSpeed -= 0.10f;
                current_energyGainOnHit *= 2;
            }

            if (pd_instance.equippedCharm_19) //Shaman Stone
            {
                current_fireRateCooldown *= 0.75f;
                current_heatPerShot += 10;
            }

            if (pd_instance.equippedCharm_21) //Soul Eater
            {
                current_soulGainedPerKill += 24;
                current_soulGainedPerHit += 3;
                current_soulRegenSpeed -= 0.015f;
                current_heatPerShot += 15;
                current_minWeaponSpreadFactor += 3;
            }

            if (pd_instance.equippedCharm_25) //Strength
            {
                current_damagePerShot += 3;
                current_damagePerLevel += 2;
                current_fireRateCooldown *= 0.75f;
                current_heatPerShot += 15f;
            }

            Log("currentFireCooldown " + current_fireRateCooldown);

            if (pd_instance.equippedCharm_28) //Shape of Unn
            {
                current_bulletVelocity *= 1.2f;
                current_heatPerShot -= 10;
                current_minWeaponSpreadFactor -= 3;
            }

            if (pd_instance.equippedCharm_27) //Joni
            {
                current_soulRegenSpeed -= 0.033f;
            }

            //Dash Master
            if (pd_instance.equippedCharm_31) current_boostMultiplier *= 1.35f;

            //Spell Twister
            if (pd_instance.equippedCharm_33) current_soulCostPerShot = (int)(current_soulCostPerShot * 0.75f);


            if (pd_instance.equippedCharm_7) current_energyGainOnHit = (int)(current_energyGainOnHit * 1.5f);
            current_energyRequirement = (pd_instance.equippedCharm_34) ? 140 : 100; //Deep Focus
            current_energyPenalty = (pd_instance.equippedCharm_23) ? 0.7f : 0.5f; // Fragile Heart

            if (pd_instance.equippedCharm_24) //Fragile Greed
            {
                current_soulGainedPerKill *= 3;
                current_soulGainedPerHit *= 0;
                current_soulRegenTimer *= 2f;
                current_fireRateCooldown *= 0.80f;
            }

            recentlyKilledTimer = 0;
            hc_instance.NAIL_CHARGE_TIME_DEFAULT = 0.75f;

            //Charge Abilities
            adrenalineCharges = 0;
            adrenalineEnergy = 0 ;
            heal_ChargesDecayTimer = 0;
            adenalineCooldownTimer = 0;
            adrenalineOnCooldown = false;

            fireModeIcon?.Invoke("hudicon_omni.png");
            adrenalineChargeIcons?.Invoke("0");
            nailArtIcon?.Invoke("true");

            //Minimum value setters, NOTE: soul cost doesnt like having it at 1 so i set it up as 2 minimum
            if (current_soulCostPerShot < 2) current_soulCostPerShot = 2;
            if (current_walkSpeed < 1) current_walkSpeed = 1;
            if (current_fireRateCooldown < 0.01f) current_fireRateCooldown = 0.05f;
            if (current_soulRegenSpeed < 0.01f) current_soulRegenSpeed = 0.005f;
            if (current_bulletVelocity > 80) current_bulletVelocity = 80;
            if (current_heatPerShot < 1) current_heatPerShot = 1;
            if (current_minWeaponSpreadFactor < 1) current_minWeaponSpreadFactor = 1;
        }

        private int OverrideFocusCost(string name, int orig)
        {
            if (name == nameof(PlayerData.focusMP_amount))
            {
                return current_soulCostPerShot;
            }
            return orig;
        }

        void Update()
        {
        }

        void FixedUpdate()
        {

            if (fireRateCooldownTimer >= 0)
            {
                fireRateCooldownTimer -= Time.deltaTime * 1f;
            }
            else
            {
                if (!canFire) canFire = true;
                if (usingGunMelee) usingGunMelee = false;
            }

            if (hc_instance.cState.isPaused || hc_instance.cState.transitioning) return;

            //Soul Cartridge Disable Cooldown Time
            if (swapTimer > 0)
            {
                swapTimer -= Time.deltaTime * 30f;
                if(swapTimer <= 0 ) canSwap = true;
            }

            if (hiveBloodHealTimer > 0)
            {
                hiveBloodHealTimer -= Time.deltaTime * 1f;
                if (hiveBloodHealTimer <= 0 && adrenalineCharges == 3)
                {
                    hiveBloodHealTimer = (pd_instance.equippedCharm_34) ? 4f : 7f;
                    HeroController.instance.AddHealth(1);
                }
            }

            if (canUseNailArtsTimer > 0)
            {
                canUseNailArtsTimer -= Time.deltaTime * 1;
                if (canUseNailArtsTimer <= 0)
                {
                    canUseNailArts = true;
                    EnableNailArts();
                }

            }

            //Soul Cartridge Decay
            //if (heal_ChargesDecayTimer > 0) heal_ChargesDecayTimer -= Time.deltaTime * 1f;
            //else if(heal_Charges > 0) ChangeBloodRushCharges(increase: false);
            
            if(recentlyTookDamageTimer > 0) recentlyTookDamageTimer -= Time.deltaTime * 1f;

            //On Kill Bonus
            if (recentlyKilledTimer > 0) recentlyKilledTimer -= Time.deltaTime * 1f;

            //Soul Gain Timer
            if (recentlyFiredTimer >= 0)
            {
                recentlyFiredTimer -= Time.deltaTime * 1;
            }
            else if (passiveSoulTimer > 0)
            {
                passiveSoulTimer -= Time.deltaTime * 1;
                if (passiveSoulTimer <= 0)
                {
                    RegenerateSoul();
                }
            }

            if (infusionTimer > 0 && infusionActivated)
            {
                infusionTimer -= Time.deltaTime * 1;

                if (infusionTimer <= 0)
                {
                    //ActivateInfusionBuff(false);
                }
            }
        }

        public void RegenerateSoul()
        {
            passiveSoulTimer = (current_class == WeaponSubClass.BREACHER && infusionActivated) ? current_soulRegenSpeed - 0.02f : current_soulRegenSpeed;
            HeroController.instance.TryAddMPChargeSpa(1);
        }

        public void IncreaseAdrenalineChargeEnergy()
        {

            int energyIncrease = current_energyGainOnHit; //Alter this value later

            //Basically once the player is in full adrenaline, slow down the adreline increase by a fraction so that they wont heal too fast

            if (adrenalineCharges == 3) energyIncrease = (int)(energyIncrease * current_energyPenalty);
            switch (pd_instance.fireballLevel)
            {
                case 1:
                    energyIncrease = (int)(energyIncrease * 1f);
                    break;
                case 2:
                    energyIncrease = (int)(energyIncrease * 1.25f);
                    break;
                default:
                    energyIncrease = (int)(energyIncrease * 0.60f);
                    break;
            }

            adrenalineEnergy += energyIncrease;

            if (adrenalineEnergy > 100)
            {
                Log("Increasing Charges Adrenaline Energy " + adrenalineEnergy);
                adrenalineEnergy = adrenalineEnergy % 100;
                //Thorns of Agony
                if (PlayerData.instance.equippedCharm_12) GameManager.instance.GetComponent<AttackHandler>().SpawnVoidSpikes();

                GameObject focusBurstAnim = Instantiate(SpellControlOverride.focusBurstAnim, HeroController.instance.transform);
                focusBurstAnim.SetActive(true);
                HeroController.instance.GetComponent<SpriteFlash>().flashWhiteQuick();
                Destroy(focusBurstAnim, 3f);
                if (adrenalineCharges < 3)
                {
                    IncDecAdrenalineCharges(1);
                }
                else if(!pd_instance.equippedCharm_29 || !pd_instance.equippedCharm_27)
                {
                    int healAmount = (pd_instance.equippedCharm_34) ? 2 : 1;
                    HeroController.instance.AddHealth(healAmount);
                }

            }
        }

        //Cartridge Level Details
        /*  -1 = Disabled
         *  0 = Dormant state, but ready to charge
         *  1 = same but now the player has 1 charge  || consuming heals 1 mask
         *  2 = same but now the player has 2 charges || consuming heals 1 mask
         *  3 = same but now the player has 3 charge  || consuming heals 2 mask
         */
        void IncDecAdrenalineCharges(int amount)
        {
            //adrenalineCharges += (increase && adrenalineCharges < 3) ? 1 : (!increase)? -1 : 0;
            adrenalineCharges += amount;
            if (adrenalineCharges > 3) adrenalineCharges = 3;
            else if (adrenalineCharges < 0) adrenalineCharges = 0;

            //Enable/Disable infusion
            if (adrenalineCharges == 3 && !infusionActivated) ActivateInfusionBuff(true);
            else if (adrenalineCharges < 3 && infusionActivated) ActivateInfusionBuff(false);

            if(adrenalineCharges == 3 && pd_instance.equippedCharm_29) hiveBloodHealTimer = (pd_instance.equippedCharm_34) ? 4f: 7f;

            adrenalineChargeIcons?.Invoke(adrenalineCharges.ToString());
        }

        public int ConsumeAdrenalineCharges(bool consumeAll = false, float cooldownOverride = 5f, int consumeAmount = -1)
        {
            //adrenalineEnergy = 0;
            if (consumeAll)
            {
                //adrenalineOnCooldown = true;
                //adenalineCooldownTimer = cooldownOverride;
                int originalAdrenaline = adrenalineCharges;
                IncDecAdrenalineCharges(-3);
                adrenalineChargeIcons?.Invoke(adrenalineCharges.ToString());
                return originalAdrenaline;
            }

            //Log("[Stats] Consuming Cartridge");
            int adrenalineChargesOnConsumption = adrenalineCharges;
            IncDecAdrenalineCharges(consumeAmount);
            adrenalineChargeIcons?.Invoke(adrenalineCharges.ToString());

            return adrenalineChargesOnConsumption;
        }

        public void TakeAdrenalineEnergyDamage(int damageAmount)
        {
            if (recentlyTookDamageTimer > 0) return;
            recentlyTookDamageTimer = 1f;

            int energyLost = 150;
            int totalPlayerEnergy = adrenalineCharges * 100 + adrenalineEnergy;

            if (pd_instance.equippedCharm_8) energyLost -= 75;
            if (pd_instance.equippedCharm_9) energyLost -= 125;
            if (pd_instance.equippedCharm_27) energyLost -= 200;
            if (energyLost <= 0) return;

            int energyRemaining = totalPlayerEnergy - energyLost;
            Log("REMAINING energyRemaining " + energyRemaining);

            adrenalineCharges = (int)(energyRemaining / 100f);
            adrenalineEnergy = energyRemaining % 100;

            if (adrenalineCharges < 0) adrenalineCharges = 0;
            if (adrenalineEnergy < 0) adrenalineEnergy = 0;
            Log("REMAINING adrenalineCharges " + adrenalineCharges);
            Log("REMAINING adrenalineCharges " + adrenalineEnergy);

            adrenalineChargeIcons?.Invoke(adrenalineCharges.ToString());
            //ConsumeAdrenalineCharges(consumeAmount: -totalAdrenalineToConsume);
        }

        void UpdateBloodRushBuffs(float runspeed, float dashcooldown, int soulusage, float timer)
        {
            //Default Dash speeds default_dash_cooldown = 0.6f; default_dash_cooldown_charm = 0.4f; default_dash_speed = 20f; default_dash_speed_sharp = 28f; default_dash_time = 0.25f; default_gravity = 0.79f;
            HeroController.instance.WALK_SPEED = runspeed;
            HeroController.instance.DASH_COOLDOWN = dashcooldown;
        }

        public void DisableNailArts(float time)
        {
            canUseNailArts = false;
            canUseNailArtsTimer = time;
            nailArtIcon?.Invoke("false");
        }

        public void EnableNailArts()
        {
            canUseNailArts = true;
            nailArtIcon?.Invoke("true");
        }

        public int AddSoulOnEnemyKill()
        {
            //prevent soul drain per shot
            recentlyKilledTimer += 0.8f;
            //HeatHandler.currentHeat -= adrenalineRushLevel * 5;

            int mpGainOnKill = current_soulGainedPerKill;
            return mpGainOnKill;
        }

        public int SoulCostPerShot()
        {
            float mpCost = current_soulCostPerShot;
            //mpCost += (HeatHandler.currentHeat / 33f);

            //If the player has recently killed someone, prevent soul drainings
            mpCost *= (recentlyKilledTimer > 0) ? 0.25f : 1;
            return (int) mpCost;
        }

        public static int SoulGainPerHit()
        {
            int soul = instance.current_soulGainedPerHit;//soulGained;
            return soul;
        }

        public void ToggleFireMode()
        {
            AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.FireSelectSFXGO);
            cardinalFiringMode = !cardinalFiringMode;
            string firemodetext = (cardinalFiringMode) ? "hudicon_cardinal.png" : "hudicon_omni.png";
            fireModeIcon?.Invoke(firemodetext);
        }

        //Gun cooldown methods inbetween shots
        public void StartFirerateCooldown(float? cooldownOverride = null)
        {
            float cooldown = current_fireRateCooldown; //set the cooldown depending on the current equipped weapon
            if (cooldownOverride != null) cooldown = (float)cooldownOverride; //used the override cooldown value in the parameter instead

            if (infusionActivated && current_class == WeaponSubClass.OBSERVER)
            {
                cooldown *= 0.70f;
            }

            fireRateCooldownTimer = -1;
            fireRateCooldownTimer = cooldown;
            canFire = false;
            recentlyFiredTimer = 1.5f;
        }

        GameObject extraShield = null;

        public void ActivateInfusionBuff(bool activate)
        {
            if (activate)
            {
                infusionActivated = true;
                //int infusionMultiplier = (pd_instance.equippedCharm_10) ? 6 : 4;
                //infusionTimer = adrenalineCharges * infusionMultiplier;
                furyParticle.SetActive(true);
                furyParticle.GetComponent<ParticleSystem>().Play();
                furyBurst.SetActive(true);
                
                hc_instance.RUN_SPEED_CH = 12f;
                hc_instance.RUN_SPEED_CH_COMBO = 14f;
                hc_instance.SHADOW_DASH_COOLDOWN = (pd_instance.equippedCharm_16)? 1.0f : 1.5f;
            }
            else
            {
                infusionActivated = false;
                furyParticle.SetActive(false);
                furyParticle.GetComponent<ParticleSystem>().Stop();
                furyBurst.SetActive(false);

                hc_instance.RUN_SPEED_CH = 10f; 
                hc_instance.RUN_SPEED_CH_COMBO = 11.5f; 
                hc_instance.SHADOW_DASH_COOLDOWN = 1.5f;
            }
        }

        void OnDestroy()
        {
            ModHooks.CharmUpdateHook -= CharmUpdate;
            ModHooks.GetPlayerIntHook -= OverrideFocusCost;
            ModHooks.LanguageGetHook -= LanguageHook;
            ModHooks.SoulGainHook -= Instance_SoulGainHook;
            On.HeroController.CanNailCharge -= HeroController_CanNailCharge;
            On.HeroController.CanDreamNail -= HeroController_CanDreamNail;
            Destroy(gameObject.GetComponent<Stats>());
            Destroy(this);
            Destroy(instance);
            //you just gotta be sure amirite
        }
        
    }
}
