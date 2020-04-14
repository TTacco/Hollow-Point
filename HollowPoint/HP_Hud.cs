using ModCommon;
using System.Collections;
using TMPro;
using UnityEngine;

namespace HollowPoint
{
    internal class HP_Hud : MonoBehaviour
    {
        /*
         * I am using this space to say that I hate TTaccoo#7358 for removing the grenades count.
         * He is no longer my hero. 😤
        */

        private GameObject _hudshard;

        void Start()
        {
            var prefab = GameManager.instance.inventoryFSM.gameObject.FindGameObjectInChildren("Geo");
            var hudCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("HudCamera").FindGameObjectInChildren("Hud Canvas");

            var shardTex = LoadAssets.spriteDictionary["shard.png"];
            var shardSprite = Sprite.Create(shardTex, new Rect(0, 0, shardTex.width, shardTex.height), new Vector2(0.5f, 0.5f));

            Modding.Logger.Log("did pepega");

            //you may change the name -----|                     
            _hudshard = CreateStatObject("Shard", HP_Stats.artifactPower.ToString(), prefab, hudCanvas.transform, shardSprite, new Vector3(2.2f, 11.4f));

            HP_Stats.ShardAmountChanged += ShardChanged;
        }

        private void ShardChanged(int amt)
        {
            var shardText = _hudshard.GetComponent<DisplayItemAmount>().textObject;

            Color color = new Color(0.2f, .4f, .75f);
            if (amt <= 0)
            {
                amt *= -1;
                color = new Color(0.7f, .35f, 0);
            }
            StartCoroutine(BadAnimation(shardText, amt.ToString(), color));
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
            go.GetComponent<BoxCollider2D>().size = new Vector2(1.5f, 0.98f);
            go.GetComponent<BoxCollider2D>().offset = new Vector2(0.5f, 0f);
            return go;
        }

        void Destroy()
            => HP_Stats.ShardAmountChanged -= ShardChanged;
    }
}