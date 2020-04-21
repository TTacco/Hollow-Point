using Modding;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using ModCommon;
using GlobalEnums;
using Modding.Menu;
using System.Collections;
using System.Collections.Generic;
using ModCommon.Util;
using System;
using TMPro;

namespace HollowPoint
{
    class HP_UIHandler : MonoBehaviour
    {
        GameObject canvas;
        CanvasGroup canvasGroup;
        Text grenadeAmountText;
        Text firesupportAmountText;

        static int artifactDisplayPowerPercent = 0;
        static int grenadeAmnt = 0;
        static float fadeOutTimer = 0f;
        float alpha = 0;

        public Image heatbarImage;
        public Image energybarImage;
        public Image heatbarImageEstimate;
        public GameObject heatbar_go;
        public GameObject heatbar_go_estimate;
        public GameObject heatbar_go_border;
        public GameObject energybar_go;
        public GameObject energybar_go_border;
        public void Start()
        {
            StartCoroutine(UI_Initializer());
        }

        public IEnumerator UI_Initializer()
        {
            while(HeroController.instance == null || PlayerData.instance == null)
            {
                yield return null;
            }


            try
            {
                CanvasUtil.CreateFonts();

                canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));

                canvasGroup = canvas.GetComponent<CanvasGroup>();
                canvas.GetComponent<Canvas>().sortingOrder = 1;

                grenadeAmountText = CanvasUtil.CreateTextPanel(canvas, "", 21, TextAnchor.MiddleLeft, new CanvasUtil.RectData(new Vector2(600, 50), new Vector2(-200, 898), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f)), true).GetComponent<Text>();
                grenadeAmountText.color = new Color(1f, 1f, 1f, 0f);
                grenadeAmountText.text = "";

