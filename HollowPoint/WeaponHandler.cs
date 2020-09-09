﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobalEnums;
using static HollowPoint.HollowPointEnums;
using static Modding.Logger;
using ModCommon.Util;


namespace HollowPoint
{
    //===========================================================
    //Weapon Swap
    //===========================================================
    public class WeaponSwapAndStatHandler : MonoBehaviour
    {
        public static WeaponSwapAndStatHandler instance = null;

        public WeaponType currentWeapon = WeaponType.Melee;
        public Gun currentEquippedGun;
        SpriteRenderer gunSpriteRenderer = null;

        public Dictionary<WeaponModifierName, Gun> weaponModifierDictionary = new Dictionary<WeaponModifierName, Gun>();

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
        }

        public void Update()
        {
            return;
        }

        public Gun EquipWeapon()
        {
            //List of charms with conversion kits attached to them
            List<int> conversionCharms = new List<int>(){ 11, 13, 15, 18, 20, 32 };
            List<int> equippedCharms = PlayerData.instance.equippedCharms;

            //Check the list of intersecting charms, if theres more than 2 then the player is not allowed to fire
            List<int> equippedConversionCharms = conversionCharms.Intersect(equippedCharms).ToList();
            int intersectedCharmsCount = equippedConversionCharms.Count();
            bool incompatibleCharmCombination = (intersectedCharmsCount >= 2)? true : false;

            if (incompatibleCharmCombination)
            {
                //If the player has equipped one or more conversion kit, disable the gun from firing until they swap out, bumping soul cost is enough for this
                currentEquippedGun = instance.weaponModifierDictionary[WeaponModifierName.RIFLE];
                currentEquippedGun.soulCostPerShot = 999;
                ChangeWeaponSprite(currentWeapon);
            }
            else
            {
                //Ensures first that the list contains intersected charms, otherwise [0] will throw an out of bounds exception
                //having 0 count also means that the player has no conversion kits equipped, thus will retain the base Rifle gun
                int charmEquipped = (intersectedCharmsCount > 0)? equippedConversionCharms[0] : 0;

                switch (charmEquipped)
                {
                    case 13:
                        currentEquippedGun = instance.weaponModifierDictionary[WeaponModifierName.SNIPER];
                        break;
                    case 32:
                        currentEquippedGun = instance.weaponModifierDictionary[WeaponModifierName.LMG];
                        break;
                    default:
                        currentEquippedGun = instance.weaponModifierDictionary[WeaponModifierName.RIFLE];
                        break;
                }

                ChangeWeaponSprite(currentWeapon);
            }

            return currentEquippedGun;
        }

        public void SwapWeapons()
        {
            Stats.instance.swapTimer = (PlayerData.instance.equippedCharm_26) ? 1.5f : 7f;
            Stats.instance.canSwap = false;

            HeroController.instance.spellControl.SetState("Inactive");
            Modding.Logger.Log("Swaping weapons");

            if (instance.currentWeapon == WeaponType.Ranged)
            {
                //Holster gun
                /*the ACTUAL attack cool down variable, i did this to ensure the player wont have micro stutters 
                 * on animation because even at 0 animation time, sometimes they play for a quarter of a milisecond
                 * thus giving that weird head jerk anim playing on the knight */

                HeroController.instance.SetAttr<float>("attack_cooldown", 0.1f);
                instance.SwapBetweenNail();
                AudioHandler.instance.PlayDrawHolsterSound("holster");
            }
            else
            {
                //Equip gun
                instance.SwapBetweenNail();
                AudioHandler.instance.PlayDrawHolsterSound("draw");
            }

            HeroController.instance.spellControl.SetState("Inactive");
        }

        void ChangeWeaponSprite(WeaponType wt)
        {
            string spriteName = "";
            try
            {
                if (gunSpriteRenderer == null) gunSpriteRenderer = HollowPointSprites.gunSpriteGO.GetComponent<SpriteRenderer>();

                string gunNameLowerCaps = currentEquippedGun.gunName.ToString().ToLower();
                spriteName = "weapon_sprite_" + gunNameLowerCaps;
                spriteName += (wt == WeaponType.Ranged) ? ".png" : "_nohands.png";

                gunSpriteRenderer.sprite = GunSpriteRenderer.weaponSpriteDictionary[spriteName];
            }
            catch(Exception e)
            {
                Log("[WeaponHandler] Cannot find texture of the name " + spriteName);
            }
        }

