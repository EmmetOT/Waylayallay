using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Event = Simplex.Event;

namespace Simplex
{
    public class CameraController : MonoBehaviour
    {
        #region Components

        [SerializeField]
        private Camera m_main;
        public Camera Main { get { return m_main; } }

        #endregion
    }
}


