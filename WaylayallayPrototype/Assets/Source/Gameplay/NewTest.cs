using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

[RequireComponent(typeof(MeshFilter))]
public class NewTest : MonoBehaviour
{
    [BoxGroup("Control Settings")]
    [SerializeField]
    private float m_splitSpeed = 1f;
    
    [SerializeField]
    [OnValueChanged("ChangedPlane")]
    private BisectionPlane m_bisectionPlane = new BisectionPlane();

    private MeshStretcher m_stretcher;

    private MeshFilter m_meshFilter;
    public MeshFilter MeshFilter
    {
        get
        {
            if (m_meshFilter == null)
                m_meshFilter = GetComponent<MeshFilter>();

            return m_meshFilter;
        }
    }

    private Transform m_transform;
    public Transform Transform
    {
        get
        {
            if (m_transform == null)
                m_transform = GetComponent<Transform>();

            return m_transform;
        }
    }

    private void Awake()
    {
        m_stretcher = new MeshStretcher(MeshFilter, m_bisectionPlane);
    }

    private void Update()
    {
        float scrollDelta = Input.mouseScrollDelta.y * Time.deltaTime * m_splitSpeed;

        if (scrollDelta == 0f || m_stretcher == null)
            return;

        m_stretcher.SetStretch(m_stretcher.CurrentStretch + scrollDelta);
    }
    
    private void OnDrawGizmos()
    {
        if (m_stretcher == null)
        {
            m_bisectionPlane.DrawGizmo(Transform);
        }
        else
        {
            m_bisectionPlane.DrawGizmo(Transform, m_stretcher.CapOffset);
            m_bisectionPlane.DrawGizmo(Transform, -m_stretcher.CapOffset);
        }
    }

    private void ChangedPlane()
    {
        if (m_stretcher == null)
            return;

        m_stretcher.Calculate();
    }

    //#region Debug

    //[BoxGroup("Gizmo Settings")]
    //[SerializeField]
    //private bool m_showBisectionPlane = false;

    //[BoxGroup("Gizmo Settings")]
    //[SerializeField]
    //private bool m_showPointsOfIntersection = false;

    //[BoxGroup("Gizmo Settings")]
    //[SerializeField]
    //private bool m_labelNewTrianglePoints = false;

    //[BoxGroup("Gizmo Settings")]
    //[SerializeField]
    //private bool m_showNewTriangles = false;

    //private void ShowNormals(Mesh mesh, Color colour, float multiplier = 1f, Vector3 offset = default(Vector3))
    //{
    //    Vector3[] normals = mesh.normals;
    //    Vector3[] vertices = mesh.vertices;

    //    if (normals.Length == 0)
    //        Debug.Log("No normals!");

    //    Gizmos.color = colour;

    //    for (int i = 0; i < normals.Length; i++)
    //    {
    //        Gizmos.DrawLine(offset + vertices[i], offset + vertices[i] + normals[i] * multiplier);
    //    }
    //}

    //private void LabelPoints(Color colour, IList<int> triangles, IList<Vector3> vertices, int startingIndex = -1)
    //{
    //    GUIStyle handleStyle = new GUIStyle();
    //    handleStyle.normal.textColor = colour;

    //    Gizmos.color = colour;

    //    if (startingIndex == -1)
    //    {
    //        for (int i = 0; i < triangles.Count; i++)
    //        {
    //            Gizmos.DrawSphere(vertices[triangles[i]], 0.04f);
    //            Handles.Label(vertices[triangles[i]], i.ToString(), handleStyle);
    //        }
    //    }
    //    else
    //    {
    //        Gizmos.DrawSphere(vertices[triangles[startingIndex]], 0.04f);
    //        Handles.Label(vertices[triangles[startingIndex]], startingIndex.ToString(), handleStyle);

    //        Gizmos.DrawSphere(vertices[triangles[startingIndex + 1]], 0.04f);
    //        Handles.Label(vertices[triangles[startingIndex + 1]], startingIndex.ToString(), handleStyle);

    //        Gizmos.DrawSphere(vertices[triangles[startingIndex + 2]], 0.04f);
    //        Handles.Label(vertices[triangles[startingIndex + 2]], startingIndex.ToString(), handleStyle);
    //    }
    //}

    //private void LabelTriangle(Color colour, string label, int startingIndex, IList<int> triangles, IList<Vector3> vertices)
    //{
    //    Vector3 centroid = Vector3.zero;
    //    centroid += vertices[triangles[startingIndex]];
    //    centroid += vertices[triangles[startingIndex + 1]];
    //    centroid += vertices[triangles[startingIndex + 2]];
    //    centroid /= 3f;

    //    GUIStyle handleStyle = new GUIStyle();
    //    handleStyle.normal.textColor = colour;

    //    Handles.Label(centroid, label, handleStyle);
    //}

    //private void ShowPolygons(Color colour, IList<int> triangles, IList<Vector3> vertices, Vector3 offset = default(Vector3))
    //{
    //    Gizmos.color = colour;

    //    for (int i = 0; i < triangles.Count; i++)
    //    {
    //        Gizmos.DrawLine(vertices[triangles[i]] + offset, vertices[triangles[(i + 1) % triangles.Count]] + offset);
    //    }
    //}

    //public void DrawNormal(Color colour, Vector3 a, Vector3 b, Vector3 c, Vector3? normal = null)
    //{
    //    if (normal == null)
    //        normal = (new Plane(a, b, c)).normal;

    //    Vector3 centroid = (a + b + c) * 0.333333f;

    //    Gizmos.color = colour;
    //    Gizmos.DrawLine(centroid, centroid + (Vector3)normal);
    //}

    //private void ShowTriangles(Color colour, IList<int> triangles, IList<Vector3> vertices, int startingIndex = -1, Vector3 offset = default(Vector3))
    //{
    //    Gizmos.color = colour;

    //    if (startingIndex == -1)
    //    {
    //        for (int i = 0; i < triangles.Count; i += 3)
    //        {
    //            Gizmos.DrawLine(vertices[triangles[i]] + offset, vertices[triangles[i + 1]] + offset);
    //            Gizmos.DrawLine(vertices[triangles[i + 1]] + offset, vertices[triangles[i + 2]] + offset);
    //            Gizmos.DrawLine(vertices[triangles[i + 2]] + offset, vertices[triangles[i]] + offset);
    //        }
    //    }
    //    else
    //    {
    //        Gizmos.DrawLine(vertices[triangles[startingIndex]] + offset, vertices[triangles[startingIndex + 1]] + offset);
    //        Gizmos.DrawLine(vertices[triangles[startingIndex + 1]] + offset, vertices[triangles[startingIndex + 2]] + offset);
    //        Gizmos.DrawLine(vertices[triangles[startingIndex + 2]] + offset, vertices[triangles[startingIndex]] + offset);
    //    }
    //}

    //private void ShowPoints(Color colour, IList<Vector3> points)
    //{
    //    for (int i = 0; i < points.Count; i++)
    //        ShowPoint(colour, points[i]);
    //}

    //private void ShowPoint(Color colour, Vector3 point, float size = 0.03f)
    //{
    //    Gizmos.color = colour;
    //    Gizmos.DrawSphere(point, size);
    //}

    //#endregion

}