using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using GlobalEnums;
using static Modding.Logger;
using ModCommon.Util;
using System.Reflection;

namespace HollowPoint
{
    class HP_Sprites : MonoBehaviour
    {
        //Holds all the projectile sprites

        public static GameObject gunSpriteGO;
        public static GameObject flashSpriteGO;
        public static GameObject muzzleFlashGO;
        public static GameObject whiteFlashGO;

        System.Random shakeNum = new System.Random();
        static private Vector3 defaultWeaponPos = new Vector3(-0.2f, -0.84f, -0.0001f);

        int rotationNum = 0;

        public static float lowerGunTimer = 0;
        float spriteRecoilHeight = 0;
        float spriteSprintDropdownHeight = 0;

        public static bool isFiring = false;
        public static bool startFiringAnim = false;
        public static bool idleAnim = true;
        public static bool isWallClimbing = false;
        public static bool facingNorthFirstTime = false;

        public static GameObject transformSlave;
        public static Transform ts;

        bool isSprinting = false;
        bool? prevFaceRightVal;

        private tk2dSpriteAnimator tk2d = null;

        public void Start()
        {
            Log("[HOLLOW POINT] Intializing Weapon Sprites");
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


            try
            {
                prevFaceRightVal = HeroController.instance.cState.facingRight;

                gunSpriteGO = new GameObject("HollowPointGunSprite", typeof(SpriteRenderer), typeof(HP_GunSpriteRenderer), typeof(AudioSource));
                gunSpriteGO.transform.position = HeroController.instance.transform.position;
                gunSpriteGO.transform.localPosition = new Vector3(0, 0, 0);
                gunSpriteGO.SetActive(true);

                transformSlave = new GameObject("slaveTransform", typeof(Transform));
                ts = transformSlave.GetComponent<Transform>();
                //ts.transform.SetParent(HeroController.instance.transform);
                gunSpriteGO.transform.SetParent(ts);

                defaultWeaponPos = new Vector3(-0.2f, -0.84f, -0.0001f);
                shakeNum = new System.Random();

                whiteFlashGO = CreateGameObjectSprite("lightflash.png", "lightFlashGO", 30);
                muzzleFlashGO = CreateGameObjectSprite("muzzleflash.png", "bulletFadePrefabObject", 150);
            }
            catch (Exception e)
            {
                Log(e);
            }


            whiteFlashGO.SetActive(false);

            DontDestroyOnLoad(whiteFlashGO);
            DontDestroyOnLoad(transformSlave);
            DontDestroyOnLoad(gunSpriteGO);
            DontDestroyOnLoad(muzzleFlashGO);

            try
            {
                tk2d = HeroController.instance.GetComponent<tk2dSpriteAnimator>();
            }
            catch (Exception e)
            {
                Log(e);
            }
        }

        public static GameObject CreateGameObjectSprite(string spriteName, string gameObjectName, float pixelsPerUnit) 
        {
            LoadAssets.spriteDictionary.TryGetValue(spriteName, out Texture2D texture);
            GameObject gameObjectWithSprite = new GameObject(gameObjectName, typeof(SpriteRenderer));
            gameObjectWithSprite.GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);

            return gameObjectWithSprite;
        }

