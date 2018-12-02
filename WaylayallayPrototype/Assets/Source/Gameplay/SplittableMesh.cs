using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using Sone;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SplittableMesh : MonoBehaviour
{
    [SerializeField]
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

    private Vector3 m_positionCache;
    private Quaternion m_rotationCache;
    private Vector3 m_scaleCache;
    
    private void Awake()
    {
        m_positionCache = Transform.position;
        m_rotationCache = Transform.rotation;
        m_scaleCache = Transform.lossyScale;

        Debug.Log("Awake! (" + name + ")", this);

        m_stretcher = new MeshStretcher(MeshFilter, Manager.PlaneController.BisectionPlane);

        this.BindUntilDestroy(Sone.Event.FullyRecalculateSplittableMeshes, CalculateMesh);
        this.BindUntilDestroy<float>(Sone.Event.SetSplittableMeshStretching, UpdateMeshStretching);
        this.BindUntilDestroy<Controls.Code>(Sone.Event.OnCodeDown, OnCodeDown);
    }

    private void LateUpdate()
    {
        if (Transform.position != m_positionCache || Transform.rotation != m_rotationCache || Transform.lossyScale != m_scaleCache)
        {
            m_positionCache = Transform.position;
            m_rotationCache = Transform.rotation;
            m_scaleCache = Transform.lossyScale;

            m_stretcher.UpdateCalculationVertices();
            m_stretcher.Calculate();
        }
    }

    private void CalculateMesh()
    {
        m_stretcher.Calculate();
    }

    private void UpdateMeshStretching(float stretch)
    {
        m_stretcher.SetStretch(stretch);
    }

    private void OnDrawGizmosSelected()
    {
        if (m_stretcher == null)
            return;

        if (Manager.PlaneController.DrawCalculationPoints)
            m_stretcher.DrawCalculationPoints(Color.red);
    }

    private void OnCodeDown(Controls.Code code)
    {
        if (code == Controls.Code.COMBINE_GENERATED)
        {
            m_stretcher.Combine();
            Destroy(gameObject);
        }

    }
}
