using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using UnityEngine;
using GlobalEnums;
using SetSpriteRendererSprite = HutongGames.PlayMaker.Actions.SetSpriteRendererSprite;
using static Modding.Logger;

using System.IO;


namespace HollowPoint
{
    class GunSpriteRenderer : MonoBehaviour
    {
        Sprite gunSprite;
        SpriteRenderer gunRenderer;

        public void Start()
        {
            StartCoroutine(CheckForInstance());
        }

        public IEnumerator CheckForInstance()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);

            Log("[HOLLOW POINT] Creating Sprite");


            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string res in asm.GetManifestResourceNames())
            {
                if (!res.EndsWith(".png"))
                {
                    //Steal 56's Lightbringer code :weary:
                    continue;
                }

                using (Stream s = asm.GetManifestResourceStream(res))
                {
                    if (s == null) continue;
                    byte[] buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Dispose();
                    //Create texture from bytes 
                    Texture2D tex = new Texture2D(1, 1);
                    tex.LoadImage(buffer);
                    //Create sprite from texture 
                    gunSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    Log("Created sprite from embedded image: " + res);
                }
            }

            if(gunSprite == null)
            {
                Log("Sprite empty");
            }

            gunRenderer = GameManager.instance.gameObject.GetComponent<SpriteRenderer>();


            if (gunRenderer== null)
            {
                Log("renderer Sprite empty");
            }

            gunRenderer.GetComponent<SpriteRenderer>().sprite = gunSprite;
        }

        public void Update()
        { 
            gunRenderer.transform.localPosition = HeroController.instance.transform.localPosition;
            gunRenderer.transform.position = HeroController.instance.transform.position;
        }

    }
}
