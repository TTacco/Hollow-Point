using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using System.Collections;
using ModCommon.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using GlobalEnums;

namespace HollowPoint
{
    class SpellControl : MonoBehaviour
    {
        public static bool airStrikeInProgress = false;
        private System.Random random;

        GameObject grenade;
        Texture2D grenadeTexture;
        float speed;

        PlayMakerFSM nailArtFSM = HeroController.instance.gameObject.LocateMyFSM("Nail Art");

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

            grenade.AddComponent<Rigidbody2D>();
            grenade.AddComponent<SpriteRenderer>();
            grenade.AddComponent<Transform>();

            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string res in asm.GetManifestResourceNames())
            {
                if (!res.EndsWith(".png"))
                {
                    //Steal 56's Lightbringer code :weary:
                    continue;
                }
            }


            Modding.Logger.Log("[HOLLOW POINT] SpellControl.cs sucessfully initialized!");
        }

        public void FSMInitialization()
        {
            nailArtFSM = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");

            if(nailArtFSM == null)
            {
                Log("it is null");
            }

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



            nailArtFSM.InsertAction("Flash 2", new CallMethod
            {
                behaviour = GameManager.instance.GetComponent<SpellControl>(),
                methodName = "SuperShot",
                parameters = new FsmVar[0],
                everyFrame = false
            }
            , 1);

        }

        public void RemoveFireballTransition()
        {
            Modding.Logger.Log("[Hollow Point] Tried using regular fireball");
            HeroController.instance.spellControl.SetState("Cancel All");
            ThrowGrenade();
            //This should be when the knight can throw a grenade, also manually remove soul
        }

        public void ThrowGrenade()
        {
            Log("Throwing nade");
            GameObject grenadeClone = Instantiate(grenade, HeroController.instance.transform.position - new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
            grenadeClone.GetComponent<Transform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
            grenadeClone.GetComponent<SpriteRenderer>().color = Color.red;

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
            //airStrikeBullet = Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position, new Quaternion(0, 0, 0, 0));

            StartCoroutine(AirStrikeOnProgress());

        }

        public IEnumerator AirStrikeOnProgress()
        {
            yield return new WaitForSeconds(0.2f);
            HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.airStrikeSoundFX[0]);
            yield return new WaitForSeconds(0.3f);
            HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.airStrikeSoundFX[1]);
            yield return new WaitForSeconds(1.5f);
            HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.airStrikeSoundFX[0]);
            Log("AirStrike has ended");
            //airStrikeInProgress = false;
        }

        public void SuperShot()
        {
            Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position - new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
            nailArtFSM.SetState("G Slash End");
            StartCoroutine("BounceBack");
        }

        public IEnumerator BounceBack()
        {
            Log("" +HeroController.instance.RECOIL_HOR_VELOCITY_LONG);
            HeroController.instance.RECOIL_HOR_VELOCITY_LONG = 55;

            yield return new WaitForEndOfFrame();
            // HeroController.instance.ShroomBounce();

            if (HeroController.instance.cState.facingRight)
            {
                HeroController.instance.RecoilLeftLong();
            }
            else if (!HeroController.instance.cState.facingRight)
            {
                HeroController.instance.RecoilRightLong();
            }
        }


        public void Update()
        {

        }

        private void Log(String s)
        {
            Modding.Logger.Log("[HOLLOW POINT] " + s);
        }


        public void OnDestroy()
        {
            Destroy(gameObject.GetComponent<SpellControl>());
            Destroy(grenade);
        }
    }
}
