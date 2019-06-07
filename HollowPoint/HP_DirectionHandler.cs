using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MonoMod.Utils;
using MonoMod;
using HutongGames;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using ModCommon;
using ModCommon.Util;
using GlobalEnums;

namespace HollowPoint
{
    class HP_DirectionHandler : MonoBehaviour
    {
        public static bool up;
        public static bool down;
        public static bool right;
        public static bool left;
        public static bool facingRight;
        public static float finalDegreeDirection;

        public void Update()
        {
            up = InputHandler.Instance.inputActions.up;
            down = InputHandler.Instance.inputActions.down;
            right = InputHandler.Instance.inputActions.right;
            left = InputHandler.Instance.inputActions.left;
            facingRight = HeroController.instance.cState.facingRight;

            if (facingRight)
            {
                finalDegreeDirection = 0;
            }
            else
            {
                finalDegreeDirection = 180;
            }

            if(up && !(right || left))
            {
                finalDegreeDirection = 90;
            }
            else if (down && !(right || left))
            {
                finalDegreeDirection = 270;
            }
            else if (up)
            {
                if (right)
                {
                    finalDegreeDirection = 45;
                }
                else if (left)
                {
                    finalDegreeDirection = 135;
                }
            }
            else if (down)
            {
                if (right)
                {
                    finalDegreeDirection = 315;
                }
                else if (left)
                {
                    finalDegreeDirection = 225;
                }
            }


        }

    }
}
