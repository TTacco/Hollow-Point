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
        public int soulCostPerShot = 1;
        public float soulRegenTimer = 3f;
        public float max_soul_regen = 33;
        public float passiveSoulTimer = 5f;
        public float walkSpeed = 3f;
        public float fireRateCooldown = 5f;
        public float fireRateCooldownTimer = 5f;
        public float bulletLifetime = 0;
        public float heatPerShot = 0;
        public float bulletVelocity = 0;
        public bool canFire = false;
        public bool usingGunMelee = false;
        public bool cardinalFiringMode = false;
        public bool slowWalk = false;
        public int soulGained = 0;
        public bool hasActivatedAdrenaline = false;

        public float recentlyFiredTimer = 0;
        private float recentlyKilledTimer;
        int totalGeo = 0;

        //Dash float values
        public PlayerData pd_instance;
        public HeroController hc_instance;
        public AudioManager am_instance;

        //New Soul Cartridge System
        int heal_Charges;
        int soulC_Energy;
        float heal_ChargesCoolDown;
        float heal_ChargesDecayTimer;
        bool heal_OnCooldown;



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

        //On.BuildEquippedCharms.BuildCharmList += BuildCharm;

            ModHooks.Instance.CharmUpdateHook += CharmUpdate;
            ModHooks.Instance.LanguageGetHook += LanguageHook;
            ModHooks.Instance.SoulGainHook += Instance_SoulGainHook;
            ModHooks.Instance.BlueHealthHook += Instance_BlueHealthHook;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
            On.HeroController.CanDreamNail += HeroController_CanDreamNail;
            On.HeroController.CanFocus += HeroController_CanFocus;
            //On.HeroController.AddGeo += HeroController_AddGeo;
        }

        private bool HeroController_CanFocus(On.HeroController.orig_CanFocus orig, HeroController self)
        {
            if (heal_Charges < 0) return false; //If your soul charges are less than 1, you cant heal bud

            return orig(self);
        }

        private bool HeroController_CanDreamNail(On.HeroController.orig_CanDreamNail orig, HeroController self)
        {
            if (WeaponSwapHandler.instance.currentWeapon == WeaponType.Ranged) return false;

            return orig(self); 
        }

        private int Instance_SoulGainHook(int num)
        {
            return 10;
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
            bulletLifetime = 0.26f;//SMG 0.29f
            bulletVelocity = 25f; //SMG 24f
            fireRateCooldown = 0.08f; //0.06 SMG22f
            soulCostPerShot = 8;
            heatPerShot = 10f;
            soulGained = 2;
            soulRegenTimer = 2.75f;
            walkSpeed = 3f;
                 
            //Minimum value setters, NOTE: soul cost doesnt like having it at 1 so i set it up as 2 minimum
            soulCostPerShot = (soulCostPerShot < 2) ? 2 : soulCostPerShot;
            walkSpeed = (walkSpeed < 1) ? 1 : walkSpeed;
            fireRateCooldown = (fireRateCooldown < 0.01f)? 0.01f: fireRateCooldown;

            FireModeIcon?.Invoke("hudicon_omni.png");
            //AdrenalineIcon?.Invoke("0");
            recentlyKilledTimer = 0;
            HeroController.instance.NAIL_CHARGE_TIME_DEFAULT = 0.5f;

            soulRegenTimer = 0.1f;
            recentlyFiredTimer = 0.1f;

            //Cartridge
            heal_Charges = 0;
            soulC_Energy = 0 ;
            heal_ChargesDecayTimer = 0;
            heal_ChargesCoolDown = 0;
            heal_OnCooldown = false;
            bloodRushIcon?.Invoke("0");

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
                    passiveSoulTimer = 0.06f;
                    HeroController.instance.TryAddMPChargeSpa(1);
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

        public void ExtendCartridgeDecayTime(bool enemyKilled)
        {
            //Log("[Stats] Extending Decay Time");
            heal_ChargesDecayTimer += (enemyKilled) ? 4: 2;
            heal_ChargesDecayTimer = (heal_ChargesDecayTimer > 10) ? 10 : heal_ChargesDecayTimer;
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

            int mpGainOnKill = 10;
            //mpGainOnKill += (adrenalineRushLevel-2 > 0)? 1 : adrenalineRushLevel - 2;
            return mpGainOnKill;
        }

        public int SoulCostPerShot()
        {
            float mpCost = soulCostPerShot;
            //mpCost += (HeatHandler.currentHeat / 33f);

            //If the player has recently killed someone, prevent soul drainings
            mpCost *= (recentlyKilledTimer > 0) ? 0.25f : 1;
            return (int) mpCost;
        }

        public static int DamageInflictedByShot(Vector3 bulletOriginPosition, Vector3 enemyPosition, BulletBehaviour hpbb)
        {
            int dam = 3 + (PlayerData.instance.nailSmithUpgrades * 2);
            return dam;
        }

        public static int SoulGainPerHit()
        {
            int soul = 2;//soulGained;
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
            recentlyFiredTimer = 1.5f;
        }

        public void StartBothCooldown(float overrideCooldown)
        {
            fireRateCooldownTimer = -1;
            fireRateCooldownTimer = overrideCooldown; 
            canFire = false;
            recentlyFiredTimer = 1.5f;
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
