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
        public static event Action<string> AdrenalineIcon;

        const int DEFAULT_SINGLE_COST = 3;
        const int DEFAULT_BURST_COST = 1;
        const float DEFAULT_ATTACK_SPEED = 0.41f;
        const float DEFAULT_ATTACK_SPEED_CH = 0.25f;
        const float DEFAULT_ANIMATION_SPEED = 0.35f;
        const float DEFAULT_ANIMATION_SPEED_CH = 0.28f;

        //static ShitStack extraWeavers = new ShitStack();
        public int soulCostPerShot = 1;
        public int burstSoulCost = 15;
        public float soulRegenTimer = 3f;
        public float max_soul_regen = 33;
        public float passiveSoulTimer = 3f;
        public float walkSpeed = 3f;
        public float fireRateCooldown = 5f;
        public float fireRateCooldownTimer = 5.75f;
        public float bulletRange = 0;
        public float heatPerShot = 0;
        public float bulletVelocity = 0;
        public bool canFire = false;
        public bool usingGunMelee = false;
        public bool cardinalFiringMode = false;
        public bool slowWalk = false;
        static float recentlyFiredTimer = 60f;
        public int soulGained = 0;
        public bool hasActivatedAdrenaline = false;
        //Adrenaline Rush Vars
        private int adrenalineRushLevel;
        private int adrenalineRushPoints;
        private float adrenalineRushTimer;
        private float adrenalineFreezeTimer;
        public bool canGainAdrenaline;
        private float recentlyKilledTimer;
        int totalGeo = 0;
        //Dash float values
        public static int currentPrimaryAmmo;
        public PlayerData pd_instance;
        public HeroController hc_instance;
        public AudioManager am_instance;

        //New Soul Cartridge System
        int soulC_Charges;
        int soulC_Energy;
        float soulC_CooldownTimer;
        float soulC_DecayTimer;
        bool soulC_OnCooldown;

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

            Log("Default Dash Cooldown " + hc_instance.DASH_COOLDOWN);
            Log("Default Dash Cooldown Charm " + hc_instance.DASH_COOLDOWN_CH);
            Log("Default Dash Speed " + hc_instance.DASH_SPEED);
            Log("Default Dash Speed Sharp " + hc_instance.DASH_SPEED_SHARP);
            Log("Default Dash Time " + hc_instance.DASH_TIME);
            Log("Default Dash Gravity " + hc_instance.DEFAULT_GRAVITY);
            //Log(am_instance.GetAttr<float>("Volume"));

            adrenalineRushLevel = 0;
            adrenalineRushPoints = 0;
            adrenalineRushTimer = 0;

        //On.BuildEquippedCharms.BuildCharmList += BuildCharm;

            ModHooks.Instance.CharmUpdateHook += CharmUpdate;
            ModHooks.Instance.LanguageGetHook += LanguageHook;
            ModHooks.Instance.SoulGainHook += Instance_SoulGainHook;
            ModHooks.Instance.BlueHealthHook += Instance_BlueHealthHook;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
            On.HeroController.CanDreamNail += HeroController_CanDreamNail;
            //On.HeroController.AddGeo += HeroController_AddGeo;
        }

        private bool HeroController_CanDreamNail(On.HeroController.orig_CanDreamNail orig, HeroController self)
        {
            if (WeaponSwapHandler.instance.currentWeapon == WeaponType.Ranged) return false;

            return orig(self); 
        }

        private int Instance_SoulGainHook(int num)
        {
            return 5;
        }

        private void HeroController_AddGeo(On.HeroController.orig_AddGeo orig, HeroController self, int amount)
        {
            totalGeo += amount;

            if(totalGeo >= 50)
            {
                HeroController.instance.AddMPChargeSpa(15);
                totalGeo = 0;
            }

            orig(self, amount);
        }

        private int Instance_BlueHealthHook()
        {

            return 0;
        }

        private bool HeroController_CanNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self)
        {
            if (WeaponSwapHandler.instance.currentWeapon == WeaponType.Melee)
                return orig(self);

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
            Log("Charm Update Called");
            //Initialise stats
            currentPrimaryAmmo = 10;
            bulletRange = .20f + (PlayerData.instance.nailSmithUpgrades * 0.02f);
            bulletVelocity = 35f;
            burstSoulCost = 1;
            fireRateCooldown = 3.5f; 
            soulCostPerShot = 3;
            heatPerShot = 0.7f;
            max_soul_regen = 25;
            soulGained = 2;
            soulRegenTimer = 2.75f;
            walkSpeed = 3f;

            //Charm 3 Grubsong
            soulRegenTimer = (PlayerData.instance.equippedCharm_3) ? 1.25f : 2.75f;

            //Charm 6 Fury of the Fallen
            if (PlayerData.instance.equippedCharm_6)
            {
                walkSpeed += 2f;
                soulRegenTimer -= 1f;
                fireRateCooldown -= 0.4f;
            }

            //Charm 8 Lifeblood Heart
            currentPrimaryAmmo += (PlayerData.instance.equippedCharm_8)? 2 : 0;

            //Charm 11 Flukenest, add additional soul cost
            if (PlayerData.instance.equippedCharm_11)
            {
                heatPerShot += 3f;
                soulCostPerShot += 7;
                fireRateCooldown += 6.5f;
                bulletRange += -0.025f;
            }

            //Charm 13 Mark of Pride, increase range, increases heat, increases soul cost, decrease firing speed (SNIPER MODULE)
            if (PlayerData.instance.equippedCharm_13)
            {
                bulletRange += 0.5f;
                bulletVelocity += 15f;
                heatPerShot -= 0.55f;
                soulCostPerShot += 5;
                fireRateCooldown += 3.75f;
                walkSpeed += -1f;
            }
            //yeet

            //Charm 14 Steady Body 
            //walkSpeed = (PlayerData.instance.equippedCharm_14) ? (walkSpeed) : walkSpeed;

            //Charm 16 Sharp Shadow and Fury of the fallen sprite changes
            //bulletSprite = (PlayerData.instance.equippedCharm_16) ? "shadebullet.png" : bulletSprite;
            //bulletSprite = (PlayerData.instance.equippedCharm_6) ? "furybullet.png" : bulletSprite;

            //Charm 18 Long Nail
            bulletVelocity += (PlayerData.instance.equippedCharm_18) ? 20f : 0;

            //soulGained += (PlayerData.instance.equippedCharm_20) ? 1 : 0;

            //Charm 21 Soul Eater
            if (PlayerData.instance.equippedCharm_21)
            {
                soulCostPerShot -= 2;
                max_soul_regen += 10;
                soulGained += 2;
            }

            //Charm 23 Fragile/Unbrekable Heart
            hasActivatedAdrenaline = (PlayerData.instance.equippedCharm_23) ? false : true;

            //Charm 25 Fragile Strength
            heatPerShot += (PlayerData.instance.equippedCharm_25) ? 0.25f : 0;

            //Charm 32 Quick Slash, increase firerate, decrease heat, 
            if (PlayerData.instance.equippedCharm_32)
            {
                heatPerShot += 0.3f;
                soulCostPerShot += 1;
                fireRateCooldown -= 2.5f;
                walkSpeed += -2.25f;
            }

            //Charm 37 Sprint
            walkSpeed += 1.75f;

            //Charm 37 Sprintmaster 

            //Minimum value setters, NOTE: soul cost doesnt like having it at 1 so i set it up as 2 minimum
            soulCostPerShot = (soulCostPerShot < 2) ? 2 : soulCostPerShot;
            walkSpeed = (walkSpeed < 1) ? 1 : walkSpeed;
            fireRateCooldown = (fireRateCooldown < 1f)? 1f: fireRateCooldown;

            FireModeIcon?.Invoke("hudicon_omni.png");
            //AdrenalineIcon?.Invoke("0");
            adrenalineFreezeTimer = 0;
            canGainAdrenaline = true;
            recentlyKilledTimer = 0;
            //Adrenaline
            //adrenalineRushLevel = 0;
            //adrenalineRushPoints = 0;
            //adrenalineRushTimer = 0;
            HeroController.instance.NAIL_CHARGE_TIME_DEFAULT = 3f;


            //Cartridge
            soulC_Charges = 0;
            soulC_Energy = 0 ;
            soulC_DecayTimer = 0;
            soulC_CooldownTimer = 0;
            soulC_OnCooldown = false;
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
                fireRateCooldownTimer -= Time.deltaTime * 30f;
                //canFire = false;
            }
            else
            { 
                if(!canFire) canFire = true;

                if (usingGunMelee) usingGunMelee = false;
            }

            return;
            //actually put this on the weapon handler so its not called 24/7
            if (WeaponSwapHandler.instance.currentWeapon == WeaponType.Ranged) // && !HP_HeatHandler.overheat
            {
                hc_instance.ATTACK_DURATION = 0.0f;
                hc_instance.ATTACK_DURATION_CH = 0f;

                hc_instance.ATTACK_COOLDOWN_TIME = 500f;
                hc_instance.ATTACK_COOLDOWN_TIME_CH = 500f;
            }
            else
            {
                hc_instance.ATTACK_COOLDOWN_TIME = DEFAULT_ANIMATION_SPEED;
                hc_instance.ATTACK_COOLDOWN_TIME_CH = DEFAULT_ANIMATION_SPEED_CH;

                hc_instance.ATTACK_DURATION = DEFAULT_ATTACK_SPEED;
                hc_instance.ATTACK_DURATION_CH = DEFAULT_ATTACK_SPEED_CH;
            }       
        }

        void FixedUpdate()
        {
            if (hc_instance.cState.isPaused) return;

            //Soul Cartridge Disable Cooldown Time
            if(soulC_CooldownTimer > 0) soulC_CooldownTimer -= Time.deltaTime * 1f;
            else if (soulC_OnCooldown)
            {
                ChangeCartridgeCharges(increase: true);
                soulC_OnCooldown = false;          
                Log("[Stats] Player is now off cooldown");
            }  

            //Soul Cartridge Decay
            if (soulC_DecayTimer > 0) soulC_DecayTimer -= Time.deltaTime * 1f;
            else if(soulC_Charges > 0) ChangeCartridgeCharges(increase: false);

            //Soul Gain Timer
            if (recentlyFiredTimer >= 0) recentlyFiredTimer -= Time.deltaTime * 30f;
            else if (passiveSoulTimer > 0) passiveSoulTimer -= Time.deltaTime * 30f;

        }

        public void IncreaseCartridgeEnergy()
        {
            //If the player is on cooldown, disable soul gain
            if (soulC_OnCooldown) return;
            if (soulC_Charges == 0) 
            {
                ChangeCartridgeCharges(increase: true);
                soulC_Energy = 0;
            } 


            int energyIncrease = 33; //Alter this value later
            soulC_Energy += energyIncrease;
            if (soulC_Energy > 100)
            {
                ChangeCartridgeCharges(increase: true);
                soulC_Energy = 0;
            }
        }

        //Cartridge Level Details
        /*  -1 = Empty State, has 0 charges, any energy increases automatically transitions to next level
         *  0 = Currently in base charging state, timer is started, if timer runs out revert back 1 level
         *  1 = same but now the player has 1 charge  || consuming heals 1 mask
         *  2 = same but now the player has 2 charges || consuming heals 1 mask
         *  3 = same but now the player has 3 charge  || consuming heals 3 mask
         */
        void ChangeCartridgeCharges(bool increase)
        {
            soulC_Charges += (increase && soulC_Charges < 3) ? 1 : (!increase)? -1 : 0;
            if(soulC_Charges > 0) soulC_DecayTimer = 10;

            Log("[Stats] Changing Soul Cartridge, INCREASING? " + increase + "   CURRENT LEVEL? " + soulC_Charges);

            //Update the UI
            AdrenalineIcon?.Invoke(soulC_Charges.ToString());
        }

        void ExtendCartridgeDecayTime(bool enemyKilled)
        {
            Log("[Stats] Extending Decay Time");
            soulC_DecayTimer += (enemyKilled) ? 3: 1;
            soulC_DecayTimer = (soulC_DecayTimer > 10) ? 10 : soulC_DecayTimer;
        }

        void ConsumeCartridge()
        {
            Log("[Stats] Consuming Cartridge");
            soulC_Charges = 0;
            soulC_OnCooldown = true;
            soulC_CooldownTimer = 5f;
            AdrenalineIcon?.Invoke(soulC_Charges.ToString());
        }

        //TODO: can actually just merge this with ChangeAdrenaline
        public void Stats_TakeDamageEvent()
        {
            ConsumeCartridge();
        }

        void UpdateAdrenalineStats(float runspeed, float dashcooldown, int soulusage, float timer)
        {
            //Default Dash speeds default_dash_cooldown = 0.6f; default_dash_cooldown_charm = 0.4f; default_dash_speed = 20f; default_dash_speed_sharp = 28f; default_dash_time = 0.25f; default_gravity = 0.79f;
            HeroController.instance.WALK_SPEED = runspeed;
            HeroController.instance.DASH_COOLDOWN = dashcooldown;

            adrenalineRushTimer = timer;
        }

        public int MPChargeOnKill()
        {
            //prevent soul drain per shot
            recentlyKilledTimer = 3f;
            HeatHandler.currentHeat -= adrenalineRushLevel * 5;

            int mpGainOnKill = 15;
            //mpGainOnKill += (adrenalineRushLevel-2 > 0)? 1 : adrenalineRushLevel - 2;
            mpGainOnKill += adrenalineRushLevel;

            return mpGainOnKill;
        }

        public int MPCostOnShot()
        {
            int mpCost = 2;
            mpCost += (int)(HeatHandler.currentHeat / 33f);

            //If the player has recently killed someone, prevent soul draining
            mpCost *= (recentlyKilledTimer > 0) ? 0 : 1;
            return mpCost;
        }

        public static int CalculateDamage(Vector3 bulletOriginPosition, Vector3 enemyPosition, BulletBehaviour hpbb)
        {
            int dam = 3 + (PlayerData.instance.nailSmithUpgrades * 2);
            return dam;
        }

        public static int CalculateSoulGain()
        {
            int soul = 4;//soulGained;
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
            fireRateCooldownTimer = fireRateCooldown;
            canFire = false;
            recentlyFiredTimer = 60;
        }

        public void StartBothCooldown(float overrideCooldown)
        {
            fireRateCooldownTimer = -1;
            fireRateCooldownTimer = overrideCooldown; 
            canFire = false;
            recentlyFiredTimer = 60;
        }

        void OnDestroy()
        {
            ModHooks.Instance.CharmUpdateHook -= CharmUpdate;
            ModHooks.Instance.LanguageGetHook -= LanguageHook;
            ModHooks.Instance.SoulGainHook -= Instance_SoulGainHook;
            ModHooks.Instance.BlueHealthHook -= Instance_BlueHealthHook;
            On.HeroController.CanNailCharge -= HeroController_CanNailCharge;
            On.HeroController.CanDreamNail -= HeroController_CanDreamNail;
            Destroy(gameObject.GetComponent<Stats>());
            Destroy(this);
            Destroy(instance);
            //you just gotta be sure amirite
        }
        
    }
}