        //Swap between guns or nail
        void SwapBetweenNail()
        {
            WeaponType prevWep = currentWeapon;
            currentWeapon = (currentWeapon == WeaponType.Melee) ? WeaponType.Ranged : WeaponType.Melee;
            ChangeWeaponSprite(currentWeapon);

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

            weaponModifierDictionary.Add(WeaponModifierName.SHOTGUN, new Gun
            {
                boostMultiplier = 12,
                bulletLifetime = 0.17f,
                bulletSize = new Vector3(0.62f, 0.59f, 0),
                bulletVelocity = 45f,
                damageBase = 2,
                damageScale = 1,
                fireRate = 0.18f,
                heatPerShot = 20,
                gunName = WeaponModifierName.SHOTGUN,
                soulCostPerShot = 21,
                soulGainOnHit = 0,
                soulGainOnKill = 22,
                soulRegenSpeed = 0.06f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.SMG, new Gun
            {
                boostMultiplier = 9,
                bulletLifetime = 0.19f,
                bulletSize = new Vector3(0.65f, 0.7f, 0),
                bulletVelocity = 30f,
                damageBase = 2,
                damageScale = 2,
                fireRate = 0.09f,
                heatPerShot = 11,
                gunName = WeaponModifierName.SMG,
                soulCostPerShot = 5,
                soulGainOnHit = 4,
                soulGainOnKill = 15,
                soulRegenSpeed = 0.045f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.CARBINE, new Gun
            {
                boostMultiplier = 15,
                bulletLifetime = 0.22f,
                bulletSize = new Vector3(0.7f, 0.7f, 0),
                bulletVelocity = 35f,
                damageBase = 3,
                damageScale = 2,
                fireRate = 0.40f,
                heatPerShot = 10,
                gunName = WeaponModifierName.CARBINE,
                soulCostPerShot = 33,
                soulGainOnHit = 2,
                soulGainOnKill = 24,
                soulRegenSpeed = 0.011f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.RIFLE, new Gun
            {
                boostMultiplier = 5,
                bulletLifetime = 0.22f,
                bulletSize = new Vector3(0.8f, 0.7f, 0),
                bulletVelocity = 40f,
                damageBase = 3,
                damageScale = 3,
                fireRate = 0.15f,
                heatPerShot = 10,
                gunName = WeaponModifierName.RIFLE,
                soulCostPerShot = 5,
                soulGainOnHit = 0,
                soulGainOnKill = 0,
                soulRegenSpeed = 0.05f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.LMG, new Gun
            {
                boostMultiplier = 1,
                bulletLifetime = 0.37f,
                bulletSize = new Vector3(0.8f, 0.8f, 0),
                bulletVelocity = 34f,
                damageBase = 4,
                damageScale = 3,
                fireRate = 0.09f,
                heatPerShot = 14,
                gunName = WeaponModifierName.LMG,
                soulCostPerShot = 3,
                soulGainOnHit = 2,
                soulGainOnKill = 9,
                soulRegenSpeed = 0.12f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.DMR, new Gun
            {
                boostMultiplier = 5,
                bulletLifetime = 0.28f,
                bulletSize = new Vector3(1.2f, 0.8f, 0),
                bulletVelocity = 55f,
                damageBase = 8,
                damageScale = 5,
                fireRate = 0.25f,
                heatPerShot = 6,
                gunName = WeaponModifierName.DMR,
                soulCostPerShot = 15,
                soulGainOnHit = 15,
                soulGainOnKill = 20,
                soulRegenSpeed = 0.06f,
            });

            weaponModifierDictionary.Add(WeaponModifierName.SNIPER, new Gun
            {
                boostMultiplier = 5,
                bulletLifetime = 0.33f,
                bulletSize = new Vector3(1.4f, 0.8f, 0),
                bulletVelocity = 70f,
                damageBase = 10,
                damageScale = 7,
                fireRate = 1f,
                heatPerShot = 0,
                gunName = WeaponModifierName.SNIPER,
                soulCostPerShot = 33,
                soulGainOnHit = 33,
                soulGainOnKill = 50,
                soulRegenSpeed = 0.15f,
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

    public struct Gun
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
}
