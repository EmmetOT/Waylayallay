using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(MeshFilter))]
public class ExtrudedMeshTrail : MonoBehaviour
{
    [Header("Settings")]

    [SerializeField]
    [Tooltip("If angle between sections exceeds the given number of degrees, smooth it by the smoothing factor")]
    [MinValue(0f)]
    private float m_smoothAnglesAbove = 80f;

    [SerializeField]
    [Range(0f, 1f)]
    private float m_smoothingFactor = 0.5f;

    [SerializeField]
    private MeshFilter m_meshFilter;

    private Mesh m_mesh;

    // Generates an extrusion trail from the attached mesh
    // Uses the MeshExtrusion algorithm in MeshExtrusion.cs to generate and preprocess the mesh.
    public bool m_autoCalculateOrientation = true;
    public float minDistance = 0.1f;
    public bool invertFaces = false;
    MeshExtrusion.Edge[] precomputedEdges;

    private List<ExtrudedTrailSection> m_sections = new List<ExtrudedTrailSection>();

    public struct ExtrudedTrailSection
    {
        public Vector3 Point { get; }
        public Matrix4x4 Matrix { get; }

        public ExtrudedTrailSection(Vector3 point, Matrix4x4 matrix)
        {
            Point = point;
            Matrix = matrix;
        }
    }

    private void Start()
    {
        m_mesh = m_meshFilter.sharedMesh;
        precomputedEdges = MeshExtrusion.BuildManifoldEdges(m_mesh);
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        Vector3 scale = transform.localScale;

        // Add a new trail section to beginning of array
        if (m_sections.Count == 0 || (m_sections[m_sections.Count - 1].Point - position).sqrMagnitude > minDistance * minDistance)
        {
            m_sections.Add(new ExtrudedTrailSection(position, transform.localToWorldMatrix));
        }

        // We need at least 2 sections to create the line
        if (m_sections.Count < 2)
            return;

        var worldToLocal = transform.worldToLocalMatrix;
        Matrix4x4[] finalSections = new Matrix4x4[m_sections.Count];
        Quaternion previousRotation = default(Quaternion);

        for (int i = m_sections.Count - 1; i >= 0; --i)
        {
            if (m_autoCalculateOrientation)
            {
                Vector3 direction;
                Quaternion rotation;

                if (i == m_sections.Count - 1)
                {
                    direction = m_sections[m_sections.Count - 1].Point - m_sections[m_sections.Count - 2].Point;
                    rotation = Quaternion.LookRotation(direction, Vector3.up);

                    previousRotation = rotation;
                    finalSections[i] = worldToLocal * Matrix4x4.TRS(position, rotation, scale);
                }
                // all elements get the direction by looking up the next section
                else if (i != 0)
                {
                    direction = m_sections[i].Point - m_sections[i - 1].Point;
                    rotation = Quaternion.LookRotation(direction, Vector3.up);

                    // When the angle of the rotation compared to the last segment is too high
                    // smooth the rotation a little bit. Optimally we would smooth the entire sections array.
                    if (Quaternion.Angle(previousRotation, rotation) > m_smoothAnglesAbove)
                        rotation = Quaternion.Slerp(previousRotation, rotation, m_smoothingFactor);

                    previousRotation = rotation;
                    finalSections[i] = worldToLocal * Matrix4x4.TRS(m_sections[i].Point, rotation, scale);
                }
                // except the last one, which just copies the previous one
                else
                {
                    finalSections[i] = finalSections[i + 1];
                }
            }
            else
            {
                if (i == m_sections.Count - 1)
                {
                    finalSections[i] = Matrix4x4.identity;
                }
                else
                {
                    finalSections[i] = worldToLocal * m_sections[i].Matrix;
                }
            }
        }

        // Rebuild the extrusion mesh	
        MeshExtrusion.ExtrudeMesh(m_mesh, GetComponent<MeshFilter>().mesh, finalSections, precomputedEdges, invertFaces);
    }
    
}