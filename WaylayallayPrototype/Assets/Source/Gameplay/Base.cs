using UnibusEvent;
using UnityEngine;

namespace Simplex
{
    /// <summary>
    /// A base is any component in the entity-element framework.
    /// </summary>
    public abstract class Base : MonoBehaviour
    {
        #region Variables

        private Transform m_transform;
        public Transform Transform
        {
            get
            {
                if (m_transform == null)
                    m_transform = transform;

                return m_transform;
            }
        }
        
        private bool m_addedListeners = false;
        
        #endregion

        #region Unity Callbacks

        protected virtual void Awake()
        {
            m_transform = transform;

            if (!m_addedListeners)
                AddListeners();
        }

        protected virtual void OnEnable()
        {
            if (!m_addedListeners)
                AddListeners();

            m_toggleEventsHandler?.Invoke(true);
        }

        protected virtual void OnDisable()
        {
            m_toggleEventsHandler?.Invoke(false);
        }

        #endregion

        #region Events

        protected delegate void ToggleEvent(bool toggle);
        protected event ToggleEvent m_toggleEventsHandler;

        protected abstract void ListenLocally<T, U>(string tag, OnEvent<T, U> onEvent);

        protected abstract void ListenLocally<T>(string tag, OnEvent<T> onEvent);

        protected abstract void ListenLocally(string tag, OnEvent onEvent);

        public abstract void ShoutLocally<T, U>(string tag, T action1, U action2);

        public abstract void ShoutLocally<T>(string tag, T action);

        public abstract void ShoutLocally(string tag);

        /// <summary>
        /// Add a listener which will hear any non-local event with the same tag and types.
        /// </summary>
        protected void ListenGlobally<T, U>(string tag, OnEvent<T, U> onEvent)
        {
            m_toggleEventsHandler += (bool toggle) =>
            {
                if (toggle)
                    Unibus.Subscribe(tag, onEvent);
                else
                    Unibus.Unsubscribe(tag, onEvent);
            };
        }

        /// <summary>
        /// Add a listener which will hear any non-local event with the same tag and type.
        /// </summary>
        protected void ListenGlobally<T>(string tag, OnEvent<T> onEvent)
        {
            m_toggleEventsHandler += (bool toggle) =>
            {
                if (toggle)
                    Unibus.Subscribe(tag, onEvent);
                else
                    Unibus.Unsubscribe(tag, onEvent);
            };
        }

        /// <summary>
        /// Add a listener which will hear any non-local event with the same tag.
        /// </summary>
        protected void ListenGlobally(string tag, OnEvent onEvent)
        {
            m_toggleEventsHandler += (bool toggle) =>
            {
                if (toggle)
                    Unibus.Subscribe(tag, onEvent);
                else
                    Unibus.Unsubscribe(tag, onEvent);
            };
        }

        protected void ShoutGlobally<T, U>(string tag, T action1, U action2)
        {
            Unibus.Dispatch(tag, action1, action2);
        }

        protected void ShoutGlobally<T>(string tag, T action)
        {
            Unibus.Dispatch(tag, action);
        }

        protected void ShoutGlobally(string tag)
        {
            Unibus.Dispatch(tag);
        }

        /// <summary>
        /// Override this method and add your 'AddListener' calls to have them 
        /// handled correctly on Enable/Disable.
        /// </summary>
        protected virtual void AddListeners()
        {
            m_addedListeners = true;
        }

        #endregion
    }
}
