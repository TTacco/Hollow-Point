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

            finalDegreeDirection = facingRight ? 0 : 180;
            directionOrientation = DirectionalOrientation.Horizontal;

            if (!up && !down) return;

            finalDegreeDirection = up ? 90 : 270;
            directionOrientation = DirectionalOrientation.Vertical;

            if (!right && !left) return;

            int sign = (up ? 1 : -1) * (left ? 1 : -1);
            finalDegreeDirection += 45 * sign;

            if(finalDegreeDirection % 45 == 0) directionOrientation = DirectionalOrientation.Diagonal;

            if (Stats.instance.cardinalFiringMode)
            {
                //If the player has activated cardinal only firing, disables diagonal degree direction
                directionOrientation = DirectionalOrientation.Vertical;
                finalDegreeDirection = (finalDegreeDirection >= 0 && finalDegreeDirection < 180) ? 90 : 270 ;
            }

            return;
        }

        void OnDestroy()
        {
            Destroy(gameObject.GetComponent<OrientationHandler>());
        }

    }
}
