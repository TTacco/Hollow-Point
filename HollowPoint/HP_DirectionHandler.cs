using UnityEngine;

namespace HollowPoint
{
    class HP_DirectionHandler : MonoBehaviour
    {
        public static bool up;
        public static bool down;
        public static bool right;
        public static bool left;
        public static bool facingRight;
        public static bool holdingAttack;
        public static bool pressingAttack;
        public static bool heldAttack;
        public static float finalDegreeDirection;

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

        void OnDestroy()
        {
            Destroy(gameObject.GetComponent<HP_DirectionHandler>());
        }

    }
}
