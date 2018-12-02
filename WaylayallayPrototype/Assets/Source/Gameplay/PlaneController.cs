using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sone;
using UnibusEvent;
using Simplex;

public class PlaneController : Singleton<PlaneController>
{
    [BoxGroup("Control Settings")]
    [SerializeField]
    [MinValue(1f)]
    private float m_splitSpeed = 1f;

    [SerializeField]
    [OnValueChanged("UpdateSplittableMeshes")]
    private BisectionPlane m_bisectionPlane = new BisectionPlane();
    public BisectionPlane BisectionPlane { get { return m_bisectionPlane; } }

    [SerializeField]
    [BoxGroup("Debug")]
    private bool m_drawPlaneGizmo = false;

    [SerializeField]
    [BoxGroup("Debug")]
    private bool m_drawCalculationPoints = false;
    public bool DrawCalculationPoints { get { return m_drawCalculationPoints; } }

    [SerializeField]
    [MinValue(0f)]
    [OnValueChanged("UpdateSplit")]
    private float m_split = 0f;

    [SerializeField]
    private Transform m_generatedMeshRoot;
    public Transform GeneratedMeshRoot { get { return m_generatedMeshRoot; } }

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

    protected override void Awake()
    {
        base.Awake();

        Unibus.Subscribe<Controls.Code>(Sone.Event.OnCodeDown, OnCodeDown);
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

    private void UpdateSplittableMeshes()
    {
        Unibus.Dispatch(Sone.Event.FullyRecalculateSplittableMeshes);
    }

    private void OnCodeDown(Controls.Code code)
    {
        if (code == Controls.Code.COMBINE_GENERATED)
        {
            m_split = 0f;
        }
    }

}
