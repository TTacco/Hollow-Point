using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HollowPoint
{
    public static class LoadAssets
    {
        public static Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Texture2D> spriteDictionary = new Dictionary<string, Texture2D>();

        public static void LoadBulletSounds()
        {
            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (res.EndsWith(".wav"))
                {
                    Modding.Logger.Log("[HOLLOW POINT] Found sound effect! Saving it. [NAME]: " + res);
                    Stream audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
                    if(audioStream != null)
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        string restemp = res.Replace("HollowPoint.assets.", "");
                        sfxDictionary.Add(restemp, WavUtility.ToAudioClip(buffer));
                    }
                }
                else if (res.EndsWith(".png"))
                {
                    using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
                    {
                        if (s == null) continue;
                        byte[] buffer = new byte[s.Length];
                        s.Read(buffer, 0, buffer.Length);
                        s.Dispose();
                        string restemp = res.Replace("HollowPoint.assets.", "");
                        Texture2D currSprite;
                        currSprite = new Texture2D(1, 1);
                        currSprite.LoadImage(buffer);
                        currSprite.Apply();
                        spriteDictionary.Add(restemp, currSprite);
                        Modding.Logger.Log("[HOLLOW POINT] Created sprite from embedded image: " + restemp);
                    }
                }
            }
        }


    }
}