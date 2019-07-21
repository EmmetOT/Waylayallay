using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using NaughtyAttributes;

public class MorphTest : MonoBehaviour
{
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

    [Button]
    public void CreateMorphFromMesh()
    {
        m_morph = new Morph(m_testMesh);
        m_testMesh.gameObject.SetActive(false);
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
        if (m_morph == null)
            return;

        m_morph.FlipNormals();
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
    }

    private void OnDrawGizmos()
    {
        if (m_morph == null || !m_drawGizmos)
            return;

        m_morph.DrawGizmo();
    }
}
