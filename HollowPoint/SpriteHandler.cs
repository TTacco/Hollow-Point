﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using GlobalEnums;
using static Modding.Logger;
using System.Reflection;
using static HollowPoint.HollowPointEnums;

namespace HollowPoint
{
    class HollowPointSprites : MonoBehaviour
    {
        //Holds all the projectile sprites

        public static GameObject gunSpriteGO;
        public static GameObject flashSpriteGO;
        public static GameObject muzzleFlashGO;
        public static GameObject muzzleFlashGOAlt;
        public static GameObject whiteFlashGO;

        static System.Random rand = new System.Random();
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
        public static bool altMuzzleflash = false;
        static bool lowerGunCoroutineActive = false;

        public static HeroActions ha;

        public static GameObject transformSlave;
        public static Transform ts;

        bool isSprinting = false;
        bool? prevFaceRightVal;

        private tk2dSpriteAnimator tk2d = null;

        public static MonoBehaviour instance;

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

            instance = this;

            ha = InputHandler.Instance.inputActions;

            try
            {
                prevFaceRightVal = HeroController.instance.cState.facingRight;

                gunSpriteGO = new GameObject("HollowPointGunSprite", typeof(SpriteRenderer), typeof(GunSpriteRenderer), typeof(AudioSource));
                gunSpriteGO.transform.position = HeroController.instance.transform.position;
                gunSpriteGO.transform.localPosition = new Vector3(0, 0, 0);
                gunSpriteGO.SetActive(true);

                transformSlave = new GameObject("slaveTransform", typeof(Transform));
                ts = transformSlave.GetComponent<Transform>();
                //ts.transform.SetParent(HeroController.instance.transform);
                gunSpriteGO.transform.SetParent(ts);

                defaultWeaponPos = new Vector3(-0.2f, -0.84f, -0.0001f);
                rand = new System.Random();

                whiteFlashGO = CreateGameObjectSprite("lightflash.png", "lightFlashGO", 65);
                muzzleFlashGO = CreateGameObjectSprite("muzzleflash.png", "bulletFadePrefabObject", 150);
                muzzleFlashGOAlt = CreateGameObjectSprite("muzzleflashAlt.png", "bulletFadePrefabObjectAlt", 150);
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
            DontDestroyOnLoad(muzzleFlashGOAlt);

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
            bool usingMelee = WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Melee;
            float howFarTheGunIsAwayFromTheKnightsBody = (usingMelee) ? 0.20f : 0.35f; //|| HP_HeatHandler.overheat
            float howHighTheGunIsAwayFromTheKnightsBody = (usingMelee) ? -0.9f : -1.1f; // || HP_HeatHandler.overheat


            if (tk2d.CurrentClip.name.Contains("Sit")) 
            {
                howFarTheGunIsAwayFromTheKnightsBody = 0;
                howHighTheGunIsAwayFromTheKnightsBody = (usingMelee)? -0.70f : -0.84f;
            }

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
                StartCoroutine(ShootAnimation(OrientationHandler.finalDegreeDirection));
            }
        }

        static float currentDegree = 0;

