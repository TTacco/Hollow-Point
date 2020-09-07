using System.Collections;
using UnityEngine;
using System;
using static Modding.Logger;
using static UnityEngine.Random;
using System.Collections.Generic;

namespace HollowPoint
{
    public class AudioHandler : MonoBehaviour
    {
        public static AudioHandler instance;

        /*
        static GameObject emptyGunSFX;
        static GameObject shootSFX;
        static GameObject enemyHitSFX;
        static GameObject terrainHitSFX;
        static GameObject infusionSFX;
        */

        Dictionary<string, GameObject> sfxGameObjectDictionary = new Dictionary<string, GameObject>();
        public enum HollowPointSoundType
        {
            ShootSFXGO,
            EnemyHitSFXGO,
            EnemyKillSFXGO,
            TerrainHitSFXGO,
            ClickSFXGO,
            InfusionSFXGO,
            DiveDetonateSFXGO,
            MortarWhistleSFXGO,
            MortarExplosionSFXGO,
            DrawAndHolsterGun
        }

        public void Awake()
        {
            if (instance == null) instance = this;

            StartCoroutine(AudioHandlerInit());
        }

        public IEnumerator AudioHandlerInit()
        {
            foreach (HollowPointSoundType enumName in (HollowPointSoundType[])Enum.GetValues(typeof(HollowPointSoundType)))
            {
                string goName = enumName.ToString();
                GameObject sgo = new GameObject(goName, typeof(AudioSource));
                sfxGameObjectDictionary.Add(goName, sgo);
                DontDestroyOnLoad(sgo);
            }

            yield return null;
        }

        public void PlayGunSoundEffect(string gunName)
        {
            try
            {
                LoadAssets.sfxDictionary.TryGetValue("shoot_sfx_" + gunName.ToLower() + ".wav", out AudioClip ac);
                AudioSource audios = sfxGameObjectDictionary["ShootSFXGO"].GetComponent<AudioSource>();
                audios.clip = ac;
                audios.pitch = Range(0.9f, 1.1f);
                audios.PlayOneShot(audios.clip, GameManager.instance.GetImplicitCinematicVolume());
            }
            catch (Exception e)
            {
                Log("HP_AudioHandler.cs, cannot find the SFX " + gunName + " " + e);
            }
        }

        public void PlayMiscSoundEffect(HollowPointSoundType hpst, bool alteredPitch = true)
        {
            try
            {
                string soundName = "";
                switch (hpst)
                {
                    case HollowPointSoundType.EnemyHitSFXGO:
                        soundName = "enemyhit" + Range(1, 6);
                        break;
                    case HollowPointSoundType.EnemyKillSFXGO:
                        soundName = "enemydead" + Range(1, 3);
                        break;
                    case HollowPointSoundType.TerrainHitSFXGO:
                        soundName = "impact_0" + Range(1, 5);
                        break;
                    case HollowPointSoundType.ClickSFXGO:
                        soundName = "cantfire";
                        break;
                    case HollowPointSoundType.InfusionSFXGO:
                        soundName = "infusionsound";
                        break;
                    case HollowPointSoundType.DiveDetonateSFXGO:
                        soundName = "divedetonate";
                        break;
                    case HollowPointSoundType.MortarWhistleSFXGO:
                        soundName = "mortarclose";
                        break;
                    case HollowPointSoundType.MortarExplosionSFXGO:
                        soundName = "mortarexplosion";
                        break;
                    default:
                        Log(hpst.ToString() + " Sound Enum Not Yet Implemented Please Fix This You Moron");
                        break;
                }

                LoadAssets.sfxDictionary.TryGetValue(soundName + ".wav", out AudioClip ac);
                AudioSource audios = sfxGameObjectDictionary[hpst.ToString()].GetComponent<AudioSource>();

                audios.clip = ac;
                if (alteredPitch) audios.pitch = Range(0.85f, 1.15f);
                else audios.pitch = 1;
                audios.PlayOneShot(audios.clip, GameManager.instance.GetImplicitCinematicVolume());
            }
            catch (Exception e)
            {
                Log("HP_AudioHandler.cs, cannot find the SFX " + e);
            }
        }

        public void PlayDrawHolsterSound(string soundName)
        {
            LoadAssets.sfxDictionary.TryGetValue(soundName + ".wav", out AudioClip ac);
            AudioSource audios = sfxGameObjectDictionary["DrawAndHolsterGun"].GetComponent<AudioSource>();
        }
    }
}
