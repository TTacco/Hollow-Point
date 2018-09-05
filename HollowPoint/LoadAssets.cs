using System.IO;
using System.Reflection;
using UnityEngine;

namespace HollowPoint
{
    public static class LoadAssets
    {
        public static AudioClip bulletSoundFX;

        public static void LoadBulletSounds()
        {
            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (res.EndsWith(".wav"))
                {
                    Modding.Logger.Log("Found sound effect! Saving it.");
                    Stream audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
                    if (audioStream != null)
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        bulletSoundFX = WavUtility.ToAudioClip(buffer);
                    }
                }
            }
        }
        

    }
}