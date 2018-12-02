using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simplex
{
    /// <summary>
    /// Controllers provide an interface between an Entity and 
    /// either AI or player input.
    /// </summary>
    public abstract class Controller : Element
    {
        #region Controls

        protected virtual void SendMoveOrder(Vector3 vec)
        {
            Owner.GiveOrder(Entity.Order.MOVE, vec);
        }

        protected virtual void SendJumpOrder()
        {
            Owner.GiveOrder(Entity.Order.JUMP);
        }

        #endregion
    }
}