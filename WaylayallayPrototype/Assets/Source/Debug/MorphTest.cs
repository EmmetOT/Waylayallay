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

    public void Reinit()
    {
        m_morph = new Morph(m_testMesh, m_collapseColocatedPoints);
        m_testMesh.gameObject.SetActive(false);

        m_resultMeshFilter.sharedMesh = m_morph.ToMesh();
        m_resultMeshFilter.gameObject.SetActive(true);
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
        if (m_morph.IsConnected(m_connectedQueryOne, m_connectedQueryTwo))
            Debug.Log("Connected!");
        else
            Debug.Log("Not Connected!");
    }

    [Button]
    private void Flip()
    {
        m_morph = new Morph(m_testMesh, m_collapseColocatedPoints);
        m_testMesh.gameObject.SetActive(false);
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
        if (m_morph == null || !m_drawGizmos)
            return;

        m_morph.DrawGizmo();
    }
}
