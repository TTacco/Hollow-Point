using System.Collections;
using UnityEngine;
using static Modding.Logger;


namespace HollowPoint
{
    class GunSpriteRenderer : MonoBehaviour
    {
        SpriteRenderer gunRenderer;
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
        }

        public void switchGuns(int gunNumber)
        {
            if (HeroController.instance == null || GameManager.instance == null)
            {
                Modding.Logger.Log("[Hollow Point] Unable to switch guns because hero not loaded.");
                return;
            }

            if (gunNumber >= LoadAssets.gunSprites.Length)
            {
                Modding.Logger.Log("[Hollow Point] Gun out of range! Gun loaded is " + gunNumber);
                return;
            }
            
            gunRenderer.sprite = Sprite.Create(LoadAssets.gunSprites[gunNumber],
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
