using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace HollowPoint
{
    public static class LoadAssets
    {
        public static Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Texture2D> spriteDictionary = new Dictionary<string, Texture2D>();

        public static Shader glow;

        public static Material perpetuaTMPMat;
        public static Texture2D perpetuaTMPTex;

        public static TMP_FontAsset perpetua;

        static AssetBundle fontAssetBundle;



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
                    //Modding.Logger.Log("[HOLLOW POINT] Created sound effect " + res);
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
                        //Modding.Logger.Log("[HOLLOW POINT] Created sprite from embedded image: " + restemp);
                    }
                }
                else if (res.EndsWith(".shader"))
                {
                    //Modding.Logger.Log(Path.Combine(Application.streamingAssetsPath, "glowshader.shader"));
                    //string path = "";//Path.Combine(Application.streamingAssetsPath, "/glowshader.shader");
                    //path = Application.streamingAssetsPath;
                    //path = path.Substring(0, path.Length - 1);
                    //path += "/glowshader";
                    //Modding.Logger.Log(path);
                    //Shader s = Resources.Load<Shader>(path);

                    //Modding.Logger.Log("Is the shader empty? " + (s == null));

                }
            }
        }

        public static void InitializeFont()
        {

            

            //AssetBundle ab = AssetBundle.LoadFromFile(Application.dataPath + "/Managed/Mods/fontasset");


            //string manualPath = "C:Program Files(x86)/Steam/SteamApps/common/Hollow Knight/hollow_knight_Data/Managed/Mods/fontassetbundle.assets";

            Modding.Logger.Log(Path.Combine(Application.streamingAssetsPath, "fontassetbundle.assets"));

            string dataPath = "C:\\Program Files(x86)\\Steam\\SteamApps\\common\\Hollow Knight\\hollow_knight_Data\\StreamingAssets\\fontassetbundle.assets";

            //fontAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "fontassetbundle.assets"));


            fontAssetBundle = AssetBundle.LoadFromFile(@"C:\Users\Personal\Desktop\MyFuckingAsssetBundle\fontassetbundle.assets");
            Modding.Logger.Log((fontAssetBundle != null) ? "SUCCESSFULLY LOADED THE ASSET BUNDLE" : "FAILED TO LOAD THE ASSET BUNDLE");

            //Object[] assets = fontAssetBundle.LoadAllAssets();
            //foreach (Object a in assets)
            //{
            //    Modding.Logger.Log("found asset with name " + a.name + " of type " + a.GetType());
            //}



            /*
            Object[] meme = fontAssetBundle.LoadAllAssets();
            foreach (Object m1 in meme)
            {
                Modding.Logger.Log("found asset with name " + m1.name + " of type " + m1.GetType());
            }
            */

            /*
            perpetua.AddFaceInfo(new FaceInfo()
            {
                Name = "Perpetua",
                PointSize = 262f,
                Scale = 0.6825f,
                CharacterCount = 77,
                LineHeight = 474.5f,
                Baseline = 0f,
                Ascender = 160.5f,
                CapHeight = 0,
                Descender = -52.4375f,
                CenterLine = 0f,
                SuperscriptOffset = 208.5f,
                SubscriptOffset = -38.890625f,
                SubSize = 0.5f,
                Underline = -38.890625f,
                UnderlineThickness = 13.048828f,
                Padding = 5f,
                TabWidth = 0f,
                AtlasWidth = 2048,
                AtlasHeight = 2048
            });
            */
        }
    }
}