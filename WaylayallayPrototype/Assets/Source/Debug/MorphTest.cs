using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using NaughtyAttributes;

public class MorphTest : MonoBehaviour
{
    [SerializeField]
    [OnValueChanged("Reinit")]
    private bool m_collapseColocatedPoints = false;

    [SerializeField]
    private bool m_moving = false;

    [SerializeField]
    private float m_moveSpeed = 100f;

    [SerializeField]
    private int m_movingPoint = 0;

    private bool m_initialized = false;

    [SerializeField]
    private bool m_drawGizmos = false;

    [SerializeField]
    private Morph m_morph;

    [SerializeField]
    private Vector3 m_testVecA;

    [SerializeField]
    private Vector3 m_testVecB;

    [SerializeField]
    private int m_connectedQueryOne;

    [SerializeField]
    private int m_connectedQueryTwo;

    [SerializeField]
    private MeshFilter[] m_testMeshes;

    [SerializeField]
    private MeshFilter m_resultMeshFilter;

    [Button]
    private void TestUnityVector2SignedAngle()
    {
        Debug.Log("(0, -1) and (-1, 0) gives " + Vector2.SignedAngle(new Vector2(0f, -1f), new Vector2(-1f, 0f)).ToString());
        Debug.Log("(-1, 0) and (0, 1) gives " + Vector2.SignedAngle(new Vector2(-1f, 0f), new Vector2(0f, 1f)).ToString());
        Debug.Log("(-2, 0) and (0, 2) gives " + Vector2.SignedAngle(new Vector2(-2f, 0f), new Vector2(0f, 2f)).ToString());
        Debug.Log("(-2, 0) and (0, 1) gives " + Vector2.SignedAngle(new Vector2(-2f, 0f), new Vector2(0f, 1f)).ToString());
        Debug.Log("(-2, 0) and (0, 0.5) gives " + Vector2.SignedAngle(new Vector2(-2f, 0f), new Vector2(0f, 0.5f)).ToString());
        Debug.Log("(0, -1) and (0, 1) gives " + Vector2.SignedAngle(new Vector2(0f, -1f), new Vector2(0f, 1f)).ToString());
    }

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
    private void CreateMesh()
    {
        if (m_morph == null)
            m_morph = new Morph();

        m_morph.AddEdge(m_testVecA, m_testVecB);
    }

    [Button]
    private void CreateForwardMesh()
    {
        m_morph = new Morph();

        m_morph.AddTriangle(new Vector3(0f, 1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f));
    }

    [Button]
    private void CreateBackwardMesh()
    {
        m_morph = new Morph();

        m_morph.AddTriangle(new Vector3(0f, 1f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f));
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

    [Button]
    private void MovePointA()
    {
        if (m_morph == null)
            return;

        m_morph.GetPoint(0).Position += Vector3.right * 0.1f;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Q))
            CreateMorphFromMesh();

        if (m_moving)
        {
            m_movingPoint = m_movingPoint % m_morph.PointCount;

            m_morph.GetPoint(m_movingPoint).Position += Vector3.right * Time.deltaTime * m_moveSpeed;

            m_resultMeshFilter.sharedMesh = m_morph.ToMesh();
        }
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
        {
            return face;
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        if (!m_drawGizmos || !Application.isPlaying)
            return;

        if (m_morph != null)
        {
            m_morph.DrawFaces(m_resultMeshFilter?.transform);

            //Gizmos.color = Color.black;

            //List<Morph.Point> perimeter = GetPerimeter();

            //if (!perimeter.IsNullOrEmpty())
            //{
            //    Matrix4x4 originalGizmoMatrix = Gizmos.matrix;
            //    Gizmos.matrix = m_resultMeshFilter.transform.localToWorldMatrix;

            //    foreach (Morph.Point point in perimeter)
            //    {
            //        Gizmos.DrawSphere(point.Position, 0.4f);
            //    }

            //    Gizmos.matrix = originalGizmoMatrix;
            //}

            //DrawPerimeterCalc();
        }
    }

