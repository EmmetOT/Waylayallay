using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using NaughtyAttributes;

public class MorphTest : MonoBehaviour
{
    public bool HasMorph
    {
        get
        {
            return m_morph != null;
        }
    }

    public int PointCount
    {
        get
        {
            if (!HasMorph)
                return 0;

            return m_morph.PointCount;
        }
    }

    public Vector3 GetPoint(int i)
    {
        Morph.Point point = m_morph.GetPoint(i);
        return point == null ? default : point.LocalPosition;
    }

    public void SetPoint(int i, Vector3 vec)
    {
        m_morph.SetPoint(i, vec);
        m_resultMeshFilter.sharedMesh = m_morph.ToMesh();

        m_resultMeshFilter.sharedMesh.RecalculateNormals();
    }
    
    [SerializeField]
    private Morph m_morph;
    public Morph Morph { get { return m_morph; } }
    
    private bool m_initialized = false;

    [SerializeField]
    private bool m_drawOriginalMeshGizmos = false;

    [SerializeField]
    private bool m_drawGizmos = false;
    
    [SerializeField]
    private int m_connectedQueryOne;

    [SerializeField]
    private int m_connectedQueryTwo;

    [SerializeField]
    private MeshFilter[] m_testMeshes;

    [SerializeField]
    private MeshFilter m_resultMeshFilter;
    
    private void Awake()
    {
        Reinit();
    }

    public void Reinit()
    {
        Material[] mats = m_testMeshes[0].GetComponent<Renderer>().sharedMaterials;
        m_morph = new Morph(m_testMeshes);

        for (int i = 0; i < m_testMeshes.Length; i++)
            m_testMeshes[i].gameObject.SetActive(false);

        m_resultMeshFilter.sharedMesh = m_morph.ToMesh();
        m_resultMeshFilter.gameObject.SetActive(true);
        m_resultMeshFilter.GetComponent<Renderer>().sharedMaterials = mats;
    }

    [Button]
    public void CreateMorphFromMesh()
    {
        Reinit();
    }
    
    [Button]
    private void CheckIfConnected()
    {
        if (m_connectedQueryOne > m_morph.PointCount || m_connectedQueryTwo > m_morph.PointCount)
            return;

        if (m_morph.IsConnected(m_connectedQueryOne, m_connectedQueryTwo))
            Debug.Log("Connected!");
        else
            Debug.Log("Not Connected!");
    }

    [Button]
    private void Flip()
    {
        if (m_morph == null)
            Reinit();

        m_morph.FlipNormals();
        m_resultMeshFilter.sharedMesh = m_morph.ToMesh();
    }
    
    private List<Morph.Point> m_perimeter = null;

    private List<Morph.Point> GetPerimeter()
    {
        if (m_morph == null)
            return null;

        if (m_perimeter != null)
            return m_perimeter;

        foreach (Morph.Face face in m_morph.Faces)
        {
            m_perimeter = face.GetPerimeter();
            break;
        }

        return m_perimeter;
    }

    private Morph.Face GetFace()
    {
        foreach (Morph.Face face in m_morph.Faces)
            return face;

        return null;
    }

    private void OnDrawGizmos()
    {
        if (!m_drawOriginalMeshGizmos)
            return;

        //for (int i = 0; i < m_testMeshes.Length; i++)
        //{
        //    Vector3[] vertices = m_testMeshes[i].sharedMesh.vertices;
        //    Vector3[] normals = m_testMeshes[i].sharedMesh.normals;

        //    Matrix4x4 matrix = m_testMeshes[i].transform.localToWorldMatrix;

        //    for (int j = 0; j < vertices.Length; j++)
        //    {
        //        Gizmos.color = Color.blue;

        //        Gizmos.DrawSphere(matrix * vertices[j], 0.1f);
        //        Gizmos.DrawLine(matrix * vertices[j], matrix * (vertices[j] + normals[j] * 0.3f));

        //        Gizmos.color = Color.red;

        //        Gizmos.DrawSphere(vertices[j], 0.1f);
        //    }
        //}
    }

    //private void OnDrawGizmos()
    //{
    //    if (!m_drawGizmos || !Application.isPlaying)
    //        return;

    //    if (m_morph != null)
    //    {
    //        m_morph.DrawGizmo(transform);
    //        //m_morph.DrawFaces(transform);

    //        //Gizmos.color = Color.black;

    //        List<Morph.Point> perimeter = GetPerimeter();

    //        if (!perimeter.IsNullOrEmpty())
    //        {
    //            Matrix4x4 originalGizmoMatrix = Gizmos.matrix;
    //            Gizmos.matrix = m_resultMeshFilter.transform.localToWorldMatrix;

    //            foreach (Morph.Point point in perimeter)
    //                Gizmos.DrawSphere(point.LocalPosition, 0.1f);

    //            Gizmos.matrix = originalGizmoMatrix;
    //        }

    //        //DrawPerimeterCalc();
    //    }
    //}

