using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class HoldManipulator : PointerManipulator
{
    private readonly MonoBehaviour m_target;
    private readonly float m_holdTime;
    private readonly Action m_onHeld;
    private Coroutine m_holdRoutine;

    public HoldManipulator(MonoBehaviour p_target, float p_holdTime, Action p_onHeld)
    {
        m_target = p_target;
        m_holdTime = p_holdTime;
        m_onHeld = p_onHeld;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(DownCallback, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerUpEvent>(UpCallback);
        target.RegisterCallback<PointerCancelEvent>(UpCallback);
        target.RegisterCallback<PointerLeaveEvent>(UpCallback);
    }

    private void DownCallback(PointerDownEvent _)
    {
        m_holdRoutine = m_target.StartCoroutine(HoldRoutine(m_holdTime, m_onHeld));
    }

    private void UpCallback(PointerUpEvent _)
    {
        if (m_holdRoutine != null) m_target.StopCoroutine(m_holdRoutine);
    }

    private void UpCallback(PointerCancelEvent _)
    {
        if (m_holdRoutine != null) m_target.StopCoroutine(m_holdRoutine);
    }

    private void UpCallback(PointerLeaveEvent _)
    {
        if (m_holdRoutine != null) m_target.StopCoroutine(m_holdRoutine);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerLeaveEvent>(UpCallback);
        target.UnregisterCallback<PointerCancelEvent>(UpCallback);
        target.UnregisterCallback<PointerUpEvent>(UpCallback);
        target.UnregisterCallback<PointerDownEvent>(DownCallback, TrickleDown.TrickleDown);
    }

    private static IEnumerator HoldRoutine(float p_holdTime, Action p_onHeld)
    {
        float timer = 0f;
        while (timer < p_holdTime)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        p_onHeld?.Invoke();
    }
}