    private void DrawPerimeterCalc()
    {
        if (!m_drawGizmos || !Application.isPlaying || m_morph == null || GetPerimeter() == null || m_resultMeshFilter == null)
            return;

        Morph.Face face = GetFace();

        if (face == null)
            return;

        List<Morph.Point> points = face.GetPoints();

        List<Morph.Point> perimeter = new List<Morph.Point>();

        // to make maths easier, this quaternion turns our maths 2D
        Quaternion normalizingRotation = Quaternion.FromToRotation(face.Normal, -Vector3.forward);

        Gizmos.color = Color.red;

        Vector2 _2dPos;
        for (int i = 0; i < points.Count; i++)
        {
            _2dPos = normalizingRotation * points[i].Position;
            Gizmos.DrawSphere(_2dPos, 0.2f);
        }

        // STEP 1: Find the leftmost (and if necessary, bottommost) point and start there

        Morph.Point leftMost = null;
        float leastX = Mathf.Infinity;
        float leastY = Mathf.Infinity;

        for (int i = 0; i < points.Count; i++)
        {
            _2dPos = normalizingRotation * points[i].Position;
            if (_2dPos.x == leastX)
            {
                if (_2dPos.y < leastY)
                {
                    leftMost = points[i];
                    leastY = _2dPos.y;
                }
            }
            else if (_2dPos.x < leastX)
            {
                leftMost = points[i];
                leastX = _2dPos.x;
                leastY = _2dPos.y;
            }
        }

        perimeter.Add(leftMost);

        Vector2 referenceDirection = Vector2.down;
        Morph.Point start = perimeter[perimeter.Count - 1];
        Morph.Point current = start;

        Gizmos.color = Color.blue;
        _2dPos = normalizingRotation * current.Position;
        Gizmos.DrawSphere(_2dPos, 0.4f);

        float bestAngle;
        float bestDistance;
        Morph.Point bestPoint;

        // iteration 1

        bestAngle = -Mathf.Infinity;
        bestDistance = Mathf.Infinity;
        bestPoint = null;

        for (int i = 0; i < points.Count; i++)
        {
            // don't ever look back!
            if (points[i] == current)
                continue;

            float angle = Vector2.SignedAngle(referenceDirection, ((normalizingRotation * points[i].Position) - (normalizingRotation * current.Position)).normalized);
            
            if (angle > bestAngle)
            {
                bestAngle = angle;
                bestPoint = points[i];
                bestDistance = (points[i].Position - current.Position).sqrMagnitude;
            }
            else if (angle == bestAngle)
            {
                float sqrDist = (points[i].Position - current.Position).sqrMagnitude;

                if (sqrDist < bestDistance)
                {
                    bestPoint = points[i];
                    bestDistance = sqrDist;
                }
            }
        }
        
        referenceDirection = ((normalizingRotation * current.Position) - (normalizingRotation * bestPoint.Position)).normalized;
        current = bestPoint;

        Gizmos.color = Color.blue;
        _2dPos = normalizingRotation * current.Position;
        Gizmos.DrawSphere(_2dPos, 0.4f);

        // iteration 2

        bestAngle = -Mathf.Infinity;
        bestDistance = Mathf.Infinity;
        bestPoint = null;

        for (int i = 0; i < points.Count; i++)
        {
            // don't ever look back!
            if (points[i] == current)
                continue;

            float angle = Vector2.SignedAngle(referenceDirection, ((normalizingRotation * points[i].Position) - (normalizingRotation * current.Position)).normalized);

            if (angle > bestAngle)
            {
                bestAngle = angle;
                bestPoint = points[i];
                bestDistance = (points[i].Position - current.Position).sqrMagnitude;
            }
            else if (angle == bestAngle)
            {
                float sqrDist = (points[i].Position - current.Position).sqrMagnitude;

                if (sqrDist < bestDistance)
                {
                    bestPoint = points[i];
                    bestDistance = sqrDist;
                }
            }
        }

        referenceDirection = ((normalizingRotation * bestPoint.Position) - (normalizingRotation * current.Position)).normalized;
        current = bestPoint;

        Gizmos.color = Color.blue;
        _2dPos = normalizingRotation * current.Position;
        Gizmos.DrawSphere(_2dPos, 0.4f);

    }
}
