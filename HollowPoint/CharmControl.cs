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

        }

        public void OnDestroy()
        {
            ModHooks.Instance.CharmUpdateHook -= RestockAmmunition;
            Destroy(gameObject.GetComponent<CharmControl>());
            Destroy(this);
        }
    }
}
