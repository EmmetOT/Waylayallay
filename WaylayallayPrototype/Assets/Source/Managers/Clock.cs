using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnibusEvent;

public class Clock : MonoBehaviour
{
    [SerializeField]
    private float m_timeInterval = 10f;

    [SerializeField, ReadOnly]
    private float m_totalTimeElapsed = 0f;

    private float m_timeElapsedInterval = 0f;

    private void Update()
    {
        m_totalTimeElapsed += Time.deltaTime;
        m_timeElapsedInterval += Time.deltaTime;
        
        if (m_timeElapsedInterval >= m_timeInterval)
        {
            m_timeElapsedInterval = 0f;
            Unibus.Dispatch(Simplex.Event.OnTimeStep);
        }
    }
}
