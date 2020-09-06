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
    public class WeaponSwapAndStatHandler : MonoBehaviour
    {
        public static WeaponSwapAndStatHandler instance = null;

        public WeaponType currentWeapon = WeaponType.Melee;
        public GunType currentGun = GunType.Primary;

        public Dictionary<WeaponModifierName, WeaponModifier> weaponModifierDictionary = new Dictionary<WeaponModifierName, WeaponModifier>();

        const float DEFAULT_ATTACK_SPEED = 0.41f;
        const float DEFAULT_ATTACK_SPEED_CH = 0.25f;
        const float DEFAULT_ANIMATION_SPEED = 0.35f;
        const float DEFAULT_ANIMATION_SPEED_CH = 0.28f;

        void Awake()
        {
            if(instance == null) instance = this;
            //DontDestroyOnLoad(this);

            StartCoroutine(InitWeaponSwapHandler());
        }

        public IEnumerator InitWeaponSwapHandler()
        {
            //Initialize all the ammunitions for each gun
            while (HeroController.instance == null)
            {
                yield return null;
            }


            CreateGuns();
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

            weaponModifierDictionary.Add(WeaponModifierName.SHOTGUN, new WeaponModifier
            {
                boostMultiplier = 5,
                bulletLifetime = 0.24f,
                bulletSize = new Vector3(1.2f, 0.8f, 0),
                bulletVelocity = 60f,
                damageBase = 6,
                damageScale = 4,
                fireRate = 0.18f,
                heatPerShot = 20,
                gunName = WeaponModifierName.SHOTGUN,
                soulCostPerShot = 8,
                soulGainOnHit = 4,
                soulGainOnKill = 8,
                soulRegenSpeed = 0.06f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.SMG, new WeaponModifier
            {
                boostMultiplier = 9,
                bulletLifetime = 0.21f,
                bulletSize = new Vector3(0.9f, 0.7f, 0),
                bulletVelocity = 24f,
                damageBase = 2,
                damageScale = 2,
                fireRate = 0.08f,
                heatPerShot = 15,
                gunName = WeaponModifierName.RIFLE,
                soulCostPerShot = 10,
                soulGainOnHit = 2,
                soulGainOnKill = 7,
                soulRegenSpeed = 0.02f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.CARBINE, new WeaponModifier
            {
                boostMultiplier = 12,
                bulletLifetime = 0.24f,
                bulletSize = new Vector3(1f, 0.7f, 0),
                bulletVelocity = 28f,
                damageBase = 4,
                damageScale = 2,
                fireRate = 0.42f,
                heatPerShot = 60,
                gunName = WeaponModifierName.CARBINE,
                soulCostPerShot = 21,
                soulGainOnHit = 2,
                soulGainOnKill = 24,
                soulRegenSpeed = 0.024f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.RIFLE, new WeaponModifier
            {
                boostMultiplier = 6,
                bulletLifetime = 0.3f,
                bulletSize = new Vector3(1.2f, 0.7f, 0),
                bulletVelocity = 32f,
                damageBase = 4,
                damageScale = 3,
                fireRate = 0.15f,
                heatPerShot = 10,
                gunName = WeaponModifierName.RIFLE,
                soulCostPerShot = 6,
                soulGainOnHit = 3,
                soulGainOnKill = 6,
                soulRegenSpeed = 0.06f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.LMG, new WeaponModifier
            {
                boostMultiplier = 5,
                bulletLifetime = 0.22f,
                bulletSize = new Vector3(1.2f, 0.8f, 0),
                bulletVelocity = 60f,
                damageBase = 6,
                damageScale = 4,
                fireRate = 0.18f,
                heatPerShot = 20,
                gunName = WeaponModifierName.LMG,
                soulCostPerShot = 8,
                soulGainOnHit = 4,
                soulGainOnKill = 8,
                soulRegenSpeed = 0.06f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.DMR, new WeaponModifier
            {
                boostMultiplier = 5,
                bulletLifetime = 0.24f,
                bulletSize = new Vector3(1.2f, 0.8f, 0),
                bulletVelocity = 60f,
                damageBase = 6,
                damageScale = 4,
                fireRate = 0.18f,
                heatPerShot = 20,
                gunName = WeaponModifierName.DMR,
                soulCostPerShot = 8,
                soulGainOnHit = 4,
                soulGainOnKill = 8,
                soulRegenSpeed = 0.06f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.SNIPER, new WeaponModifier
            {
                boostMultiplier = 5,
                bulletLifetime = 0.24f,
                bulletSize = new Vector3(1.2f, 0.8f, 0),
                bulletVelocity = 60f,
                damageBase = 6,
                damageScale = 4,
                fireRate = 0.18f,
                heatPerShot = 20,
                gunName = WeaponModifierName.SNIPER,
                soulCostPerShot = 8,
                soulGainOnHit = 4,
                soulGainOnKill = 8,
                soulRegenSpeed = 0.06f,
            });



        }

        void OnDestroy()
        {
            Destroy(gameObject.GetComponent<WeaponSwapAndStatHandler>());
        }
    }




    //===========================================================
    //Gun Struct
    //===========================================================
    public enum WeaponModifierName
    {
        SHOTGUN,
        SMG,
        CARBINE,
        RIFLE,
        LMG,
        DMR,
        SNIPER    
    }

    public struct WeaponModifier
    {
        public WeaponModifierName gunName;
        public int soulCostPerShot;
        public int soulGainOnHit;
        public int soulGainOnKill;
        public float soulRegenSpeed;
        public int damageBase;
        public int damageScale;
        public float fireRate;
        public float bulletVelocity;
        public float bulletLifetime;
        public float heatPerShot;
        public float boostMultiplier;
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
