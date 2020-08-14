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

        //static ShitStack extraWeavers = new ShitStack();

        public static int soulCostPerShot = 1;
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
        public static float fireRateCooldown = 5f;
        public static float fireRateCooldownTimer = 5.75f;

        public static float bulletRange = 0;
        public static float heatPerShot = 0;
        public static float bulletVelocity = 0;

        public static bool canFire = false;
        public static bool usingGunMelee = false;
        public static bool cardinalFiringMode = false;
        static float recentlyFiredTimer = 60f;

        public static int soulGained = 0;

        public static bool hasActivatedAdrenaline = false;

        //Adrenaline Rush Vars
        private static int adrenalineRushLevel;
        private static int adrenalineRushPoints;
        private static float adrenalineRushTimer;
        private static float adrenalineFreezeTimer;
        public static bool canGainAdrenaline;

        private static float recentlyKilledTimer;

        int totalGeo = 0;

        //Dash float values
        public static int currentPrimaryAmmo;

        public static string soundName = "";
        public static string bulletSprite = "";

        public PlayerData pd_instance;
        public HeroController hc_instance;
        public AudioManager am_instance;

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
            ModHooks.Instance.FocusCostHook += FocusCost;
            ModHooks.Instance.LanguageGetHook += LanguageHook;
            ModHooks.Instance.SoulGainHook += Instance_SoulGainHook;
            ModHooks.Instance.BlueHealthHook += Instance_BlueHealthHook;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
            On.HeroController.CanDreamNail += HeroController_CanDreamNail;
            //On.HeroController.AddGeo += HeroController_AddGeo;
        }

        private bool HeroController_CanDreamNail(On.HeroController.orig_CanDreamNail orig, HeroController self)
        {
            if (WeaponSwapHandler.currentWeapon == WeaponType.Ranged) return false;

            return orig(self); 
        }

        private int Instance_SoulGainHook(int num)
        {
            return 2;
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
            if (WeaponSwapHandler.currentWeapon == WeaponType.Melee)
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
            if (hasActivatedAdrenaline && PlayerData.instance.equippedCharm_23) return 3f;

            return 1f;
        }

        public void CharmUpdate(PlayerData data, HeroController controller)
        {
            Log("Charm Update Called");
            AttackHandler.airStrikeActive = false;
            bulletSprite = "";

            //Initialise stats
            currentPrimaryAmmo = 10;
            bulletRange = .20f + (PlayerData.instance.nailSmithUpgrades * 0.02f);
            bulletVelocity = 35f;
            burstSoulCost = 1;
            fireRateCooldown = 5.5f; 
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
            bulletSprite = (PlayerData.instance.equippedCharm_16) ? "shadebullet.png" : bulletSprite;
            bulletSprite = (PlayerData.instance.equippedCharm_6) ? "furybullet.png" : bulletSprite;

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
            SetAdrenalineLevel(0, false);
            adrenalineFreezeTimer = 0;
            canGainAdrenaline = true;
            recentlyKilledTimer = 0;
            //Adrenaline
            //adrenalineRushLevel = 0;
            //adrenalineRushPoints = 0;
            //adrenalineRushTimer = 0;
            HeroController.instance.NAIL_CHARGE_TIME_DEFAULT = 3f;
        }



        void Update()
        {
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

            if(recentlyKilledTimer >= 0)
            {
                recentlyKilledTimer -= Time.deltaTime * 1f;
            }

            if(adrenalineFreezeTimer > 0 && !canGainAdrenaline)
            {
                adrenalineFreezeTimer -= Time.deltaTime * 1f;
                if(adrenalineFreezeTimer <= 0)
                {
                    canGainAdrenaline = true;
                }
            }

            if(adrenalineRushTimer < 0 && adrenalineRushLevel != 0)
            {
                Log("[Stats] Adrenaline DECREASED to " + (adrenalineRushLevel-1)); 
                SetAdrenalineLevel(--adrenalineRushLevel, true, increased: false);
                adrenalineRushPoints = (int)(adrenalineRushPoints / 2f);
                //Level down adrenaline
            }
            else if(adrenalineRushTimer > 0)
            {
                adrenalineRushTimer -= Time.deltaTime * 1f;
            }

            //actually put this on the weapon handler so its not called 24/7
            if (WeaponSwapHandler.currentWeapon == WeaponType.Ranged) // && !HP_HeatHandler.overheat
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

            //Soul Gain Timer
            if (recentlyFiredTimer >= 0)
            {
                recentlyFiredTimer -= Time.deltaTime * 30f;
            }
            else if (passiveSoulTimer > 0)
            {
                passiveSoulTimer -= Time.deltaTime * 30f;
            }
        }

        public static void IncreaseAdrenalinePoints(int points)
        {
            int[] adrenalineLevelRequirement = {0, 15, 25, 35, 50, 75 };
            adrenalineRushPoints += points;

            if (adrenalineRushPoints > adrenalineLevelRequirement[adrenalineRushLevel])
            {
                adrenalineRushPoints = 0;

                SetAdrenalineLevel(++adrenalineRushLevel, false);
                Log("[Stats] Adrenaline INCREASED to " + (adrenalineRushLevel));
            }

        }

        private static void SetAdrenalineLevel(int adrenalineLevel, bool lowerAdrenalineTimer, bool increased = true)
        {
            if (increased)
            {

            }

            AdrenalineIcon?.Invoke(adrenalineLevel.ToString());
            float runspeed = 2.5f;
            float dashcooldown = 0.6f;
            int soulcost = 0;
            float timer = -1;

            switch (adrenalineLevel)
            {
                case 1:
                    runspeed = 2.5f;
                    dashcooldown = 0.55f;
                    soulcost = 0;
                    timer = 12;
                    break;
                case 2:
                    runspeed = 3f;
                    dashcooldown = 0.45f;
                    soulcost = 0;
                    timer = 10;
                    break;
                case 3:
                    runspeed = 4f;
                    dashcooldown = 0.32f;
                    soulcost = 0;
                    timer = 8;
                    break;
                case 4:
                    runspeed = 5f;
                    dashcooldown = 0.27f;
                    soulcost = 0;
                    timer = 6;
                    break;
                case 5:
                    runspeed = 6f;
                    dashcooldown = 0.22f;
                    soulcost = 0;
                    timer = 4;
                    break;
                default:
                    runspeed = 2.5f;
                    dashcooldown = 0.6f;
                    soulcost = 0;
                    timer = -1;
                    break;
            }
            timer = lowerAdrenalineTimer ? (timer/3): timer;

            UpdateAdrenalineStats(runspeed, dashcooldown, soulcost, timer);
        }

        public static void ExtendAdrenalineTime(float time)
        {
            float[] extensionLimitByLevel = {-1, 10, 8, 6, 5, 4};

            if(adrenalineRushLevel >= 1)
            {
                adrenalineRushTimer += time;
                adrenalineRushTimer = Mathf.Clamp(adrenalineRushTimer, 0, extensionLimitByLevel[adrenalineRushLevel]);
            }

        }

        //TODO: can actually just merge this with ChangeAdrenaline
        public static void Stats_TakeDamageEvent()
        {
            SetAdrenalineLevel(0, false, increased: false);
            adrenalineFreezeTimer = 5f;
            canGainAdrenaline = false;
        }

        static void UpdateAdrenalineStats(float runspeed, float dashcooldown, int soulusage, float timer)
        {
            //Default Dash speeds default_dash_cooldown = 0.6f; default_dash_cooldown_charm = 0.4f; default_dash_speed = 20f; default_dash_speed_sharp = 28f; default_dash_time = 0.25f; default_gravity = 0.79f;
            HeroController.instance.WALK_SPEED = runspeed;
            HeroController.instance.DASH_COOLDOWN = dashcooldown;

            adrenalineRushTimer = timer;
        }

        public static int MPChargeOnKill()
        {

            //prevent soul drain per shot
            recentlyKilledTimer = 3f;
            HeatHandler.currentHeat -= adrenalineRushLevel * 5;

            int mpGainOnKill = 2;
            //mpGainOnKill += (adrenalineRushLevel-2 > 0)? 1 : adrenalineRushLevel - 2;
            mpGainOnKill += adrenalineRushLevel;

            return mpGainOnKill;
        }

        public static int MPCostOnShot()
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

        public static void ToggleFireMode()
        {
            cardinalFiringMode = !cardinalFiringMode;
            string firemodetext = (cardinalFiringMode) ? "hudicon_cardinal.png" : "hudicon_omni.png";
            FireModeIcon?.Invoke(firemodetext);
        }

        //Gun cooldown methods inbetween shots
        public static void StartBothCooldown()
        {
            fireRateCooldownTimer = -1;
            fireRateCooldownTimer = fireRateCooldown;
            canFire = false;
            recentlyFiredTimer = 60;
        }

        public static void StartBothCooldown(float overrideCooldown)
        {
            fireRateCooldownTimer = -1;
            fireRateCooldownTimer = overrideCooldown; 
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
            Destroy(gameObject.GetComponent<Stats>());
        }
        
    }
}
