using System.Collections;
using UnityEngine;

namespace HollowPoint
{
    class AmmunitionControl : MonoBehaviour
    {
        //public static Ammunition[] ammoInstance = new Ammunition[3];

        public static Ammunition currAmmoType;
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
            Modding.Logger.Log("[HOLLOW POINT] AmmunitionControl.cs sucessfully initialized!");
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
                currAmmoType.CurrAmmo = currAmmoType.MaxAmmo;
                currAmmoType.CurrMag--;
                reloading = false;
                reloadPercent = 0;
            }

            //Handles how much recoil is accumulated, if its great than 0 then tick it down slowly
            if (currAmmoType.CurrRecoilDeviation > 0 && recoilTime <= 0)
            {
                recoilTime = 0.20f;
                currAmmoType.CurrRecoilDeviation--;
            }
            else if (currAmmoType.CurrRecoilDeviation > 0)
            {
                recoilTime -= Time.deltaTime;
            }

            //Handles Ammo Changing
            if ((HeroController.instance.cState.onGround && InputHandler.Instance.inputActions.up.WasPressed) && !reloading && !firing)
            {
                tapUp++;
            }

            if ((HeroController.instance.cState.onGround && InputHandler.Instance.inputActions.down.WasPressed) && !reloading && !firing)
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
                HeroController.instance.ATTACK_COOLDOWN_TIME = currAmmoType.Firerate;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = currAmmoType.Firerate;

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
