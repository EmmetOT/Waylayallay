using UnityEngine;
using UnibusEvent;
using Simplex;

namespace Simplex
{
    /// <summary>
    /// Entities are 'nouns' and can have a number of Elements which provide them
    /// with additional functionality.
    /// </summary>
    public abstract class Entity : Base
    {
        #region Components

        [Header("Entity Components")]
        
        private Collider[] m_colliders;
        
        /// <summary>
        /// All colliders attached to this GameObject and its children. (Same criteria as Rigidbodies.)
        /// </summary>
        public Collider[] Colliders
        {
            get
            {
                if (m_colliders == null)
                    m_colliders = this.FindAll<Collider>();

                return m_colliders;
            }
        }
        
        #endregion
        
        #region Unity Callbacks

        protected override void Awake()
        {
            base.Awake();

            m_colliders = this.FindAll<Collider>();
        }
        
        #endregion
        
        #region Miscellaneous Useful Methods
        
        protected Vector3 ToLocalRotation(Vector3 vec)
        {
            return Maths.RotateAround(vec, Transform.position, Transform.eulerAngles);
        }
        
        #endregion

        #region Events
        
        /// <summary>
        /// Add a listener method which will hear events thrown by this entity and any of its elements.
        /// </summary>
        protected override void ListenLocally<T, U>(string tag, OnEvent<T, U> onEvent)
        {
            m_toggleEventsHandler += (bool toggle) =>
            {
                if (toggle)
                    Unibus.Subscribe(tag, onEvent, this);
                else
                    Unibus.Unsubscribe(tag, onEvent, this);
            };
        }

        /// <summary>
        /// Add a listener method which will hear events thrown by this entity and any of its elements.
        /// </summary>
        protected override void ListenLocally<T>(string tag, OnEvent<T> onEvent)
        {
            m_toggleEventsHandler += (bool toggle) =>
            {
                if (toggle)
                    Unibus.Subscribe(tag, onEvent, this);
                else
                    Unibus.Unsubscribe(tag, onEvent, this);
            };
        }

        /// <summary>
        /// Add a listener method which will hear events thrown by this entity and any of its elements.
        /// </summary>
        protected override void ListenLocally(string tag, OnEvent onEvent)
        {
            m_toggleEventsHandler += (bool toggle) =>
            {
                if (toggle)
                    Unibus.Subscribe(tag, onEvent, this);
                else
                    Unibus.Unsubscribe(tag, onEvent, this);
            };
        }

        /// <summary>
        /// Send an event which will only be heard by this entity and any of its elements.
        /// </summary>
        public override void ShoutLocally<T, U>(string tag, T action1, U action2)
        {
            Unibus.Dispatch(tag, action1, action2, this);
        }

        /// <summary>
        /// Send an event which will only be heard by this entity and any of its elements.
        /// </summary>
        public override void ShoutLocally<T>(string tag, T action)
        {
            Unibus.Dispatch(tag, action, this);
        }

        /// <summary>
        /// Send an event which will only be heard by this entity and any of its elements.
        /// </summary>
        public override void ShoutLocally(string tag)
        {
            Unibus.Dispatch(tag, this as Base);
        }

        protected virtual void OnFrontCollision(GameObject gameObject) { }
        protected virtual void OnEndFrontCollision(GameObject gameObject) { }

        #endregion

        #region Orders

        protected virtual bool CanJump { get { return false; } }

        /// <summary>
        /// Gives this entity some order, such as move, stop, attack...
        /// 
        /// Returns whether the order succeeded.
        /// </summary>
        public virtual bool GiveOrder(Order order, object arg = null)
        {
            switch (order)
            {
                case Order.MOVE:
                    return GiveMoveOrder(arg);
                case Order.JUMP:
                    return GiveJumpOrder();
            }

            return false;
        }

        /// <summary>
        /// Send the entity a move order. Args must be a single vector3.
        /// </summary>
        private bool GiveMoveOrder(object arg)
        {
            Vector3 direction;

            if (!General.ProcessArg(arg, out direction))
                return false;

            Move(direction);

            return true;
        }

        /// <summary>
        /// Send the entity a jump order. No args.
        /// </summary>
        private bool GiveJumpOrder()
        {
            if (!CanJump)
                return false;

            Jump();

            return true;
        }

        public enum Order
        {
            NONE,
            STOP,
            MOVE,
            JUMP
        }

        #endregion

        #region Actions

        protected virtual void Move(Vector3 direction) { }

        protected virtual void Jump() { }

        #endregion

        #region Debug/Editor

        protected virtual bool AllowModificationOfAirborneEventBool()
        {
            return true;
        }

        #endregion

    }
}