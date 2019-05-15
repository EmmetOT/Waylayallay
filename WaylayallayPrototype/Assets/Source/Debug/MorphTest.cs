using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using NaughtyAttributes;

public class MorphTest : MonoBehaviour
{
    private bool m_initialized = false;

    [SerializeField]
    private Morph m_morph;
    
    [Button]
    private void CreateMesh()
    {
        m_morph = new Morph();

        Morph.Triangle t0 = m_morph.AddTriangle(new Vector3(0f, 1f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f));
        Morph.Triangle t1 = m_morph.AddTriangle(new Vector3(0f, -1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f));

        //m_morph.DebugMode = true;
        
        //m_morph.AddEdge(0, 3);
        
        //m_morph.AddEdge(m_morph.GetPoint(0), new Vector3(0.444f, 1f, 0.2f));
        //m_morph.AddEdge(new Vector3(0.444f, 1f, 0.2f), m_morph.GetPoint(2));
    }

    [Button]
    private void MovePointA()
    {
        if (m_morph == null)
            return;

        m_morph.GetPoint(0).Position += Vector3.right * 0.1f;
    }

    private void OnDrawGizmos()
    {
        if (m_morph == null)
            return;

        m_morph.DrawGizmo();
    }
}
