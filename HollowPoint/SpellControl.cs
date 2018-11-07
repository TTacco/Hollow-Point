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

        //Fireball modification
        GameObject fireballInstance = null;
        PlayMakerFSM fireballInstanceFSM = null;
        GameObject fireballControl = null;
        PlayMakerFSM fireballControlFSM = null;

        PlayMakerFSM nailArtFSM = HeroController.instance.gameObject.LocateMyFSM("Nail Art");

        public void Start()
        {
            StartCoroutine(SpellInitialize());
            On.HeroController.DoDoubleJump += DoubleJumpStarted;
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
                methodName = "GreatSlashOverride",
                parameters = new FsmVar[0],
                everyFrame = false
            }
            , 1);

            nailArtFSM.InsertAction("Flash", new CallMethod
            {
                behaviour = GameManager.instance.GetComponent<SpellControl>(),
                methodName = "CycloneSpinOverride",
                parameters = new FsmVar[0],
                everyFrame = false
            }
            , 1);

        }

        #region FIREBALL FSM CHANGES

        public void RemoveFireballTransition()
        {
            //Modding.Logger.Log("[Hollow Point] Tried using regular fireball");
            //HeroController.instance.spellControl.SetState("Cancel All");
            //ThrowGrenade();
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
            //Log("Tried calling in an airstrike");
            //CallInAirstrike();
            //HeroController.instance.spellControl.SetState("Cancel All");
        }

        public void CallInAirstrike()
        {
            Log("CALL IN AN AIRSTRIKE");
            //airStrikeInProgress = true;
            //airStrikeBullet = Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position, new Quaternion(0, 0, 0, 0));

            //StartCoroutine(AirStrikeOnProgress());

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

        #endregion

        #region might transfer all of these in a seperate class

        public void GreatSlashOverride()
        {
            if (AmmunitionControl.gunIsActive)
            {
                nailArtFSM.SetState("G Slash End");
                AmmunitionControl.firing = true;
                AmmunitionControl.lowerGunTimer = 0.5f;
                AmmunitionControl.gunHeat += 30;
                SuperShot();
            }
        }

        public void CycloneSpinOverride()
        {
            if (AmmunitionControl.gunIsActive)
            {
                nailArtFSM.SetState("Cyclone End");
                AmmunitionControl.firing = true;
                AmmunitionControl.lowerGunTimer = 0.5f;
                AmmunitionControl.gunHeat += 30;
                SuperShot();
            }
        }

        public void SuperShot()
        {
            InputHandler ih = InputHandler.Instance;
            bool flag = (ih.inputActions.down.IsPressed || ih.inputActions.up.IsPressed);
            if (flag)
            {
                StartCoroutine("BoostVertical");
            }
            else
            {
                StartCoroutine("BoostHorizontal");
            }
        }

        //Welcome to the "I fucking hate this" section
        public IEnumerator BoostVertical()
        {
            fireballInstance = Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position - new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
            fireballInstanceFSM = fireballInstance.LocateMyFSM("Fireball Cast");

            //HOLY FUCK I SHOULD CLEAN THIS INTO SOMETHING THATS ACTUALLY FUCKING READABLE 

            //Launches the player upwards on charge shot
            if ((InputHandler.Instance.inputActions.down.IsPressed) || ((InputHandler.Instance.inputActions.down.IsPressed) && (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)))
            {
                //fireballInstance.transform.position += new Vector3(-0.8f, 0f, 0f);
                if (HeroController.instance.cState.facingRight)
                {
                    fireballInstance.transform.Rotate(new Vector3(0, 0, 90.0f));
                    fireballInstanceFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).position = new Vector3(0, -0.5f, 0); //HeroController.instance.transform.position + new Vector3(0, -0.3f, 0);
                    fireballInstanceFSM.GetAction<SetVelocityAsAngle>("Cast Right", 9).angle = -91f;
                }
                else if (!HeroController.instance.cState.facingRight)
                {
                    fireballInstance.transform.Rotate(new Vector3(0, 0, -90.0f));
                    fireballInstanceFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).position = new Vector3(0, -0.5f, 0); //HeroController.instance.transform.position + new Vector3(0, -0.3f, 0);
                    fireballInstanceFSM.GetAction<SetVelocityAsAngle>("Cast Left", 6).angle = -89f;
                }
                yield return new WaitForEndOfFrame();
                HeroController.instance.ShroomBounce();
            }

            //SHOULD launch the player upwards
            else if ((InputHandler.Instance.inputActions.up.IsPressed) || ((InputHandler.Instance.inputActions.up.IsPressed) && (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)))
            {
                HeroController.instance.SHROOM_BOUNCE_VELOCITY = -30;
                if (HeroController.instance.cState.facingRight)
                {
                    fireballInstance.transform.Rotate(new Vector3(0, 0, -90.0f));
                    fireballInstanceFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).position = new Vector3(0, -0.5f, 0); //HeroController.instance.transform.position + new Vector3(0, -0.3f, 0);
                    fireballInstanceFSM.GetAction<SetVelocityAsAngle>("Cast Right", 9).angle = 91f;
                }
                else if (!HeroController.instance.cState.facingRight)
                {
                    fireballInstance.transform.Rotate(new Vector3(0, 0, 90.0f));
                    fireballInstanceFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).position = new Vector3(0, -0.5f, 0); //HeroController.instance.transform.position + new Vector3(0, -0.3f, 0);
                    fireballInstanceFSM.GetAction<SetVelocityAsAngle>("Cast Left", 6).angle = 89f;
                }
                yield return new WaitForEndOfFrame();
                HeroController.instance.ShroomBounce();
                HeroController.instance.SHROOM_BOUNCE_VELOCITY = 25;
            }

        }



        public IEnumerator BoostHorizontal()
        {
            fireballInstance = Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position - new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));

            Log("IMMERSE YOURSELF IN LOVE");
            yield return null;
        }


        #endregion

        public void DoubleJumpStarted(On.HeroController.orig_DoDoubleJump orig, HeroController self)
        {
            //Log("WEARY");

            orig(self);
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
            On.HeroController.DoDoubleJump -= DoubleJumpStarted;
            Destroy(gameObject.GetComponent<SpellControl>());
            Destroy(grenade);
        }
    }
}
