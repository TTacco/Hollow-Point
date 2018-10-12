using System.Collections;
using UnityEngine;

namespace HollowPoint
{
    class AmmunitionControl : MonoBehaviour
    {
        //public static Ammunition[] ammoInstance = new Ammunition[3];
        public static bool reloading = false;
        public static bool firing = false;
        public static float reloadPercent = 0;

        public static float gunHeat = 0;
        public static bool gunHeatBreak = false;
        public static float lowerGunTimer;
        private float heatTimer;

        public static bool gunIsActive = true;
        bool swapWeaponStart = false;
        float swapWeaponTimer = 0f;
        int tapDown;

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

            //Tick down the players gunheat if its greater than 0
            if (heatTimer > 0)
            {
                heatTimer -= Time.deltaTime;
            }
            else
            {
                if (gunHeat>0)
                {
                    gunHeat--;
                }

                if (gunHeatBreak)
                {
                    heatTimer = 0.08f;
                }
                else
                {
                    heatTimer = 0.05f;
                }

            }

            //Make a bool that locks firing if player breaks heat limit
            if (gunHeat >= 100)
            {
                gunHeatBreak = true;
            }

            //Player now has 0 gunHeat and can fire again
            if(gunHeatBreak && gunHeat <= 0)
            {
                gunHeatBreak = false;
                GunSpriteController.DefaultWeaponPos();
            }


            if ((InputHandler.Instance.inputActions.down.WasPressed))
            {
                tapDown++;
            }

            if ((tapDown == 1) && !swapWeaponStart)
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
                }
            }

            if (tapDown >= 2)
            {
                if (gunIsActive)
                {
                    gunIsActive = false;
                }
                else
                {
                    gunIsActive = true;
                    GunSpriteController.DefaultWeaponPos();
                }

                tapDown = 0;
                swapWeaponTimer = 0;
                swapWeaponStart = false;
            }

        }

        public void OnDestroy()
        {
            Destroy(gameObject.GetComponent<AmmunitionControl>());
            Destroy(this);
        }

    }
}
