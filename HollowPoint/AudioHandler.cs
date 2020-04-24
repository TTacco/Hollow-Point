using System.Collections;
using UnityEngine;
using static Modding.Logger;
using Modding;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using static HollowPoint.HollowPointEnums;

namespace HollowPoint
{
    public class AudioHandler : MonoBehaviour
    {
        GameObject clickSFX;
        GameObject shootSFX;
        GameObject enemyHitSFX;
        GameObject terrainHitSFX;

        public static Dictionary<string, GameObject> sfxGODict = new Dictionary<string, GameObject>();

        public void Awake()
        {
            StartCoroutine(AudioHandlerInit());
        }

        public IEnumerator AudioHandlerInit()
        {
            while (HeroController.instance == null)
            {
                yield return null;
            }


        }

    }
}
