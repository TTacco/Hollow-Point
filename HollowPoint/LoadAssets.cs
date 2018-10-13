using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace HollowPoint
{
    public static class LoadAssets
    {
        public static AudioClip bulletSoundFX;
        public static AudioClip[] airStrikeSoundFX = new AudioClip[3];
        public static AudioClip[] enemyHurtSFX = new AudioClip[3];
        public static AudioClip[] enemyDeadSFX = new AudioClip[3];

        public static Texture2D gunSprite;
        public static Texture2D bulletSprite;
        public static Texture2D flash;
        public static Texture2D muzzleFlash;

        public static Dictionary<string, AudioClip> AudioDictionary = new Dictionary<string, AudioClip>();

        public static void LoadBulletSounds()
        {
            int enemyHurtCount = 0;
            int airsupportCount = 0;
            int enemyDeadCount = 0;

            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (res.EndsWith(".wav"))
                {
                    Modding.Logger.Log("[HOLLOW POINT] Found sound effect! Saving it. [NAME]: " + res);
                    Stream audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
                    if(audioStream != null && res.Contains("support"))
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        airStrikeSoundFX[airsupportCount++] = WavUtility.ToAudioClip(buffer);
                        
                    }
                    else if (audioStream != null && res.Contains("enemyhurt"))
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        enemyHurtSFX[enemyHurtCount++] = WavUtility.ToAudioClip(buffer);
                    }
                    else if (audioStream != null && res.Contains("enemydead"))
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        enemyDeadSFX[enemyDeadCount++] = WavUtility.ToAudioClip(buffer);
                    }
                    else if (audioStream != null)
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        bulletSoundFX = WavUtility.ToAudioClip(buffer);
                    }
                } else if (res.EndsWith(".png"))
                {
                    using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
                    {
                        if (s == null) continue;
                        byte[] buffer = new byte[s.Length];
                        s.Read(buffer, 0, buffer.Length);
                        s.Dispose();
                        //Create texture from bytes 
                        if (res.Contains("bullet"))
                        {
                            bulletSprite = new Texture2D(1, 1);
                            bulletSprite.LoadImage(buffer);
                            bulletSprite.Apply();
                        }
                        else if(res.Contains("Rifle"))
                        {
                            gunSprite = new Texture2D(1, 1);
                            gunSprite.LoadImage(buffer);
                            gunSprite.Apply();
                        }
                        else if (res.Contains("glow"))
                        {
                            flash = new Texture2D(1, 1);
                            flash.LoadImage(buffer);
                            flash.Apply();
                        }
                        else if (res.Contains("muzzleflash"))
                        {
                            muzzleFlash = new Texture2D(1, 1);
                            muzzleFlash.LoadImage(buffer);
                            muzzleFlash.Apply();
                        }
                        Modding.Logger.Log("[HOLLOW POINT] Created sprite from embedded image: " + res);
                    }
                }
            }
        }
        

    }
}