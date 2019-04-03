using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using Simplex;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class BisectionPlane
{
    [SerializeField]
    private Vector3 m_position = Vector3.zero;
    public Vector3 Position { get { return m_position; } }

    [SerializeField]
    private Vector3 m_normal = Vector3.one;
    public Vector3 Normal { get { return m_normal; } }

    [SerializeField]
    private float m_size;
    public float Size { get { return m_size; } }

    [SerializeField]
    private Color m_colour;
    
    public void DrawGizmo(Transform transform, Vector3 positionOffset = default(Vector3))
    {
        Quaternion rotation = Quaternion.LookRotation(transform.TransformDirection(m_normal));
        Matrix4x4 trs = Matrix4x4.TRS(transform.TransformPoint(m_position + positionOffset), rotation, Vector3.one * m_size);
        Gizmos.matrix = trs;

        Color32 color = m_colour;
        color.a = 125;
        Gizmos.color = color;

        Gizmos.DrawCube(Vector3.zero, new Vector3(1f, 1f, 0.0001f));
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
    }

    public static implicit operator Plane(BisectionPlane bP)
    {
        return new Plane(bP.Normal, bP.Position);
    }
}
