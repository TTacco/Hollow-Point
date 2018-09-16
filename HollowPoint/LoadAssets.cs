using System.IO;
using System.Reflection;
using UnityEngine;

namespace HollowPoint
{
    public static class LoadAssets
    {
        public static AudioClip[] bulletSoundFX = new AudioClip[5];
        public static AudioClip[] airStrikeSoundFX = new AudioClip[3];

        public static void LoadBulletSounds()
        {
            int count = 0;
            int aircount = 0;
            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (res.EndsWith(".wav"))
                {
                    Modding.Logger.Log("[HOLLOW POINT] Found sound effect! Saving it.");
                    Stream audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
                    if(audioStream != null && res.Contains("support"))
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        airStrikeSoundFX[aircount++] = WavUtility.ToAudioClip(buffer);
                    }
                    else if (audioStream != null)
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        bulletSoundFX[count++] = WavUtility.ToAudioClip(buffer);
                    }
                }
            }
        }
        

    }
}