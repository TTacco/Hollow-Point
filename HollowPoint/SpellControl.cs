using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using ModCommon.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using GlobalEnums;

namespace HollowPoint
{
    class SpellControl : MonoBehaviour
    {
        public void Start()
        {
            StartCoroutine(SpellInitialize());
        }

        public IEnumerator SpellInitialize()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);

            FSMInitialization();
            Modding.Logger.Log("[Hollow Point] Succesfully initialized Spell Control");
        }

        public void FSMInitialization()
        {
            HeroController.instance.spellControl.InsertAction("Fireball Antic", new CallMethod
            {
                behaviour = GameManager.instance.GetComponent<SpellControl>(),
                methodName = "RemoveFireballTransition",
                parameters = new FsmVar[0],
                everyFrame = false
            }
            , 1);

        }

        public void RemoveFireballTransition()
        {
            Modding.Logger.Log("[Hollow Point] Tried using regular fireball");
            HeroController.instance.spellControl.SetState("Cancel All");

            //This should be when the knight can throw a grenade, also manually remove soul
        }

        public void Update()
        {

        }
    }
}
