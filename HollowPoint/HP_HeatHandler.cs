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
            currentMultiplier = 1;

            if (cooldownPause > 0)
            {
                cooldownPause -= Time.deltaTime;
                return;
            }


            if(currentHeat > 0) currentHeat -= Time.deltaTime * 50;

            if (currentHeat < 0)
            {
                currentHeat = 0;
                overheat = false;
            }
        }

        public static void IncreaseHeat()
        {
            currentHeat += HP_WeaponHandler.currentGun.gunHeatGain;
            cooldownPause = 0.60f;

            if (currentHeat > MAX_HEAT)
            {
                overheat = true;
                cooldownPause = 1f;
            }
        }

    }
}
