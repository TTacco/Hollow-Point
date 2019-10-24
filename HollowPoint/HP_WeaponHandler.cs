using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HollowPoint
{

    //===========================================================
    //Weapon Swap
    //===========================================================
    class HP_WeaponSwapHandler : MonoBehaviour
    {
        int tapDown;
        int tapUp;
        int weaponIndex;
        float swapWeaponTimer = 0;
        bool swapWeaponStart = false;

        public void Update()
        {
        }

        public void CheckIndexBound()
        {
            if (weaponIndex > HP_WeaponHandler.allGuns.Length - 1)
            {
                weaponIndex = 0;
            }
            else if(weaponIndex < 0)
            {
                weaponIndex = HP_WeaponHandler.allGuns.Length - 1;
            }
        }


    }


    //===========================================================
    //Weapon Initializer
    //===========================================================
    class HP_WeaponHandler : MonoBehaviour
    {
        public static HP_Gun currentGun;
        public static HP_Gun[] allGuns; 

        public void Awake()
        {
            StartCoroutine(InitRoutine());
        }

        public IEnumerator InitRoutine()
        {
            //Initialize all the ammunitions for each gun
            while (HeroController.instance == null)
            {
                yield return null;
            }

            allGuns = new HP_Gun[2];

            allGuns[0] = new HP_Gun("Nail", 4, 9999, 9999, 0, "Nail", 2, 10, 1, 0.40f, 0, false, "Old Nail");
            allGuns[1] = new HP_Gun("Rifle", 5, 9999, 9999, 20, "Weapon_RifleSprite.png", 4, 40, 60, 0.90f, 0.42f, false, "Primary Fire");
            //Add an LMG and a flamethrower later

            currentGun = allGuns[0];
        }
    }

    //===========================================================
    //Gun Struct
    //===========================================================
    struct HP_Gun
    {
        public String gunName;
        public int gunDamage;
        public int gunAmmo;
        public int gunAmmo_Max;
        public int gunHeatGain;
        public String spriteName;
        public float gunDeviation;
        public float gunBulletSpeed;
        public float gunDamMultiplier;
        public float gunBulletSize;
        public float gunCooldown;
        public bool gunIgnoresInvuln;
        public String flavorName;

        public HP_Gun(string gunName, int gunDamage, int gunAmmo, int gunAmmo_Max, int gunHeatGain, string spriteName, 
            float gunDeviation, float gunBulletSpeed, float gunDamMultiplier, float gunBulletSize, float gunCooldown, bool gunIgnoresInvuln, String flavorName)
        {
            this.gunName = gunName;
            this.gunDamage = gunDamage;
            this.gunAmmo = gunAmmo;
            this.gunAmmo_Max = gunAmmo_Max;
            this.gunHeatGain = gunHeatGain;
            this.spriteName = spriteName;
            this.gunDeviation = gunDeviation;
            this.gunBulletSpeed = gunBulletSpeed;
            this.gunDamMultiplier = gunDamMultiplier;
            this.gunBulletSize = gunBulletSize;
            this.gunCooldown = gunCooldown;
            this.gunIgnoresInvuln = gunIgnoresInvuln;
            this.flavorName = flavorName;
        }
    }

    //===========================================================
    //Static Utilities
    //===========================================================

    public class SpreadDeviationControl
    {
        public static int ExtraDeviation()
        {
            

            if (HeroController.instance.hero_state == GlobalEnums.ActorStates.airborne)
            {
                return 7;
            }

            if (HeroController.instance.hero_state == GlobalEnums.ActorStates.running)
            {
                return 2;
            }

            if (HeroController.instance.hero_state == GlobalEnums.ActorStates.wall_sliding)
            {
                return 5;
            }

            return 1;
        }
    }
}
