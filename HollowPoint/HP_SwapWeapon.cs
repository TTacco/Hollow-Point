using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HollowPoint
{
    class HP_SwapWeapon : MonoBehaviour
    {
        int tapDown;
        float swapWeaponTimer = 0;
        bool swapWeaponStart = false;

        public void Update()
        {
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
                if (HP_Handler.gunActive)
                {
                    HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.holsterSFX);
                    HP_Handler.gunActive = false;
                }
                else
                {
                    HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.drawSFX);
                    HP_Handler.gunActive = true;
                    //GunSpriteController.DefaultWeaponPos();
                }

                tapDown = 0;
                swapWeaponTimer = 0;
                swapWeaponStart = false;
            }
        }
    }
}
