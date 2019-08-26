using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

namespace HollowPoint
{
    class HP_HeatHandler : MonoBehaviour
    {
        public static float currentHeat;
        public static float currentMultiplier;
        public static float cooldownMultiplier; 
        public static float cooldownPause; //This is so whenever the player fires, theres a short pause before the heat goes down
        public const int MAX_HEAT = 100;
        public static bool overheat = false;

        public void Start()
        {
            StartCoroutine(InitializeHeatMeter());
        }

        IEnumerator InitializeHeatMeter()
        {
            while (HeroController.instance == null || PlayerData.instance == null)
            {
                currentMultiplier = 1;
                yield return null;
            }
        }

        public void FixedUpdate()
        {
            //Multiplyer
            if (cooldownMultiplier > 0)
            {
                cooldownMultiplier -= Time.deltaTime;

            }

            if (currentMultiplier > 1 && cooldownMultiplier < 0)
            {
                cooldownMultiplier = 0.20f;
                currentMultiplier -= 0.1f;
            }

            //Heat
            if (cooldownPause > 0)
            {
                cooldownPause -= Time.deltaTime;
                return;
            }

            if (currentHeat > 0) currentHeat -= Time.deltaTime * 40;

            if (currentHeat < 0)
            {
                currentHeat = 0;
                overheat = false;
            }

        }

        public static void IncreaseHeat()
        {
            currentHeat += HP_WeaponHandler.currentGun.gunHeatGain;
            cooldownPause = (HP_WeaponHandler.currentGun.flavorName.Equals("Low Power")) ? 0 : 0.60f;

            if (currentHeat > MAX_HEAT && !overheat)
            {
                overheat = true;
                cooldownPause = 1f;
                HP_WeaponSwapHandler.ForceLowPowerMode();
            }
        }

        public static void IncreaseMultiplier(float multIncrease)
        {
            cooldownMultiplier = 0.40f;
            currentMultiplier += multIncrease;

            float multMax = (PlayerData.instance.nailSmithUpgrades * 0.5f) + 2;
            if (currentMultiplier > multMax)
            {
                currentMultiplier = multMax;
            }
        }

    }
}
