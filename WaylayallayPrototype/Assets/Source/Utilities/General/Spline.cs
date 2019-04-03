using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Spline : MonoBehaviour
{
    private static List<Spline> m_curves = new List<Spline>();
    public static IList<Spline> Splines { get { return m_curves.AsReadOnly(); } }

    #region Editor Variables

    public static float DirectionScale = 0.5f;
    public static int StepsPerSpline = 30;

    [HideInInspector]
    public int SelectedPointIndex = -1;

    #endregion

    [SerializeField]
    private Material m_material;

    [SerializeField]
    private List<BezierControlPointMode> m_modes = new List<BezierControlPointMode>();

    [SerializeField]
    private List<Vector3> m_points = new List<Vector3>();

    [SerializeField, HideInInspector]
    private float m_length = -1;
    public float Length
    {
        get
        {
            if (m_length <= 0f)
                m_length = CalculateLength();

            return m_length;
        }
    }

    public Vector3 this[int index]
    {
        get { return m_points[index]; }
        set
        {
            if (index % 3 == 0)
            {
                Vector3 delta = value - m_points[index];

                if (m_loop)
                {
                    if (index == 0)
                    {
                        m_points[1] += delta;
                        m_points[m_points.Count - 2] += delta;
                        m_points[m_points.Count - 1] = value;
                    }
                    else if (index == m_points.Count - 1)
                    {
                        m_points[0] = value;
                        m_points[1] += delta;
                        m_points[index - 1] += delta;
                    }
                    else
                    {
                        m_points[index - 1] += delta;
                        m_points[index + 1] += delta;
                    }
                }
                else
                {
                    if (index > 0)
                        m_points[index - 1] += delta;

                    if (index + 1 < m_points.Count)
                        m_points[index + 1] += delta;

                }
            }

            m_points[index] = value;
            EnforceMode(index);
            GenerateMesh();
        }
    }

    public Vector3 Start
    {
        get { return this[0]; }
        set
        {
            this[0] = value;
            this[1] = transform.InverseTransformPoint(GetPoint(0.25f));
        }
    }

    public Vector3 End
    {
        get { return this[m_points.Count - 1]; }
        set
        {
            this[m_points.Count - 1] = value;
            this[m_points.Count - 2] = transform.InverseTransformPoint(GetPoint(0.75f));
        }
    }

    public BezierControlPointMode GetControlPointMode(int index)
    {
        return m_modes[(index + 1) / 3];
    }

    public void SetControlPointMode(int index, BezierControlPointMode mode)
    {
        int modeIndex = (index + 1) / 3;
        m_modes[modeIndex] = mode;

        if (m_loop)
        {
            if (modeIndex == 0)
                m_modes[m_modes.Count - 1] = mode;
            else if (modeIndex == m_modes.Count - 1)
                m_modes[0] = mode;
        }

        EnforceMode(index);
        GenerateMesh();
    }

    public int Count
    {
        get
        {
            return m_points.Count;
        }
    }

    public int SegmentCount
    {
        get
        {
            return (m_points.Count - 1) / 3;
        }
    }

    [SerializeField]
    private bool m_loop = false;

    public bool Loop
    {
        get
        {
            return m_loop;
        }
        set
        {
            m_loop = value;
            if (m_loop)
            {
                m_modes[m_modes.Count - 1] = m_modes[0];
                this[0] = m_points[0];
            }
            GenerateMesh();
        }
    }

    #region Unity Callbacks

    private void Awake()
    {
        m_length = -1;  // force recalculate length

        m_curves.Add(this);
        SetColour(Color.black);
    }

    public void Reset()
    {
        SelectedPointIndex = -1;

        m_points.Clear();

        m_points.Add(new Vector3(0f, 0f, 0f));
        m_points.Add(new Vector3(1f, 0f, 0f));
        m_points.Add(new Vector3(2f, 0f, 0f));
        m_points.Add(new Vector3(3f, 0f, 0f));

        m_modes.Clear();

        m_modes.Add(BezierControlPointMode.Free);
        m_modes.Add(BezierControlPointMode.Free);

        m_isGeneratingMesh = false;
        m_showMeshGizmos = false;

        SetColour(Color.black);

        Refresh();
    }

    public void Refresh()
    {
        GenerateEvenPoints();
        m_length = CalculateLength();
    }

    #endregion

    #region Main Public Methods

    /// <summary>
    /// Get point along the spline where 0 = the beginning and 1 = the end.
    /// </summary>
    public Vector3 GetPoint(float t)
    {
        t = Mathf.Clamp01(t);

        int i;
        if (t >= 1f)
        {
            t = 1f;
            i = m_points.Count - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * SegmentCount;
            i = (int)t;
            t -= i;
            i *= 3;
        }
        
        return transform.TransformPoint(Maths.GetBezierPoint(m_points[i], m_points[i + 1], m_points[i + 2], m_points[i + 3], t));
    }

	public Vector3[] GetPoints(float[] points)
	{
		Vector3[] returnValues = new Vector3[points.Length];

		for (int i = 0; i < points.Length; i++)
		{
			returnValues[i] = GetPoint(points[i]);
		}

		return returnValues;
	}

    /// <summary>
    /// Get velocity at a point on the spline where 0 = the beginning and 1 = the end.
    /// </summary>
    public Vector3 GetVelocity(float t)
    {
        int i;
        if (t >= 1f)
        {
            t = 1f;
            i = m_points.Count - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * SegmentCount;
            i = (int)t;
            t -= i;
            i *= 3;
        }
        
        // Because it produces a velocity vector and not a point, it should not be affected by the position of the curve, so we subtract that after transforming.
        return transform.TransformPoint(Maths.GetFirstDerivativeBezierPoint(m_points[i], m_points[i + 1], m_points[i + 2], m_points[i + 3], t)) - transform.position;
    }

    /// <summary>
    /// Get direction at a point on the spline where 0 = the beginning and 1 = the end.
    /// </summary>
    public Vector3 GetDirection(float t)
    {
        // if the spline doesnt loop, we could potentially return a zero
        // direction vector if right at the beginning or end of the spline.
        // so move t a tiny bit away from the ends
        if (!m_loop && (t == 0f || t == 1f))
            t = t == 0f ? 0.01f : 0.99f;
        
        return GetVelocity(t).normalized;
    }

    /// <summary>
    /// Add another bezier curve to the spline.
    /// </summary>
    public void AddSegment()
    {
        Vector3 segmentStart = m_points[m_points.Count - 1];
        m_points.Add(segmentStart + Vector3.right * 1f);
        m_points.Add(segmentStart + Vector3.right * 2f);
        m_points.Add(segmentStart + Vector3.right * 3f);

        m_modes.Add(m_modes[m_modes.Count - 1]);

        EnforceMode(m_points.Count - 4);

        if (m_loop)
        {
            m_points[m_points.Count - 1] = m_points[0];
            m_modes[m_modes.Count - 1] = m_modes[0];
            EnforceMode(0);
        }

        GenerateMesh();
    }

    #endregion

    private void EnforceMode(int index)
    {
        int modeIndex = (index + 1) / 3;

        BezierControlPointMode mode = m_modes[modeIndex];

        if (mode == BezierControlPointMode.Free || !m_loop && (modeIndex == 0 || modeIndex == m_modes.Count - 1))
            return;

        int middleIndex = modeIndex * 3;
        int fixedIndex;
        int enforcedIndex;

        if (index <= middleIndex)
        {
            fixedIndex = middleIndex - 1;

            if (fixedIndex < 0)
                fixedIndex = m_points.Count - 2;

            enforcedIndex = middleIndex + 1;

            if (enforcedIndex >= m_points.Count)
                enforcedIndex = 1;
        }
        else
        {
            fixedIndex = middleIndex + 1;

            if (fixedIndex >= m_points.Count)
                fixedIndex = 1;

            enforcedIndex = middleIndex - 1;

            if (enforcedIndex < 0)
                enforcedIndex = m_points.Count - 2;
        }

        Vector3 middle = m_points[middleIndex];
        Vector3 enforcedTangent = middle - m_points[fixedIndex];

        if (mode == BezierControlPointMode.Aligned)
            enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, m_points[enforcedIndex]);

        m_points[enforcedIndex] = middle + enforcedTangent;
    }

    #region Mesh Generation

    [SerializeField]
    private bool m_isGeneratingMesh = true;
    public bool IsGeneratingMesh { get { return m_isGeneratingMesh; } set { m_isGeneratingMesh = value; } }
    
    [SerializeField]
    private MeshRenderer m_meshRenderer;

    [SerializeField]
    private MeshFilter m_meshFilter;

    [SerializeField]
    private MeshCollider m_meshCollider;

    [SerializeField]
    private Color m_colour = Color.black;
    public Color Colour { get { return m_colour; } }

    [SerializeField]
    private bool m_showMeshGizmos = true;
    public bool ShowMeshGizmos { get { return m_showMeshGizmos; } set { m_showMeshGizmos = value; } }

    private Mesh m_mesh;

    [SerializeField]
    private int m_meshChunks = 30;
    public int MeshChunks { get { return m_meshChunks; } }

    [SerializeField]
    private float m_lineWidth = 0.1f;
    public float LineWidth { get { return m_lineWidth; } }

    private List<Vector3> m_vertices = new List<Vector3>();
    public IList<Vector3> Vertices { get { return m_vertices.AsReadOnly(); } }

    [SerializeField]
    private List<int> m_triangles = new List<int>();

    public void SetColour(Color colour)
    {
        m_colour = colour;
        //m_material.SetColor("_Color", m_colour);
    }

    private void GetMeshComponents()
    {
        if (m_meshRenderer == null)
            m_meshRenderer = GetComponent<MeshRenderer>();
        
        if (m_meshRenderer == null)
            m_meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (m_meshFilter == null)
            m_meshFilter = GetComponent<MeshFilter>();

        if (m_meshFilter == null)
            m_meshFilter = gameObject.AddComponent<MeshFilter>();

        if (m_meshCollider == null)
            m_meshCollider = GetComponent<MeshCollider>();

        if (m_meshCollider == null)
            m_meshCollider = gameObject.AddComponent<MeshCollider>();
    }

    public void GenerateMesh(int chunks = -1, float lineWidth = -1f)
    {
        if (!m_isGeneratingMesh)
            return;

        GetMeshComponents();

        m_meshFilter.mesh = m_mesh = new Mesh();
        m_mesh.name = "Spline [" + name + "]";
        m_meshRenderer.material = m_material;

        if (chunks == -1)
            chunks = m_meshChunks;

        m_meshChunks = Mathf.Max(2, chunks);

        if (lineWidth < 0f)
            lineWidth = m_lineWidth;

        m_lineWidth = Mathf.Max(0f, lineWidth);

        m_vertices.Clear();
        m_triangles.Clear();

        for (int i = 0; i < m_meshChunks; i++)
        {
            float increment = i / (m_meshChunks - 1f);
            Vector3 point = GetPoint(increment);

            Vector3 direction = GetDirection(increment);
            Vector3 cross = Vector3.Cross(direction, transform.TransformDirection(Vector3.forward));

            m_vertices.Add(transform.InverseTransformPoint(point - cross * m_lineWidth));
            m_vertices.Add(transform.InverseTransformPoint(point + cross * m_lineWidth));
        }

        for (int i = 0; i < ((m_vertices.Count / 2) * 2) - 3; i += 2)
        {
            m_triangles.Add(i);
            m_triangles.Add(i + 3);
            m_triangles.Add(i + 1);

            m_triangles.Add(i);
            m_triangles.Add(i + 2);
            m_triangles.Add(i + 3);
        }

        //if (m_loop)
        //{
        //    int max = m_vertices.Count - 1;

        //    m_triangles.Add(max);
        //    m_triangles.Add(0);
        //    m_triangles.Add(1);

        //    m_triangles.Add(max - 2);
        //    m_triangles.Add(0);
        //    m_triangles.Add(1);
        //}

        m_mesh.vertices = m_vertices.ToArray();
        m_mesh.triangles = m_triangles.ToArray();

        m_meshCollider.sharedMesh = m_mesh;
    }

    #endregion

    #region Evenly Spaced Points
    
    [SerializeField]
    private bool m_showEvenPoints = false;
    public bool ShowEvenPoints { get { return m_showEvenPoints; } set { m_showEvenPoints = value; } }

    [SerializeField]
    private int m_numberOfEvenPoints = 20;
    public int NumberOfEvenPoints { get { return m_numberOfEvenPoints; } set { m_numberOfEvenPoints = value; } }

    [SerializeField]
    private int m_evenPointGenerationIterations = 20;
    public int EvenPointGenerationIterations { get { return m_evenPointGenerationIterations; } set { m_evenPointGenerationIterations = value; } }

    [SerializeField]
    private Vector3[] m_evenPoints;
    public Vector3[] EvenPoints { get { return m_evenPoints; } }
    
    public Vector3[] GenerateEvenPoints(int evenPointsCount = -1, int iterations = -1, float from = 0f, float to = 1f)
    {
        from = Mathf.Clamp01(from);
        to = Mathf.Clamp01(to);

        evenPointsCount = evenPointsCount < 0 ? m_numberOfEvenPoints : evenPointsCount;
        iterations = iterations < 0 ? m_evenPointGenerationIterations : iterations;

        m_evenPoints = new Vector3[evenPointsCount];

        for (int i = 0; i < m_evenPoints.Length; i++)
        {
            float t = Mathf.Lerp(from, to, i / (m_evenPoints.Length - 1f));
            m_evenPoints[i] = GetPoint(t);
        }

        for (int i = 0; i < iterations; i++)
        {
            Vector3[] newPoints = new Vector3[m_evenPoints.Length];

            newPoints[0] = m_evenPoints[0];
            newPoints[newPoints.Length - 1] = m_evenPoints[newPoints.Length - 1];

            for (int j = 1; j < m_evenPoints.Length - 1; j++)
            {
                Vector3 b0 = m_evenPoints[j - 1];
                Vector3 b1 = m_evenPoints[j];
                Vector3 b2 = m_evenPoints[j + 1];

                float dist0 = Vector3.Distance(b0, b1);
                float dist1 = Vector3.Distance(b1, b2);

                float r = 0.5f * (dist1 - dist0) / (dist0 + dist1);

                if (r > 0f)
                {
                    newPoints[j] = b1 + r * (b2 - b1);
                }
                else if (r < 0f)
                {
                    newPoints[j] = b1 + r * (b1 - b0);
                }
            }

            m_evenPoints = newPoints;
        }

        return m_evenPoints;
    }
    
    #endregion

    /// <summary>
    /// Returns the length of this spline by measuring the distance between points along it.
    /// 
    /// Parameter gives number of steps to take measurements between and should be a number around 20-30.
    /// 
    /// Any lower and the accuracy becomes inaccurate. Any higher the values converge and it's just unnecessary.
    /// </summary>
    public float CalculateLength(int steps = 30)
    {
        return CalculateSplineDistance(0f, 1f, steps);
    }

    /// <summary>
    /// Returns the "spline distance" between any two normalized points along the spline by measuring the distance between points along it.
    /// 
    /// Parameter gives number of steps to take measurements between and should be a number around 20-30.
    /// 
    /// Any lower and the accuracy becomes inaccurate. Any higher the values converge and it's just unnecessary.
    /// </summary>
    public float CalculateSplineDistance(float from, float to, int steps = 30)
    {
        from = Mathf.Clamp01(from);
        to = Mathf.Clamp01(to);
        steps = Mathf.Max(2, steps);

        float sum = 0f;

        Vector3 point = GetPoint(from);

        for (int i = 1; i < steps; i++)
        {
            Vector3 nextPoint = GetPoint(Mathf.Lerp(from, to, i / (steps - 1f)));

            sum += Vector3.Distance(point, nextPoint);

            point = nextPoint;
        }

        return sum;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (m_showEvenPoints && !m_evenPoints.IsNullOrEmpty())
        {
            Gizmos.color = Color.red;

            for (int i = 0; i < m_evenPoints.Length; i++)
                Gizmos.DrawSphere(m_evenPoints[i], 0.2f);
        }

        if (m_vertices == null || !m_showMeshGizmos)
            return;

        Vector3 offset = transform.TransformDirection(Vector3.right * 0.2f);

        for (int i = 0; i < m_vertices.Count; i++)
        {
            Vector3 v = transform.TransformPoint(m_vertices[i]);

            Gizmos.color = Color.black;
            Gizmos.DrawSphere(v, 0.05f);
            Handles.Label(v + offset, i.ToString(), EditorStyles.boldLabel);
        }

        for (int i = 0; i < m_triangles.Count - 1; i++)
        {
            Vector3 v0 = transform.TransformPoint(m_vertices[m_triangles[i]]);
            Vector3 v1 = transform.TransformPoint(m_vertices[m_triangles[i + 1]]);

            Gizmos.DrawLine(v0, v1);
        }
    }
#endif
}

public enum BezierControlPointMode
{
    Free,
    Aligned,
    Mirrored
}