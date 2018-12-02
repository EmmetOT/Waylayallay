using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Spline))]
public class SplineInspector : Editor
{
    public const float HANDLE_SIZE = 0.04f;
    public const float PICK_SIZE = 0.1f;

    private static Color[] m_modeColours =
    {
        Color.white,
        Color.yellow,
        Color.cyan
    };

    private Spline m_spline;
    private Transform m_curveTransform;
    private Quaternion m_curveRotation;

    public override void OnInspectorGUI()
    {
        // draw script reference
        GUI.enabled = false;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"), true, new GUILayoutOption[0]);
        GUI.enabled = true;

        m_spline = target as Spline;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        bool showEvenPoints = EditorGUILayout.Toggle("Show Even Points", m_spline.ShowEvenPoints);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(m_spline, "Toggle Show Even Points");
            EditorUtility.SetDirty(m_spline);
            m_spline.ShowEvenPoints = showEvenPoints;
        }

        EditorGUI.BeginChangeCheck();

        bool showMeshGizmos = EditorGUILayout.Toggle("Show Mesh Gizmos", m_spline.ShowMeshGizmos);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(m_spline, "Toggle Show Mesh Gizmos");
            EditorUtility.SetDirty(m_spline);
            m_spline.ShowMeshGizmos = showMeshGizmos;
        }

        EditorGUI.BeginChangeCheck();

        float newDirectionScale = EditorGUILayout.FloatField("Direction Scale", Spline.DirectionScale);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Changed Direction Scale");
            EditorUtility.SetDirty(this);
            Spline.DirectionScale = newDirectionScale;
            SceneView.RepaintAll();
        }

        EditorGUI.BeginChangeCheck();

        int newStepsPerSpline = EditorGUILayout.IntField("Steps Per Spline", Spline.StepsPerSpline);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Changed Steps Per Spline");
            EditorUtility.SetDirty(this);
            Spline.StepsPerSpline = Mathf.Max(0, newStepsPerSpline);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spline", EditorStyles.boldLabel);

        GUI.enabled = false;

        EditorGUILayout.FloatField("Length", m_spline.Length);

        GUI.enabled = true;

        EditorGUI.BeginChangeCheck();

        bool loop = EditorGUILayout.Toggle("Loop", m_spline.Loop);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(m_spline, "Toggle Loop");
            EditorUtility.SetDirty(m_spline);
            m_spline.Loop = loop;
        }

        if (GUILayout.Button("Add Spline"))
        {
            Undo.RecordObject(m_spline, "Add Spline");
            m_spline.AddSegment();
            EditorUtility.SetDirty(m_spline);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        m_spline.IsGeneratingMesh = EditorGUILayout.Toggle("Generate Mesh", m_spline.IsGeneratingMesh);

        if (EditorGUI.EndChangeCheck())
        {
            if (m_spline.IsGeneratingMesh)
                m_spline.GenerateMesh();
        }

        EditorGUI.BeginChangeCheck();

        if (m_spline.IsGeneratingMesh)
        {
            int meshChunks = Mathf.Max(2, EditorGUILayout.IntField("Mesh Chunks", m_spline.MeshChunks));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_spline, "Changed Mesh Chunks");
                EditorUtility.SetDirty(m_spline);
                m_spline.GenerateMesh(meshChunks, -1f);
            }

            EditorGUI.BeginChangeCheck();

            float lineWidth = Mathf.Max(0, EditorGUILayout.FloatField("Line Width", m_spline.LineWidth));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_spline, "Changed Line Width");
                EditorUtility.SetDirty(m_spline);
                m_spline.GenerateMesh(-1, lineWidth);
            }

            EditorGUI.BeginChangeCheck();

            Color colour = EditorGUILayout.ColorField("Colour ", m_spline.Colour);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_spline, "Changed Colour");
                EditorUtility.SetDirty(m_spline);
                m_spline.SetColour(colour);
            }

            if (GUILayout.Button("Regenerate Mesh"))
            {
                Undo.RecordObject(m_spline, "Regenerate Mesh");
                m_spline.GenerateMesh();
                EditorUtility.SetDirty(m_spline);
            }
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Evenly Spaced Points", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();

        m_spline.NumberOfEvenPoints = Mathf.Max(2, EditorGUILayout.IntField("Number of Points", m_spline.NumberOfEvenPoints));
        m_spline.EvenPointGenerationIterations = Mathf.Max(1, EditorGUILayout.IntField("Iterations", m_spline.EvenPointGenerationIterations));

        if (GUILayout.Button("Generate Even Points"))
        {
            Undo.RecordObject(m_spline, "Generating Even Points");
            m_spline.GenerateEvenPoints();
            EditorUtility.SetDirty(m_spline);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(m_spline, "Changed Spline's Evenly Spaced Point Settings");
            m_spline.GenerateEvenPoints();
            EditorUtility.SetDirty(m_spline);
        }

        EditorGUILayout.Space();

        DrawSelectedPointInspector();
    }

    private void OnSceneGUI()
    {
        m_spline = target as Spline;
        m_curveTransform = m_spline.transform;
        m_curveRotation = Tools.pivotRotation == PivotRotation.Local ? m_curveTransform.rotation : Quaternion.identity;

        Vector3 p0 = ShowPoint(0);
        for (int i = 1; i < m_spline.Count; i += 3)
        {
            Vector3 p1 = ShowPoint(i);
            Vector3 p2 = ShowPoint(i + 1);
            Vector3 p3 = ShowPoint(i + 2);

            Handles.color = Color.gray;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);

            Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
            p0 = p3;
        }

        ShowDirections();
    }

    private void DrawSelectedPointInspector()
    {
        if (m_spline.SelectedPointIndex == -1)
            return;

        GUILayout.Label("Selected Point (" + (m_spline.SelectedPointIndex) + ")", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        Vector3 point = EditorGUILayout.Vector3Field("Position", m_spline[m_spline.SelectedPointIndex]);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(m_spline, "Move Point");
            m_spline.Refresh();
            m_spline[m_spline.SelectedPointIndex] = point;
            EditorUtility.SetDirty(m_spline);
        }

        EditorGUI.BeginChangeCheck();
        BezierControlPointMode mode = (BezierControlPointMode)EditorGUILayout.EnumPopup("Mode", m_spline.GetControlPointMode(m_spline.SelectedPointIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(m_spline, "Change Point Mode");
            m_spline.SetControlPointMode(m_spline.SelectedPointIndex, mode);
            m_spline.Refresh();
            EditorUtility.SetDirty(m_spline);
        }

        if (GUILayout.Button("Decrement Selected Point"))
        {
            m_spline.SelectedPointIndex = m_spline.SelectedPointIndex - 1;

            if (m_spline.SelectedPointIndex < 0)
                m_spline.SelectedPointIndex += m_spline.Count;

            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Increment Selected Point"))
        {
            m_spline.SelectedPointIndex = (m_spline.SelectedPointIndex + 1) % m_spline.Count;

            SceneView.RepaintAll();
        }
    }

    private void ShowDirections()
    {
        float distAlongSpline = 0f;

        Vector3 point = m_spline.GetPoint(distAlongSpline);
        Handles.DrawLine(point, point + m_spline.GetDirection(0f) * Spline.DirectionScale);

        int steps = Spline.StepsPerSpline * m_spline.SegmentCount;

        for (int i = 1; i <= steps; i++)
        {
            distAlongSpline = i / (float)steps;

            point = m_spline.GetPoint(distAlongSpline);
            m_spline.Refresh();

            Vector3 direction = m_spline.GetVelocity(distAlongSpline);
            
            Handles.color = Color.Lerp(Color.green, Color.red, Mathf.InverseLerp(0f, 20f, direction.magnitude));

            Handles.DrawLine(point, point + direction * Spline.DirectionScale);
        }
    }

    private Vector3 ShowPoint(int index)
    {
        Vector3 point = m_curveTransform.TransformPoint(m_spline[index]);
        Handles.color = m_modeColours[(int)m_spline.GetControlPointMode(index)];

        float size = HandleUtility.GetHandleSize(point) * 3f;

        if (index == 0)
            size *= 2f;
        
        Handles.DrawCapFunction cap;

        if (index % 3 != 0)
            cap = Handles.CubeCap;
        else
            cap = Handles.SphereCap;

        if (Handles.Button(point, m_curveRotation, size * HANDLE_SIZE, size * PICK_SIZE, cap))
        {
            m_spline.SelectedPointIndex = index;
            Repaint(); // repaint to update the point in the inspector
        }

        if (m_spline.SelectedPointIndex == index)
        {
            EditorGUI.BeginChangeCheck();

            point = Handles.DoPositionHandle(point, m_curveRotation);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_spline, "Moved Point " + index);
                EditorUtility.SetDirty(m_spline);
                m_spline[index] = m_curveTransform.InverseTransformPoint(point);
                m_spline.GenerateMesh();
                m_spline.Refresh();
            }
        }

        return point;
    }
}