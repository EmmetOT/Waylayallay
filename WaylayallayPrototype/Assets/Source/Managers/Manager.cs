using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Simplex
{
    public class Manager : Singleton<Manager>
    {
        #region Flags

        [Header("Flags")]

        [SerializeField]
        [ReadOnly]
        private bool m_hasStarted;
        public static bool HasStarted { get { return Instance.m_hasStarted; } }

        #endregion
        
        #region Configs

        [Header("Configs")]

        [SerializeField]
        private UniversalControlSettings m_universalControlSettings;
        public static UniversalControlSettings UniversalControlSettings { get { return Instance.m_universalControlSettings; } }

        #endregion

        #region Submanagers

        [Header("Submanagers")]

        [SerializeField]
        private Controls m_controls;
        public static Controls Controls { get { return Instance.m_controls; } }
        
        [SerializeField]
        private CameraController m_camera;
        public static CameraController Camera { get { return Instance.m_camera; } }
        
        [SerializeField]
        private Clock m_clock;
        public static Clock Clock { get { return Instance.m_clock; } }

        [SerializeField]
        private PlaneController m_planeController;
        public static PlaneController PlaneController { get { return Instance.m_planeController; } }

        #endregion

        #region Unity Callbacks

        protected override void Awake()
        {
            base.Awake();

            UniversalControlSettings.Init();
        }

        protected IEnumerator Start()
        {
            yield return null;

            m_hasStarted = true;
        }

        #endregion
    }
}
