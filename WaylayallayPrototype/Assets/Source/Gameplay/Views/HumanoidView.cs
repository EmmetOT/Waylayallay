using UnityEngine;

namespace Simplex
{
    public class HumanoidView : View
    {
        #region Parameters

        private const string INPUT_ANGLE = "InputAngle";
        private const string INPUT_MAGNITUDE = "InputMagnitude";

        #endregion

        #region Events

        protected override void OnMove(Vector3 moveVec)
        {
            base.OnMove(moveVec);

            SetFloat(INPUT_MAGNITUDE, moveVec.magnitude * 7);
        }

        protected override void OnJump()
        {
            base.OnJump();


        }

        #endregion
    }
}