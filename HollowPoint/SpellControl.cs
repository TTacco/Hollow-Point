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
        public GameObject airStrikeBullet;
        public static bool airStrikeInProgress = false;
        private System.Random random;

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

            HeroController.instance.spellControl.InsertAction("Scream Get?", new CallMethod
            {
                behaviour = GameManager.instance.GetComponent<SpellControl>(),
                methodName = "RemoveScreamTransition",
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

        public void RemoveScreamTransition()
        {
            //This should be when the knight can call an airstrike, also manually remove soul
            Log("Tried calling in an airstrike");
            CallInAirstrike();
            HeroController.instance.spellControl.SetState("Cancel All");
        }

        public void CallInAirstrike()
        {
            Log("CALL IN AN AIRSTRIKE");
            airStrikeInProgress = true;
            airStrikeBullet = Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position, new Quaternion(0, 0, 0, 0));

            StartCoroutine(AirStrikeOnProgress());

        }

        public IEnumerator AirStrikeOnProgress()
        {
            yield return new WaitForSeconds(0.2f);
            HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.airStrikeSoundFX[0]);
            yield return new WaitForSeconds(0.3f);
            HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.airStrikeSoundFX[2]);
            yield return new WaitForSeconds(1.5f);
            HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.airStrikeSoundFX[0]);
            Log("AirStrike has ended");
            //airStrikeInProgress = false;
        }

        public void Update()
        {

        }

        private void Log(String s)
        {
            Modding.Logger.Log("[HOLLOW POINT] " + s);
        }
    }
}