        public void LateUpdate()
        {
            //if (HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name.Contains("Sprint") && !AmmunitionControl.gunHeatBreak)
            isWallClimbing = HeroController.instance.cState.wallSliding;

            //Log(HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name);// ENTER = when the player enters
            //This just makes it so the gun is more stretched out on wherever the knight is facing, rather than staying in his center
            int directionMultiplier = (HeroController.instance.cState.facingRight) ? 1 : -1;

            //Make it so the gun stretches out more on the opposite if the player is wall sliding
            if (isWallClimbing) directionMultiplier *= -1;

            //fuck your standard naming conventions, if it works, it fucking works
            float howFarTheGunIsAwayFromTheKnightsBody = (HP_WeaponHandler.currentGun.gunName == "Nail") ? 0.20f : 0.35f; //|| HP_HeatHandler.overheat
            float howHighTheGunIsAwayFromTheKnightsBody = (HP_WeaponHandler.currentGun.gunName == "Nail") ? -0.9f : -1.1f; // || HP_HeatHandler.overheat

            ts.transform.position = HeroController.instance.transform.position + new Vector3(howFarTheGunIsAwayFromTheKnightsBody * directionMultiplier, howHighTheGunIsAwayFromTheKnightsBody, -0.001f); ;
            //gunSpriteGO.transform.position = HeroController.instance.transform.position + new Vector3(0.2f * directionMultiplier, -1f, -0.001f);
            

            //:TODO: Tentative changes
            // gunSpriteGO.transform.localPosition = gunSpriteGO.transform.position + new Vector3(0.2f * directionMultiplier, -1f, -0.001f);
            defaultWeaponPos = gunSpriteGO.transform.position + new Vector3(0.2f * directionMultiplier, -1f, -0.001f);

            //flips the sprite on player face direction
            float x = gunSpriteGO.transform.eulerAngles.x;
            float y = gunSpriteGO.transform.eulerAngles.y;
            float z = gunSpriteGO.transform.eulerAngles.z;
            bool faceRight = HeroController.instance.cState.facingRight;

            if (isWallClimbing)
            {
                gunSpriteGO.transform.eulerAngles = (faceRight) ? new Vector3(x, 0, z) : new Vector3(x, 180, z);
            }
            else
            {
                gunSpriteGO.transform.eulerAngles = (faceRight) ? new Vector3(x, 180, z) : new Vector3(x, 0, z);
            }

            //the player starts shooting
            ShootAnim();
          
            //player starts running
            SprintAnim();

            //weapon in the back when the nail is the current active weapon
            WeaponBehindBack();
            /*
            Log("=============================================");
            Log("KNIGHT POSITION " + HeroController.instance.transform.position);
            Log("KNIGHT LOCAL POSITION" + HeroController.instance.transform.localPosition);
            
            Log("TS POSITION " + ts.position);
            Log("TS LOCAL POSITION" + ts.localPosition);
            

            Log("GUN POSITION " +gunSpriteGO.transform.position);
            Log("GUN LOCAL POSITION" +gunSpriteGO.transform.localPosition);
            */


        }

        public void ShootAnim()
        {
            if (startFiringAnim)
            {
                //Log("PLAYER IS NOW FIRING");
                isFiring = true;
                idleAnim = false;
                startFiringAnim = false;
                StartCoroutine(ShootAnimation(HP_DirectionHandler.finalDegreeDirection));
            }
        }

        
        public void SprintAnim()
        {
            //If the player is ducking and collecting, lower gun aim further

            if (isFiring) //If the player fires, make it so that they put the gun at a straight angle, otherwise make the gun lower
            {
                StopCoroutine("SprintingShake");
                lowerGunTimer -= Time.deltaTime;
                gunSpriteGO.transform.SetRotationZ(SpriteRotation() * -1); //Point gun at the direction you are shooting

                float gunHeightWhenPointingUpwards = (SpriteRotation() == 90 || SpriteRotation() == 45) ? 0.3f: 0f;

                //gunSpriteGO.transform.localPosition.x 
                gunSpriteGO.transform.localPosition = new Vector3(gunSpriteGO.transform.localPosition.x, 0.10f + gunHeightWhenPointingUpwards, -0.0001f); //0.10f * yMult

                if (lowerGunTimer < 0)
                {
                    isFiring = false;
                    isSprinting = false;
                    gunSpriteGO.transform.localPosition = new Vector3(0, 0.10f, -0.0001f);
                }
            }
            else if (HeroController.instance.hero_state == ActorStates.running && !isFiring) //Shake gun a bit while moving
            {
                // gunSpriteGO.transform.SetRotationZ(25); 
                if (!isSprinting) //This bool check prevents the couroutine from running multiple times && !HP_WeaponHandler.currentGun.gunName.Equals("Nail")
                {
                    StartCoroutine("SprintingShake");
                    StartCoroutine("SprintingShakeRotation");
                    isSprinting = true;
                }
            }
            //Idle animation/ Knight standing still
            else if (!isFiring)
            {
                isSprinting = false;
                StopCoroutine("SprintingShake");
                StopCoroutine("SprintingShakeRotation");
                //gunSpriteGO.transform.localPosition = defaultWeaponPos;

                if (BadStareDown())
                {
                    gunSpriteGO.transform.SetRotationZ(50);
                }
                else
                {
                    gunSpriteGO.transform.SetRotationZ(35);
                }

                gunSpriteGO.transform.localPosition = new Vector3(gunSpriteGO.transform.localPosition.x, 0, -0.001f);
            }
        }

        void WeaponBehindBack()
        {

            
            if (BadAnimFace())
            {
                gunSpriteGO.transform.SetPositionZ(0.01f);
            }

            else if (HP_WeaponHandler.currentGun.gunName == "Nail") //HP_HeatHandler.overheat
            {
                gunSpriteGO.transform.SetRotationZ(-34); //-23 
                gunSpriteGO.transform.SetPositionZ(0.01f);
                // gunSpriteGO.transform.localPosition = new Vector3(-0.01f, -0.84f, 0.0001f); 

                if (HeroController.instance.hero_state == ActorStates.running )
                {
                    gunSpriteGO.transform.SetRotationZ(-28); //-17
                }
            }
            else
            {
                gunSpriteGO.transform.localPosition = new Vector3(gunSpriteGO.transform.localPosition.x, gunSpriteGO.transform.localPosition.y, -0.0001f);
            }
        }


