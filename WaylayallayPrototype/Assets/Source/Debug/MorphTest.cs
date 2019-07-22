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
    private MeshFilter m_testMesh;

    [SerializeField]
    private MeshFilter m_resultMeshFilter;

    private void Awake()
    {
        Reinit();
    }

    public void Reinit()
    {
        Material[] mats = m_testMesh.GetComponent<Renderer>().sharedMaterials;
        m_morph = new Morph(m_testMesh, m_collapseColocatedPoints);

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

    private void OnDrawGizmos()
    {
        if (!m_drawGizmos || !Application.isPlaying)
            return;

        if (m_morph != null)
            m_morph.DrawGizmo(m_resultMeshFilter?.transform);

        if (m_testMesh != null)
        {
            GUIStyle handleStyle = new GUIStyle();
            handleStyle.normal.textColor = Color.black;
            handleStyle.fontSize = 10;

            Vector3[] vertices = m_testMesh.sharedMesh.vertices;
            Vector2[] uv = m_testMesh.sharedMesh.uv;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                UnityEditor.Handles.Label(m_testMesh.transform.position + vertices[i] + Vector3.up * 0.3f, uv[i].ToString(), handleStyle);
            }

        }
    }
}
