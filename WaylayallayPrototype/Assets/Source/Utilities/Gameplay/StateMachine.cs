using UnityEngine;
using System;

namespace Simplex
{
    /// <summary>
    /// This is a very simple traditional state machine. 
    /// Not a monobehaviour or anything. No stacks, no pushing,
    /// no popping. Just a current state, which can transition to any
    /// other state.
    /// </summary>
    public class StateMachine<T> where T : MonoBehaviour
    {
        #region Variables

        protected BaseState<T> m_currentState = null;                       // The state the machine is currently in.
        protected BaseState<T> m_lastState = null;                          // The state the machine was in before this one.
        protected T m_owner;                                                // The state machine controls this object's behaviour.
        protected BaseState<T> m_newState = null;

        private bool m_paused = false;                                      // A paused state machine won't update.

        #endregion

        #region Properties

        public BaseState<T> CurrentState { get { return m_currentState; } }
        public BaseState<T> LastState { get { return m_lastState; } }
        public BaseState<T> NewState { get { return m_newState; } }

        public T Owner { get { return m_owner; } }
        public Type OwnerType { get { return typeof(T); } }

        #endregion

        #region Public Methods

        /// <summary>
        /// Construct the state machine. Accepts a unit and a start state.
        /// </summary>
        public StateMachine(T owner)
        {
            m_owner = owner;

            if (m_currentState != null)
                m_currentState.Enter(owner, this);
        }

        /// <summary>
        /// Transition this state machine to the given state, provided the correct arguments are given.
        /// </summary>
        public virtual bool ChangeState<BaseState>(params object[] args) where BaseState : BaseState<T>, new()
        {
            m_newState = Activator.CreateInstance(typeof(BaseState)) as BaseState<T>;

            if (m_currentState != null && m_currentState.GetType() == m_newState.GetType())
            {
                return false;
            }

            if (m_newState.Enter(m_owner, this, args))
            {
                if (m_currentState != null)
                    m_currentState.Exit();

                m_lastState = m_currentState;
                m_currentState = m_newState;

                return true;
            }

            Debug.LogWarning(m_newState.GetType().ToString() + " received incorrect arguments.");

            return false;
        }

        /// <summary>
        /// Transition this state machine to the given state, provided the correct arguments are given.
        /// </summary>
        public virtual bool ChangeState(Type state, params object[] args)
        {
            if (!state.IsSubclassOf(typeof(BaseState<T>)))
            {
                Debug.LogError("Given type '" + state.ToString() + "' does not inherit from BaseState<" + OwnerType.ToString() + ">!");
                return false;
            }

            m_newState = Activator.CreateInstance(state) as BaseState<T>;

            if (m_currentState != null && m_currentState.GetType() == m_newState.GetType())
            {
                return false;
            }

            if (m_newState.Enter(m_owner, this, args))
            {
                if (m_currentState != null)
                    m_currentState.Exit();

                m_lastState = m_currentState;
                m_currentState = m_newState;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Perform the update behaviours of this state machine's current state.
        /// Call this in the state machine's owner's update callback method.
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            if (m_currentState != null && !m_paused)
                m_currentState.Update(deltaTime);
        }

        #endregion

        #region Pausing

        public void Pause()
        {
            m_paused = true;

            if (m_currentState != null)
                m_currentState.Pause();
        }

        public void Unpause()
        {
            m_paused = false;

            if (m_currentState != null)
                m_currentState.Unpause();
        }

        #endregion

        #region Events

        /// <summary>
        /// Called whenever a keycode is pressed down.
        /// 
        /// Calls the equivalent method in the state.
        /// </summary>
        private void OnKeyDown(KeyCode key)
        {
            if (m_currentState != null)
                CurrentState.OnKeyDown(key);
        }

        /// <summary>
        /// Called whenever a keycode is pressed up.
        /// 
        /// Calls the equivalent method in the state.
        /// </summary>
        private void OnKeyUp(KeyCode key)
        {
            if (m_currentState != null)
                CurrentState.OnKeyUp(key);
        }

        /// <summary>
        /// Called whenever a keycode is held.
        /// 
        /// Calls the equivalent method in the state.
        /// </summary>
        private void OnKey(KeyCode key)
        {
            if (m_currentState != null)
                CurrentState.OnKey(key);
        }

        #endregion
    }
}
