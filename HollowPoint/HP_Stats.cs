using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HollowPoint
{
    class HP_Stats : MonoBehaviour
    {
        public static int statShock;
        public static int statSapper;
        public static int statSurgeon;
        public static int statScout;

        public void Awake()
        {
            StartCoroutine(InitStats());
        }

        public IEnumerator InitStats()
        {
            yield return null;
        }

    }
}
