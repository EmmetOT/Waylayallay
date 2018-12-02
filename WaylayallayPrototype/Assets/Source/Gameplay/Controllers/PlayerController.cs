using UnityEngine;
using Event = Sone.Event;

namespace Simplex
{
    /// <summary>
    /// PlayerController overrides Controller and calls its control methods
    /// via event calls from the Controls class. In other words, it allows
    /// player input.
    /// </summary>
    public class PlayerController : Controller
    {
        protected override void AddListeners()
        {
            base.AddListeners();

            ListenGlobally<Controls.Code>(Event.OnCodeDown, OnCodeDown);
            ListenGlobally<Controls.Code>(Event.OnCodeHeldPerFrame, OnCodeHeldPerFrame);
            ListenGlobally<Controls.Code>(Event.OnCodeHeldFixed, OnCodeHeldFixed);
            ListenGlobally<Controls.Code>(Event.OnCodeUp, OnCodeUp);
        }

        #region Control Methods

        protected virtual void OnCodeDown(Controls.Code code)
        {
            if (code == Controls.Code.JUMP)
                SendJumpOrder();
        }
        
        protected virtual void OnCodeUp(Controls.Code code) { }

        protected virtual void OnCodeHeldPerFrame(Controls.Code code) { }
        
        protected virtual void OnCodeHeldFixed(Controls.Code code)
        {
            switch (code)
            {
                case Controls.Code.MOVE_UP:
                    SendMoveOrder(Vector3.forward);
                    return;
                case Controls.Code.MOVE_DOWN:
                    SendMoveOrder(-Vector3.forward);
                    return;
                case Controls.Code.MOVE_RIGHT:
                    SendMoveOrder(Vector3.right);
                    return;
                case Controls.Code.MOVE_LEFT:
                    SendMoveOrder(-Vector3.right);
                    return;
            }
        }

        #endregion
    }
}