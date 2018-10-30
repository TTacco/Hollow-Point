using System.Collections;
using UnityEngine;
using GlobalEnums;
using static Modding.Logger;


namespace HollowPoint
{
    class GunSpriteController : MonoBehaviour
    {
        public static GameObject gunSpriteGO;
        GameObject flashSpriteGO;
        GameObject muzzleFlashGO;

        System.Random shakeNum = new System.Random();
        static private Vector3 defaultWeaponPos = new Vector3(-0.2f, -0.81f, -0.0001f);

        float recoiler;
        public static bool startShake = false;

        bool walkWeaponShake = true;

        public void Start()
        {
            Log("[HOLLOW POINT] Creating Sprite");

            StartCoroutine(SpriteRoutine());
        }

        //Initalizes the sprite game objects
        IEnumerator SpriteRoutine()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);

            gunSpriteGO = new GameObject("HollowPointGunSprite", typeof(SpriteRenderer), typeof(GunSpriteRenderer));
            gunSpriteGO.transform.parent = HeroController.instance.spellControl.gameObject.transform;
            gunSpriteGO.transform.localPosition = new Vector3(-0.2f, -0.85f, -0.0001f);
            gunSpriteGO.SetActive(true);

            flashSpriteGO = new GameObject("HollowPointFlashSprite", typeof(SpriteRenderer), typeof(FlashSpriteRenderer));
            flashSpriteGO.transform.parent = HeroController.instance.spellControl.gameObject.transform;
            flashSpriteGO.transform.localPosition = new Vector3(0f, 0f, -5f);
            flashSpriteGO.SetActive(false);

            muzzleFlashGO = new GameObject("HollowPointMuzzleSprite", typeof(SpriteRenderer), typeof(MuzzleSpriteRenderer));
            muzzleFlashGO.transform.parent = gunSpriteGO.transform;
            muzzleFlashGO.transform.localPosition = new Vector3(-1.9f, 0, -2f);
            muzzleFlashGO.SetActive(false);

            StartCoroutine(StartFlash());
        }

        public void Update()
        {
            /*
            if (HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name.Contains("Sprint") && !AmmunitionControl.gunHeatBreak)
            */
            RecoilWeaponShake();
            SprintWeaponShake();
            WeaponBehindBack();
        }

        void RecoilWeaponShake()
        {
            if (startShake)
            {
                startShake = false;
                StartCoroutine(StartFlash());
                StartCoroutine(GunRecoilAnimation());
            }
        }

        void SprintWeaponShake()
        {
            //If the player fires, make it so that they put the gun at a straight angle, otherwise make the gun lower
            if (AmmunitionControl.firing)
            {
                AmmunitionControl.lowerGunTimer -= Time.deltaTime;
                //Point gun at the direction you are shooting
                gunSpriteGO.transform.SetRotationZ(SpriteRotation()*-1);

                if (AmmunitionControl.lowerGunTimer < 0)
                {
                    AmmunitionControl.firing = false;
                }
            }
            else if (HeroController.instance.hero_state == ActorStates.running && !AmmunitionControl.firing)
            {
                gunSpriteGO.transform.SetRotationZ(25);
            }
            else if (!AmmunitionControl.firing)
            {
                gunSpriteGO.transform.SetRotationZ(0);
            }
        }

        void WeaponBehindBack()
        {
            if (AmmunitionControl.gunHeatBreak || !AmmunitionControl.gunIsActive)
            {
                gunSpriteGO.transform.SetRotationZ(-23); // 23
                gunSpriteGO.transform.localPosition = new Vector3(-0.07f, -0.84f, 0.0001f);
               // gunSpriteGO.transform.localPosition = new Vector3(-0.01f, -0.84f, 0.0001f);

                if (HeroController.instance.hero_state == ActorStates.running)
                {
                    gunSpriteGO.transform.SetRotationZ(-17);
                }
            }
        }

        public static void DefaultWeaponPos()
        {
            gunSpriteGO.transform.localPosition = defaultWeaponPos;
        }


        IEnumerator StartFlash()
        {
            flashSpriteGO.SetActive(true);
            muzzleFlashGO.SetActive(true);
            yield return new WaitForSeconds(0.05f);
            flashSpriteGO.SetActive(false);
            muzzleFlashGO.SetActive(false);
        }

        IEnumerator GunRecoilAnimation()
        {
            recoiler = -0.53f;
            //gunSpriteGO.transform.localPosition = defaultWeaponPos + new Vector3(0.07f, 0.10f, -0.0000001f);
            gunSpriteGO.transform.SetRotationZ(15);

            do
            {
                recoiler -= 0.01f;
                gunSpriteGO.transform.localPosition = new Vector3(0f, recoiler, -0.0001f);
                yield return new WaitForEndOfFrame();
            }
            while (recoiler > -0.84);

            //-0.2f, -0.85f, -0.0001f
            recoiler = 0;
            gunSpriteGO.transform.localPosition = defaultWeaponPos;
            gunSpriteGO.transform.SetRotationZ(0);

            yield return null;
        }

        //returns the degree of the gun's sprite depending on what the player inputs while shooting
        //basically it just rotates the gun based on shooting direction
        static float SpriteRotation()
        {
            if (InputHandler.Instance.inputActions.up.IsPressed && !(InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed))
            {
                return 90;
            }

            if (InputHandler.Instance.inputActions.down.IsPressed && !(InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed))
            {
                return-90;
            }

            if (InputHandler.Instance.inputActions.up.IsPressed)
            {
                if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                {
                    return 45;
                }
            }
            else if(InputHandler.Instance.inputActions.down.IsPressed)
            {
                if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                {
                    return -45;
                }
            }


            return 0;
        }

    }

    #region SeperateRentererComponents

    class GunSpriteRenderer : MonoBehaviour
    {
        static SpriteRenderer gunRenderer;
        private const int PIXELS_PER_UNIT = 50;

        public void Start()
        {
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


    class FlashSpriteRenderer : MonoBehaviour
    {
        static SpriteRenderer flashRenderer;
        private const int PIXELS_PER_UNIT = 60;

        public void Start()
        {
            flashRenderer = gameObject.GetComponent<SpriteRenderer>();
            flashRenderer.sprite = Sprite.Create(LoadAssets.flash,
                new Rect(0, 0, LoadAssets.flash.width, LoadAssets.flash.height),
                new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);
            flashRenderer.color = Color.white;
            flashRenderer.enabled = true;
        }

        public void OnDestroy()
        {
            Destroy(flashRenderer);
            Destroy(this);
        }
    }

    class MuzzleSpriteRenderer : MonoBehaviour
    {
        static SpriteRenderer muzzleFlashRenderer;
        private const int PIXELS_PER_UNIT = 220;

        public void Start()
        {
            muzzleFlashRenderer = gameObject.GetComponent<SpriteRenderer>();
            muzzleFlashRenderer.sprite = Sprite.Create(LoadAssets.muzzleFlash,
                new Rect(0, 0, LoadAssets.muzzleFlash.width, LoadAssets.muzzleFlash.height),
                new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);
            muzzleFlashRenderer.color = Color.white;
            muzzleFlashRenderer.enabled = true;
        }

        public void OnDestroy()
        {
            Destroy(muzzleFlashRenderer);
            Destroy(this);
        }
    }


    #endregion
}
