using System.Collections;
using UnityEngine;

namespace HollowPoint
{
    class AmmunitionControl : MonoBehaviour
    {
        //public static Ammunition[] ammoInstance = new Ammunition[3];
        public static int currAmmoIndex;
        int tapUp = 0;
        int tapDown = 0;
        bool tapStart = true;
        public static bool reloading = false;
        public static bool firing = false;
        public static float reloadPercent = 0;
        float tapTimer = 0;
        float time = 0;
        float recoilTime = 0;

        public static float gunHeat = 0;

        public void Start()
        {

        StartCoroutine(CreateAmmoInstance());
        }

        public IEnumerator CreateAmmoInstance()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);
            Modding.Logger.Log("[HOLLOW POINT] AmmunitionControl.cs sucessfully initialized!");
        }

        public void Update()
        {


            //Handles how much recoil is accumulated, if its great than 0 then tick it down slowly

           
        }

        public void OnDestroy()
        {
            Destroy(gameObject.GetComponent<AmmunitionControl>());
            Destroy(this);
        }

    }
}