        //Player is facing the front
        bool BadAnimFace()
        {
            //Log(HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name); //ENTER = when the player enters
            string animName = tk2d.CurrentClip.name;


            if (!facingNorthFirstTime && animName.Contains("Enter"))
            {
                facingNorthFirstTime = true;
                //HeroController.instance.cState.facingRight = !HeroController.instance.cState.facingRight;
                if (HeroController.instance.cState.facingRight)
                {
                    HeroController.instance.FaceLeft();
                }
                else
                {
                    HeroController.instance.FaceRight();
                }
            }
            else if(facingNorthFirstTime && !animName.Contains("Enter"))
            {
                facingNorthFirstTime = false;
            }

            return (animName.Contains("Enter") || animName.Contains("Challenge") || animName.Contains("Prostrate"));
        }

        bool BadStareDown()
        {
            //Log(HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name); //ENTER = when the player enters
            string animName = tk2d.CurrentClip.name;

            return (animName.Contains("Collect Normal") || animName.Contains("RoarLock") || animName.Contains("SD"));
        }

        //================================ ANIMATION COROUTINES ======================================== 
        IEnumerator SprintingShake()
        {
            spriteSprintDropdownHeight = 0;

            while (true)
            {
                yield return new WaitForSeconds(0.02f);
                float y = Mathf.Sin(Time.time * 16)/100;
                //gunSpriteGO.transform.SetRotationZ(shakeNum.Next(15, 24));
                gunSpriteGO.transform.localPosition += new Vector3(0, y, 0);
            }
        }