    //private void DrawPerimeterCalc()
    //{
    //    if (!m_drawGizmos || !Application.isPlaying || m_morph == null || GetPerimeter() == null || m_resultMeshFilter == null)
    //        return;

    //    Morph.Face face = GetFace();

    //    if (face == null)
    //        return;

    //    List<Morph.Point> points = face.GetPoints();

    //    List<Morph.Point> perimeter = new List<Morph.Point>();

    //    // to make maths easier, this quaternion turns our maths 2D
    //    Quaternion normalizingRotation = Quaternion.FromToRotation(face.Normal, -Vector3.forward);

    //    Gizmos.color = Color.red;

    //    Vector2 _2dPos;
    //    for (int i = 0; i < points.Count; i++)
    //    {
    //        _2dPos = normalizingRotation * points[i].LocalPosition;
    //        Gizmos.DrawSphere(_2dPos, 0.2f);
    //    }

    //    // STEP 1: Find the leftmost (and if necessary, bottommost) point and start there

    //    Morph.Point leftMost = null;
    //    float leastX = Mathf.Infinity;
    //    float leastY = Mathf.Infinity;

    //    for (int i = 0; i < points.Count; i++)
    //    {
    //        _2dPos = normalizingRotation * points[i].LocalPosition;
    //        if (_2dPos.x == leastX)
    //        {
    //            if (_2dPos.y < leastY)
    //            {
    //                leftMost = points[i];
    //                leastY = _2dPos.y;
    //            }
    //        }
    //        else if (_2dPos.x < leastX)
    //        {
    //            leftMost = points[i];
    //            leastX = _2dPos.x;
    //            leastY = _2dPos.y;
    //        }
    //    }

    //    perimeter.Add(leftMost);

    //    Vector2 referenceDirection = Vector2.down;
    //    Morph.Point start = perimeter[perimeter.Count - 1];
    //    Morph.Point current = start;

    //    Gizmos.color = Color.blue;
    //    _2dPos = normalizingRotation * current.LocalPosition;
    //    Gizmos.DrawSphere(_2dPos, 0.4f);

    //    float bestAngle;
    //    float bestDistance;
    //    Morph.Point bestPoint;

    //    // iteration 1

    //    bestAngle = -Mathf.Infinity;
    //    bestDistance = Mathf.Infinity;
    //    bestPoint = null;

    //    for (int i = 0; i < points.Count; i++)
    //    {
    //        // don't ever look back!
    //        if (points[i] == current)
    //            continue;

    //        float angle = Vector2.SignedAngle(referenceDirection, ((normalizingRotation * points[i].LocalPosition) - (normalizingRotation * current.LocalPosition)).normalized);

    //        if (angle > bestAngle)
    //        {
    //            bestAngle = angle;
    //            bestPoint = points[i];
    //            bestDistance = (points[i].LocalPosition - current.LocalPosition).sqrMagnitude;
    //        }
    //        else if (angle == bestAngle)
    //        {
    //            float sqrDist = (points[i].LocalPosition - current.LocalPosition).sqrMagnitude;

    //            if (sqrDist < bestDistance)
    //            {
    //                bestPoint = points[i];
    //                bestDistance = sqrDist;
    //            }
    //        }
    //    }

    //    referenceDirection = ((normalizingRotation * current.LocalPosition) - (normalizingRotation * bestPoint.LocalPosition)).normalized;
    //    current = bestPoint;

    //    Gizmos.color = Color.blue;
    //    _2dPos = normalizingRotation * current.LocalPosition;
    //    Gizmos.DrawSphere(_2dPos, 0.4f);

    //    // iteration 2

    //    bestAngle = -Mathf.Infinity;
    //    bestDistance = Mathf.Infinity;
    //    bestPoint = null;

    //    for (int i = 0; i < points.Count; i++)
    //    {
    //        // don't ever look back!
    //        if (points[i] == current)
    //            continue;

    //        float angle = Vector2.SignedAngle(referenceDirection, ((normalizingRotation * points[i].LocalPosition) - (normalizingRotation * current.LocalPosition)).normalized);

    //        if (angle > bestAngle)
    //        {
    //            bestAngle = angle;
    //            bestPoint = points[i];
    //            bestDistance = (points[i].LocalPosition - current.LocalPosition).sqrMagnitude;
    //        }
    //        else if (angle == bestAngle)
    //        {
    //            float sqrDist = (points[i].LocalPosition - current.LocalPosition).sqrMagnitude;

    //            if (sqrDist < bestDistance)
    //            {
    //                bestPoint = points[i];
    //                bestDistance = sqrDist;
    //            }
    //        }
    //    }

    //    referenceDirection = ((normalizingRotation * bestPoint.LocalPosition) - (normalizingRotation * current.LocalPosition)).normalized;
    //    current = bestPoint;

    //    Gizmos.color = Color.blue;
    //    _2dPos = normalizingRotation * current.LocalPosition;
    //    Gizmos.DrawSphere(_2dPos, 0.4f);

    //}
}
