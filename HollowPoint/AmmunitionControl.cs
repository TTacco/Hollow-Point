using System.Collections;
using UnityEngine;

namespace HollowPoint
{
    class AmmunitionControl : MonoBehaviour
    {
        //public static Ammunition[] ammoInstance = new Ammunition[3];

        public static Ammunition currAmmoType;
        int currAmmoIndex;
        int tapUp = 0;
        int tapDown = 0;
        bool tapStart = true;
        public static bool reloading = false;
        public static float reloadPercent = 0;
        float tapTimer = 0;
        float time = 0;

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
            currAmmoType = Ammo.ammoTypes[0];
        }

        public void Update()
        {
            //RELOADING METHODS 
            if (0 > time && reloading)
            {
                time = currAmmoType.ReloadTime; //time should be reloadTime next... time
                reloadPercent++;
            }
            else if (reloading)
            {
                time -= Time.deltaTime;
            }

            if (reloadPercent > 100 && reloading)
            {
                Modding.Logger.Log("Reload Complete");
                currAmmoType.CurrAmmo = currAmmoType.MaxAmmo;
                currAmmoType.CurrMag--;
                reloading = false;
                reloadPercent = 0;
            }

            //Handles Ammo Changing
            if ((HeroController.instance.cState.onGround && InputHandler.Instance.inputActions.up.WasPressed) && !reloading)
            {
                tapUp++;
            }

            if ((HeroController.instance.cState.onGround && InputHandler.Instance.inputActions.down.WasPressed) && !reloading)
            {
                tapDown++;
            }

            //SWAP AMMO
            if ((tapUp == 1 || tapDown == 1) && !tapStart)
            {
                tapTimer = 0.4f;
                tapStart = true;
            }

            if (tapUp >= 2 || tapDown >= 2)
            {
                Modding.Logger.Log("SWITCH AMMO!");

                Ammo.ammoTypes[currAmmoIndex] = currAmmoType;

                //If player taps up twice, cycle up, if player taps down twice, cycle down
                if (tapUp >= 2)
                {
                    currAmmoIndex++;
                }
                else if (tapDown >= 2)
                {
                    currAmmoIndex--;
                }

                //Prevents null pointer exceptions, also allowing them to cycle through the entire weapon index
                if (currAmmoIndex >= Ammo.ammoTypes.Length)
                {
                    currAmmoIndex = 0;
                }
                else if (currAmmoIndex < 0)
                {
                    currAmmoIndex = Ammo.ammoTypes.Length - 1;
                }

                currAmmoType = Ammo.ammoTypes[currAmmoIndex];

                tapUp = 0;
                tapDown = 0;
                tapTimer = 0;
                tapStart = false;
            }

            if (tapTimer > 0)
            {
                tapTimer -= Time.deltaTime;
            }
            else
            {
                tapUp = 0;
                tapDown = 0;
                tapTimer = 0;
                tapStart = false;
            }
        }


    }
}
