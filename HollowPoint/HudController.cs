using ModCommon;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

namespace HollowPoint
{
    internal class HudController : MonoBehaviour
    {
        /*
         * I am using this space to say that I hate TTaccoo#7358 for removing the grenades count.
         * He is no longer my hero. 😤
         * 
         * i cant believe Sid would do this i am literally shaking and crying right now
        */

        private GameObject directionalFireModeHudIcon;
        private GameObject adrenalineHudIcon;
        private GameObject canNailArtHudIcon;
        private Dictionary<string, Sprite> hudSpriteDictionary = new Dictionary<string, Sprite>();
        private readonly string[] textureNames = 
        {
            "hudicon_omni.png",
            "hudicon_cardinal.png",
            "hudicon_adrenaline0.png",
            "hudicon_adrenaline1.png",
            "hudicon_adrenaline2.png",
            "hudicon_adrenaline3.png",
            "hudicon_adrenaline4.png",
            "hudicon_adrenaline5.png",
            "hudicon_cancastnailart_true.png",
            "hudicon_cancastnailart_false.png"
        };

        void Start()
        {
            Modding.Logger.Log("INTIALIZING HUDCONTROLLER");
            var prefab = GameManager.instance.inventoryFSM.gameObject.FindGameObjectInChildren("Geo");
            var hudCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("HudCamera").FindGameObjectInChildren("Hud Canvas");

            foreach (var textureName in textureNames)
            {
                var hudIconTexture = LoadAssets.spriteDictionary[textureName];
                var hudIconSprite = Sprite.Create(hudIconTexture, new Rect(0, 0, hudIconTexture.width, hudIconTexture.height), new Vector2(0.5f, 0.5f));
                hudSpriteDictionary.Add(textureName, hudIconSprite);
            }

            //Modding.Logger.Log("did pepega");
            //you may change the name -----|                     
            adrenalineHudIcon = CreateStatObject("AdrenalineLevel", "", prefab, hudCanvas.transform, hudSpriteDictionary["hudicon_adrenaline0.png"], new Vector3(3f, 11.4f));
            directionalFireModeHudIcon = CreateStatObject("FireModeSetting", " ", prefab, hudCanvas.transform, hudSpriteDictionary["hudicon_omni.png"], new Vector3(5f, 11.4f));
            canNailArtHudIcon = CreateStatObject("CanNailArt", " ", prefab, hudCanvas.transform, hudSpriteDictionary["hudicon_cancastnailart_true.png"], new Vector3(6f, 11.4f));

            Stats.FireModeIcon += UpdateFireModeIcon;
            Stats.adrenalineChargeIcons += UpdateAdrenalineIcon;
            Stats.nailArtIcon += UpdateNailArtIcon;
        }


        private void UpdateFireModeIcon(string firemode)
        {
            try
            {
                var fireModeText = directionalFireModeHudIcon.GetComponent<DisplayItemAmount>().textObject;

                directionalFireModeHudIcon.GetComponent<SpriteRenderer>().sprite = hudSpriteDictionary[firemode];
                Color color = new Color(0.55f, 0.55f, 0.55f);
                StartCoroutine(BadAnimation(fireModeText, "", color));
            }
            catch(Exception e)
            {
                Modding.Logger.Log("[HudController] Exception in UpdateFireModeIcon() Method" + e);
            }

        }

        private void UpdateAdrenalineIcon(string chargeLevel)
        {
            string[] romanNumeral = { "...", "I", "II", "III", "IV", "V" };
            //TODO: clean up the HudController
            int level = 0;
            switch (int.Parse(chargeLevel))
            {
                case 0:
                    level = 1; //Charge Ready
                    break;
                case 1:
                    level = 2; //Currently charging
                    break;
                case 2:
                    level = 3; //1 Charge
                    break;
                case 3:
                    level = 4; //2 Charge
                    break;
                case 4:
                    level = 5; //3 Charge
                    break;
                default:
                    level = 0; //Cannot Charge
                    break;
            }

            try
            {
                var AdrenalineText = adrenalineHudIcon.GetComponent<DisplayItemAmount>().textObject;
                //adrenalineHudIcon.GetComponent<SpriteRenderer>().sprite = hudSpriteDictionary["hudicon_adrenaline"+adrenalineLevel+".png"];
                adrenalineHudIcon.GetComponent<SpriteRenderer>().sprite = hudSpriteDictionary["hudicon_adrenaline" + level + ".png"];
                Color color = Color.grey;

                switch (level)
                {
                    case 1:
                        color = new Color(0, 1f, 0); //186, 227, 39
                        break;
                    case 2:
                        color = new Color(0.76f, 0.9f, 0.07f);
                        break;
                    case 3:
                        color = Color.yellow;
                        break;
                    case 4:
                        color = new Color(1f, 0.5f, 0);
                        break;
                    case 5:
                        color = new Color(1f, 0, 0);
                        break;
                    default:
                        color = Color.grey;
                        break;
                }

                StartCoroutine(BadAnimation(AdrenalineText, " ", color));
            }
            catch (Exception e)
            {
                Modding.Logger.Log("[HudController] Exception in UpdateAdrenalineIcon() Method" + e);
            }

        }

        private void UpdateNailArtIcon(string obj)
        {
            try
            {
                var canNailArtText = canNailArtHudIcon.GetComponent<DisplayItemAmount>().textObject;

                canNailArtHudIcon.GetComponent<SpriteRenderer>().sprite = hudSpriteDictionary["hudicon_cancastnailart_" + obj + ".png"];
                Color color = new Color(0.55f, 0.55f, 0.55f);
                StartCoroutine(BadAnimation(canNailArtText, "", color));
            }
            catch (Exception e)
            {
                Modding.Logger.Log("[HudController] Exception in UpdateFireModeIcon() Method" + e);
            }
        }

        IEnumerator BadAnimation(TextMeshPro shardText, string amount, Color color)
        {
            shardText.text = amount;
            shardText.color = color;         
            yield return new WaitForSeconds(0.8f);
            //shardText.color = Color.white;

        }

        private GameObject CreateStatObject(string name, string text, GameObject prefab, Transform parent, Sprite sprite, Vector3 postoAdd)
        {
            var go = UnityEngine.Object.Instantiate(prefab, parent, true);
            go.transform.position += postoAdd;
            go.GetComponent<DisplayItemAmount>().playerDataInt = name;
            go.GetComponent<DisplayItemAmount>().textObject.text = text;
            go.GetComponent<SpriteRenderer>().sprite = sprite;
            go.SetActive(true);
            go.GetComponent<BoxCollider2D>().size = new Vector2(1.5f, 0.95f);
            go.GetComponent<BoxCollider2D>().offset = new Vector2(0.5f, 0f);
            return go;
        }

        void OnDestroy()
        {
            Stats.FireModeIcon -= UpdateFireModeIcon;
            Stats.adrenalineChargeIcons -= UpdateAdrenalineIcon;
            Stats.nailArtIcon -= UpdateNailArtIcon;
        }
    }
}