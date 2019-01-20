using Modding;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using ModCommon;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using GlobalEnums;
using Modding.Menu;
using System.Collections;
using ModCommon.Util;
using System;

namespace HollowPoint
{
    class HP_UIHandler : MonoBehaviour
    {
        GameObject canvas;
        Text heatDisplay;
        Text gunActiveDisplay;

        public void Start()
        {
            CanvasUtil.CreateFonts();
            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));

            heatDisplay = CanvasUtil.CreateTextPanel(canvas, "", 25, TextAnchor.MiddleLeft, new CanvasUtil.RectData(new Vector2(600, 50), new Vector2(-530, 820), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f)), true).GetComponent<Text>();
            heatDisplay.color = new Color(1f, 1f, 1f, 1f);
            heatDisplay.text = "";

            gunActiveDisplay = CanvasUtil.CreateTextPanel(canvas, "", 25, TextAnchor.MiddleLeft, new CanvasUtil.RectData(new Vector2(600, 50), new Vector2(-530, 820), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f)), true).GetComponent<Text>();
            gunActiveDisplay.color = new Color(1f, 1f, 1f, 1f);
            gunActiveDisplay.text = "";



            DontDestroyOnLoad(canvas);
        }

        public void OnGUI()
        {
            if (HP_Handler.currentHeat >= 100)
            {
                HP_Handler.currentHeat = 100;
            }

            heatDisplay.text = "HEAT: " + (int) HP_Handler.currentHeat + " %";

        }

        public void OnDestroy()
        {
            Destroy(gameObject.GetComponent<HP_UIHandler>());
            Destroy(canvas);
        }

    }
}
