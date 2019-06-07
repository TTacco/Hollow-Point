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
                swapWeaponTimer = 0.4f;
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
            }
        }

        public void CheckIndexBound()
        {
            if (weaponIndex > 5)
            {
                weaponIndex = 0;
            }
            else if(weaponIndex < 0)
            {
                weaponIndex = 5;
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

            allGuns[0] = new HP_Gun("Nail", 4, 9999, 9999, 5, "NoSprite", 2, 40, 1, false);
            allGuns[1] = new HP_Gun("Rifle", 4, 9999, 9999, 5, "AssaultRifleAlter", 3, 40, 1, false);
            allGuns[2] = new HP_Gun("Shotgun", 4, 9999, 9999, 5, "AssaultRifleAlter", 7, 40, 1, false);
            allGuns[3] = new HP_Gun("MachineGun", 4, 9999, 9999, 5, "AssaultRifleAlter", 6, 40, 1, false);
            allGuns[4] = new HP_Gun("Sniper", 4, 9999, 9999, 5, "AssaultRifleAlter", 3, 40, 1, false);
            allGuns[5] = new HP_Gun("Rocket", 4, 9999, 9999, 5, "AssaultRifleAlter", 3, 40, 1, false);

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
        public bool gunIgnoresInvuln;

        public HP_Gun(string gunName, int gunDamage, int gunAmmo, int gunAmmo_Max, int gunHeatGain, string spriteName, float gunDeviation, float gunBulletSpeed, float gunDamMultiplier, bool gunIgnoresInvuln)
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
            this.gunIgnoresInvuln = gunIgnoresInvuln;
        }
    }
}
