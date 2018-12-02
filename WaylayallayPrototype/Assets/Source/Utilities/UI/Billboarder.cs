using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboarder : MonoBehaviour
{
    private Transform m_transform;
    private Transform m_camera;

    private void Awake()
    {
        m_transform = transform;
        m_camera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        m_transform.rotation = m_camera.rotation;
    }
}
