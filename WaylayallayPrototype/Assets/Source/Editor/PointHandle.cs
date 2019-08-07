using Simplex;
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

        foreach (Morph.Point point in morphTest.Points)
        {
            Vector3 pos = morphTest.transform.localToWorldMatrix * point.LocalPosition;

            EditorGUI.BeginChangeCheck();
            Vector3 vec = Handles.FreeMoveHandle(pos, Quaternion.identity, 0.02f, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(morphTest, "Changed Point " + point.ID);
                morphTest.SetPoint(point, vec);
            }
        }

        Handles.matrix = originalHandlesMatrix;
    }

    [DrawGizmo(GizmoType.Selected)]
    private static void DrawGizmos(MorphTest morphTest, GizmoType gizmoType)
    {
        if (!morphTest.DrawGizmos)
            return;

        if (morphTest.PointCount != 0)
        {
            //morphTest.Morph.DrawGizmo(morphTest.transform);
            morphTest.Morph.DrawFaces(morphTest.transform, label: true);
            morphTest.Morph.DrawTriangles(morphTest.transform, label: true);
            morphTest.Morph.DrawPoints(morphTest.transform);
        }
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