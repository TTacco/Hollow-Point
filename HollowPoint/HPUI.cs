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
    class HPUI : MonoBehaviour
    {
        GameObject canvas;
        Text caliber;
        Text ammo;
        Text magazine;
        char bulletIcon = '\u204d';


        public void Start()
        {
            CanvasUtil.CreateFonts();
            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            UnityEngine.Object.DontDestroyOnLoad(canvas);
            caliber = CanvasUtil.CreateTextPanel(canvas, "", 25, TextAnchor.MiddleLeft, new CanvasUtil.RectData(new Vector2(600, 50), new Vector2(-560, 805), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f)), true).GetComponent<Text>();
            caliber.color = new Color(0.420f, 0.420f, 0.420f, 1f);
            caliber.text = "";

        }


        public void OnGUI()
        {
            //Current CALIBER
            caliber.text = "HEAT: " + AmmunitionControl.gunHeat + " %";
        }

        public void OnDestroy()
        {
            Destroy(gameObject.GetComponent<HPUI>());
            Destroy(canvas);
        }

    }
}
