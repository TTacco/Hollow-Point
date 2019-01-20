using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HollowPoint
{
    public class HP_DirectionHandler : MonoBehaviour
    {
        public static bool forwardPressed;
        public static bool upPressed;
        public static bool downPressed;

        public static bool diagonalUpwardsPressed;
        public static bool diagonalDownwardsPressed;

        //The speed the object will travel in the screen, both x and y
        public static float xVelocity;
        public static float yVelocity;

        public void Update()
        {
            forwardPressed = false;
            upPressed = false;
            downPressed = false;
            diagonalDownwardsPressed = false;
            diagonalUpwardsPressed = false;

            xVelocity = 35;
            yVelocity = 0;


            if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
            {
                forwardPressed = true;
            }

            if (InputHandler.Instance.inputActions.down.IsPressed)
            {
                downPressed = true;
            }

            if (InputHandler.Instance.inputActions.up.IsPressed)
            {
                upPressed = true;
            }

            //Checks if the player is pressing the diagonal keys
            if(forwardPressed)
            {
                if (downPressed)
                {
                    diagonalDownwardsPressed = true;
                }
                else if (upPressed)
                {
                    diagonalUpwardsPressed = true;
                }
            }

            //Determines at what direction the bullet should travel

            if (!forwardPressed && (downPressed || upPressed))
            {
                xVelocity = 0;
            }

            yVelocity = (upPressed) ? 35f : (downPressed)? -35f : 0f;
        }
    }
}
