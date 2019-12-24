using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using ModCommon.Util;
using MonoMod;
using Language;


namespace HollowPoint
{
    class HP_Stats : MonoBehaviour
    {
        public static int soulBurstCost = 12;

        public static float main_cooldown = 5f;
        public static float burst_cooldown = 14f;

        public static float current_main_cooldown = main_cooldown;
        public static float current_burst_cooldown = burst_cooldown;

        public static bool canFireMain = true;
        public static bool canFireBurst = false;


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

            //On.BuildEquippedCharms.BuildCharmList += BuildCharm;

            ModHooks.Instance.CharmUpdateHook += CharmUpdate;
           // ModHooks.Instance.LanguageGetHook += LanguageHook;
        }
     
        public void CharmUpdate(PlayerData data, HeroController controller)
        {
            Modding.Logger.Log("Charm Update Called");
        }

        public string LanguageHook(string key, string sheet)
        {
            string txt = Language.Language.GetInternal(key, sheet);
            Modding.Logger.Log("KEY: " + key + " displays this text: " + txt);

            return txt;
        }

        public void Update()
        {
            if(current_burst_cooldown > 0)
            {
                current_burst_cooldown -= Time.deltaTime * 30f;
                canFireBurst = false;
            }
            else
            {
                canFireBurst= true;
            }

            if (current_main_cooldown > 0)
            {
                current_main_cooldown -= Time.deltaTime * 30f;
                canFireMain = false;
            }
            else
            {
                canFireMain = true;
            }



        }


        //Utility Methods
        public static void StartBothCooldown()
        {
            StartMainCooldown();
            StartBurstCooldown();
        }

        public static void StartMainCooldown()
        {
            current_main_cooldown = main_cooldown;
        }
        
        public static void StartBurstCooldown()
        {
            current_burst_cooldown = burst_cooldown;
        }

    }
}
