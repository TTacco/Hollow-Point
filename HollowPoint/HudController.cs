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
        private Dictionary<string, Sprite> hudSpriteDictionary = new Dictionary<string, Sprite>();
        private readonly string[] texturenames = { "hudicon_omni.png", "hudicon_cardinal.png"};

        void Start()
        {
            Modding.Logger.Log("INTIALIZING HUDCONTROLLER");
            var prefab = GameManager.instance.inventoryFSM.gameObject.FindGameObjectInChildren("Geo");
            var hudCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("HudCamera").FindGameObjectInChildren("Hud Canvas");

            foreach (var texture in texturenames)
            {
                var shardTex = LoadAssets.spriteDictionary[texture];
                var shardSprite = Sprite.Create(shardTex, new Rect(0, 0, shardTex.width, shardTex.height), new Vector2(0.5f, 0.5f));
                hudSpriteDictionary.Add(texture, shardSprite);
            }

            Modding.Logger.Log("did pepega");

            //you may change the name -----|                     
            directionalFireModeHudIcon = CreateStatObject("FireModeSetting", Stats.currentPrimaryAmmo.ToString(), prefab, hudCanvas.transform, hudSpriteDictionary["hudicon_omni.png"], new Vector3(2.2f, 11.4f));

            Stats.ShardAmountChanged += ShardChangedText;
        }

        private void ShardChangedText(string firemode)
        {
            try
            {
                var shardText = directionalFireModeHudIcon.GetComponent<DisplayItemAmount>().textObject;
                directionalFireModeHudIcon.GetComponent<SpriteRenderer>().sprite = hudSpriteDictionary[firemode];
                Color color = new Color(0.55f, 0.55f, 0.55f);
                StartCoroutine(BadAnimation(shardText, "", color));
            }
            catch(Exception e)
            {
                Modding.Logger.Log("[HudController] Exception in ShardChangedText() Method");
            }

        }

        IEnumerator BadAnimation(TextMeshPro shardText, string amount, Color color)
        {
            shardText.text = amount;
            shardText.color = color;         
            yield return new WaitForSeconds(0.8f);
            shardText.color = Color.white;

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

        void Destroy()
            => Stats.ShardAmountChanged -= ShardChangedText;
    }
}