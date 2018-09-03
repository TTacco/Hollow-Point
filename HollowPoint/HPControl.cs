using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonoMod.Utils;
using MonoMod;
using HutongGames;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using ModCommon;
using ModCommon.Util;
using GlobalEnums;


namespace HollowPoint
{
    class HPControl : MonoBehaviour
    {
        private bool reloading;
        private GameObject fireball;
        private PlayMakerFSM fireballFSM;
        private PlayMakerFSM fireballControlFSM;
        private readonly System.Random recoilNum = new System.Random();
        private float recoilVal;

        AttackDirection ad;


        //INTIALIZATION
        public void Awake()
        {
            ModHooks.Instance.ObjectPoolSpawnHook += BooletSize;
        }

        public void Start()
        {
            ModHooks.Instance.AttackHook += Attack_Hook;
            On.NailSlash.StartSlash += Start_Slash;
            ModHooks.Instance.HitInstanceHook += SpellDam;


            StartCoroutine(CoroutineTest());
        }

        public IEnumerator CoroutineTest()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);
            Modding.Logger.Log("Initialized");           
        }


        //SHOOT/FIRE METHOD
        public void Attack_Hook(AttackDirection ad)
        {
            this.ad = ad;
        }

        public void Start_Slash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            if (ad != AttackDirection.downward && !AmmunitionControl.reloading)
            {
                StartCoroutine(CheckIfNull(HeroController.instance.cState.facingRight));
                if (AmmunitionControl.currAmmoType.AmmoName.Contains("9MM"))
                {
                    StartCoroutine(BurstFire(5));
                }
                else if (AmmunitionControl.currAmmoType.AmmoName.Contains("5.56"))
                {
                    StartCoroutine(BurstFire(3));
                }
                else
                {
                    AmmunitionControl.currAmmoType.CurrAmmo--;
                    Schutz(ad);
                }

                if(AmmunitionControl.currAmmoType.CurrAmmo <= 0)
                {
                    Modding.Logger.Log("START RELOADING NOW");
                    AmmunitionControl.reloading = true;
                }
            }
            else
            {
                orig(self);
            }
        }

        public IEnumerator BurstFire(int x)
        {
            for(int i = x; i > 0 && AmmunitionControl.currAmmoType.CurrAmmo > 0; i--)
            {
                Schutz(ad);
                AmmunitionControl.currAmmoType.CurrAmmo--;
                yield return new WaitForSeconds(0.10f);
            }

            yield return null;
        }
    
        public void Schutz(AttackDirection ad)
        {
            _fireball = Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position - new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));

            _fireball.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            _fireballFSM = _fireball.LocateMyFSM("Fireball Cast");

            _fireballFSM.FsmVariables.GetFsmFloat("Fire Speed").Value = 70;

            //Shooting toward the right, removes audio and shake
            if (HeroController.instance.cState.facingRight && ad == AttackDirection.normal)
            {
                _fireball.transform.position += new Vector3(0.80f, -0.5f, 0f);
                _fireballFSM.GetAction<SendEventByName>("Cast Right", 1).sendEvent = "";
                _fireballFSM.GetAction<AudioPlayerOneShotSingle>("Cast Right", 6).volume = 0;
                _fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).position = new Vector3(0, 0, 0);

                //add bullet deviation/recoil
                recoilVal = recoilNum.Next(-7, 7);
                _fireball.transform.Rotate(new Vector3(0, 0, recoilVal));
                _fireballFSM.GetAction<SetVelocityAsAngle>("Cast Right", 9).angle = 0 + recoilVal;
            }
            //Shooting toward the left, removes audio and shake
            else if (!HeroController.instance.cState.facingRight && ad == AttackDirection.normal)
            {
                _fireball.transform.position += new Vector3(-0.80f, -0.5f, 0f);
                _fireballFSM.GetAction<SendEventByName>("Cast Left", 1).sendEvent = "";
                _fireballFSM.GetAction<AudioPlayerOneShotSingle>("Cast Left", 3).volume = 0;
                _fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).position = new Vector3(0, 0, 0);

                //add bullet deviation
                recoilVal = recoilNum.Next(-7, 7);
                _fireball.transform.Rotate(new Vector3(0, 0, recoilVal));
                _fireballFSM.GetAction<SetVelocityAsAngle>("Cast Left", 6).angle = 180 + recoilVal;
            }
        }

        public IEnumerator CheckIfNull(bool facingRight)
        {
            do
            {
                yield return null;
            }
            while (_fireballFSM.FsmVariables.GetFsmGameObject("Fireball").Value.LocateMyFSM("Fireball Control") == null);
            //_fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).storeObject.Value == null || _fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).storeObject.Value == null          

            _fireballControlFSM = _fireballFSM.FsmVariables.GetFsmGameObject("Fireball").Value.LocateMyFSM("Fireball Control");

            Log("Wall Impact reached");
            _fireballControlFSM.GetAction<SendEventByName>("Wall Impact", 2).sendEvent = "";

            yield return null;
        }

        public HitInstance SpellDam(Fsm owner, HitInstance hit)
        {
            if (hit.AttackType == AttackTypes.Spell)
            {
                //hit.DamageDealt = 8;
            }

            return hit;
        }

        //BULLET SIZE CHANGES
        public GameObject BooletSize(GameObject go)
        {
            if (go.name.Contains("Fireball"))
            {
                Log("Fireball");
                StartCoroutine(ShrinkBooletSize(go));
            }
            return go;
        }

        public IEnumerator ShrinkBooletSize(GameObject go)
        {
            yield return new WaitForEndOfFrame();
            go.GetComponent<Transform>().localScale = new Vector3(0.5f, 0.2f, 1f);
            go.LocateMyFSM("Fireball Control").GetAction<SendEventByName>("Wall Impact", 2).sendEvent = "";
            go.LocateMyFSM("Fireball Control").GetAction<SetFsmInt>("Set Damage", 2).setValue = 1;
            yield return null;
            //go.LocateMyFSM("Fireball Control").GetAction<DamageEnemies>().
        }

       
        //MISC
        public void Log(String s)
        {
            Modding.Logger.Log(s);
        }

        public void OnDestroy()
        {
            ModHooks.Instance.AttackHook -= Attack_Hook;
            On.NailSlash.StartSlash -= Start_Slash;
            ModHooks.Instance.HitInstanceHook -= SpellDam;
            Destroy(_fireball);
            Destroy(_fireballFSM);
            Destroy(_fireballControlFSM);
        }

    }
}
