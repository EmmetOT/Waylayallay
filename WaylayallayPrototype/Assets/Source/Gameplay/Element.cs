using Simplex;
using UnibusEvent;

namespace Simplex
{
    /// <summary>
    /// Elements are 'adjectives' and provide additional functionality
    /// for some Entity.
    /// </summary>
    public abstract class Element : Base
    {
        private Entity m_owner;
        public Entity Owner { get { return m_owner ?? (m_owner = this.Find<Entity>()); } }

        protected override void Awake()
        {
            base.Awake();

            m_owner = this.Find<Entity>();
        }

        #region Events

        /// <summary>
        /// Add a listener method which will hear events thrown by this element's entity and any sibling elements.
        /// </summary>
        protected override void ListenLocally<T, U>(string tag, OnEvent<T, U> onEvent)
        {
            m_toggleEventsHandler += (bool toggle) =>
            {
                if (toggle)
                    Unibus.Subscribe(tag, onEvent, m_owner);
                else
                    Unibus.Unsubscribe(tag, onEvent, m_owner);
            };
        }

        /// <summary>
        /// Add a listener method which will hear events thrown by this element's entity and any sibling elements.
        /// </summary>
        protected override void ListenLocally<T>(string tag, OnEvent<T> onEvent)
        {
            m_toggleEventsHandler += (bool toggle) =>
            {
                if (toggle)
                    Unibus.Subscribe(tag, onEvent, m_owner);
                else
                    Unibus.Unsubscribe(tag, onEvent, m_owner);
            };
        }

        /// <summary>
        /// Add a listener method which will hear events thrown by this element's entity and any sibling elements.
        /// </summary>
        protected override void ListenLocally(string tag, OnEvent onEvent)
        {
            m_toggleEventsHandler += (bool toggle) =>
            {
                if (toggle)
                    Unibus.Subscribe(tag, onEvent, m_owner);
                else
                    Unibus.Unsubscribe(tag, onEvent, m_owner);
            };
        }

        /// <summary>
        /// Send an event which will only be heard by this element's entity and any sibling elements. 
        /// </summary>
        public override void ShoutLocally<T, U>(string tag, T action1, U action2)
        {
            Unibus.Dispatch(tag, action1, action2, m_owner);
        }

        /// <summary>
        /// Send an event which will only be heard by this element's entity and any sibling elements. 
        /// </summary>
        public override void ShoutLocally<T>(string tag, T action)
        {
            Unibus.Dispatch(tag, action, m_owner);
        }

        /// <summary>
        /// Send an event which will only be heard by this element's entity and any sibling elements. 
        /// </summary>
        public override void ShoutLocally(string tag)
        {
            Unibus.Dispatch(tag, m_owner as Base);
        }

        #endregion
    }
}