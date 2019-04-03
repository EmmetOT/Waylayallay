using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using Simplex;
using UnibusEvent;

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
        
        m_stretcher = new MeshStretcher(MeshFilter, Manager.PlaneController.BisectionPlane);

        Unibus.Subscribe(Simplex.Event.FullyRecalculateSplittableMeshes, CalculateMesh);
        Unibus.Subscribe<float>(Simplex.Event.SetSplittableMeshStretching, UpdateMeshStretching);
        Unibus.Subscribe(Simplex.Event.BakeSplitMeshes, BakeSplitMesh);
    }

    private void OnDestroy()
    {
        Unibus.Unsubscribe(Simplex.Event.FullyRecalculateSplittableMeshes, CalculateMesh);
        Unibus.Unsubscribe<float>(Simplex.Event.SetSplittableMeshStretching, UpdateMeshStretching);
        Unibus.Unsubscribe(Simplex.Event.BakeSplitMeshes, BakeSplitMesh);
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
        m_stretcher.SetStretch(stretch, gameObject);
    }

    private void OnDrawGizmos()
    {
        if (m_stretcher == null)
            return;

        if (Manager.PlaneController.DrawCalculationPoints)
            m_stretcher.DrawCalculationPoints(Color.red);

        if (Manager.PlaneController.DrawUpperMeshSimplifierGizmos)
            m_stretcher.DrawMeshSimplifierGizmos(0, Vector3.left * 0.1f);

        if (Manager.PlaneController.DrawLowerMeshSimplifierGizmos)
            m_stretcher.DrawMeshSimplifierGizmos(1, Vector3.left * -0.1f);

        if (Manager.PlaneController.DrawMeshSimplifierEdges)
        {
            m_stretcher.DrawMeshSimplifierEdges(0, Color.red, Vector3.right * 0.1f);
            m_stretcher.DrawMeshSimplifierEdges(1, Color.blue, Vector3.right * -0.1f);

        }
    }

    private void BakeSplitMesh()
    {
        m_stretcher.Combine();
        Destroy(gameObject);
    }
}
