using UnityEngine;
using Event = Sone.Event;

namespace Simplex
{
    /// <summary>
    /// View elements handle all audiovisual feedback for an entity,
    /// in particular controlling the animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public abstract class View : Element
    {
        [Header("View Components")]

        [SerializeField]
        private Animator m_animator;
        public Animator Animator { get { return m_animator ?? (m_animator = GetComponent<Animator>()); } }

        protected virtual void Reset()
        {
            m_animator = GetComponent<Animator>();
        }
        
        protected override void AddListeners()
        {
            base.AddListeners();

            ListenLocally<Vector3>(Event.OnMove, OnMove);
            //ListenLocally(Event.OnJump, OnJump);
        }
        
        #region Events

        protected virtual void OnMove(Vector3 direction)
        {
            
        }

        protected virtual void OnJump()
        {

        }

        #endregion

        #region Animator

        protected virtual void SetInt(string id, int value)
        {
            Animator.SetInteger(id, value);
        }

        protected virtual void SetFloat(string id, float value)
        {
            Animator.SetFloat(id, value);
        }

        protected virtual void SetTrigger(string id)
        {
            Animator.SetTrigger(id);
        }

        protected virtual void SetBool(string id, bool value)
        {
            Animator.SetBool(id, value);
        }

        #endregion
    }
}