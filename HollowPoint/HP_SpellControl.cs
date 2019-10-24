using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using ModCommon.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;


namespace HollowPoint
{
    class HP_SpellControl : MonoBehaviour
    {
        public static bool isUsingGun = false;

        public void Awake()
        {
            StartCoroutine(InitSpellControl());
        }


        public IEnumerator InitSpellControl()
        {
            while (HeroController.instance == null)
            {
                yield return null;
            }

            PlayMakerFSM dive = HeroController.instance.spellControl;
            try
            {
                //dive.RemoveAction("Quake1 Land", 14);

                /*
                FsmOwnerDefault fsmgo = dive.GetAction<ActivateGameObject>("Quake1 Land", 4).gameObject;
                Modding.Logger.Log(fsmgo.GameObject.Name);
                prefab1 = fsmgo.GameObject.Value;
                */

                FsmGameObject fsmgo = dive.GetAction<CreateObject>("Scream Burst 1", 2).gameObject;
                fsmgo.Value.gameObject.transform.position = new Vector3(0, 0, 0);
                fsmgo.Value.gameObject.transform.localPosition = new Vector3(0, -3, 0);
                dive.GetAction<CreateObject>("Scream Burst 1", 2).gameObject = fsmgo;


            HeroController.instance.spellControl.RemoveAction("Scream Burst 1",6);
            HeroController.instance.spellControl.RemoveAction("Scream Burst 1", 6);

            HeroController.instance.spellControl.InsertAction("Can Cast?", new CallMethod
            {
                behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                methodName = "SwapWeapon",
                parameters = new FsmVar[0],
                everyFrame = false
            }
            , 0);

            HeroController.instance.spellControl.AddAction("Quake Antic", new CallMethod
            {
                behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                methodName = "StartQuake",
                parameters = new FsmVar[0],
                everyFrame = false
            }
            );

            HeroController.instance.spellControl.AddAction("Quake1 Land", new CallMethod
            {
                behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                methodName = "ExplosionSound",
                parameters = new FsmVar[0],
                everyFrame = false
            }
            );

            HeroController.instance.spellControl.InsertAction("Has Fireball?", new CallMethod
            {
                behaviour = GameManager.instance.GetComponent<HP_SpellControl>(),
                methodName = "StartFireball",
                parameters = new FsmVar[0],
                everyFrame = false
            }
            , 0);

            }
            catch (Exception e)
            {
                Modding.Logger.Log(e);
            }

        }

        public void StartQuake()
        {
            LoadAssets.sfxDictionary.TryGetValue("divetrigger.wav", out AudioClip ac);
            AudioSource audios = HP_Sprites.gunSpriteGO.GetComponent<AudioSource>();
            //audios.clip = ac;

            audios.PlayOneShot(ac);
        }

        public void ExplosionSound()
        {
            LoadAssets.sfxDictionary.TryGetValue("divedetonate.wav", out AudioClip ac);
            AudioSource audios = HP_Sprites.gunSpriteGO.GetComponent<AudioSource>();
            //audios.clip = ac;

            audios.PlayOneShot(ac);
        }

        public void SwapWeapon()
        {
            HeroController.instance.spellControl.SetState("Spell End");
            Modding.Logger.Log("Swaping weapons");


            if (isUsingGun)
            {
                HP_WeaponHandler.currentGun = HP_WeaponHandler.allGuns[0];
            }
            else
            {
                HP_WeaponHandler.currentGun = HP_WeaponHandler.allGuns[1];
            }
            isUsingGun = !isUsingGun;
        }

        public void StartFireball()
        {

            try
            {
                HeroController.instance.spellControl.SetState("Spell End");

                float directionMultiplier = (HeroController.instance.cState.facingRight) ? 1f : -1f;
                float wallClimbMultiplier = (HeroController.instance.cState.wallSliding) ? -1f : 1f;
                directionMultiplier *= wallClimbMultiplier;
                GameObject bullet = Instantiate(HP_Prefabs.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.2f * directionMultiplier, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                bullet.SetActive(true);
                HP_BulletBehaviour hpbb = bullet.GetComponent<HP_BulletBehaviour>();
                bullet.GetComponent<BoxCollider2D>().size *= 1.7f;
                hpbb.bulletDegreeDirection = HP_DirectionHandler.finalDegreeDirection;
                hpbb.specialAttrib = "Explosion";
                hpbb.bulletSpeedMult = 2f;

                HP_Prefabs.projectileSprites.TryGetValue("specialbullet.png", out Sprite specialBulletTexture);
                bullet.GetComponent<SpriteRenderer>().sprite = specialBulletTexture;

                HP_Sprites.StartMuzzleFlash(HP_DirectionHandler.finalDegreeDirection);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");

                PlayAudio("firerocket");

            }
            catch (Exception e)
            {
                Modding.Logger.Log("[BulletBehaviour] StartExplosion() " + e);
            }



            HP_Sprites.StartGunAnims();         
        }

        public static void PlayAudio(string audioName)
        {
            LoadAssets.sfxDictionary.TryGetValue(audioName.ToLower() + ".wav", out AudioClip ac);
            AudioSource audios = HeroController.instance.spellControl.GetComponent<AudioSource>();
            audios.clip = ac;
            //HP_Sprites.gunSpriteGO.GetComponent<AudioSource>().PlayOneShot(ac);
            audios.pitch = UnityEngine.Random.Range(0.8f, 1.5f);

            audios.PlayOneShot(audios.clip);
        }

        public IEnumerator BurstShot(int burst, float directionMultiplier)
        {
            bool firingSpecialShot = true;
            for (int i = 0; i < burst; i++)
            {
                GameObject bullet = Instantiate(HP_Prefabs.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.7f * directionMultiplier, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                yield return new WaitForSeconds(0.07f);
                //HP_AttackHandler.PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
                Destroy(bullet, 10f);
            }
            firingSpecialShot = false;
        }
    }

}