        IEnumerator SprintingShakeRotation()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.082f);
                float y = Mathf.Sin(Time.time * 10) * 8;
                y += 24;
                gunSpriteGO.transform.SetRotationZ(y);
                //gunSpriteGO.transform.localPosition += new Vector3(0, y, 0);
            }
        }

        IEnumerator ShootAnimation(float degreeDirection)
        {
            float face = (HeroController.instance.cState.facingRight) ? 1 : -1;

            gunSpriteGO.transform.localPosition = new Vector3(-0.2f*face, gunSpriteGO.transform.localPosition.y, gunSpriteGO.transform.localPosition.z);
            gunSpriteGO.transform.SetRotationZ(gunSpriteGO.transform.rotation.z + shakeNum.Next(-5,6));
            yield return new WaitForSeconds(0.15f);

            // float faceX = (HeroController.instance.cState.facingRight) ? 0.1f : -0.1f;

            //float faceX = (degreeDirection >= 45 && degreeDirection <= 135)? 0.2f : 0f;
            gunSpriteGO.transform.localPosition = new Vector3(0, gunSpriteGO.transform.localPosition.y, gunSpriteGO.transform.localPosition.z);
        }

        //==================================Utilities==================================================

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

        public static void StartGunAnims()
        {
            startFiringAnim = true;
            isFiring = false;
            isFiring = true;
            lowerGunTimer = 0.4f;
        }

        public static void StartFlash()
        {
            GameObject flash = Instantiate(whiteFlashGO, HeroController.instance.transform.position + new Vector3(0, 0, -15), new Quaternion(0, 0, 0, 0));
            flash.SetActive(true);
            Destroy(flash, 0.1f);
        }

        public static void StartMuzzleFlash(float bulletDegreeDirection)
        {
            //MuzzleFlash Rotation and Spawn
            float degree = bulletDegreeDirection;
            float radian = (float)(degree * Math.PI / 180);

            float wallSlideOffset = (HeroController.instance.cState.wallSliding) ? -1 : 1;
            float flashOffsetX = (float)(wallSlideOffset * 1.6f * Math.Cos(radian));
            float flashOffsetY = (float)(1.6f * Math.Sin(radian));
            float muzzleFlashWallSlide = (HeroController.instance.cState.wallSliding && !HP_DirectionHandler.facingRight) ? 180 : 0;

            //If the player is aiming upwards or downwards, nudge the muzzle flash a bit because facing the right or left, the muzzle is a bit "forward"
            flashOffsetX += (bulletDegreeDirection == 90) ? (HP_DirectionHandler.facingRight) ? -0.2f : 0.2f : 0;

            //So if the player is firing forward or upwards while wall sliding, make it so to lower the muzzle flash so it doesnt look weird
            flashOffsetY += ((bulletDegreeDirection <= 180 && bulletDegreeDirection >= 0) && HeroController.instance.cState.wallSliding) ? -0.6f : 0;

   

            GameObject muzzleFlashClone = Instantiate(muzzleFlashGO, gunSpriteGO.transform.position + new Vector3(flashOffsetX, flashOffsetY + 0.3f, -1f), new Quaternion(0, 0, 0, 0));
            muzzleFlashClone.transform.Rotate(0, 0, bulletDegreeDirection + SpriteRotationWallSlide() + muzzleFlashWallSlide, 0);

            //muzzleFlashClone.transform.localPosition += new Vector3(0, 0, -2f);

            Destroy(muzzleFlashClone, 0.04f);
        }

        public static float SpriteRotationWallSlide()
        {
            if (HeroController.instance.cState.wallSliding)
            {
                if (InputHandler.Instance.inputActions.up.IsPressed)
                {
                    if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                    {
                        return 90;
                    }
                }
                else if (InputHandler.Instance.inputActions.down.IsPressed)
                {
                    if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                    {
                        return -90;
                    }
                }
            }
            return 0;
        }

        public void OnDestroy()
        { 
            Destroy(gameObject.GetComponent<HP_Sprites>());
            Destroy(gunSpriteGO);
            Destroy(whiteFlashGO);
            Destroy(transformSlave);

            Destroy(muzzleFlashGO);
        }

    }

    class HP_GunSpriteRenderer : MonoBehaviour
    {
        public static SpriteRenderer gunRenderer;
        public static Dictionary<String, Sprite> weaponSpriteDicitionary = new Dictionary<String, Sprite>();

        private const int PIXELS_PER_UNIT = 180;
        private tk2dSpriteAnimator tk2d = null; 

        public void Start()
        {
            gunRenderer = gameObject.GetComponent<SpriteRenderer>();

            LoadAssets.spriteDictionary.TryGetValue("Weapon_ShotgunSprite.png", out Texture2D rifleTextureInit);
            gunRenderer.sprite = Sprite.Create(rifleTextureInit,
                new Rect(0, 0, rifleTextureInit.width, rifleTextureInit.height),
                new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);

            foreach (KeyValuePair<String, Texture2D> wepTexture in LoadAssets.spriteDictionary)
            {
                if (wepTexture.Key.Contains("Weapon"))
                {
                   Texture2D texture = wepTexture.Value;
                   Sprite s = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);

                    weaponSpriteDicitionary.Add(wepTexture.Key, s);
                }
            }

            gunRenderer.color = Color.white;
            gunRenderer.enabled = true;

            try
            {
               tk2d = HeroController.instance.GetComponent<tk2dSpriteAnimator>();
            }
            catch (Exception e)
            {
                Log(e);
            }
        }

        public void Update()
        {
            try
            {
                if (MakeGunInvisibleCheck())
                {
                    gunRenderer.enabled = false;
                }
                else
                {
                    gunRenderer.enabled = true;
                }
            }
            catch (Exception e)
            {
                Log(e);
            }
        }

        bool MakeGunInvisibleCheck()
        {
            /*I am gonna say it now, this is probably singlehandedly one of the worst thing ive ever written in this mod, i have no idea where to
              find anim that handles getting new abilities and i am not gonna bother because its probably tucked into a hundred individual FSMs
              that i am not gonna waste my time in looking at, so unless this actually tanks performance, i dont care.
            */
            if(tk2d == null)
            {
                tk2d = HeroController.instance.GetComponent<tk2dSpriteAnimator>();
                return true;
            }

            string animName = tk2d.CurrentClip.name;

            return !HeroController.instance.CanInput() &&
            !HeroController.instance.cState.transitioning &&
            !animName.Contains("Enter") &&
            !animName.Contains("Challenge") &&
            !animName.Contains("Prostrate") && 
            !animName.Contains("Collect Normal") && 
            !animName.Contains("RoarLock") &&
            !animName.Contains("Super Hard Land") &&
            !animName.Contains("Wake Up") &&
            !animName.Contains("Sit") &&
            !animName.Contains("SD") &&
            !animName.Contains("Get Off") &&
            !animName.Contains("DN") &&
            !HeroController.instance.cState.isPaused;
        }

        public static void SwapWeapon(String weaponName)
        {
            if (weaponName.Equals("Nail")) return;
            try
            {
                weaponSpriteDicitionary.TryGetValue(weaponName, out Sprite swapTheCurrentGunSpriteWithThisOne);
                gunRenderer.sprite = swapTheCurrentGunSpriteWithThisOne;
            }
            catch(Exception e)
            {
                Log("No sprite with the name " + weaponName + " was found");
            }
        }

        public void OnDestroy()
        {
            Destroy(gunRenderer);
            Destroy(gameObject.GetComponent<HP_GunSpriteRenderer>());
        }
    }

}
