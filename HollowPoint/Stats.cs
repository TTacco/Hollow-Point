using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Random;
using static Modding.Logger;
using Modding;
using ModCommon.Util;
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

        public bool canUseNailArts = false;
        public bool canFire = false;
        public bool usingGunMelee = false;
        public bool cardinalFiringMode = false;
        public bool slowWalk = false;
        public bool hasActivatedAdrenaline = false;

        //Update Timers
        public float canUseNailArtsTimer = 1f;
        public float recentlyFiredTimer = 0;
        public float passiveSoulTimer = 3f;
        private float recentlyKilledTimer;
        public float fireRateCooldownTimer = 5f;
        float recentlyTookDamageTimer = 0f;

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

            ModHooks.Instance.CharmUpdateHook += CharmUpdate;
            ModHooks.Instance.LanguageGetHook += LanguageHook;
            ModHooks.Instance.SoulGainHook += Instance_SoulGainHook;
            ModHooks.Instance.OnEnableEnemyHook += Instance_OnEnableEnemyHook;
            ModHooks.Instance.SceneChanged += Instance_SceneChanged;
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

            if (adrenalineCharges < 1) return false; //If your soul charges are less than 1, you cant heal bud

            return orig(self);
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

        public string LanguageHook(string key, string sheet)
        {
            string txt = Language.Language.GetInternal(key, sheet);
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

            if (pd_instance.equippedCharm_8) //Fury of the Fallen
            {
                current_soulGainedPerKill += 10;
                current_soulGainedPerHit += 2;
                current_damagePerLevel += 2;
                current_soulRegenSpeed -= 0.05f;
            }

            if (pd_instance.equippedCharm_16) //Shaman Stone
            {
                current_damagePerShot -= 1;
                current_damagePerLevel -= 1;
            }

            if (pd_instance.equippedCharm_19) //Shaman Stone
            {
                current_fireRateCooldown *= 0.80f;
                current_heatPerShot += 10;
            }

            if (pd_instance.equippedCharm_21) //Soul Eater
            {
                current_soulGainedPerKill += 24;
                current_soulGainedPerHit += 3;
                current_soulRegenSpeed -= 0.08f;
                current_heatPerShot += 15;
                current_minWeaponSpreadFactor += 3;
            }

            if (pd_instance.equippedCharm_31) //Dash Master
            {
                current_boostMultiplier *= 1.35f;
            }

            if (pd_instance.equippedCharm_33) //Spell Twister
            {
                current_soulCostPerShot = (int)(current_soulCostPerShot * 0.75f);
            }

            if (pd_instance.equippedCharm_34) //Deep Focus
            {
                current_energyGainOnHit += 3;
            }

            if (pd_instance.equippedCharm_26) //Fragile Greed
            {
                current_soulGainedPerKill *= 2;
                current_soulGainedPerHit *= 0;
                current_soulRegenTimer *= 2f;
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

            PlayerData.instance.focusMP_amount = current_soulCostPerShot;

            //Minimum value setters, NOTE: soul cost doesnt like having it at 1 so i set it up as 2 minimum
            if (current_soulCostPerShot < 2) current_soulCostPerShot = 2;
            if (current_walkSpeed < 1) current_walkSpeed = 1;
            if (current_fireRateCooldown < 0.01f) current_fireRateCooldown = 0.01f;
            if (current_soulRegenSpeed < 0.01f) current_fireRateCooldown = 0.01f;
        }



        void Update()
        {
            if (slowWalk)
            {
                //h_state.inWalkZone = true;
            }
            else
            {
                //h_state.inWalkZone = false;
            }

            if (fireRateCooldownTimer >= 0)
            {
                fireRateCooldownTimer -= Time.deltaTime * 1f;
                //canFire = false;
            }
            else
            { 
                if(!canFire) canFire = true;

                if (usingGunMelee) usingGunMelee = false;
            }
        }

        void FixedUpdate()
        {
            if (hc_instance.cState.isPaused || hc_instance.cState.transitioning) return;

            //Soul Cartridge Disable Cooldown Time
            if(adenalineCooldownTimer > 0) adenalineCooldownTimer -= Time.deltaTime * 1f;
            else if (adrenalineOnCooldown)
            {
                IncDecAdrenalineCharges(1);
                adrenalineOnCooldown = false;          
                Log("[Stats] Player is now off cooldown");
            }

            if (swapTimer > 0)
            {
                swapTimer -= Time.deltaTime * 30f;
                if(swapTimer <= 0 ) canSwap = true;
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
            //If the player is on cooldown, disable soul gain
            if (adrenalineOnCooldown) return;
            int energyIncrease = current_energyGainOnHit; //Alter this value later

            //Basically once the player is in full adrenaline, slow down the adreline increase by a fraction so that they wont heal too fast
            float energyIncreaseBuffer = (pd_instance.equippedCharm_23) ? 0.7f : 0.5f;

            if (adrenalineCharges == 3) energyIncrease = (int)(energyIncrease * energyIncreaseBuffer);
            adrenalineEnergy += energyIncrease;

            if (adrenalineEnergy > 100)
            {
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
                else
                {
                    HeroController.instance.AddHealth(1);
                }

                adrenalineEnergy = 0;
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

            adrenalineChargeIcons?.Invoke(adrenalineCharges.ToString());
        }

        public int ConsumeAdrenalineCharges(bool consumeAll = false, float cooldownOverride = 15f, int consumeAmount = -1)
        {
            adrenalineEnergy = 0;
            if (consumeAll)
            {
                adrenalineOnCooldown = true;
                adenalineCooldownTimer = cooldownOverride;
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


        //TODO: can actually just merge this with ChangeAdrenaline
        public void Stats_TakeDamageEvent()
        {
            if (recentlyTookDamageTimer > 0) return;
            recentlyTookDamageTimer = 0.30f;

            ConsumeAdrenalineCharges(consumeAmount: -2);
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
                switch (currentEquippedGun.gunName)
                {
                    case WeaponModifierName.DMR:
                        cooldown = 0.18f;
                        break;
                    case WeaponModifierName.SNIPER:
                        cooldown = 0.45f;
                        break;
                    default:
                        cooldown *= 0.70f;
                        break;
                }
            }

            fireRateCooldownTimer = -1;
            fireRateCooldownTimer = cooldown;
            canFire = false;
            recentlyFiredTimer = 1.5f;
        }

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

                /*
                GameObject artChargeFlash = Instantiate(HeroController.instance.artChargedFlash, HeroController.instance.transform);
                artChargeFlash.SetActive(true);
                Destroy(artChargeFlash, 0.5f);
                GameObject dJumpFlash = Instantiate(HeroController.instance.dJumpFlashPrefab, HeroController.instance.transform);
                dJumpFlash.SetActive(true);
                Destroy(dJumpFlash, 0.5f);
                */

                //Instantiate(SpellControlOverride.sharpFlash, HeroController.instance.transform).SetActive(true);
                //Instantiate(SpellControlOverride.focusBurstAnim, HeroController.instance.transform).SetActive(true);
                //GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                //AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.InfusionSFXGO, alteredPitch: false);


            }
            else
            {
                infusionActivated = false;
                furyParticle.SetActive(false);
                furyParticle.GetComponent<ParticleSystem>().Stop();
                furyBurst.SetActive(false);

            }
        }

        void OnDestroy()
        {
            ModHooks.Instance.CharmUpdateHook -= CharmUpdate;
            ModHooks.Instance.LanguageGetHook -= LanguageHook;
            ModHooks.Instance.SoulGainHook -= Instance_SoulGainHook;
            On.HeroController.CanNailCharge -= HeroController_CanNailCharge;
            On.HeroController.CanDreamNail -= HeroController_CanDreamNail;
            Destroy(gameObject.GetComponent<Stats>());
            Destroy(this);
            Destroy(instance);
            //you just gotta be sure amirite
        }
        
    }
}
