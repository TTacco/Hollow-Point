using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;

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
            AmmunitionControl.currAmmoType.CurrAmmo = Ammo.ammoTypes[AmmunitionControl.currAmmoIndex].MaxAmmo;
            AmmunitionControl.currAmmoType.CurrMag = Ammo.ammoTypes[AmmunitionControl.currAmmoIndex].MaxMag;

            for (int i = 1; i<Ammo.ammoTypes.Length-1; i++)
            {
                Ammo.ammoTypes[i].CurrAmmo = Ammo.ammoTypes[i].MaxAmmo;
                Ammo.ammoTypes[i].CurrMag = Ammo.ammoTypes[i].MaxMag;
            }
        }
    }
}
