using System.Collections;
using UnityEngine;
using System;
using static Modding.Logger;
using Modding;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using static HollowPoint.HollowPointEnums;

namespace HollowPoint
{
    public class AudioHandler : MonoBehaviour
    {
        static GameObject emptyGunSFX;
        static GameObject shootSFX;
        static GameObject enemyHitSFX;
        static GameObject terrainHitSFX;

        

        public void Awake()
        {
            StartCoroutine(AudioHandlerInit());
        }

        public IEnumerator AudioHandlerInit()
        {
            while (HeroController.instance == null)
            {
                yield return null;
            }
            emptyGunSFX = new GameObject("EmptySFX", typeof(AudioSource));
            shootSFX = new GameObject("ShootSFX", typeof(AudioSource));
            enemyHitSFX = new GameObject("EmptySFX", typeof(AudioSource));
            terrainHitSFX = new GameObject("ShootSFX", typeof(AudioSource));

            DontDestroyOnLoad(emptyGunSFX);
            DontDestroyOnLoad(shootSFX);
            DontDestroyOnLoad(enemyHitSFX);
            DontDestroyOnLoad(terrainHitSFX);
        }


        public static void PlayGunSounds(string gunName)
        {

            try
            {
                LoadAssets.sfxDictionary.TryGetValue("shoot_sfx_" + gunName.ToLower() + ".wav", out AudioClip ac);
                AudioSource audios = shootSFX.GetComponent<AudioSource>();
                audios.clip = ac;
                audios.pitch = UnityEngine.Random.Range(0.9f, 1.1f);    
                audios.PlayOneShot(audios.clip, GameManager.instance.GetImplicitCinematicVolume());

                //Play subsonic WOOOP whenever you fire
                //LoadAssets.sfxDictionary.TryGetValue("subsonicsfx.wav", out ac);
                //audios.PlayOneShot(ac);

            }
            catch (Exception e)
            {
                Log("HP_AudioHandler.cs, cannot find the SFX " + gunName + " " + e);
            }
        }

        public static void PlayGunSounds(string gunName, float pitch)
        {

            try
            {
                LoadAssets.sfxDictionary.TryGetValue("shoot_sfx_" + gunName.ToLower() + ".wav", out AudioClip ac);
                AudioSource audios = shootSFX.GetComponent<AudioSource>();
                audios.clip = ac;
                audios.pitch = pitch;
                audios.PlayOneShot(audios.clip, GameManager.instance.GetImplicitCinematicVolume());

                //Play subsonic WOOOP whenever you fire
                //LoadAssets.sfxDictionary.TryGetValue("subsonicsfx.wav", out ac);
                //audios.PlayOneShot(ac);

            }
            catch (Exception e)
            {
                Log("HP_AudioHandler.cs, cannot find the SFX " + gunName + " " + e);
            }
        }

        public static void PlaySoundsMisc(string soundName, float? pitch = null)
        {
            try
            {
                //HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.enemyHurtSFX[soundRandom.Next(0, 2)]);
                LoadAssets.sfxDictionary.TryGetValue(soundName + ".wav", out AudioClip ac);
                AudioSource audios = emptyGunSFX.GetComponent<AudioSource>();

                audios.clip = ac;
                audios.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
                if (pitch != null) audios.pitch = (float)pitch;

                audios.PlayOneShot(audios.clip, GameManager.instance.GetImplicitCinematicVolume());
            }
            catch (Exception e)
            {
                Log("HP_AudioHandler.cs, cannot find the SFX " + soundName + " " + e);
            }
        }
    }
}