        public void SprintAnim()
        {
            //If the player is ducking and collecting, lower gun aim further

            if (isFiring) //If the player fires, make it so that they put the gun at a straight angle, otherwise make the gun lower
            {
                StopCoroutine("SprintingShake");
                StopCoroutine("SprintingShakeRotation");
                lowerGunTimer -= Time.deltaTime;

                currentDegree = Mathf.Lerp(currentDegree, SpriteRotation() * -1, Time.deltaTime * 28);
                gunSpriteGO.transform.SetRotationZ(currentDegree); //Point gun at the direction you are shooting
                float gunHeightWhenPointingUpwards = (SpriteRotation() == 90 || SpriteRotation() == 45) ? 0.3f: 0f;
                gunSpriteGO.transform.localPosition = new Vector3(gunSpriteGO.transform.localPosition.x, 0.10f + gunHeightWhenPointingUpwards, -0.0001f); //0.10f * yMult

                if (lowerGunTimer < 0)
                {
                    isFiring = false;
                    isSprinting = false;
                    gunSpriteGO.transform.localPosition = new Vector3(0, 0, -0.0001f);
                    currentDegree = 0;
                }
            }
            else if (HeroController.instance.hero_state == ActorStates.running && !isFiring) //Shake gun a bit while moving
            {
                // gunSpriteGO.transform.SetRotationZ(25); 
                if (!isSprinting) //This bool check prevents the couroutine from running multiple times 
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

                float lowerGunThreshold = (BadStareDown()) ? 50 : 43;
                currentDegree = Mathf.Lerp(currentDegree, lowerGunThreshold, Time.deltaTime * 28);
                gunSpriteGO.transform.SetRotationZ(currentDegree);
                gunSpriteGO.transform.localPosition = new Vector3(gunSpriteGO.transform.localPosition.x, 0, -0.001f);
            }
        }

        void WeaponBehindBack()
        {      
            if (BadAnimFace())
            {
                gunSpriteGO.transform.SetPositionZ(0.01f);
            }

            else if (WeaponSwapAndStatHandler.instance.currentWeapon == WeaponType.Melee) //HP_HeatHandler.overheat
            {
                Transform transform = gunSpriteGO.transform;
                transform.SetRotationZ(-45); //-23 
                transform.SetPositionZ(0.01f);

                if (HeroController.instance.hero_state == ActorStates.running )
                {
                    gunSpriteGO.transform.SetRotationZ(-34); //-17
                }
            }
            else
            {
                gunSpriteGO.transform.localPosition = new Vector3(gunSpriteGO.transform.localPosition.x, gunSpriteGO.transform.localPosition.y, -0.0001f);
            }
        }

