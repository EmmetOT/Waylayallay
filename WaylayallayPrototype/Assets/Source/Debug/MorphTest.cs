using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using NaughtyAttributes;
using UnityEditor;

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

    public IEnumerable<Morph.Point> Points
    {
        get
        {
            foreach (Morph.Point point in m_morph.Points)
                yield return point;
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

    public void SetPoint(Morph.Point point, Vector3 vec)
    {
        m_morph.SetPoint(point, vec);
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
    public bool DrawGizmos { get { return m_drawGizmos; } }

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

        m_resultMeshFilter.sharedMesh = m_morph.ToMesh();

        for (int i = 0; i < m_testMeshes.Length; i++)
        {
            m_testMeshes[i].gameObject.SetActive(false);
        }

        m_resultMeshFilter.gameObject.SetActive(true);
        m_resultMeshFilter.GetComponent<Renderer>().sharedMaterials = mats;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (m_morph != null)
            {
                m_morph.TryRetriangulateFace(0);
                //m_morph.TryRetriangulateFace(1);
                //m_morph.TryRetriangulateFace(2);
                //m_morph.TryRetriangulateFace(3);
                m_resultMeshFilter.sharedMesh = m_morph.ToMesh();
            }
        }
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

        if (m_morph.HasPath(m_connectedQueryOne, m_connectedQueryTwo))
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
}
