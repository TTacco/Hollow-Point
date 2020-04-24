using UnityEngine;
using static HollowPoint.HollowPointEnums;

namespace HollowPoint
{
    class OrientationHandler : MonoBehaviour
    {
        public static bool up;
        public static bool down;
        public static bool right;
        public static bool left;
        public static bool facingRight;
        public static bool holdingAttack;
        public static bool pressingAttack = true;
        public static bool heldAttack;
        public static float finalDegreeDirection;
        public static DirectionalOrientation directionOrientation = DirectionalOrientation.Horizontal;

        public void Update()
        {
            up = InputHandler.Instance.inputActions.up;
            down = InputHandler.Instance.inputActions.down;
            right = InputHandler.Instance.inputActions.right;
            left = InputHandler.Instance.inputActions.left;
            holdingAttack = InputHandler.Instance.inputActions.attack.WasRepeated;
            pressingAttack = InputHandler.Instance.inputActions.attack.WasPressed;
            heldAttack = InputHandler.Instance.inputActions.attack.IsPressed;
            facingRight = HeroController.instance.cState.facingRight;

            if (facingRight)
            {
                finalDegreeDirection = 0;
                directionOrientation = DirectionalOrientation.Horizontal;
            }
            else
            {
                finalDegreeDirection = 180;
                directionOrientation = DirectionalOrientation.Horizontal;
            }

            if(up && !(right || left))
            {
                finalDegreeDirection = 90;
                directionOrientation = DirectionalOrientation.Vertical;
            }
            else if (down && !(right || left))
            {
                finalDegreeDirection = 270;
                directionOrientation = DirectionalOrientation.Vertical;
            }
            else if (up)
            {
                if (right)
                {
                    finalDegreeDirection = 45;
                    directionOrientation = DirectionalOrientation.Diagonal;
                }
                else if (left)
                {
                    finalDegreeDirection = 135;
                    directionOrientation = DirectionalOrientation.Diagonal;
                }
            }
            else if (down)
            {
                if (right)
                {
                    finalDegreeDirection = 315;
                    directionOrientation = DirectionalOrientation.Diagonal;
                }
                else if (left)
                {
                    finalDegreeDirection = 225;
                    directionOrientation = DirectionalOrientation.Diagonal;
                }
            }


        }

        void OnDestroy()
        {
            Destroy(gameObject.GetComponent<OrientationHandler>());
        }

    }
}
