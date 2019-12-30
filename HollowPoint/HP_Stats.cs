using System;
using System.Collections;
using UnityEngine;
using Modding;
using ModCommon.Util;
using MonoMod;
using Language;
using System.Xml;


namespace HollowPoint
{
    class HP_Stats : MonoBehaviour
    {
        public static int soulSingleCost = 3;
        public static int soulBurstCost = 15;

        const int DEFAULT_SINGLE_COST = 3;
        const int DEFAULT_BURST_COST = 15;

        const float DEFAULT_ATTACK_SPEED = 0.41f;
        const float DEFAULT_ATTACK_SPEED_CH = 0.25f;

        const float DEFAULT_ANIMATION_SPEED = 0.35f;
        const float DEFAULT_ANIMATION_SPEED_CH = 0.28f;
        float soulRegenTimer = 3f;
        float max_soul_regen = 33;
        float passiveSoulTimer = 3f;

        public static float DEFAULT_SINGLEFIRE_COOLDOWN = 3f;
        public static float DEFAULT_BURSTFIRE_COOLDOWN = 14f;

        public static float singlefire_cooldown = DEFAULT_SINGLEFIRE_COOLDOWN;
        public static float burstfire_cooldown = DEFAULT_BURSTFIRE_COOLDOWN;

        public static bool canFireSingle = true;
        public static bool canFireBurst = false;
        static float recentlyFiredTimer = 60f;

        public static bool hasActivatedAdrenaline = false;

        int totalGeo = 0;

        public static int fireSupportAmnt = 0;
        public static int grenadeAmnt = 0;

        public PlayerData pd_instance;
        public HeroController hc_instance;

        public void Awake()
        {
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

            //On.BuildEquippedCharms.BuildCharmList += BuildCharm;

            ModHooks.Instance.CharmUpdateHook += CharmUpdate;
            ModHooks.Instance.FocusCostHook += FocusCost;
            ModHooks.Instance.LanguageGetHook += LanguageHook;
            ModHooks.Instance.SoulGainHook += Instance_SoulGainHook;
            ModHooks.Instance.BlueHealthHook += Instance_BlueHealthHook;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
            //On.HeroController.AddGeo += HeroController_AddGeo;
        }

        private int Instance_SoulGainHook(int num)
        {
            return 6;
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
            if (HP_WeaponHandler.currentGun.gunName == "Nail")
                return orig(self);

            return false;
        }

        private float FocusCost()
        {
            return (float)PlayerData.instance.GetInt("MPCharge") / 35.0f;
        }

        public void CharmUpdate(PlayerData data, HeroController controller)
        {
            Modding.Logger.Log("Charm Update Called");
            grenadeAmnt = 3;
            fireSupportAmnt = 1;

            //Charm 3 Grubsong
            soulRegenTimer = (PlayerData.instance.equippedCharm_3) ? 2 : 8;

            //Charm 9 Lifeblood Heart
            max_soul_regen = (PlayerData.instance.equippedCharm_8)? 33 : 25;
            
            //Charm 9 Lifeblood Core
            soulSingleCost = (PlayerData.instance.equippedCharm_9)? 0 : DEFAULT_SINGLE_COST;

            //Charm 23 Fragile/Unbrekable Heart
            hasActivatedAdrenaline = (PlayerData.instance.equippedCharm_23) ? false : true;

            //Charm 33 Spell Twister
            grenadeAmnt += (PlayerData.instance.equippedCharm_33) ? 2 : 0;
            //fireSupportAmnt += (PlayerData.instance.equippedCharm_33) ? 1 : 0;

            HP_UIHandler.UpdateDisplay();
        }

        public string LanguageHook(string key, string sheet)
        {
            string txt = Language.Language.GetInternal(key, sheet);
            //Modding.Logger.Log("KEY: " + key + " displays this text: " + txt);

            string nodePath = "/TextChanges/Text[@name=\'" + key + "\']"; 
            XmlNode newText = LoadAssets.textChanges.SelectSingleNode(nodePath);

            if(newText == null)
            {
                return txt;
            }
            Modding.Logger.Log("NEW TEXT IS " + newText.InnerText);
            return newText.InnerText;
        }

        void Update()
        {
            //actually put this on the weapon handler so its not called 24/7
            if (HP_WeaponHandler.currentGun.gunName != "Nail") // && !HP_HeatHandler.overheat
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

            if (burstfire_cooldown > 0)
            {
                burstfire_cooldown -= Time.deltaTime * 30f;
                canFireBurst = false;
            }
            else
            {
                canFireBurst= true;
            }

            if (singlefire_cooldown > 0)
            {                
                singlefire_cooldown -= Time.deltaTime * 30f;
                canFireSingle = false;
            }
            else
            {
                canFireSingle = true;
            }

            //Soul Gain Timer
            if (recentlyFiredTimer >= 0)
            {
                recentlyFiredTimer -= Time.deltaTime * 30f;
            }
            else if (passiveSoulTimer > 0)
            {
                passiveSoulTimer -= Time.deltaTime * 30f;
            }
            else if(pd_instance.MPCharge < max_soul_regen)
            {
                passiveSoulTimer = soulRegenTimer;
                HeroController.instance.AddMPCharge(1);
            }
        }


        //Utility Methods
        public static void StartBothCooldown()
        {
            StartMainCooldown();
            StartBurstCooldown();
            recentlyFiredTimer = 60;
        }

        public static void StartMainCooldown()
        {
            singlefire_cooldown = DEFAULT_SINGLEFIRE_COOLDOWN;
            canFireSingle = false;
        }
        
        public static void StartBurstCooldown()
        {
            burstfire_cooldown = DEFAULT_BURSTFIRE_COOLDOWN;
            canFireBurst = false;
        }

    }
}
