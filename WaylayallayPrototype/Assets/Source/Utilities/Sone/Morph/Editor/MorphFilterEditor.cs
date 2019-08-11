using Simplex;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MorphFilter)), CanEditMultipleObjects]
public class MorphFilterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MorphFilter morphFilter = (MorphFilter)target;

        SerializedProperty meshesProperty = serializedObject.FindProperty("m_sourceMeshFilters");

        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(meshesProperty, includeChildren: meshesProperty.isExpanded);
        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck())
        {
            morphFilter.GenerateMorph();
        }

        if (GUILayout.Button("Regenerate"))
        {
            morphFilter.GenerateMorph();
        }

        if (GUILayout.Button("Test"))
        {
            morphFilter.Test();
        }
    }

    protected virtual void OnSceneGUI()
    {
        MorphFilter morphFilter = (MorphFilter)target;

        if (morphFilter == null || morphFilter.Morph == null || morphFilter.Morph.PointCount == 0)
            return;

        Transform morphFilterTransform = morphFilter.transform;

        Handles.color = Color.black;
        Matrix4x4 originalHandlesMatrix = Handles.matrix;
        Handles.matrix = morphFilterTransform.localToWorldMatrix;

        foreach (Point point in morphFilter.Morph.Points)
        {
            Vector3 pos = morphFilterTransform.localToWorldMatrix * point.LocalPosition;

            EditorGUI.BeginChangeCheck();
            Vector3 vec = Handles.FreeMoveHandle(pos, Quaternion.identity, 0.02f, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(morphFilter, "Changed Point " + point.ID);
                morphFilter.SetPointAndRegenerateMesh(point, vec);
            }
        }

        Handles.matrix = originalHandlesMatrix;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.NotInSelectionHierarchy)]
    private static void DrawGizmos(MorphFilter morphFilter, GizmoType gizmoType)
    {
        if (morphFilter == null || morphFilter.Morph == null || morphFilter.Morph.PointCount == 0)
            return;

        Transform morphFilterTransform = morphFilter.transform;

        morphFilter.Morph.DrawFaces(morphFilterTransform, label: true);
        morphFilter.Morph.DrawTriangles(morphFilterTransform, label: true);
        //morphFilter.Morph.DrawPoints(morphFilterTransform);
    }
}
