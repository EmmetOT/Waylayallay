using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sone;
using UnibusEvent;
using Simplex;

public class PlaneController : Singleton<PlaneController>
{
    [BoxGroup("Controls")]
    [SerializeField]
    [MinValue(1f)]
    private float m_splitSpeed = 1f;

    [SerializeField]
    [MinValue(0f)]
    [BoxGroup("Controls")]
    [OnValueChanged("UpdateSplit")]
    private float m_split = 0f;

    [SerializeField]
    [OnValueChanged("OnPlaneChanged")]
    private BisectionPlane m_bisectionPlane = new BisectionPlane();
    public BisectionPlane BisectionPlane { get { return m_bisectionPlane; } }

    [SerializeField]
    [BoxGroup("Settings")]
    private Transform m_generatedMeshRoot;
    public Transform GeneratedMeshRoot { get { return m_generatedMeshRoot; } }

    [SerializeField]
    [BoxGroup("Settings")]
    private bool m_generateRigidbodies = false;
    public bool GenerateRigidbodies { get { return m_generateRigidbodies; } }

    [SerializeField]
    [BoxGroup("Debug")]
    private bool m_drawPlaneGizmo = false;

    [SerializeField]
    [BoxGroup("Debug")]
    private bool m_drawCalculationPoints = false;
    public bool DrawCalculationPoints { get { return m_drawCalculationPoints; } }

    public Vector3 PlaneOffset { get { return m_bisectionPlane.Normal.normalized * m_split * 0.5f; } }

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
    
    private void OnDrawGizmos()
    {
        if (m_drawPlaneGizmo)
        {
            Vector3 offset = PlaneOffset;

            m_bisectionPlane.DrawGizmo(Transform, PlaneOffset);

            if (offset != Vector3.zero)
                m_bisectionPlane.DrawGizmo(Transform, -PlaneOffset);
        }
    }

    private void Update()
    {
        float scrollDelta = Input.mouseScrollDelta.y * Time.deltaTime * m_splitSpeed;

        if (scrollDelta == 0f)
            return;

        m_split = Mathf.Max(0f, m_split + scrollDelta);

        UpdateSplit();
    }

    private void UpdateSplit()
    {
        Unibus.Dispatch(Sone.Event.SetSplittableMeshStretching, m_split);
    }

    private void OnPlaneChanged()
    {
        Unibus.Dispatch(Sone.Event.FullyRecalculateSplittableMeshes);

        if (m_split > 0f)
        {
            Unibus.Dispatch(Sone.Event.BakeSplitMeshes);
            m_split = 0f;
        }
    }
}
