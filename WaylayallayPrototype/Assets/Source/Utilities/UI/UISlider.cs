using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UISlider : MonoBehaviour
{
    [SerializeField]
    private Image m_slider;
    public Image Slider {get{return m_slider;}}

    [SerializeField]
    private float m_changeDuration = 0.2f;

    private Coroutine m_sliderCoroutine;
    
    /// <summary>
    /// Set the slider to the given value, where 0 is empty and 1 is full.
    /// Can use the optional "force" parameter to immediately set the fill, otherwise it will
    /// smooth step over the duration specified in the editor.
    /// </summary>
    public void SetSlider(float fill, bool instant = false)
    {
        fill = Mathf.Clamp01(fill);

        if (m_sliderCoroutine != null)
        {
            StopCoroutine(m_sliderCoroutine);
            m_sliderCoroutine = null;
        }

        if (instant)
            m_slider.fillAmount = fill;
        else
            m_sliderCoroutine = StartCoroutine(Cr_ChangeSlider(m_changeDuration, m_slider.fillAmount, fill));
    }
    
    /// <summary>
    /// Coroutine to nicely handle the slider moving up and down as ingredients are changed.
    /// </summary>
    private IEnumerator Cr_ChangeSlider(float duration, float from, float to)
    {
        float timer = duration;

        m_slider.fillAmount = from;

        while (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;

            m_slider.fillAmount = Mathf.SmoothStep(to, from, timer / duration);

            yield return null;
        }

        m_slider.fillAmount = to;
    }
}
