using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Simplex
{
    /// <summary>
    /// Allows the designer to tweak control settings which are universal to all players.
    /// </summary>
    public class UniversalControlSettings : ScriptableObject
    {
        [Header("Layers")]

        [SerializeField]
        [Tooltip("The ground, obstacles... Anything that can be walked on.")]
        [OnValueChanged("SetSettingsDirty")]
        private LayerMask m_walkableLayer;
        public LayerMask WalkableLayer { get { return m_walkableLayer; } }
        
        [Header("Steps")]

        [SerializeField]
        [MinValue(0f)]
        [Tooltip("The maximum difference in y for an object to be considered a step.")]
        [OnValueChanged("SetSettingsDirty")]
        private float m_maxStepHeight = 1f;
        public float MaxStepHeight { get { return m_maxStepHeight; } }

        [SerializeField]
        [MinValue(0f)]
        [Tooltip("How far forward a surface must extend for it to be considered 'steppable'.")]
        [OnValueChanged("SetSettingsDirty")]
        private float m_minimumStepDepth;
        public float MinimumStepDepth { get { return m_minimumStepDepth; } }

        [SerializeField]
        [MinValue(0f)]
        [Tooltip("If a steppable surface of the right height is within this distance ahead of a character" +
            "and they move forward, it's a step.")]
        [OnValueChanged("SetSettingsDirty")]
        private float m_stepLookahead = 1f;
        public float StepLookahead { get { return m_stepLookahead; } }

        [Header("Jumping")]

        [SerializeField]
        [MinValue(0f)]
        [Tooltip("The time, in seconds, after leaving the ground by falling or something," +
            "where an agent is still allowed to jump.")]
        [OnValueChanged("SetSettingsDirty")]
        private float m_jumpGracePeriod = 0f;
        public float JumpGracePeriod { get { return m_jumpGracePeriod; } }

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Generic Value. Useful for testing.")]
        [OnValueChanged("SetSettingsDirty")]
        private float m_testVal = 0f;
        public float TestVal { get { return m_testVal; } }

        public float GetSetting(Setting setting)
        {
            switch (setting)
            {
                case Setting.MAX_STEP_HEIGHT:
                    return m_maxStepHeight;
                case Setting.STEP_LOOKAHEAD:
                    return m_stepLookahead;
                case Setting.JUMP_GRACE_PERIOD:
                    return m_jumpGracePeriod;
                case Setting.MINIMUM_STEP_DEPTH:
                    return m_minimumStepDepth;
                case Setting.TEST_VAL:
                    return m_testVal;
            }

            return 0f;
        }

        public enum Setting
        {
            FIXED,
            MAX_STEP_HEIGHT,
            STEP_LOOKAHEAD,
            JUMP_GRACE_PERIOD,
            MINIMUM_STEP_DEPTH,

            TEST_VAL = 10000
        }

        public delegate void SetDirtyDelegate();
        public static event SetDirtyDelegate SetDirtyEvent;
        
        public static void Init()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.undoRedoPerformed += SetSettingsDirty;
#endif

            SetSettingsDirty();
        }

        public static void SetSettingsDirty()
        {
            if (SetDirtyEvent != null)
                SetDirtyEvent.Invoke();
        }
    }
}
