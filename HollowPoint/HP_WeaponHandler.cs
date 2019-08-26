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
            if (HP_HeatHandler.overheat) return;

            if ((InputHandler.Instance.inputActions.down.WasPressed))
            {
                tapDown++;
            }
            if ((InputHandler.Instance.inputActions.up.WasPressed))
            {
                tapUp++;
            }

            if ((tapDown == 1 || tapUp == 1) && !swapWeaponStart)
            {
                swapWeaponTimer = 0.20f;
                swapWeaponStart = true;
            }
            else if (swapWeaponStart)
            {
                swapWeaponTimer -= Time.deltaTime;

                if (swapWeaponTimer < 0)
                {
                    swapWeaponStart = false;
                    tapDown = 0;
                    tapUp = 0;
                }
            }

            //Cycle the weapon
            if (tapDown >= 2)
            {
                tapDown = 0;
                tapUp = 0;
                swapWeaponTimer = 0;
                swapWeaponStart = false;
                weaponIndex++;
                CheckIndexBound();
                HP_WeaponHandler.currentGun = HP_WeaponHandler.allGuns[weaponIndex];
                HP_GunSpriteRenderer.SwapWeapon(HP_WeaponHandler.currentGun.spriteName);
            }
            if (tapUp >= 2)
            { 
                tapDown = 0;
                tapUp = 0;
                swapWeaponTimer = 0;
                swapWeaponStart = false;
                weaponIndex--;
                CheckIndexBound();
                HP_WeaponHandler.currentGun = HP_WeaponHandler.allGuns[weaponIndex];
                HP_GunSpriteRenderer.SwapWeapon(HP_WeaponHandler.currentGun.spriteName);
            }
        }

        public static void ForceLowPowerMode()
        {
            HP_WeaponHandler.currentGun = HP_WeaponHandler.allGuns[1];
            HP_GunSpriteRenderer.SwapWeapon(HP_WeaponHandler.currentGun.spriteName);
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

            allGuns = new HP_Gun[6];

            allGuns[0] = new HP_Gun("Nail", 4, 9999, 9999, 0, "Nail", 2, 10, 1, 0.40f, 0, false, "Old Nail");
            allGuns[1] = new HP_Gun("Submachinegun", 2, 9999, 9999, 0, "Weapon_RifleSprite.png", 1, 33, 40, 0.40f, 0.15f, false, "Low Power");
            allGuns[2] = new HP_Gun("Submachinegun", 5, 9999, 9999, 10, "Weapon_RifleSprite.png", 1, 33, 40, 0.80f, 0.15f, false, "Regular Charge");
            allGuns[3] = new HP_Gun("Shotgun", 3, 24, 24, 30, "Weapon_ShotgunSprite.png", 9, 22, 20, 0.40f, 0.60f, false, "Scattershot");
            allGuns[4] = new HP_Gun("Rifle", 5, 9999, 9999, 20, "Weapon_RifleSprite.png", 4, 40, 60, 0.90f, 0.42f, false, "Burst Fire");
            allGuns[5] = new HP_Gun("Sniper", 35, 15, 15, 35, "Weapon_RifleSprite.png", 0, 70, 150, 1.3f, 0.50f, true, "Overcharge");
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

            if (HP_WeaponHandler.currentGun.gunName.Contains("Submachinegun"))
            {
                return 1;
            }

            if (HeroController.instance.hero_state == GlobalEnums.ActorStates.airborne)
            {
                return 6;
            }

            if (HeroController.instance.hero_state == GlobalEnums.ActorStates.running)
            {
                return 3;
            }

            if (HeroController.instance.hero_state == GlobalEnums.ActorStates.wall_sliding)
            {
                return 2;
            }

            return 0;
        }
    }
}
