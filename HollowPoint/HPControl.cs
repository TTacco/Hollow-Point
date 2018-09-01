using System;
using System.Collections;
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
        bool reloading;
        GameObject _fireball;
        PlayMakerFSM _fireballFSM;
        PlayMakerFSM _fireballControlFSM;

        System.Random recoilNum;
        float recoilVal;
        float xVal = 0.3f;
        float yVal = 0.3f;
        AttackDirection ad;

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
            DontDestroyOnLoad(_fireball);

            recoilNum = new System.Random();

            Modding.Logger.Log("Initialized");
        }

        public void Attack_Hook(AttackDirection ad)
        {
            this.ad = ad;          
        }

        public void Start_Slash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            if (!reloading)
            {
                //reloading = true;
                //StartCoroutine(Reload());
            }

            if(ad != AttackDirection.downward && !reloading)
            {
                StartCoroutine(CheckIfNull(HeroController.instance.cState.facingRight));
                Schutz(ad);
                //AmmoReduce();
            }
            else
            {
                orig(self);
            }
        }

        public void Schutz(AttackDirection ad)
        {
            _fireball = Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position - new Vector3(0, 0, 0), new Quaternion(0,0,0,0));
            _fireball.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            _fireballFSM = _fireball.LocateMyFSM("Fireball Cast");

            _fireballFSM.FsmVariables.GetFsmFloat("Fire Speed").Value = 80;
      
            //FACING RIGHT WHILE FIRING STOP AUDIO AND SHAKE
            if (HeroController.instance.cState.facingRight && ad == AttackDirection.normal)
            {
                _fireball.transform.position += new Vector3(0.80f, -0.5f, 0f);
                _fireballFSM.GetAction<SendEventByName>("Cast Right", 1).sendEvent = "";              
                _fireballFSM.GetAction<AudioPlayerOneShotSingle>("Cast Right", 6).volume = 0;
                _fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).position = new Vector3(0, 0, 0);

                //RECOIL RIGHT
                recoilVal = recoilNum.Next(-15, 15);
                _fireball.transform.Rotate(new Vector3(0, 0, recoilVal));
                _fireballFSM.GetAction<SetVelocityAsAngle>("Cast Right", 9).angle = 0 + recoilVal;
            }
            //FACING LEFT WHILE FIRING STOP AUDIO AND SHAKE
            else if(!HeroController.instance.cState.facingRight && ad == AttackDirection.normal)
            {
                _fireball.transform.position += new Vector3(-0.80f, -0.5f, 0f);
                _fireballFSM.GetAction<SendEventByName>("Cast Left", 1).sendEvent = "";
                _fireballFSM.GetAction<AudioPlayerOneShotSingle>("Cast Left", 3).volume = 0;
                _fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).position = new Vector3(0, 0, 0);

                //RECOIL LEFT
                recoilVal = recoilNum.Next(-15, 15);
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
            while (_fireballFSM.FsmVariables.GetFsmGameObject("Fireball").Value.LocateMyFSM("Fireball Control")==null);
            //_fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).storeObject.Value == null || _fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).storeObject.Value == null

            _fireballControlFSM = _fireballFSM.FsmVariables.GetFsmGameObject("Fireball").Value.LocateMyFSM("Fireball Control");

            Log("Changing bullet sizes");
            _fireballControlFSM.GetAction<SetScale>("Set Damage", 0).x = xVal;
            _fireballControlFSM.GetAction<SetScale>("Set Damage", 0).y = yVal;

            _fireballControlFSM.GetAction<SendEventByName>("Wall Impact", 2).sendEvent = "";

            yield return null;
        }

        public HitInstance SpellDam(Fsm owner, HitInstance hit)
        {
            if(hit.AttackType == AttackTypes.Spell)
            {
                hit.DamageDealt = 8;
            }

            return hit;
        }

        public void AmmoReduce()
        {
            //HPUI.currentAmmo--;
        }

        public IEnumerator Reload()
        {
            //yield return new WaitForSeconds(2f);
            //HPUI.currentAmmo = HPUI.maxAmmo;
            //HPUI.currentMagazine -= 1;
            //reloading = false;
            yield return null;
        }

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
