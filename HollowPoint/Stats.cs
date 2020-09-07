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
    //I have no idea why generic stacks wont even show up on my Generic classes so wtv. 
    class ShitStack
    {
        List<GameObject> stack;
        int position;

        public ShitStack()
        {
            stack = new List<GameObject>();
            position = -1;
        }

        public void Push(GameObject go)
        {
            stack.Add(go);
            position++;
        }

        public GameObject Pop()
        {
            if (position == -1) return null;
            GameObject poppedGO = stack[position];
            stack.RemoveAt(position);
            position--;
            return poppedGO;
        }

        public GameObject[] PopAll()
        {
            GameObject[] allGO = stack.ToArray();
            stack.Clear();
            position = -1;
            return allGO;
        }
    }

    class Stats : MonoBehaviour
    {
        public static Stats instance = null;

        public static event Action<string> FireModeIcon;
        public static event Action<string> bloodRushIcon;

        const int DEFAULT_SINGLE_COST = 3;
        const int DEFAULT_BURST_COST = 1;
        const float DEFAULT_ATTACK_SPEED = 0.41f;
        const float DEFAULT_ATTACK_SPEED_CH = 0.25f;
        const float DEFAULT_ANIMATION_SPEED = 0.35f;
        const float DEFAULT_ANIMATION_SPEED_CH = 0.28f;

        //static ShitStack extraWeavers = new ShitStack();
        //TODO: clean unused variables
        //Setters for values values
        public float current_bulletLifetime = 0;
        public float current_bulletVelocity = 0;
        public float current_boostMultiplier = 0;
        public int current_damagePerShot = 0;
        public int current_damagePerLevel = 0;
        public float current_fireRateCooldown = 5f;
        public float current_heatPerShot = 0;
        public int current_soulCostPerShot = 1;
        public float current_soulRegenSpeed = 1;
        public float current_soulRegenTimer = 3f;
        public float current_walkSpeed = 3f;
        public int current_soulGainedPerHit = 0;
        public int current_soulGainedPerKill = 0;
        
        public bool canFire = false;
        public bool usingGunMelee = false;
        public bool cardinalFiringMode = false;
        public bool slowWalk = false;
        public bool hasActivatedAdrenaline = false;

        //Update Timers
        public float recentlyFiredTimer = 0;
        public float passiveSoulTimer = 3f;
        private float recentlyKilledTimer;
        public float fireRateCooldownTimer = 5f;

        //Dash float values
        public PlayerData pd_instance;
        public HeroController hc_instance;
        public AudioManager am_instance;

        //Variables for the healing mechanic
        public int heal_Charges;
        int soulC_Energy;
        float heal_ChargesCoolDown;
        float heal_ChargesDecayTimer;
        bool heal_OnCooldown;


        //Infusion Stuff
        GameObject furyParticle = null;
        GameObject furyBurst = null;
        float infusionTimer = 0;
        bool infusionActivated = false;

        public WeaponModifier currentWeapon;

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

            //On.BuildEquippedCharms.BuildCharmList += BuildCharm;

            ModHooks.Instance.CharmUpdateHook += CharmUpdate;
            ModHooks.Instance.LanguageGetHook += LanguageHook;
            ModHooks.Instance.SoulGainHook += Instance_SoulGainHook;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
            On.HeroController.CanDreamNail += HeroController_CanDreamNail;
            On.HeroController.CanFocus += HeroController_CanFocus;
            On.HeroController.CanNailArt += HeroController_CanNailArt;
            //On.HeroController.AddGeo += HeroController_AddGeo;
        }


        private bool HeroController_CanFocus(On.HeroController.orig_CanFocus orig, HeroController self)
        {
            if (heal_Charges < 0) return false; //If your soul charges are less than 1, you cant heal bud

            return orig(self);
        }

        private bool HeroController_CanDreamNail(On.HeroController.orig_CanDreamNail orig, HeroController self)
        {
            if (WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Ranged) return false;

            return orig(self); 
        }

        private int Instance_SoulGainHook(int num)
        {
            return 11;
        }

        private void HeroController_AddGeo(On.HeroController.orig_AddGeo orig, HeroController self, int amount)
        {

            orig(self, amount);
        }

        private bool HeroController_CanNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self)
        {
            if (WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Melee)
                return orig(self);

            return false;
        }

        private bool HeroController_CanNailArt(On.HeroController.orig_CanNailArt orig, HeroController self)
        {
            //throw new NotImplementedException();

            return orig(self);
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
            currentWeapon = WeaponSwapAndStatHandler.instance.weaponModifierDictionary[WeaponModifierName.CARBINE];

            current_damagePerShot = currentWeapon.damageBase;
            current_damagePerLevel = currentWeapon.damageScale;
            current_bulletLifetime = currentWeapon.bulletLifetime;
            current_boostMultiplier = currentWeapon.boostMultiplier;
            current_bulletVelocity = currentWeapon.bulletVelocity; //SMG 24f
            current_fireRateCooldown = currentWeapon.fireRate; //0.06 SMG22f
            current_soulCostPerShot = currentWeapon.soulCostPerShot;
            current_heatPerShot = currentWeapon.heatPerShot;
            current_soulGainedPerHit = currentWeapon.soulGainOnHit;
            current_soulGainedPerKill = currentWeapon.soulGainOnHit;
            current_soulRegenSpeed = currentWeapon.soulRegenSpeed;
            current_walkSpeed = 3f;

            recentlyKilledTimer = 0;
            HeroController.instance.NAIL_CHARGE_TIME_DEFAULT = 0.5f;

            //Charge Abilities
            heal_Charges = 0;
            soulC_Energy = 0 ;
            heal_ChargesDecayTimer = 0;
            heal_ChargesCoolDown = 0;
            heal_OnCooldown = false;
            FireModeIcon?.Invoke("hudicon_omni.png");
            bloodRushIcon?.Invoke("0");

            //Minimum value setters, NOTE: soul cost doesnt like having it at 1 so i set it up as 2 minimum
            if (current_soulCostPerShot < 2) current_soulCostPerShot = 2;
            if (current_walkSpeed < 1) current_walkSpeed = 1;
            if (current_fireRateCooldown < 0.01f) current_fireRateCooldown = 0.01f;
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
            if(heal_ChargesCoolDown > 0) heal_ChargesCoolDown -= Time.deltaTime * 1f;
            else if (heal_OnCooldown)
            {
                ChangeBloodRushCharges(increase: true);
                heal_OnCooldown = false;          
                Log("[Stats] Player is now off cooldown");
            }  

            //Soul Cartridge Decay
            //if (heal_ChargesDecayTimer > 0) heal_ChargesDecayTimer -= Time.deltaTime * 1f;
            //else if(heal_Charges > 0) ChangeBloodRushCharges(increase: false);


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
                    passiveSoulTimer = current_soulRegenSpeed;
                    HeroController.instance.TryAddMPChargeSpa(1);
                }
            }

            if (infusionTimer > 0 && infusionActivated)
            {
                infusionTimer -= Time.deltaTime * 1;

                if (infusionTimer <= 0)
                {
                    ActivateInfusionBuff(false);
                }
            }
        }

        public void IncreaseBloodRushEnergy()
        {
            //If the player is on cooldown, disable soul gain
            if (heal_OnCooldown) return;
            if (heal_Charges == 0) 
            {
                ChangeBloodRushCharges(increase: true);
                soulC_Energy = 0;
                return;
            } 


            int energyIncrease = 8; //Alter this value later
            //Log("Increasing Energy, current is " + soulC_Energy);
            soulC_Energy += energyIncrease;
            if (soulC_Energy > 100)
            {
                ChangeBloodRushCharges(increase: true);
                soulC_Energy = 0;
            }
        }

        //Cartridge Level Details
        /*  -1 = Empty State, has 0 charges, any energy increases automatically transitions to next level
         *  0 = Dormant state, but ready to charge
         *  1 = 0 charges but is currently charging, can still go back to 0
         *  2 = same but now the player has 1 charge  || consuming heals 1 mask
         *  3 = same but now the player has 2 charges || consuming heals 1 mask
         *  4 = same but now the player has 3 charge  || consuming heals 3 mask
         */
        void ChangeBloodRushCharges(bool increase)
        {
            heal_Charges += (increase && heal_Charges < 4) ? 1 : (!increase)? -1 : 0;
            //if(heal_Charges > 0) heal_ChargesDecayTimer = 10;

            //Log("[Stats] Changing Soul Cartridge, INCREASING? " + increase + "   CURRENT LEVEL? " + soulC_Charges);

            //Update the UI
            bloodRushIcon?.Invoke(heal_Charges.ToString());
        }

        public void ConsumeBloodRushCharges(bool consumeAll = true)
        {
            //Log("[Stats] Consuming Cartridge");
            if(!consumeAll)
            {
                ChangeBloodRushCharges(false);
                bloodRushIcon?.Invoke(heal_Charges.ToString());
                return;
            }

            heal_Charges = -1;
            heal_OnCooldown = true;
            heal_ChargesCoolDown = 10f;
            bloodRushIcon?.Invoke(heal_Charges.ToString());
        }


        //TODO: can actually just merge this with ChangeAdrenaline
        public void Stats_TakeDamageEvent()
        {
            ConsumeBloodRushCharges(true);
        }

        void UpdateBloodRushBuffs(float runspeed, float dashcooldown, int soulusage, float timer)
        {
            //Default Dash speeds default_dash_cooldown = 0.6f; default_dash_cooldown_charm = 0.4f; default_dash_speed = 20f; default_dash_speed_sharp = 28f; default_dash_time = 0.25f; default_gravity = 0.79f;
            HeroController.instance.WALK_SPEED = runspeed;
            HeroController.instance.DASH_COOLDOWN = dashcooldown;
        }

        public int Stats_EnemyKilled()
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
            cardinalFiringMode = !cardinalFiringMode;
            string firemodetext = (cardinalFiringMode) ? "hudicon_cardinal.png" : "hudicon_omni.png";
            FireModeIcon?.Invoke(firemodetext);
        }

        //Gun cooldown methods inbetween shots
        public void StartBothCooldown()
        {
            fireRateCooldownTimer = -1;
            fireRateCooldownTimer = current_fireRateCooldown;
            canFire = false;
            recentlyFiredTimer = 1.5f;
        }

        public void StartBothCooldown(float overrideCooldown)
        {
            fireRateCooldownTimer = -1;
            fireRateCooldownTimer = overrideCooldown; 
            canFire = false;
            recentlyFiredTimer = 1.5f;
        }

        public void ActivateInfusionBuff(bool activate)
        {
            if (activate)
            {
                infusionActivated = true;
                infusionTimer = 10;
                furyParticle.SetActive(true);
                furyParticle.GetComponent<ParticleSystem>().Play();
                furyBurst.SetActive(true);

                GameObject artChargeFlash = Instantiate(HeroController.instance.artChargedFlash, HeroController.instance.transform);
                artChargeFlash.SetActive(true);
                Destroy(artChargeFlash, 0.5f);
                GameObject dJumpFlash = Instantiate(HeroController.instance.dJumpFlashPrefab, HeroController.instance.transform);
                dJumpFlash.SetActive(true);
                Destroy(dJumpFlash, 0.5f);

                Instantiate(SpellControlOverride.sharpFlash, HeroController.instance.transform).SetActive(true);
                //Instantiate(SpellControlOverride.focusBurstAnim, HeroController.instance.transform).SetActive(true);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.InfusionSFXGO, alteredPitch: false);
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
