using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;


namespace HollowPoint
{
    class HP_SpellControl : MonoBehaviour
    {
        public void Awake()
        {
            StartCoroutine(InitSpellControl());
        }

        public IEnumerator InitSpellControl()
        {
            while(HeroController.instance == null)
            {
                yield return null;
            }

            //HeroController.instance.spellControl.
        }

    }
}
