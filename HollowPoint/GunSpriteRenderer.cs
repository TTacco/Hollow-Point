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
            Log("[HOLLOW POINT] Creating Sprite");

            gunRenderer = gameObject.GetComponent<SpriteRenderer>();
            gunRenderer.sprite = Sprite.Create(LoadAssets.gunSprite,
                new Rect(0, 0, LoadAssets.gunSprite.width, LoadAssets.gunSprite.height),
                new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);
            gunRenderer.color = Color.white;
            gunRenderer.enabled = true;
        }

        public void OnDestroy()
        {
            Destroy(gunRenderer);
            Destroy(this);
        }
    }
}
