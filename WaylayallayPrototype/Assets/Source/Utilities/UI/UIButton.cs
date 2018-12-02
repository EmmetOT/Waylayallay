using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using NaughtyAttributes;

/// <summary>
/// Because the normal unity button can't handle right clicks for some awful reason.
/// 
/// This class is basically meant to emulate the UI Button but with support for right click.
/// </summary>
public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Graphic[] TargetGraphics;

    private bool m_hasGraphic = true;

    [SerializeField]
    private bool m_interactable = true;
    public bool Interactable
    {
        get { return m_interactable; }
        set { SetInteractable(value); }
    }
    
    [SerializeField]
    // allow mouse in / mouse out behaviours even when interactable is set to false
    private bool m_alwaysAllowHoverBehaviour;
    public bool AlwaysAllowHoverBehaviour { get { return m_alwaysAllowHoverBehaviour; } }

    [SerializeField]
    // if true, this button wont get loaded by the button navigator during OnLoadButtons
    private bool m_ignoredByButtonNavigator = false;
    public bool IgnoredByButtonNavigator { get { return m_ignoredByButtonNavigator; } }

    public float FadeDuration = 0.1f;

    public Color DefaultColor;
    public Color HoveredColor;
    public Color LeftClickColor;
    public Color RightClickColor;
    public Color DisabledColor;
    
    public UnityEvent OnLeftClick;
    public UnityEvent OnRightClick;

    public UnityEvent OnLeftClickUp;
    public UnityEvent OnRightClickUp;

    public UnityEvent OnMouseEntered;
    public UnityEvent OnMouseLeft;
    
    [SerializeField, ReadOnly]
    private bool m_mouseInside = false;
    public bool MouseInside { get { return m_mouseInside; } }

    private RectTransform m_rectTransform;
    public RectTransform RectTransform {
        get {
            if(m_rectTransform == null)
                m_rectTransform = gameObject.GetComponent<RectTransform>();
            return m_rectTransform;
        }
    }

    [System.Flags]
    public enum MouseDown
    {
        NONE = 0,
        LEFT = 1,
        RIGHT = 2,
        BOTH = 3
    }

    [SerializeField, ReadOnly]
    private MouseDown m_mouseDown = MouseDown.NONE;
    public MouseDown CurrentMouseHeld { get { return m_mouseDown; } }

    public virtual void Awake()
    {
        if (TargetGraphics == null || TargetGraphics.Length == 0 || TargetGraphics[0] == null)
            m_hasGraphic = false;
        else
        {
            for (int i = 0; i < TargetGraphics.Length; i++)
            {
                TargetGraphics[i].color = DefaultColor;
            }            
        }
    }

    private void OnEnable()
    {
        UpdateButtonColor();
    }

    #region Events

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            LeftClickDown();
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RightClickDown();
        }
    }

    public void LeftClickDown()
    {
        if (m_interactable)
        {
			ColourTransition(LeftClickColor);
			OnLeftClick.Invoke();
        }

        m_mouseDown |= MouseDown.LEFT;
    }

    public void RightClickDown()
    {
        if (m_interactable)
        {
			ColourTransition(RightClickColor);
			OnRightClick.Invoke();
        }

        m_mouseDown |= MouseDown.RIGHT;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            LeftClickUp();
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RightClickUp();
        }
    }

    public void LeftClickUp()
    {
        if (m_interactable)
        {
            ColourTransition((m_mouseDown & MouseDown.RIGHT) == MouseDown.RIGHT ? RightClickColor : m_mouseInside ? HoveredColor : DefaultColor);
			OnLeftClickUp.Invoke();
		}

		m_mouseDown ^= MouseDown.LEFT;
    }

    public void RightClickUp()
    {
        if (m_interactable)
        {
            ColourTransition((m_mouseDown & MouseDown.LEFT) == MouseDown.LEFT ? LeftClickColor : m_mouseInside ? HoveredColor : DefaultColor);
			OnRightClickUp.Invoke();
		}

		m_mouseDown ^= MouseDown.RIGHT;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MouseEnter();
    }

    public void MouseEnter()
    {
        m_mouseInside = true;

        if ((m_mouseDown == MouseDown.NONE && m_interactable) || m_alwaysAllowHoverBehaviour)
        {
            ColourTransition(HoveredColor);
			OnMouseEntered.Invoke();
		}
	}

    public void OnPointerExit(PointerEventData eventData)
    {
        MouseExit();
    }

    public void MouseExit()
    {
        m_mouseInside = false;

        if ((m_mouseDown == MouseDown.NONE && m_interactable) || m_alwaysAllowHoverBehaviour)
        {
			ColourTransition((m_mouseDown != MouseDown.NONE) ? HoveredColor : DefaultColor);
			OnMouseLeft.Invoke();
        }
    }

    #endregion

    #region Colour Stuff

    private void ColourTransition(Color to)
    {
        if (!m_hasGraphic)
            return;

        StopAllCoroutines();

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(Cr_ColourTransition(TargetGraphics[0].color, m_interactable ? to : DisabledColor));
        }
        else
        {
            foreach (Graphic graphic in TargetGraphics)
                graphic.color = m_interactable ? to : DisabledColor;
        }
    }

    private IEnumerator Cr_ColourTransition(Color from, Color to)
    {
        if (m_hasGraphic)
        {
            float timer = FadeDuration;

            foreach (Graphic graphic in TargetGraphics)
                graphic.color = from;

            while (timer > 0f)
            {
                yield return null;

                timer -= Time.unscaledDeltaTime;

                Color newCol = Color.Lerp(from, to, (1f - (timer / FadeDuration)));

                foreach (Graphic graphic in TargetGraphics)
                    graphic.color = newCol;
            }

            foreach (Graphic graphic in TargetGraphics)
                graphic.color = to;
        }
    }

    public void SetAllColours(Color colour)
    {
        DefaultColor = colour;
        HoveredColor = colour;
        LeftClickColor = colour;
        RightClickColor = colour;
        DisabledColor = colour;

        foreach (Graphic graphic in TargetGraphics)
            graphic.color = colour;
    }

    protected void UpdateButtonColor()
    {
        if (m_hasGraphic && TargetGraphics.Length > 0)
        {
            for (int i = 0; i < TargetGraphics.Length; i++)
            {
                if (m_interactable)
                    TargetGraphics[i].color = DefaultColor;
                else
                    TargetGraphics[i].color = DisabledColor;
            }
        }
    }

    #endregion

    public bool IsMouseOver
    {
        get
        {
            return EventSystem.current.IsPointerOverGameObject(); 
        }
    }

    /// <summary>
    /// Manually set this button to no mouse inside, no mouse down.
    /// </summary>
    public void Reset()
    {
        m_mouseDown = MouseDown.NONE;
        m_mouseInside = false;
    }

    public void ClearTargetGraphics()
    {
        m_hasGraphic = false;
        TargetGraphics = null;
    }
    
    /// <summary>
    /// Toggles this buttons interactability. Counts as a "mouse out" if setting false,
    /// and a "mouse in" if setting true and the mouse is over the button.
    /// </summary>
    public void SetInteractable(bool interactable, bool force = false)
    {
        if (m_interactable == interactable)
            return;

        m_interactable = interactable;
        
        Color targetColour;

        if (m_interactable)
        {
            if (m_mouseInside)
            {
                targetColour = HoveredColor;
                OnMouseEntered.Invoke();
            }
            else
            {
                targetColour = DefaultColor;
            }
        }
        else
        {
            targetColour = DisabledColor;

            if (m_mouseInside)
                OnMouseLeft.Invoke();
        }

        if (force)
        {
            foreach (Graphic graphic in TargetGraphics)
                graphic.color = targetColour;
        }
        else
        {
            ColourTransition(targetColour);
        }
    }
}
