using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using On;
using UnityEngine;
using GlobalEnums;
using static HollowPoint.HollowPointEnums;

namespace HollowPoint
{
    //===========================================================
    //Weapon Swap
    //===========================================================
    public class WeaponSwapHandler : MonoBehaviour
    {
        public static WeaponSwapHandler instance = null;

        public WeaponType currentWeapon = WeaponType.Melee;
        public GunType currentGun = GunType.Primary;

        public Dictionary<string, PrimaryModifiers> primaryModifierDictionary = new Dictionary<string, PrimaryModifiers>();

        const float DEFAULT_ATTACK_SPEED = 0.41f;
        const float DEFAULT_ATTACK_SPEED_CH = 0.25f;
        const float DEFAULT_ANIMATION_SPEED = 0.35f;
        const float DEFAULT_ANIMATION_SPEED_CH = 0.28f;

        void Awake()
        {
            if(instance == null) instance = this;
            //DontDestroyOnLoad(this);

            StartCoroutine(InitWeaponSwapHandler());

            CreateGuns();
        }

        public IEnumerator InitWeaponSwapHandler()
        {
            //Initialize all the ammunitions for each gun
            while (HeroController.instance == null)
            {
                yield return null;
            }
            
            currentWeapon = WeaponType.Melee;
            currentGun = GunType.Primary;
        }

        public void Update()
        {
            return;
        }

        //Swap inbetween primary and secondary guns
        public void SwapBetweenGun()
        {

            GunType prevGun = currentGun;
            currentGun = (currentGun == GunType.Primary) ? GunType.Secondary : GunType.Primary;

            Modding.Logger.Log(String.Format("Changing guns from {0} to {1}", prevGun, currentGun));

        }

        //Swap between guns or nail
        public void SwapBetweenNail()
        {
            WeaponType prevWep = currentWeapon;
            currentWeapon = (currentWeapon == WeaponType.Melee) ? WeaponType.Ranged : WeaponType.Melee;

            Modding.Logger.Log(String.Format("Changing weapons from {0} to {1}", prevWep, currentWeapon));

            if (currentWeapon == WeaponType.Ranged)
            {
                HeroController.instance.ATTACK_DURATION = 0.0f;
                HeroController.instance.ATTACK_DURATION_CH = 0f;

                HeroController.instance.ATTACK_COOLDOWN_TIME = 1000f;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = 1000f;
            }
            else
            {
                HeroController.instance.ATTACK_COOLDOWN_TIME = DEFAULT_ANIMATION_SPEED;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = DEFAULT_ANIMATION_SPEED_CH;

                HeroController.instance.ATTACK_DURATION = DEFAULT_ATTACK_SPEED;
                HeroController.instance.ATTACK_DURATION_CH = DEFAULT_ATTACK_SPEED_CH;
            }


        }

        public static float ExtraCooldown()
        {
            ActorStates hstate = HeroController.instance.hero_state;

            switch (hstate)
            {
                case ActorStates.airborne:
                    return 1f;

                case ActorStates.running:
                    return 0.50f;

                case ActorStates.wall_sliding:
                    return 1; 

                default:
                    return 0;
            }
        }


        void CreateGuns()
        {
            Modding.Logger.Log("[WeaponHandler] Creating Gun Structs");

            primaryModifierDictionary.Add("Sniper", new PrimaryModifiers
            {
                modifierName = "Rhagio",
                soulCost = 10,
                soulGainOnHit = 4,
                soulGainOnKill = 8,
                damageBase = 6,
                damageScale = 4,
                fireRate = 0.18f,
                velocity = 60f,
                heatGain = 20,
                liftMultiplier = 5,
                bulletSize = new Vector3(1.1f, 1.1f, 0)
            });



        }

        void OnDestroy()
        {
            Destroy(gameObject.GetComponent<WeaponSwapHandler>());
        }
    }




    //===========================================================
    //Gun Struct
    //===========================================================
    public struct PrimaryModifiers
    {
        public string modifierName;
        public int soulCost;
        public int soulGainOnHit;
        public int soulGainOnKill;
        public int damageBase;
        public int damageScale;
        public float fireRate;
        public float velocity;
        public float heatGain;
        public float liftMultiplier;
        public Vector3 bulletSize;
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
                return 0;
            }

            if (HeroController.instance.hero_state == GlobalEnums.ActorStates.running)
            {
                return 0;
            }

            if (HeroController.instance.hero_state == GlobalEnums.ActorStates.wall_sliding)
            {
                return 0;
            }

            return 1;
        }
    }
}
