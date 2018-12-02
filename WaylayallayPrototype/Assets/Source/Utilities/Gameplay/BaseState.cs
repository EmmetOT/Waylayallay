using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sone
{
    /// <summary>
    /// An abstract base state for states used by StateMachine. This state should only be used by 
    /// objects of type T.
    /// </summary>
    public abstract class BaseState<T> where T : MonoBehaviour
    {
        protected T m_owner { get; private set; }
        protected StateMachine<T> m_stateMachine { get; private set; }

        public virtual bool Enter(T owner, StateMachine<T> stateMachine, params object[] args)
        {
            m_owner = owner;
            m_stateMachine = stateMachine;

            return true;
        }

        public virtual void Update(float deltaTime) { }
        public virtual void Exit() { }
        public virtual void Pause() { }
        public virtual void Unpause() { }

        #region Events

        /// <summary>
        /// Called whenever a keycode is pressed down.
        /// 
        /// Calls the equivalent method in the state.
        /// </summary>
        public void OnKeyDown(KeyCode key) { }

        /// <summary>
        /// Called whenever a keycode is pressed up.
        /// 
        /// Calls the equivalent method in the state.
        /// </summary>
        public virtual void OnKeyUp(KeyCode key) { }

        /// <summary>
        /// Called whenever a keycode is held.
        /// 
        /// Calls the equivalent method in the state.
        /// </summary>
        public virtual void OnKey(KeyCode key) { }

        #endregion
    }
}