                firesupportAmountText = CanvasUtil.CreateTextPanel(canvas, "", 21, TextAnchor.MiddleLeft, new CanvasUtil.RectData(new Vector2(600, 50), new Vector2(-200, 877), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f)), true).GetComponent<Text>();
                firesupportAmountText.color = new Color(1f, 1f, 1f, 0f);
                firesupportAmountText.text = "";

                /*
                LoadAssets.spriteDictionary.TryGetValue("heatbarsprite.png", out Texture2D bar);

                Sprite spriteMain = Sprite.Create(bar, new Rect(0, 0, bar.width, bar.height),
                new Vector2(0.5f, 0.5f), 25);

                LoadAssets.spriteDictionary.TryGetValue("heatbarspriteestimate.png", out Texture2D barEstimate);

                Sprite spriteEstimate = Sprite.Create(barEstimate, new Rect(0, 0, bar.width, bar.height),
                new Vector2(0.5f, 0.5f), 25);

                LoadAssets.spriteDictionary.TryGetValue("heatbarborder.png", out Texture2D barBorder);

                Sprite spriteBorder = Sprite.Create(barBorder, new Rect(0, 0, barBorder.width, barBorder.height),
                new Vector2(0.5f, 0.5f), 25);
                */
                //HEAT BAR
                /*
                CanvasUtil.RectData rectDataBorder = new CanvasUtil.RectData(new Vector2(275, 30), new Vector2(0, 0), new Vector2(0.12f, 0.70f), new Vector2(0.12f, 0.70f), new Vector2(0.50f, 0.50f));
                heatbar_go_border = CanvasUtil.CreateImagePanel(canvas, spriteBorder, rectDataBorder);

                CanvasUtil.RectData rectData = new CanvasUtil.RectData(new Vector2(175, 25), new Vector2(0, 0), new Vector2(0.12f, 0.70f), new Vector2(0.12f, 0.70f), new Vector2(0.50f, 0.50f));

                heatbar_go = CanvasUtil.CreateImagePanel(canvas, spriteMain, rectData);
                heatbar_go.transform.position = new Vector3(heatbar_go.transform.position.x, heatbar_go.transform.position.y, 0);
                heatbar_go_estimate = CanvasUtil.CreateImagePanel(canvas, spriteEstimate, rectData);
                heatbar_go_estimate.transform.position = new Vector3(heatbar_go.transform.position.x, heatbar_go.transform.position.y, 1);

                heatbarImage = heatbar_go.GetComponent<Image>();
                heatbarImage.type = Image.Type.Filled;
                heatbarImage.fillMethod = Image.FillMethod.Horizontal;
                heatbarImage.preserveAspect = false;

                heatbarImageEstimate = heatbar_go_estimate.GetComponent<Image>();
                heatbarImageEstimate.type = Image.Type.Filled;
                heatbarImageEstimate.fillMethod = Image.FillMethod.Horizontal;
                heatbarImageEstimate.preserveAspect = false;
                */


                //ENERGY BAR
                /*
                LoadAssets.spriteDictionary.TryGetValue("heatbarsprite.png", out Texture2D energybar);

                spriteMain = Sprite.Create(energybar, new Rect(0, 0, bar.width, bar.height),
                new Vector2(0.5f, 0.5f), 25);

                rectDataBorder = new CanvasUtil.RectData(new Vector2(275, 30), new Vector2(0, 0), new Vector2(0.12f, 0.67f), new Vector2(0.12f, 0.67f), new Vector2(0.50f, 0.50f));
                energybar_go_border = CanvasUtil.CreateImagePanel(canvas, spriteBorder, rectDataBorder);

                rectData = new CanvasUtil.RectData(new Vector2(175, 25), new Vector2(0, 0), new Vector2(0.12f, 0.67f), new Vector2(0.12f, 0.67f), new Vector2(0.50f, 0.50f));

                energybar_go = CanvasUtil.CreateImagePanel(canvas, spriteMain, rectData);
                energybar_go.transform.position = new Vector3(energybar_go.transform.position.x, energybar_go.transform.position.y, 0);

                energybarImage = energybar_go.GetComponent<Image>();
                energybarImage.type = Image.Type.Filled;
                energybarImage.fillMethod = Image.FillMethod.Horizontal;
                energybarImage.preserveAspect = false;
                */

                //HP_DamageNumber.damageNumberGO = new GameObject("damageNumberClone", typeof(HP_DamageNumber), typeof(TextMesh), typeof(MeshRenderer));
                DontDestroyOnLoad(canvas);
                DontDestroyOnLoad(canvasGroup);

            }
            catch (Exception e)
            {
                Modding.Logger.Log(e.StackTrace);
            }

        }

       

        public static void UpdateDisplay()
        {
            grenadeAmnt = HP_Stats.grenadeAmnt;
            artifactDisplayPowerPercent = HP_Stats.currentPrimaryAmmo;
            fadeOutTimer = 70f;
        }

        /*
        -- just commnted the gui for now, you can remove the rest. I don't know how muhc you want to keep.
        public void OnGUI()
        {
            grenadeAmountText.text =       "GAS  CHARGE  : " + grenadeAmnt + " x";
            firesupportAmountText.text =   "SHARD  POWER : " + artifactDisplayPowerPercent + " x";
            //heatbarImage.fillAmount = HP_HeatHandler.currentHeat/100;
            //energybarImage.fillAmount = HP_HeatHandler.currentEnergy/100;
        } 
        */

        void FixedUpdate()
        {

            if (fadeOutTimer > 0)
            {
                alpha = 1;
                //multiDamageDisplay.color = Color.white;
                grenadeAmountText.color = Color.white;
                firesupportAmountText.color = Color.white;

                fadeOutTimer -= 30 * Time.deltaTime;
            }
            else if (alpha > 0)
            {
                if (HeroController.instance.cState.transitioning) alpha = 0f;

                Color c = new Color(1, 1, 1, alpha);

                grenadeAmountText.color = c;
                firesupportAmountText.color = c;

                alpha -= 0.90f * Time.deltaTime;

                if(alpha < 0.03f)
                {
                    c = new Color(1, 1, 1, 0);
                    grenadeAmountText.color = c;
                    firesupportAmountText.color = c;
                }
            } 
        }

        public void OnDestroy()
        {
            Destroy(gameObject.GetComponent<HP_UIHandler>());
            Destroy(canvas);
        }

    }
        

    public class HP_DamageNumber : MonoBehaviour
    {
        public static GameObject damageNumberGO;
        public static GameObject damageNumberTestGO;
        Text t;
        Color c;
        MeshRenderer mr;
        TextMesh tm;

        public String damVal;

        public void Start()
        {
            try
            {
                //gameObject.transform.localScale = new Vector3(0.075f, 0.075f);
                gameObject.transform.localScale = new Vector3(1f, 1f);
                Font arial = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

                //Modding.Logger.Log(traj. + " vs " + arial.name);
                t = gameObject.GetComponent<Text>();
                t.font = arial;
                t.text = "wack";
                t.fontSize = 48;
                t.fontStyle = FontStyle.Normal;
                t.alignment = TextAnchor.MiddleCenter;
                t.material = new Material(Shader.Find("Particles/Additive"));
                t.color = new Color(255, 0, 0);

                //mr = gameObject.GetComponent<MeshRenderer>();
                //tm = gameObject.GetComponent<TextMesh>();
                //tm.font = HP_UIHandler.perpe;
                //tm.characterSize = 1f;
                //tm.alignment = TextAlignment.Center;
                //tm.fontSize = 150;
                //tm.text = damVal;
                //tm.fontStyle = FontStyle.Normal;
                //tm.color = c;
                //mr.material.color = new Color(255, 0, 0);
                //mr.material = new Material(Shader.Find("Particles/Additive"));

                //StartCoroutine(DamageAnimation());
            }
            catch (Exception e)
            {
                Modding.Logger.Log("EXCEPTION AT THE START");
                Modding.Logger.Log(e);
            }
        }

        public IEnumerator DamageAnimation()
        {

            float xDeviation = UnityEngine.Random.Range(-0.5f, 0.5f);
            float acceleration = UnityEngine.Random.Range(0.15f, 0.25f);
            do
            {
                yield return new WaitForSeconds(0.03f);
                gameObject.transform.position += new Vector3(xDeviation * 0.25f, acceleration ,0);
                acceleration -= 0.03f;
            }
            while (acceleration > 0);

            StartCoroutine(DamageTextFadeOut());
        }

        public IEnumerator DamageTextFadeOut()
        {
            float alpha = tm.color.a;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f)
            {
                Color newColor = new Color(tm.color.r, tm.color.g, tm.color.b, Mathf.Lerp(alpha, 0, t));
                tm.color = newColor;
                yield return null;
            }
            Destroy(gameObject);
        }

        public static void ShowDamageNumbers(string damage, HealthManager target, Color c)
        {

            return;
            try
            {
                if (damageNumberGO == null)
                {
                    //damageNumberGO = new GameObject("damageNumberClone", typeof(HP_DamageNumber), typeof(TextMesh), typeof(MeshRenderer));
                    //damageNumberGO = new GameObject("damageNumberClone", typeof(HP_DamageNumber), typeof(TextMesh), typeof(MeshRenderer));
                    damageNumberGO = new GameObject("damageNumberClone", typeof(HP_DamageNumber), typeof(Text), typeof(CanvasRenderer), typeof(RectTransform), typeof(Canvas));

                    DontDestroyOnLoad(damageNumberGO);
                }

                GameObject indicator = Instantiate(damageNumberGO, target.transform.position + new Vector3(0, 2, -2), new Quaternion(0, 0, 0, 0));

                HP_DamageNumber indicatorComponent = indicator.GetComponent<HP_DamageNumber>();
                CanvasRenderer cr = indicator.GetComponent<CanvasRenderer>();
                Canvas canva = indicator.GetComponent<Canvas>();
                RectTransform rt = indicator.GetComponent<RectTransform>();

                indicatorComponent.damVal = "" + damage;
                indicatorComponent.c = c;

                canva.enabled = true;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(275, 30);
                rt.position = target.transform.position + new Vector3(0, 2, -2);

            }
            catch (Exception e)
            {
                Modding.Logger.Log("EXCEPTION AT THE DAMAGE INIT");
                Modding.Logger.Log(e);
            }
        }
    }
}
