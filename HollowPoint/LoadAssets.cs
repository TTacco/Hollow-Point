using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Xml;

namespace HollowPoint
{
    public static class LoadAssets
    {
        public static Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Texture2D> spriteDictionary = new Dictionary<string, Texture2D>();

        public static XmlDocument textChanges = new XmlDocument();

        static WAV wav_instance;
   

        public static void LoadResources()
        {
            //InitializeFont();


            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                //Modding.Logger.Log(res);         
                if (res.EndsWith(".wav"))
                {
                    Stream audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
                    if(audioStream != null)
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        string restemp = res.Replace("HollowPoint.assets.", "");
                        sfxDictionary.Add(restemp, WavUtility.ToAudioClip(buffer));
                    }
                    Modding.Logger.Log("[HOLLOW POINT] Created sound effect " + res);
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
                else if (res.EndsWith(".xml"))
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
                    {
                  
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            textChanges.LoadXml(sr.ReadToEnd());
                        }
                        
                    }

                }
            }
        }
    }
}