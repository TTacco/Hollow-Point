using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using GlobalEnums;
using static Modding.Logger;


namespace HollowPoint
{
    class HP_Sprites : MonoBehaviour
    {
        public static GameObject gunSpriteGO;
        public static GameObject flashSpriteGO;
        public static GameObject muzzleFlashGO;

        System.Random shakeNum = new System.Random();
        static private Vector3 defaultWeaponPos = new Vector3(-0.2f, -0.84f, -0.0001f);

        int rotationNum = 0;

        public static float lowerGunTimer = 0;
        float spriteRecoilHeight;
        float spriteSprintDropdownHeight;

        public static bool isFiring = false;
        public static bool startShake = false;
        bool isSprinting = false;
        bool dropDown = false;

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
            //gunSpriteGO.transform.parent = HeroController.instance.spellControl.gameObject.transform;
            gunSpriteGO.transform.parent = HeroController.instance.transform;
            gunSpriteGO.transform.position = HeroController.instance.transform.position;
            gunSpriteGO.transform.localPosition = new Vector3(-0.2f, -0.85f, -0.0001f);
            gunSpriteGO.SetActive(true);

            flashSpriteGO = new GameObject("HollowPointFlashSprite", typeof(SpriteRenderer), typeof(FlashSpriteRenderer));
            flashSpriteGO.transform.parent = HeroController.instance.transform;
            flashSpriteGO.transform.localPosition = new Vector3(0f, 0f, -5f);
            flashSpriteGO.SetActive(false);

            muzzleFlashGO = new GameObject("HollowPointMuzzleSprite", typeof(SpriteRenderer), typeof(MuzzleSpriteRenderer));
            muzzleFlashGO.transform.parent = gunSpriteGO.transform;
            muzzleFlashGO.transform.localPosition = new Vector3(-1.9f, 0, -0.3f);
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
            if (isFiring) //If the player fires, make it so that they put the gun at a straight angle, otherwise make the gun lower
            {
                StopCoroutine("SprintingShake");
                lowerGunTimer -= Time.deltaTime;
                gunSpriteGO.transform.SetRotationZ(SpriteRotation() * -1); //Point gun at the direction you are shooting

                if (lowerGunTimer < 0)
                {
                    isFiring = false;
                    isSprinting = false;
                    Log("Done firing");
                }
            }
            else if (HeroController.instance.hero_state == ActorStates.running && !isFiring) //Shake gun a bit while moving
            {
                // gunSpriteGO.transform.SetRotationZ(25); 
                if (!isSprinting && HP_Handler.gunActive) //This bool check prevents the couroutine from running multiple times
                {
                    StartCoroutine("SprintingShake");
                    isSprinting = true;
                }
            }
            else if (!isFiring)
            {
                isSprinting = false;
                StopCoroutine("SprintingShake");
                gunSpriteGO.transform.localPosition = defaultWeaponPos;
                gunSpriteGO.transform.SetRotationZ(20);
            }
        }

        void WeaponBehindBack()
        {
            if (HP_Handler.gunOverheat || !HP_Handler.gunActive)
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

        IEnumerator SprintingShake()
        {
            while (true)
            {
                //Vector3(-0.2f, -0.81f, -0.0001f);

                if (dropDown)
                {
                    //spriteSprintDropdownHeight = -.12f;
                    //gunSpriteGO.transform.localPosition = defaultWeaponPos + new Vector3(0, spriteSprintDropdownHeight, 0);
                    //dropDown = !dropDown;

                    while (spriteSprintDropdownHeight > -0.12f)
                    {
                        yield return new WaitForSeconds(0.07f);
                        spriteSprintDropdownHeight -= 0.09f;
                        gunSpriteGO.transform.SetRotationZ(shakeNum.Next(15, 24));
                        gunSpriteGO.transform.localPosition = defaultWeaponPos + new Vector3(0, spriteSprintDropdownHeight, 0);
                    }
                    dropDown = !dropDown;
                }
                else if (!dropDown)
                {
                    while (spriteSprintDropdownHeight < -0.06f)
                    {
                        yield return new WaitForSeconds(0.07f);
                        spriteSprintDropdownHeight += 0.06f;
                        gunSpriteGO.transform.SetRotationZ(shakeNum.Next(17, 27));
                        gunSpriteGO.transform.localPosition = defaultWeaponPos + new Vector3(0, spriteSprintDropdownHeight, 0);
                    }
                    dropDown = !dropDown;
                }

            }
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
            spriteRecoilHeight = -0.60f; //-0.53 the lower this is the lower the gun moves during recoil (NOTE THAT THIS IS IN NEGATIVE, -0.20 is greater than -0.50, ttacco you fucking moron
            //gunSpriteGO.transform.localPosition = defaultWeaponPos + new Vector3(0.07f, 0.10f, -0.0000001f);
            gunSpriteGO.transform.SetRotationZ(15);

            do
            {
                spriteRecoilHeight -= 0.01f;
                gunSpriteGO.transform.localPosition = new Vector3(0f, spriteRecoilHeight, -0.0001f);
                yield return new WaitForEndOfFrame();
            }
            while (spriteRecoilHeight > -0.84);

            //-0.2f, -0.85f, -0.0001f
            spriteRecoilHeight = 0;
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
                return -90;
            }

            if (InputHandler.Instance.inputActions.up.IsPressed)
            {
                if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                {
                    return 45;
                }
            }
            else if (InputHandler.Instance.inputActions.down.IsPressed)
            {
                if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                {
                    return -45;
                }
            }


            return 0;
        }

    }

    #region SeperateRendererComponents

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
