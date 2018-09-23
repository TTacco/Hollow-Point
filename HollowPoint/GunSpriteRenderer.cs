using System.Collections;
using UnityEngine;
using static Modding.Logger;


namespace HollowPoint
{
    class GunSpriteRenderer : MonoBehaviour
    {
        static SpriteRenderer gunRenderer;
        private const int PIXELS_PER_UNIT = 50;

        public void Start()
        {
            gunRenderer = gameObject.GetComponent<SpriteRenderer>();
            Log("[HOLLOW POINT] Creating Sprite");
            gunRenderer.sprite = Sprite.Create(LoadAssets.gunSprites[0],
                new Rect(0, 0, LoadAssets.gunSprites[0].width, LoadAssets.gunSprites[0].height),
                new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);
            gunRenderer.color = Color.white;
            Log("[HOLLOW POINT] Created sprite!");
            gunRenderer.enabled = false;
        }

        public static void switchGuns(int gunNumber)
        {
         
            if (HeroController.instance == null || GameManager.instance == null)
            {
                Modding.Logger.Log("[Hollow Point] Unable to switch guns because hero not loaded.");
                return;
            }

            /*
            if (gunNumber >= LoadAssets.gunSprites.Length)
            {
                Modding.Logger.Log("[Hollow Point] Gun out of range! Gun loaded is " + gunNumber);
                return;
            }
            */

            if (gunNumber == 0)
            {
                Log("Player is wielding nail, cannot display weapons");
                gunRenderer.enabled = false;
                return;
            }

                gunRenderer.enabled = true;
                gunRenderer.sprite = Sprite.Create(LoadAssets.gunSprites[gunNumber-1],
                new Rect(0, 0, LoadAssets.gunSprites[0].width, LoadAssets.gunSprites[0].height),
                new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);
        }


        public void OnDestroy()
        {
            Destroy(gunRenderer);
            Destroy(this);
        }
    }
}
