using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Timer))]
public class UIScalingButton : UIButton
{
    [SerializeField]
    private Timer m_timer;

    [SerializeField]
    private float m_hoveredScale = 1.02f;

    [SerializeField]
    private float m_hoveredTransitionDuration = 0.1f;

    private float m_initialScale;

    public override void Awake()
    {
        base.Awake();

        OnLeftClick.AddListener(OnMouseDown);
        OnLeftClickUp.AddListener(OnMouseUp);

        OnMouseEntered.AddListener(OnMouseEnter);
        OnMouseLeft.AddListener(OnMouseExit);

        m_initialScale = transform.localScale.x;
    }

    private float ButtonScale
    {
        get { return transform.localScale.x; }
        set { transform.localScale = value * Vector3.one; }
    }

    public void OnMouseDown()
    {
        ScaleContainer(m_initialScale);
    }
    
    public void OnMouseUp()
    {
        if (MouseInside)
            ScaleContainer(m_hoveredScale);
    }

    public void OnMouseEnter()
    {
        m_timer.StartSinerpTimer("UIScalingButton", ScaleContainer, m_hoveredTransitionDuration, ButtonScale, m_hoveredScale);
    }

    public void OnMouseExit()
    {
        m_timer.StartSinerpTimer("UIScalingButton", ScaleContainer, m_hoveredTransitionDuration, ButtonScale, m_initialScale);
    }

    private void ScaleContainer(float val)
    {
        ButtonScale = val;
    }
}
