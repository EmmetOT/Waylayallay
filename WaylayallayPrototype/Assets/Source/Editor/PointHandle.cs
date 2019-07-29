using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MorphTest)), CanEditMultipleObjects]
public class PointHandle : Editor
{
    protected virtual void OnSceneGUI()
    {
        MorphTest morphTest = (MorphTest)target;

        if (morphTest.PointCount == 0)
            return;

        Handles.color = Color.black;
        Matrix4x4 originalHandlesMatrix = Handles.matrix;
        Handles.matrix = morphTest.transform.localToWorldMatrix;
        for (int i = 0; i < morphTest.PointCount; i++)
        {
            Vector3 pos = morphTest.transform.localToWorldMatrix * morphTest.GetPoint(i);

            EditorGUI.BeginChangeCheck();
            Vector3 vec = Handles.FreeMoveHandle(pos, Quaternion.identity, 0.02f, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(morphTest, "Changed Point " + i);
                morphTest.SetPoint(i, vec);
            }
        }

        Handles.matrix = originalHandlesMatrix;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    private static void DrawGizmos(MorphTest morphTest, GizmoType gizmoType)
    {
        if (morphTest.PointCount == 0)
            return;

        morphTest.Morph.DrawGizmo(morphTest.transform);
    }

    private static FieldInfo m_hiddenInfo;

    public static bool ShowDefaultTransformHandle
    {
        get
        {
            if (m_hiddenInfo == null)
            {
                Type type = typeof(Tools);
                m_hiddenInfo = type.GetField("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
            }

            return (bool)m_hiddenInfo.GetValue(null);
        }
        set
        {
            if (m_hiddenInfo == null)
            {
                Type type = typeof(Tools);
                m_hiddenInfo = type.GetField("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
            }

            m_hiddenInfo.SetValue(null, value);
        }
    }
}