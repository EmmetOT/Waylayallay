using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sone.Maths;

/// <summary>
/// This component provides a handy wrapper for interpolation coroutines.
/// 
/// Optionally affected by paranoid pause, defaulting to not.
/// </summary>
[DisallowMultipleComponent]
public sealed class Timer : MonoBehaviour
{
    [SerializeField]
    private bool m_useUnscaledTime = false;

    [SerializeField]
    private List<AnimationCurve> m_customCurves;
    
    private Dictionary<object, Coroutine> m_coroutines = new Dictionary<object, Coroutine>();

    private HashSet<object> m_pausedTimers = new HashSet<object>();

    private enum InterpType
    {
        LERP,
        SMOOTHSTEP,
        SINERP,
        COSERP,
        BERP
    }

    /// <summary>
    /// Returns true if this timer contains a currently active timer with the given ID.
    /// </summary>
    public bool IsRunning(object id)
    {
        return (m_coroutines != null && m_coroutines.ContainsKey(id) && m_coroutines[id] != null);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated between values over time using linear interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="from">Interpolate from this value.</param>
    /// <param name="to">Interpolate to this value.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartLerpTimer(object id, Action<float> function, float duration, float from, float to, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, from, to, InterpType.LERP, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated between values over time using a custom animation curve supplied in the editor.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="curveIndex">The index of the animation curve, specified in the editor.</param>
    /// <param name="from">Interpolate from this value.</param>
    /// <param name="to">Interpolate to this value.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartAnimationCurveTimer(object id, Action<float> function, float duration, int curveIndex, float from, float to, Action endFunction = null, float delayStart = 0f)
    {
        if (m_customCurves == null || m_customCurves.Count == 0)
        {
            Debug.LogError("Timer has no defined animation curves!", gameObject);
            return;
        }
        else if (curveIndex < 0 || curveIndex >= m_customCurves.Count)
        {
            Debug.LogError("Invalid animation curve index for timer. Valid indices are 0 to " + (m_customCurves.Count - 1) + ", but index provided was " + curveIndex + "!", gameObject);
            return;
        }
        
        StartTimer(id, function, duration, from, to, InterpType.LERP, endFunction, delayStart, animationCurveIndex: curveIndex);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated between values over time using a custom animation curve.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="animationCurve">The animation curve to use.</param>
    /// <param name="from">Interpolate from this value.</param>
    /// <param name="to">Interpolate to this value.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartAnimationCurveTimer(object id, Action<float> function, float duration, AnimationCurve curve, float from, float to, Action endFunction = null, float delayStart = 0f)
    {
        if (m_customCurves == null || m_customCurves.Count == 0)
            m_customCurves = new List<AnimationCurve>();

        m_customCurves.Add(curve);

        StartAnimationCurveTimer(id, function, duration, m_customCurves.Count - 1, from, to, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated between values over time using smooth step interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="from">Interpolate from this value.</param>
    /// <param name="to">Interpolate to this value.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartSmoothStepTimer(object id, Action<float> function, float duration, float from, float to, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, from, to, InterpType.SMOOTHSTEP, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated between values over time using sinusoidal interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="from">Interpolate from this value.</param>
    /// <param name="to">Interpolate to this value.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartSinerpTimer(object id, Action<float> function, float duration, float from, float to, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, from, to, InterpType.SINERP, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated between values over time using cosinusoidal interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="from">Interpolate from this value.</param>
    /// <param name="to">Interpolate to this value.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartCoserpTimer(object id, Action<float> function, float duration, float from, float to, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, from, to, InterpType.COSERP, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated between values over time using "boing-like" interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="from">Interpolate from this value.</param>
    /// <param name="to">Interpolate to this value.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartBerpTimer(object id, Action<float> function, float duration, float from, float to, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, from, to, InterpType.BERP, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated from 0 to 1 over time using linear interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartLerpTimer(object id, Action<float> function, float duration, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, 0f, 1f, InterpType.LERP, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated between values over time from 0 to 1 using a custom animation curve supplied in the editor.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="curveIndex">The index of the animation curve, specified in the editor.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartAnimationCurveTimer(object id, Action<float> function, float duration, int curveIndex, Action endFunction = null, float delayStart = 0f)
    {
        StartAnimationCurveTimer(id, function, duration, curveIndex, 0f, 1f, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated between values over time from 0 to 1 using a custom animation curve.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="animationCurve">The animation curve to use.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartAnimationCurveTimer(object id, Action<float> function, float duration, AnimationCurve curve, Action endFunction = null, float delayStart = 0f)
    {
        StartAnimationCurveTimer(id, function, duration, curve, 0f, 1f, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated from 0 to 1 over time using smooth step interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartSmoothStepTimer(object id, Action<float> function, float duration, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, 0f, 1f, InterpType.SMOOTHSTEP, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated from 0 to 1 over time using sinusoidal interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartSinerpTimer(object id, Action<float> function, float duration, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, 0f, 1f, InterpType.SINERP, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated from 0 to 1 over time using cosinusoidal interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartCoserpTimer(object id, Action<float> function, float duration, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, 0f, 1f, InterpType.COSERP, endFunction, delayStart);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with value which 
    /// is interpolated from 0 to 1 over time using "boing-like" interpolation.
    /// </summary>
    /// <param name="id">A unique identifier for this timer.</param>
    /// <param name="function">The function this timer will call. Takes a float param.</param>
    /// <param name="duration">The duration of the interpolation, in seconds.</param>
    /// <param name="endFunction">This optional function will be called when the timer is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the timer, in seconds.</param>
    public void StartBerpTimer(object id, Action<float> function, float duration, Action endFunction = null, float delayStart = 0f)
    {
        StartTimer(id, function, duration, 0f, 1f, InterpType.BERP, endFunction, delayStart);
    }

    /// <summary>
    /// Stop the timer with the given ID. Start again with Resume(object).
    /// </summary>
    public void Suspend(object id)
    {
        if (m_coroutines.ContainsKey(id))
            m_pausedTimers.Add(id);
    }

    /// <summary>
    /// Resume a stopped timer with the given ID.
    /// </summary>
    public void Resume(object id)
    {
        if (m_coroutines.ContainsKey(id) && m_pausedTimers.Contains(id))
            m_pausedTimers.Remove(id);
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with a counting-down integer.
    /// 
    /// This method will always to 0.
    /// </summary>
    /// <param name="id">A unique identifier for this countdown.</param>
    /// <param name="function">The function this countdown will call. Takes an integer param.</param>
    /// <param name="from">The number to count down from. Will count down to 0.</param>
    /// <param name="increment">The time between each count, in seconds.</param>
    /// <param name="endFunction">This optional function will be called when the countdown is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the countdown, in seconds.</param>
    public void StartCount(object id, Action<int> function, int from, float increment, Action endFunction = null, float delayStart = 0f)
    {
        StopTimer(id);

        if (delayStart <= 0f)
            m_coroutines.Add(id, StartCoroutine(Cr_CountCoroutine(id, function, from, 0, increment, endFunction)));
        else
            StartCoroutine(Cr_DelayedStartCount(delayStart, id, function, from, 0, increment, endFunction));
    }

    /// <summary>
    /// Initiate a coroutine which calls a given function with a counting integer.
    /// </summary>
    /// <param name="id">A unique identifier for this countdown.</param>
    /// <param name="function">The function this countdown will call. Takes an integer param.</param>
    /// <param name="from">The number to count start counting from.</param>
    /// <param name="to">The number to count to.</param>
    /// <param name="increment">The time between each count, in seconds.</param>
    /// <param name="endFunction">This optional function will be called when the countdown is finished. Takes no params.</param>
    /// <param name="delayStart">Optional delay for the start of the countdown, in seconds.</param>
    public void StartCount(object id, Action<int> function, int from, int to, float increment, Action endFunction = null, float delayStart = 0f)
    {
        StopTimer(id);

        if (delayStart <= 0f)
            m_coroutines.Add(id, StartCoroutine(Cr_CountCoroutine(id, function, from, to, increment, endFunction)));
        else
            StartCoroutine(Cr_DelayedStartCount(delayStart, id, function, from, to, increment, endFunction));
    }

    /// <summary>
    /// Stop and destroy the timer with the given ID.
    /// </summary>
    public void StopTimer(object id)
    {
        if (m_coroutines == null)
        {
            m_coroutines = new Dictionary<object, Coroutine>();
        }
        else if (m_coroutines.ContainsKey(id))
        {
            if (m_coroutines[id] != null)
            {
                StopCoroutine(m_coroutines[id]);
                m_coroutines[id] = null;
            }

            m_coroutines.Remove(id);
        }
    }
    
    private void StartTimer(object id, Action<float> function, float duration, float from, float to, InterpType type, Action endFunction = null, float delayStart = 0f, int animationCurveIndex = -1)
    {
        StopTimer(id);

        if (!gameObject.activeInHierarchy)
            return;
        
        if (delayStart <= 0f)
            m_coroutines.Add(id, StartCoroutine(Cr_TimerCoroutine(id, function, duration, from, to, type, endFunction, animationCurveIndex: animationCurveIndex)));
        else
            StartCoroutine(Cr_DelayedStartTimer(delayStart, id, function, duration, from, to, type, endFunction, animationCurveIndex: animationCurveIndex));
    }

    /// <summary>
    /// Stop and destroy all timers.
    /// </summary>
    public void StopAllTimers()
    {
        if (m_coroutines != null)
        {
            foreach (object id in m_coroutines.Keys)
            {
                if (m_coroutines[id] != null)
                {
                    StopCoroutine(m_coroutines[id]);
                }
            }
        }

        m_coroutines = new Dictionary<object, Coroutine>();
    }

    /// <summary>
    /// Check if the timer class contains a timer with the given ID.
    /// </summary>
    public bool HasID(object id)
    {
        return (m_coroutines.ContainsKey(id) && m_coroutines[id] != null);
    }

    /// <summary>
    /// Check if the timer class contains a RUNNING (i.e. not paused) timer 
    /// with the given ID.
    /// </summary>
    public bool HasRunningTimer(object id)
    {
        return (HasID(id) && !m_pausedTimers.Contains(id));
    }
    
    private bool IsPaused(object id)
    {
        return (m_pausedTimers.Contains(id));
    }
    
    private IEnumerator Cr_DelayedStartTimer(float delay, object id, Action<float> function, float duration, float from, float to, InterpType type, Action endFunction = null, int animationCurveIndex = -1)
    {
        yield return StartCoroutine(Cr_Delay(id, delay));

        StartTimer(id, function, duration, from, to, type, endFunction, animationCurveIndex: animationCurveIndex);
    }

    private IEnumerator Cr_DelayedStartCount(float delay, object id, Action<int> function, int from, int to, float increment, Action endFunction = null)
    {
        yield return StartCoroutine(Cr_Delay(id, delay));

        StartCount(id, function, from, to, increment, endFunction);
    }

    private IEnumerator Cr_Delay(object id, float delay)
    {
        while (delay > 0f)
        {
            if (IsPaused(id))
                yield return null;
            else
                delay -= m_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }
    }

    private IEnumerator Cr_CountCoroutine(object id, Action<int> function, int from, int to, float increment, Action endFunction = null)
    {
        if (from > to)
        {
            for (int i = from; i >= to; --i)
            {
                while (IsPaused(id))
                    yield return null;
                
                function.Invoke(i);

                if (m_useUnscaledTime)
                    yield return new WaitForSecondsRealtime(increment);
                else
                    yield return new WaitForSeconds(increment);
            }
        }
        else if (to > from)
        {
            for (int i = from; i <= to; ++i)
            {
                while (IsPaused(id))
                    yield return null;

                function.Invoke(i);

                if (m_useUnscaledTime)
                    yield return new WaitForSecondsRealtime(increment);
                else
                    yield return new WaitForSeconds(increment);
            }
        }
        else
        {
            function.Invoke(from);
        }

        if (endFunction != null)
            endFunction.Invoke();
    }

    private IEnumerator Cr_TimerCoroutine(object id, Action<float> function, float duration, float from, float to, InterpType type, Action endFunction = null, int animationCurveIndex = -1)
    {
        float timer = duration;

        while (IsPaused(id))
            yield return null;

        //if (animationCurveIndex >= 0)
        //    function.Invoke(m_customCurves[animationCurveIndex].Evaluate(from));
        //else
        //    function.Invoke(from);

        do
        {
            if (!IsPaused(id))
            {
                timer -= m_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                float val;
                switch (type)
                {
                    case InterpType.SMOOTHSTEP:
                        val = Mathf.SmoothStep(from, to, 1f - (timer / duration));
                        break;
                    case InterpType.SINERP:
                        val = Maths.Sinerp(from, to, 1f - (timer / duration));
                        break;
                    case InterpType.COSERP:
                        val = Maths.Coserp(from, to, 1f - (timer / duration));
                        break;
                    case InterpType.BERP:
                        val = Maths.Berp(from, to, 1f - (timer / duration));
                        break;
                    default:
                        if (animationCurveIndex >= 0)
                            val = Mathf.Lerp(from, to, m_customCurves[animationCurveIndex].Evaluate(1f - (timer / duration)));
                        else
                            val = Mathf.Lerp(from, to, 1f - (timer / duration));
                        break;
                }

                function.Invoke(val);
            }

            yield return null;
        } while (timer > 0f);

        while (IsPaused(id))
            yield return null;

        //if (animationCurveIndex >= 0)
        //{
        //    Debug.Log("To = " + to + ", Eval = " + m_customCurves[animationCurveIndex].Evaluate(1f));
        //    function.Invoke(m_customCurves[animationCurveIndex].Evaluate(1f));
        //}
        //else
        //{
        //    function.Invoke(to);
        //}

        if (endFunction != null)
            endFunction.Invoke();
    }
}