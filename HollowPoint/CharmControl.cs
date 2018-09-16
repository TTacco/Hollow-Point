using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using static HollowPoint.AmmunitionControl;

namespace HollowPoint
{
    class CharmControl : MonoBehaviour
    {
        public static bool resupplied = false;

        public void Start()
        {
            ModHooks.Instance.CharmUpdateHook += RestockAmmunition;
        }

        public void RestockAmmunition(PlayerData pd, HeroController hc)
        {
            resupplied = true;
            currAmmoType.CurrAmmo = Ammo.ammoTypes[currAmmoIndex].MaxAmmo;
            currAmmoType.CurrMag = Ammo.ammoTypes[currAmmoIndex].MaxMag;

            for (int i = 1; i<Ammo.ammoTypes.Length-1; i++)
            {
                Ammo.ammoTypes[i].CurrAmmo = Ammo.ammoTypes[i].MaxAmmo;
                Ammo.ammoTypes[i].CurrMag = Ammo.ammoTypes[i].MaxMag;
            }
        }
    }
}
