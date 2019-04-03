using UnityEngine;
using UnibusEvent;
using Event = Simplex.Event;

namespace Simplex
{
    public class Controls : MonoBehaviour
    {
        #region Key Bindings

        [Header("Key Bindings")]

        [SerializeField]
        private Message[] m_messages;

        #endregion

        #region Unity Callbacks

        private void Update()
        {
            // key down
            for (int i = 0; i < m_messages.Length; i++)
                if (Input.GetKeyDown(m_messages[i].Key))
                    OnCodeDown(m_messages[i].Code);

            // key held
            for (int i = 0; i < m_messages.Length; i++)
                if (Input.GetKey(m_messages[i].Key))
                    OnCodeHeldPerFrame(m_messages[i].Code);

            // key up
            for (int i = 0; i < m_messages.Length; i++)
                if (Input.GetKeyUp(m_messages[i].Key))
                    OnCodeUp(m_messages[i].Code);
        }

        private void FixedUpdate()
        {
            // key held
            for (int i = 0; i < m_messages.Length; i++)
                if (Input.GetKey(m_messages[i].Key))
                    OnCodeHeldFixed(m_messages[i].Code);
        }

        #endregion

        #region Control Methods

        private void OnCodeDown(Code code)
        {
            Unibus.Dispatch(Event.OnCodeDown, code);
        }

        private void OnCodeHeldPerFrame(Code code)
        {
            Unibus.Dispatch(Event.OnCodeHeldPerFrame, code);
        }

        private void OnCodeHeldFixed(Code code)
        {
            Unibus.Dispatch(Event.OnCodeHeldFixed, code);
        }

        private void OnCodeUp(Code code)
        {
            Unibus.Dispatch(Event.OnCodeUp, code);
        }

        #endregion

        [System.Serializable]
        public struct Message
        {
            [SerializeField]
            private KeyCode m_key;
            public KeyCode Key { get { return m_key; } }

            [SerializeField]
            private Code m_code;
            public Code Code { get { return m_code; } }
        }

        public enum Code
        {
            // Player Movement
            MOVE_UP = 0,
            MOVE_DOWN,
            MOVE_LEFT,
            MOVE_RIGHT,
            JUMP,

            // Waylayallay
            COMBINE_GENERATED = 500,

            // Debug
            TIME_STEP = 1000
        }
    }
}
