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
        public static int fireSoulCost = 1;
        public static int burstSoulCost = 15;

        const int DEFAULT_SINGLE_COST = 3;
        const int DEFAULT_BURST_COST = 1;

        const float DEFAULT_ATTACK_SPEED = 0.41f;
        const float DEFAULT_ATTACK_SPEED_CH = 0.25f;

        const float DEFAULT_ANIMATION_SPEED = 0.35f;
        const float DEFAULT_ANIMATION_SPEED_CH = 0.28f;
        float soulRegenTimer = 3f;
        float max_soul_regen = 33;
        float passiveSoulTimer = 3f;

        public static float walkSpeed = 3f;
        public static float fireRateCooldown = 3.75f;
        public static float fireRateCooldownTimer = fireRateCooldown;

        public static float bulletRange = 0;
        public static float heatPerShot = 0;
        public static float bulletVelocity = 0;

        public static bool canFire = false;
        static float recentlyFiredTimer = 60f;

        int soulConsumed = 0;

        public static bool hasActivatedAdrenaline = false;

        int totalGeo = 0;

        public static int artifactPower;
        public static int grenadeAmnt = 0;

        public static string soundName = "";
        public static string spriteName = "";

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

        private float FocusCost()
        {
            //return (float)PlayerData.instance.GetInt("MPCharge") / 35.0f;
            recentlyFiredTimer = 60;

            if (PlayerData.instance.equippedCharm_27)
            {
                soulConsumed += 1;

                if (soulConsumed % 66 == 0)
                {
                    if (grenadeAmnt < 10) grenadeAmnt += 1;
                }
            }

            if (hasActivatedAdrenaline && PlayerData.instance.equippedCharm_23) return 3f;

            return 1f;
        }

        public void CharmUpdate(PlayerData data, HeroController controller)
        {
            Modding.Logger.Log("Charm Update Called");
            HP_AttackHandler.artifactActive = false;
            spriteName = "";

            //Initialise stats
            artifactPower = 1;
            bulletRange = .16f + (PlayerData.instance.nailSmithUpgrades * 0.015f);
            bulletVelocity = 35f;
            burstSoulCost = DEFAULT_BURST_COST;
            fireRateCooldown = 3.75f;
            fireSoulCost = 3;
            grenadeAmnt = 2 + (int)(Math.Floor((float)(PlayerData.instance.nailSmithUpgrades + 1) / 2));
            heatPerShot = 0.5f;
            max_soul_regen = 20;
            soulConsumed = 0;
            soulRegenTimer = 2.75f;
            walkSpeed = 5.5f;

            //Charm 3 Grubsong
            soulRegenTimer = (PlayerData.instance.equippedCharm_3) ? 1.25f : 2.75f;

            //Charm 6 Fury of the Fallen
            if (PlayerData.instance.equippedCharm_6)
            {
                walkSpeed += 2f;
                soulRegenTimer -= 1f;
                fireRateCooldown -= 1f;
            }

            //Charm 9 Lifeblood Core
            artifactPower += (PlayerData.instance.equippedCharm_9)? 2 : 0;

            //Charm 11 Flukenest, add additional soul cost
            if (PlayerData.instance.equippedCharm_11)
            {
                heatPerShot += 3f;
                fireSoulCost += 7;
                fireRateCooldown += 7.5f;
                bulletRange += -0.050f;
            }

            //Charm 13 Mark of Pride, increase range, increases heat, increases soul cost, decrease firing speed (SNIPER MODULE)
            if (PlayerData.instance.equippedCharm_13)
            {
                bulletRange += 0.5f;
                bulletVelocity += 15f;
                heatPerShot += 0.5f;
                fireSoulCost += 5;
                fireRateCooldown += 3.75f;
                walkSpeed += -1f;
            }

            //Charm 14 Steady Body 
            //walkSpeed = (PlayerData.instance.equippedCharm_14) ? (walkSpeed) : walkSpeed;

            //Charm 16 Sharp Shadow and Fury of the fallen sprite changes
            spriteName = (PlayerData.instance.equippedCharm_16) ? "shadebullet.png" : spriteName;
            spriteName = (PlayerData.instance.equippedCharm_6) ? "furybullet.png" : spriteName;

            //Charm 18 Long Nail
            bulletVelocity += (PlayerData.instance.equippedCharm_18) ? 20f : 0;

            //Charm 19 Shaman Stone
            grenadeAmnt += (PlayerData.instance.equippedCharm_19) ? (PlayerData.instance.nailSmithUpgrades + 2) : 0;

            //Charm 21 Soul Eater
            if (PlayerData.instance.equippedCharm_21)
            {
                fireSoulCost -= 4;
                max_soul_regen += 10;
            }

            //Charm 23 Fragile/Unbrekable Heart
            hasActivatedAdrenaline = (PlayerData.instance.equippedCharm_23) ? false : true;

            //Charm 25 Fragile Strength
            heatPerShot += (PlayerData.instance.equippedCharm_25) ? 0.25f : 0;

            //Charm 32 Quick Slash, increase firerate, decrease heat, 
            if (PlayerData.instance.equippedCharm_32)
            {
                heatPerShot -= 0.3f;
                fireSoulCost += 1;
                fireRateCooldown -= 2.5f;
                walkSpeed += -2.25f;
            }

            //Charm 37 Sprint
            walkSpeed += 1.75f;

            //Charm 37 Sprintmaster 

            //Minimum value setters, NOTE: soul cost doesnt like having it at 1 so i set it up as 2 minimum
            fireSoulCost = (fireSoulCost < 2) ? 2 : fireSoulCost;
            walkSpeed = (walkSpeed < 1) ? 1 : walkSpeed;
            fireRateCooldown = (fireRateCooldown < 0.3f)? 0.3f: fireRateCooldown;

            HP_UIHandler.UpdateDisplay();
        }



        void Update()
        {
            if(HP_SpellControl.buffActive && PlayerData.instance.equippedCharm_35)
            {
                HeroController.instance.SetAttr<bool>("doubleJumped", false);
            }

            if (HP_SpellControl.buffActive && PlayerData.instance.equippedCharm_4)
            {
                //HeroController.instance.cState.invulnerable = true;
            }

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

            if (fireRateCooldownTimer > 0)
            {
                fireRateCooldownTimer -= Time.deltaTime * 30f;
                canFire = false;
            }
            else
            {
                canFire= true;
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

        public static void ReduceGrenades()
        {
            if (PlayerData.instance.equippedCharm_33)
            {
                int chance = UnityEngine.Random.Range(0, 3);
                Modding.Logger.Log("chance " + chance);
                if (chance != 0)
                {
                    grenadeAmnt -= 0;
                    return;
                }
            }
            grenadeAmnt -= 1;
        }

        //Utility Methods
        public static void StartBothCooldown()
        {
            fireRateCooldownTimer = fireRateCooldown;
            //fireRateCooldownTimer = 0.1f;
            canFire = false;
            recentlyFiredTimer = 60;
        }

        void OnDestroy()
        {
            ModHooks.Instance.CharmUpdateHook -= CharmUpdate;
            ModHooks.Instance.FocusCostHook -= FocusCost;
            ModHooks.Instance.LanguageGetHook -= LanguageHook;
            ModHooks.Instance.SoulGainHook -= Instance_SoulGainHook;
            ModHooks.Instance.BlueHealthHook -= Instance_BlueHealthHook;
            On.HeroController.CanNailCharge -= HeroController_CanNailCharge;
            Destroy(gameObject.GetComponent<HP_Stats>());
        }
        
    }
}
