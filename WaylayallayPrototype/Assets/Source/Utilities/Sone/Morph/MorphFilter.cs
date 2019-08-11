using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using NaughtyAttributes;
using UnityEditor;
using System;
using System.Linq;

/// <summary>
/// This component is the Morph 'equivalent' of a MeshFilter.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MorphFilter : MonoBehaviour
{
    #region Variables

    [SerializeField]
    private MeshFilter[] m_sourceMeshFilters;

    [SerializeField, HideInInspector]
    private MeshFilter m_targetMeshFilter;

    [SerializeField, HideInInspector]
    private Renderer m_targetMeshRenderer;

    [SerializeField]
    private Morph m_morph;
    public Morph Morph { get { return m_morph; } }

    #endregion

    #region Unity Callbacks

    private void Reset()
    {
        Debug.Log("Reset");

        m_sourceMeshFilters = new MeshFilter[0];
        m_targetMeshFilter = GetComponent<MeshFilter>();
        m_targetMeshRenderer = GetComponent<Renderer>();
        GenerateMorph();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Use the data in the provided mesh filters to generate a new mesh and store it in the 
    /// attached mesh filter.
    /// </summary>
    public void GenerateMorph()
    {
        if (m_sourceMeshFilters.IsNullOrEmpty())
        {
            m_targetMeshFilter.sharedMesh = null;
            m_morph = null;
            return;
        }

        m_sourceMeshFilters = m_sourceMeshFilters.Distinct().ToArray();

        // generate the morph from the source mesh filters
        m_morph = new Morph(m_sourceMeshFilters);

        Refresh();

        // deactivate the source mesh filter gameobjects
        for (int i = 0; i < m_sourceMeshFilters.Length; i++)
            if (m_sourceMeshFilters[i] != null)
                m_sourceMeshFilters[i].gameObject.SetActive(false);

        // set the target's mesh filter's materials to that of one of the source's materials. (kind of a crude solution)
        Material[] materials = GetFirstSourceMaterials();

        if (!materials.IsNullOrEmpty())
            m_targetMeshRenderer.sharedMaterials = materials;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Refresh()
    {
        // set the target mesh filter to the newly generated mesh
        m_targetMeshFilter.sharedMesh = m_morph.ToMesh();
    }

    /// <summary>
    /// Get the point stored in the morph with the given ID, if it exists.
    /// </summary>
    public Vector3 GetPoint(int i)
    {
        Point point = m_morph.GetPoint(i);
        return point == null ? default : point.LocalPosition;
    }

    /// <summary>
    /// If the point with the given ID exists in the morph, set it to the new position, as
    /// well as any of its colocated chums, and regenerate the mesh.
    /// </summary>
    public void SetPointAndRegenerateMesh(int i, Vector3 vec)
    {
        m_morph.SetPoint(i, vec);

        // set the target mesh filter to the newly generated mesh
        m_targetMeshFilter.sharedMesh = m_morph.ToMesh();
    }
    
    /// <summary>
    /// If the given point exists in the morph, set it to the new position, as
    /// well as any of its colocated chums, and regenerate the mesh.
    /// </summary>
    public void SetPointAndRegenerateMesh(Point point, Vector3 vec)
    {
        m_morph.SetPoint(point, vec);

        // set the target mesh filter to the newly generated mesh
        m_targetMeshFilter.sharedMesh = m_morph.ToMesh();//
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Get a material array from the source mesh filters. Just the first one it can find.
    /// </summary>
    private Material[] GetFirstSourceMaterials()
    {
        for (int i = 0; i < m_sourceMeshFilters.Length; i++)
        {
            if (m_sourceMeshFilters[i] != null)
            {
                Renderer renderer = m_sourceMeshFilters[i].GetComponent<Renderer>();

                if (renderer != null)
                {
                    return renderer.sharedMaterials;
                }
            }
        }

        return null;
    }

    #endregion
}