        //Player is facing the front, not like, hes literally staring in front, like when they enter a room thats not either left or right
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
            //Log("[SPRITEHANDLER] STARTING SprintingShake()");
            while (true)
            {
                yield return new WaitForSeconds(0.02f);
                float y = Mathf.Sin(Time.time * 20)/100;
                //gunSpriteGO.transform.SetRotationZ(shakeNum.Next(15, 24));
                gunSpriteGO.transform.localPosition += new Vector3(0, y, 0);
            }
        }

        IEnumerator SprintingShakeRotation()
        {
            //Log("[SPRITEHANDLER] STARTING SprintingRotation()");

     
            float maxSprintDegree = 43; //40
            float minSprintDegree = 18; //10
            float medianDegree = (maxSprintDegree + minSprintDegree) / 2;
            while (true)
            {
                if(currentDegree < minSprintDegree || currentDegree > maxSprintDegree)
                {
                    //Rotation is out of range, lets reach the threshold for it first before using the sprint animator
                    currentDegree = Mathf.Lerp(currentDegree, medianDegree, Time.deltaTime * 40f);
                    continue;
                }
                yield return new WaitForSeconds(0.06f);
                currentDegree = Mathf.Lerp(minSprintDegree + 1, maxSprintDegree - 1, Mathf.PingPong(Time.time * 2.9f, 1f));
                gunSpriteGO.transform.SetRotationZ(currentDegree);
                //gunSpriteGO.transform.localPosition += new Vector3(0, y, 0);
                yield return null;
            }
        }

        IEnumerator ShootAnimation(float degreeDirection)
        {
            //Log("[SPRITEHANDLER] STARTING ShootAnimation()");
            float face = (HeroController.instance.cState.facingRight) ? 1 : -1;

            gunSpriteGO.transform.localPosition = new Vector3(-0.2f*face, gunSpriteGO.transform.localPosition.y, gunSpriteGO.transform.localPosition.z);
            //gunSpriteGO.transform.SetRotationZ(gunSpriteGO.transform.rotation.z + rand.Next(-7,8)); //-7 , 8
            yield return new WaitForSeconds(0.12f);

            // float faceX = (HeroController.instance.cState.facingRight) ? 0.1f : -0.1f;
            //float faceX = (degreeDirection >= 45 && degreeDirection <= 135)? 0.2f : 0f;
            gunSpriteGO.transform.localPosition = new Vector3(0, gunSpriteGO.transform.localPosition.y, gunSpriteGO.transform.localPosition.z);
        }

        //==================================Utilities==================================================
        //returns the degree of the gun's sprite depending on what the player inputs while shooting
        //basically it just rotates the gun based on shooting direction
        static float SpriteRotation()
        {
            float gunRotation = 0;
            if(!((ha.right.IsPressed || ha.left.IsPressed)) || Stats.instance.cardinalFiringMode)
            {
                gunRotation= (ha.up.IsPressed) ? 90 : (ha.down.IsPressed)? -90 : 0;
            }
            else if(ha.right.IsPressed || ha.left.IsPressed)
            {
                gunRotation = (ha.up.IsPressed) ? 45 : (ha.down.IsPressed) ? -45 : 0;
            }
            return gunRotation;// + rand.Next(-3,3);
        }

        public static void StartGunAnims()
        {
            if (lowerGunTimer > 0.51) return;
               
            startFiringAnim = true;
            isFiring = false;
            isFiring = true;
            lowerGunTimer = 0.55f;
        }

        public static void StartFlash()
        {
            GameObject flash = Instantiate(whiteFlashGO, HeroController.instance.transform.position + new Vector3(0, 0, -15), new Quaternion(0, 0, 0, 0));
            flash.SetActive(true);
            Destroy(flash, 0.1f);
        }

        public static void StartMuzzleFlash(float bulletDegreeDirection, float size)
        {
            //MuzzleFlash Rotation and Spawn
            float degree = bulletDegreeDirection;
            float radian = (float)(degree * Math.PI / 180);

            float wallSlideOffset = (HeroController.instance.cState.wallSliding) ? -1 : 1;
            float flashOffsetX = (float)(wallSlideOffset * 1.6f * Math.Cos(radian));
            float flashOffsetY = (float)(1.6f * Math.Sin(radian));
            //float muzzleFlashWallSlide = (HeroController.instance.cState.wallSliding && !OrientationHandler.facingRight) ? 180 : 0;

            if (HeroController.instance.cState.wallSliding)
            {
                degree = (HeroController.instance.cState.facingRight) ? ((degree <= 90 ? 180 : -180) - degree) : (180 - degree);
            }

            //If the player is aiming upwards or downwards, nudge the muzzle flash a bit because facing the right or left, the muzzle is a bit "forward"
            flashOffsetX += (bulletDegreeDirection == 90) ? (OrientationHandler.facingRight) ? -0.2f : 0.2f : 0;

            //So if the player is firing forward or upwards while wall sliding, make it so to lower the muzzle flash so it doesnt look weird
            flashOffsetY += ((bulletDegreeDirection <= 180 && bulletDegreeDirection >= 1) && HeroController.instance.cState.wallSliding) ? -0.6f : 0;

            Vector3 muzzleFlashSpawnPos = gunSpriteGO.transform.position + new Vector3(flashOffsetX, flashOffsetY + 0.3f, -1f);

            //Alternate between muzzle flashes
            altMuzzleflash = !altMuzzleflash;
            GameObject muzzleFlashToUse = null;
            if (altMuzzleflash) muzzleFlashToUse = muzzleFlashGO;
            else muzzleFlashToUse = muzzleFlashGOAlt;

            GameObject muzzleFlashClone = Instantiate(muzzleFlashToUse, muzzleFlashSpawnPos, new Quaternion(0, 0, 0, 0));
            muzzleFlashClone.transform.Rotate(0, 0, degree, 0);
            muzzleFlashClone.transform.localScale = new Vector3(size, size, 0.1f);

            //muzzleFlashClone.transform.localPosition += new Vector3(0, 0, -2f);

            Destroy(muzzleFlashClone, 0.04f);
            //instance.StartCoroutine(MuzzleSmoke(muzzleFlashClone)); Create a muzzle smoke
        }

        public static IEnumerator MuzzleSmoke(GameObject muzzleFlashSpawnPos)
        {
            ParticleSystem wallDust = Instantiate(HeroController.instance.wallslideDustPrefab);

            Destroy(wallDust, 0.75f);
            wallDust.transform.position = muzzleFlashSpawnPos.transform.position;
            wallDust.Emit(50);
            ParticleSystem.VelocityOverLifetimeModule v = wallDust.velocityOverLifetime;

            v.enabled = true;
            float rad = Mathf.Deg2Rad * (muzzleFlashSpawnPos.transform.eulerAngles.z);
            v.xMultiplier = 7.5f * Mathf.Cos(rad);
            v.yMultiplier = 7.5f * Mathf.Sin(rad);

            yield return new WaitForSeconds(0.3f);
            v.enabled = false;
        }

        public void OnDestroy()
        { 
            Destroy(gameObject.GetComponent<HollowPointSprites>());
            Destroy(gunSpriteGO);
            Destroy(whiteFlashGO);
            Destroy(transformSlave);

            Destroy(muzzleFlashGO);
            Destroy(muzzleFlashGOAlt);
        }

    }

    class GunSpriteRenderer : MonoBehaviour
    {
        public static SpriteRenderer gunRenderer;
        public static Dictionary<String, Sprite> weaponSpriteDictionary = new Dictionary<String, Sprite>();

        private const int PIXELS_PER_UNIT = 180;
        private tk2dSpriteAnimator tk2d = null; 

        public void Start()
        {
            gunRenderer = gameObject.GetComponent<SpriteRenderer>();
        

            LoadAssets.spriteDictionary.TryGetValue("weapon_sprite_rifle.png", out Texture2D rifleTextureInit);
            gunRenderer.sprite = Sprite.Create(rifleTextureInit,
                new Rect(0, 0, rifleTextureInit.width, rifleTextureInit.height),
                new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);

            foreach (KeyValuePair<String, Texture2D> wepTexture in LoadAssets.spriteDictionary)
            {
                if (wepTexture.Key.Contains("weapon"))
                {
                   Texture2D texture = wepTexture.Value;
                   Sprite s = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);

                    weaponSpriteDictionary.Add(wepTexture.Key, s);
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
            /*
            try
            {
                //gunRenderer.enabled = MakeGunVisibleCheck();
            }
            catch (Exception e)
            {
                Log(e);
            }
            */
        }

        string prevAnim = "";
        bool MakeGunVisibleCheck()
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

            //if (HeroController.instance.CanInput() || HeroController.instance.cState.transitioning) return true;

            bool makeGunVisible = !animName.Contains("Sit");

            return makeGunVisible;

            /*
            return
            animName.Contains("Enter") ||
            animName.Contains("Challenge") ||
            animName.Contains("Prostrate") ||
            animName.Contains("Collect Normal") ||
            animName.Contains("RoarLock") ||
            animName.Contains("Super Hard Land") ||
            animName.Contains("Wake Up") ||
            animName.Contains("Sit") ||
            animName.Contains("SD") ||
            animName.Contains("Get Off") ||
            animName.Contains("DN") ||
            HeroController.instance.cState.isPaused;
            */
        }

        public static void SwapWeapon(String weaponName)
        {
            if (weaponName.Equals("Nail")) return;
            try
            {
                weaponSpriteDictionary.TryGetValue(weaponName, out Sprite swapTheCurrentGunSpriteWithThisOne);
                gunRenderer.sprite = swapTheCurrentGunSpriteWithThisOne;
            }
            catch(Exception e)
            {
                Log("[SpriteHP_GunSpriteRenderer]No sprite with the name " + weaponName + " was found");
                Log("Exception: " + e);
            }
        }

        public void OnDestroy()
        {
            Destroy(gunRenderer);
            Destroy(gameObject.GetComponent<GunSpriteRenderer>());
        }
    }

}
