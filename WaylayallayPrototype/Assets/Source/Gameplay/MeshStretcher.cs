using Simplex;
using Sone;
using System.Collections.Generic;
using UnityEngine;
using static Sone.Graph.Graph;

[System.Serializable]
public class MeshStretcher
{
    #region Consts

    private const int TOP = 0;
    private const int BOTTOM = 1;
    private const int CENTRE = 2;

    #endregion

    #region Variables

    private Transform m_transform;

    private Mesh[] m_newMeshes = new Mesh[] { new Mesh(), new Mesh(), new Mesh() };

    [SerializeField]
    private MeshFilter[] m_meshFilters;

    [SerializeField]
    private MeshRenderer[] m_meshRenderers;

    [SerializeField]
    private MeshRenderer m_originalMeshRenderer;

    public MeshFilter TopCap { get { return m_meshFilters[0]; } }
    public MeshFilter BottomCap { get { return m_meshFilters[1]; } }
    public MeshFilter ExtrudedCentre { get { return m_meshFilters[2]; } }

    private List<Vector3>[] m_finalVertices = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };
    private List<Vector2>[] m_finalUVs = new List<Vector2>[] { new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };
    private List<int>[] m_finalTriangles = new List<int>[] { new List<int>(), new List<int>(), new List<int>() };
    private List<Vector3>[] m_finalNormals = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };

    private List<Vector3>[] m_bufferVertices = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };
    private List<Vector2>[] m_bufferUVs = new List<Vector2>[] { new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };
    private List<int>[] m_bufferTriangles = new List<int>[] { new List<int>(), new List<int>(), new List<int>() };
    private List<Vector3>[] m_bufferNormals = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };

    private Vector3[] m_intersectionVertexCache;
    private Vector2[] m_intersectionUVCache;
    private int[] m_intersectionTriangleCache;
    private Vector3[] m_intersectionNormalCache;

    private Transform m_parent;

    private Vector3[] m_calculationVertices;

    private Vector3[] m_originalVertices;
    private Vector2[] m_originalUVs;
    private int[] m_originalTriangles;
    private Vector3[] m_originalNormals;
    
    public float CurrentStretch { get; private set; }
    public BisectionPlane BisectionPlane { get; private set; }

    /// <summary>
    /// A direction vector from the center of the bisection plane to the caps.
    /// </summary>
    public Vector3 CapOffset { get { return BisectionPlane.Normal.normalized * CurrentStretch * 0.5f; } }
    
    #endregion

    #region Constructor

    public MeshStretcher(MeshFilter meshFilter, BisectionPlane plane)
    {
        BisectionPlane = plane;
        m_transform = meshFilter.transform;

        Mesh mesh = meshFilter.sharedMesh;

        m_originalMeshRenderer = meshFilter.GetComponent<MeshRenderer>();

        m_originalVertices = mesh.vertices;

        UpdateCalculationVertices();
        
        m_originalUVs = mesh.uv;
        m_originalTriangles = mesh.triangles;
        m_originalNormals = mesh.normals;

        GameObject gO = new GameObject(meshFilter.name);
        gO.transform.SetParent(Manager.PlaneController.GeneratedMeshRoot);
        gO.transform.localPosition = Vector3.zero;
        gO.transform.localRotation = Quaternion.identity;
        gO.transform.localScale = Vector3.one;

        m_parent = gO.transform;

        m_meshFilters = new MeshFilter[] 
        {
            AttachNewMeshFilter(m_originalMeshRenderer, "Top"),
            AttachNewMeshFilter(m_originalMeshRenderer, "Bottom"),
            AttachNewMeshFilter(m_originalMeshRenderer, "Centre")
        };

        m_meshRenderers = new MeshRenderer[3];

        for (int i = 0; i < m_meshFilters.Length; i++)
            m_meshRenderers[i] = m_meshFilters[i].GetComponent<MeshRenderer>();
        
        Calculate();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// The calculation vertices are the mesh vertices, rotated, scaled, and repositioned to match the vertex positions
    /// of this instance of the mesh in world space.
    /// </summary>
    public void UpdateCalculationVertices()
    {
        m_calculationVertices = new Vector3[m_originalVertices.Length];

        // convert vertices to match the transform of the object

        Matrix4x4 scaleMatrix = Matrix4x4.TRS(Vector3.zero, m_transform.rotation, m_transform.localScale);

        for (int i = 0; i < m_originalVertices.Length; i++)
        {
            // i have no idea why including the position in the matrix doesnt work. instead i just add it here.
            m_calculationVertices[i] = m_transform.position + (Vector3)(scaleMatrix * m_originalVertices[i]);
        }
    }

    /// <summary>
    /// Perform the calculations and construct the new mesh.
    /// </summary>
    public void Calculate()
    {
        SwitchRenderers();

        ClearAll();

        for (int i = 0; i < m_originalTriangles.Length; i += 3)
            TryRetriangulate(i);
        
        // cache the centre mesh information before calculating the stretch.
        // this way, if we want to redo the stretch later, we don't need to do all the other stuff again too.
        m_intersectionVertexCache = m_finalVertices[CENTRE].ToArray();
        m_intersectionUVCache = m_finalUVs[CENTRE].ToArray();
        m_intersectionTriangleCache = m_finalTriangles[CENTRE].ToArray();
        m_intersectionNormalCache = m_finalNormals[CENTRE].ToArray();

        CalculateStretch();

        ApplyMeshes();
    }

    /// <summary>
    /// Set the amount by which the mesh will be extruded in the direction of the normal of the bisecting plane.
    /// </summary>
    public void SetStretch(float stretch, GameObject gO = null)
    {
        CurrentStretch = Mathf.Max(0f, stretch);

        if (!m_intersectionVertexCache.IsNullOrEmpty())
        {
            m_finalVertices[CENTRE] = new List<Vector3>(m_intersectionVertexCache);
            m_finalUVs[CENTRE] = new List<Vector2>(m_intersectionUVCache);
            m_finalTriangles[CENTRE] = new List<int>(m_intersectionTriangleCache);
            m_finalNormals[CENTRE] = new List<Vector3>(m_intersectionNormalCache);

            CalculateStretch(gO);
        }
        else
        {
            Calculate();
        }
        
        ApplyMeshes();
    }
    
    #endregion

    #region Maths Methods

    /// <summary>
    /// Given a bisection plane and a starting index for a triangle array,
    /// return whether the plane bisects the triangle.
    /// 
    /// If the triangle is bisected, splits the new vertices and triangles into the three collections: two 'caps' and the extruded center.
    /// </summary>
    private bool TryRetriangulate(int startingIndex)
    {
        ClearBuffer();

        Plane trianglePlane = new Plane(m_calculationVertices[m_originalTriangles[startingIndex]], m_calculationVertices[m_originalTriangles[startingIndex + 1]], m_calculationVertices[m_originalTriangles[startingIndex + 2]]);

        Vector3 centroid = (m_calculationVertices[m_originalTriangles[startingIndex]] + m_calculationVertices[m_originalTriangles[startingIndex + 1]] + m_calculationVertices[m_originalTriangles[startingIndex + 2]]) * 0.3333f;

        ClearBuffer();

        bool result = false;

        Partition partition;
        if (TryBisectTriangle(startingIndex, BisectionPlane, out partition))
        {
            Triangulator triangulator;
            for (int i = 0; i < m_bufferTriangles.Length; i++)
            {
                if (m_bufferVertices[i].Count > 3)
                {
                    triangulator = new Triangulator(m_bufferVertices[i], trianglePlane.normal);
                    m_bufferTriangles[i] = new List<int>(triangulator.Triangulate());

                    // only one triangle can be split
                    break;
                }
            }

            result = true;
        }
        else
        {
            // the triangle wasn't bisected so it must lie on one side of the plane. 
            // the 'partition' enum tells us which.

            int a = m_originalTriangles[startingIndex];
            int b = m_originalTriangles[startingIndex + 1];
            int c = m_originalTriangles[startingIndex + 2];

            if (partition == Partition.BOTH_POSITIVE)
            {
                AddToBuffer(TOP, m_calculationVertices[a], m_originalUVs[a], m_originalNormals[a]);
                AddToBuffer(TOP, m_calculationVertices[b], m_originalUVs[b], m_originalNormals[b]);
                AddToBuffer(TOP, m_calculationVertices[c], m_originalUVs[c], m_originalNormals[c]);
            }
            else
            {
                AddToBuffer(BOTTOM, m_calculationVertices[a], m_originalUVs[a], m_originalNormals[a]);
                AddToBuffer(BOTTOM, m_calculationVertices[b], m_originalUVs[b], m_originalNormals[b]);
                AddToBuffer(BOTTOM, m_calculationVertices[c], m_originalUVs[c], m_originalNormals[c]);
            }
        }

        for (int i = 0; i < m_bufferTriangles.Length; i++)
            for (int j = 0; j < m_bufferTriangles[i].Count; j++)
                AddToFinal(i, m_bufferVertices[i][m_bufferTriangles[i][j]], m_bufferUVs[i][m_bufferTriangles[i][j]], m_bufferNormals[i][m_bufferTriangles[i][j]]);

        m_finalVertices[CENTRE].AddRange(m_bufferVertices[2]);
        m_finalUVs[CENTRE].AddRange(m_bufferUVs[2]);
        m_finalNormals[CENTRE].AddRange(m_bufferNormals[2]);

        return result;
    }

    /// <summary>
    /// Try to bisect a triangle with a plane, splitting the result into two pairs of vertices and triangles.
    /// 
    /// Returns whether the triangle is bisected by the plane. If not bisected, the triangle must be on one side of the plane.
    /// Use the returned partition to determine which.
    /// </summary>
    private bool TryBisectTriangle(int startIndex, Plane plane, out Partition partition)
    {
        bool bisected = false;

        partition = BisectEdge(m_originalTriangles[startIndex], m_originalTriangles[startIndex + 1]);
        bisected |= (partition == Partition.A_POSITIVE || partition == Partition.B_POSITIVE);

        partition = BisectEdge(m_originalTriangles[startIndex + 1], m_originalTriangles[startIndex + 2]);
        bisected |= (partition == Partition.A_POSITIVE || partition == Partition.B_POSITIVE);

        partition = BisectEdge(m_originalTriangles[startIndex + 2], m_originalTriangles[startIndex]);
        bisected |= (partition == Partition.A_POSITIVE || partition == Partition.B_POSITIVE);

        return bisected;
    }

    /// <summary>
    /// Returns whether the given plane bisects the line from a to b, and the point of intersection. 
    /// </summary>
    public Partition TryBisectEdge(Vector3 a, Vector3 b, Vector2 aUV, Vector2 bUV, Vector3 aNormal, Vector3 bNormal, out Vector2 intersectionUV, out Vector3 intersection, out Vector3 intersectionNormal)
    {
        intersection = default(Vector3);
        intersectionUV = default(Vector2);
        intersectionNormal = default(Vector3);

        Plane plane = BisectionPlane;

        bool isAPositive = plane.GetSide(a);
        bool isBPositive = plane.GetSide(b);

        if (isAPositive != isBPositive)
        {
            float enter;
            Ray ray = new Ray(a, (b - a).normalized);

            if (plane.Raycast(ray, out enter))
                intersection = ray.GetPoint(enter);

            float t = Vector3.Distance(a, intersection) / Vector3.Distance(a, b);

            // calculate a UV and normal for the point of intersection
            intersectionUV = Vector2.Lerp(aUV, bUV, t);

            // TODO maybe there's a better way of calculating the new normal here, but just copying
            // one of them seems to do the job.
            intersectionNormal = aNormal;// Vector2.Lerp(aNormal, bNormal, t);

            return isAPositive ? Partition.A_POSITIVE : Partition.B_POSITIVE;
        }
        else if (isAPositive)
        {
            return Partition.BOTH_POSITIVE;
        }
        else
        {
            return Partition.BOTH_NEGATIVE;
        }
    }

    private Partition BisectEdge(int vertexIndexA, int vertexIndexB)
    {
        Vector3 a = m_calculationVertices[vertexIndexA];
        Vector3 b = m_calculationVertices[vertexIndexB];

        Vector3 aUV = m_originalUVs[vertexIndexA];
        Vector3 bUV = m_originalUVs[vertexIndexB];

        Vector3 aNormal = m_originalNormals[vertexIndexA];
        Vector3 bNormal = m_originalNormals[vertexIndexB];

        Vector3 intersection;
        Vector2 intersectionUV;
        Vector3 intersectionNormal;
        Partition partition = TryBisectEdge(a, b, aUV, bUV, aNormal, bNormal, out intersectionUV, out intersection, out intersectionNormal);

        // this edge is actually bisected!
        if (partition == Partition.A_POSITIVE || partition == Partition.B_POSITIVE)
        {
            if (partition == Partition.A_POSITIVE)
            {
                // the first point given to the method is the 'upper' point

                AddToBuffer(TOP, a, aUV, aNormal, allowDuplicates: false);
                AddToBuffer(TOP, intersection, intersectionUV, intersectionNormal, allowDuplicates: false);
                AddToBuffer(BOTTOM, intersection, intersectionUV, intersectionNormal, allowDuplicates: false);
                AddToBuffer(BOTTOM, b, bUV, bNormal, allowDuplicates: false);

                m_bufferVertices[CENTRE].Add(intersection);
                m_bufferUVs[CENTRE].Add(intersectionUV);
                m_bufferNormals[CENTRE].Add(intersectionNormal);
            }
            else if (partition == Partition.B_POSITIVE)
            {
                // the first point is the 'lower' point

                AddToBuffer(BOTTOM, a, aUV, aNormal, allowDuplicates: false);
                AddToBuffer(BOTTOM, intersection, intersectionUV, intersectionNormal, allowDuplicates: false);
                AddToBuffer(TOP, intersection, intersectionUV, intersectionNormal, allowDuplicates: false);
                AddToBuffer(TOP, b, bUV, bNormal, allowDuplicates: false);

                m_bufferVertices[CENTRE].Insert(Mathf.Max(0, m_bufferVertices[CENTRE].Count - 2), intersection);
                m_bufferUVs[CENTRE].Insert(Mathf.Max(0, m_bufferUVs[CENTRE].Count - 2), intersectionUV);
                m_bufferNormals[CENTRE].Insert(Mathf.Max(0, m_bufferNormals[CENTRE].Count - 2), intersectionNormal);
            }
        }

        return partition;
    }


    private void AddToFinal(int i, Vector3 point, Vector2 uv, Vector3 normal, bool allowDuplicates = true)
    {
        if (!allowDuplicates && m_finalVertices[i].Contains(point))
            return;

        m_finalVertices[i].Add(point);
        m_finalUVs[i].Add(uv);
        m_finalTriangles[i].Add(m_finalVertices[i].Count - 1);
        m_finalNormals[i].Add(normal);
    }

    private void AddToBuffer(int i, Vector3 point, Vector2 uv, Vector3 normal, bool allowDuplicates = true)
    {
        if (!allowDuplicates && m_bufferVertices[i].Contains(point))
            return;

        m_bufferVertices[i].Add(point);
        m_bufferUVs[i].Add(uv);
        m_bufferTriangles[i].Add(m_bufferVertices[i].Count - 1);
        m_bufferNormals[i].Add(normal);
    }

    #endregion

    #region Splitting

    /// <summary>
    /// Given a 'split size' and an array of pairwise points of intersection, 'extrude' the points on either side
    /// of the bisection plane's normal by that size, and then generate the triangles to make those points into a mesh.
    /// </summary>
    private void CalculateStretch(GameObject gO = null)
    {
        Vector3 change = CapOffset;

        if (m_meshFilters[TOP] == null && gO != null)
        {
            Debug.Log("It's null - " + gO.name, gO);
        }

        m_meshFilters[TOP].transform.position = change;
        m_meshFilters[BOTTOM].transform.position = -change;
        m_meshFilters[CENTRE].transform.position = Vector3.zero;

        // take all the points of intersection, and duplicate them, to use as the end points for the stretched mesh

        if (CurrentStretch > 0f)
        {
            Vector3[] redEdge = new Vector3[m_finalVertices[2].Count];
            Vector3[] blueEdge = new Vector3[m_finalVertices[2].Count];

            // the new UVs are just the old UVs doubled, since we want them to 'stretch' the entire 
            // length of the mesh
            List<Vector2> oldUVs = new List<Vector2>(m_finalUVs[2]);
            m_finalUVs[CENTRE].Clear();
            m_finalUVs[CENTRE].AddRange(oldUVs);
            m_finalUVs[CENTRE].AddRange(oldUVs);

            List<Vector3> oldNormals = new List<Vector3>(m_finalNormals[2]);
            m_finalNormals[CENTRE].Clear();
            m_finalNormals[CENTRE].AddRange(oldNormals);
            m_finalNormals[CENTRE].AddRange(oldNormals);

            for (int i = 0; i < m_finalVertices[2].Count; i++)
            {
                redEdge[i] = m_finalVertices[CENTRE][i] + change;
                blueEdge[i] = m_finalVertices[CENTRE][i] - change;
            }

            m_finalVertices[CENTRE].Clear();
            m_finalVertices[CENTRE].AddRange(redEdge);
            m_finalVertices[CENTRE].AddRange(blueEdge);

            int halfCount = m_finalVertices[CENTRE].Count / 2;

            // we're going around the 'lips' of both caps, connecting each adjacent pair, and then connecting the pairs
            // up with their corresponding pairs on the opposite cap

            for (int i = 0; i < halfCount; i += 2)
            {
                // [1a, 2a, 1b |
                //  2a, 2b, 1b ]
                m_finalTriangles[CENTRE].Add(i);
                m_finalTriangles[CENTRE].Add(i + 1);
                m_finalTriangles[CENTRE].Add(i + halfCount);

                m_finalTriangles[CENTRE].Add(i + 1);
                m_finalTriangles[CENTRE].Add(i + 1 + halfCount);
                m_finalTriangles[CENTRE].Add(i + halfCount);
            }
        }
    }

    #endregion

    /// <summary>
    /// Combine the three meshes generated by this mesh into a single mesh and delete the other GameObjects.
    /// </summary>
    public void Combine()
    {
        Calculate();

        ClearBuffer();
        
        m_bufferVertices[CENTRE] = m_finalVertices[CENTRE];
        m_bufferUVs[CENTRE] = m_finalUVs[CENTRE];
        m_bufferTriangles[CENTRE] = m_finalTriangles[CENTRE];
        m_bufferNormals[CENTRE] = m_finalNormals[CENTRE];

        for (int i = 0; i < m_finalVertices[TOP].Count; i++)
        {
            AddToBuffer(CENTRE, m_finalVertices[TOP][i] + m_meshFilters[TOP].transform.position, m_finalUVs[TOP][i], m_finalNormals[TOP][i]);
        }

        for (int i = 0; i < m_finalVertices[BOTTOM].Count; i++)
        {
            AddToBuffer(CENTRE, m_finalVertices[BOTTOM][i] + m_meshFilters[BOTTOM].transform.position, m_finalUVs[BOTTOM][i], m_finalNormals[BOTTOM][i]);
        }

        m_finalVertices[CENTRE] = m_bufferVertices[CENTRE];
        m_finalUVs[CENTRE] = m_bufferUVs[CENTRE];
        m_finalTriangles[CENTRE] = m_bufferTriangles[CENTRE];
        m_finalNormals[CENTRE] = m_bufferNormals[CENTRE];

        GenerateMesh(CENTRE);

        ClearAll();

        Object.Destroy(m_meshFilters[TOP].gameObject);
        Object.Destroy(m_meshFilters[BOTTOM].gameObject);

        m_meshFilters[CENTRE].gameObject.AddComponent<SplittableMesh>();
        m_meshFilters[CENTRE].transform.SetParent(null);
        Object.Destroy(m_parent.gameObject);
    }

    private void ClearAll(int clearIndex = -1)
    {
        for (int i = 0; i < m_finalVertices.Length; i++)
            if (clearIndex == -1 || clearIndex == i)
                m_finalVertices[i].Clear();

        for (int i = 0; i < m_finalUVs.Length; i++)
            if (clearIndex == -1 || clearIndex == i)
                m_finalUVs[i].Clear();

        for (int i = 0; i < m_finalTriangles.Length; i++)
            if (clearIndex == -1 || clearIndex == i)
                m_finalTriangles[i].Clear();

        for (int i = 0; i < m_finalNormals.Length; i++)
            if (clearIndex == -1 || clearIndex == i)
                m_finalNormals[i].Clear();

        ClearBuffer(clearIndex);
    }

    private void ClearBuffer(int clearIndex = -1)
    {
        for (int i = 0; i < m_bufferVertices.Length; i++)
            if (clearIndex == -1 || clearIndex == i)
                m_bufferVertices[i].Clear();

        for (int i = 0; i < m_bufferUVs.Length; i++)
            if (clearIndex == -1 || clearIndex == i)
                m_bufferUVs[i].Clear();

        for (int i = 0; i < m_bufferTriangles.Length; i++)
            if (clearIndex == -1 || clearIndex == i)
                m_bufferTriangles[i].Clear();

        for (int i = 0; i < m_bufferNormals.Length; i++)
            if (clearIndex == -1 || clearIndex == i)
                m_bufferNormals[i].Clear();
    }

    /// <summary>
    /// Adds a new MeshRenderer + MeshFilter on a new GameObject as a child of this MeshStretcher's
    /// original MeshFilter.
    /// </summary>s
    public MeshFilter AttachNewMeshFilter(MeshRenderer renderer, string name)
    {
        GameObject newGameObject = new GameObject(renderer.gameObject.name + "_" + name);
        newGameObject.transform.SetParent(m_parent);

        MeshCollider mc = newGameObject.AddComponent<MeshCollider>();

        Rigidbody rb = newGameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        
        newGameObject.AddComponent<MeshRenderer>().material = renderer.material;
        return newGameObject.AddComponent<MeshFilter>();
    }

    /// <summary>
    /// Disable the original renderer and enable the new renderers.
    /// </summary>
    private void SwitchRenderers()
    {
        if (m_originalMeshRenderer != null)
            m_originalMeshRenderer.enabled = false;

        for (int i = 0; i < m_meshRenderers.Length; i++)
            if (m_meshRenderers[i] != null)
                m_meshRenderers[i].enabled = true;

    }

    private void ApplyMeshes()
    {
        GenerateMesh(TOP);
        GenerateMesh(BOTTOM);
        GenerateMesh(CENTRE, autoCalculateNormals: true);
    }   

    private void GenerateMesh(int index, bool autoCalculateNormals = false)
    {
        Mesh mesh = new Mesh
        {
            vertices = m_finalVertices[index].ToArray(),
            triangles = m_finalTriangles[index].ToArray(),
            uv = m_finalUVs[index].ToArray(),
            normals = m_finalNormals[index].ToArray()
        };

        if (autoCalculateNormals)
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();
        
        m_meshFilters[index].sharedMesh = mesh;

        MeshCollider mc = m_meshFilters[index].GetComponent<MeshCollider>();

        if (mc != null)
        {
            mc.convex = true;
            mc.inflateMesh = true;
            mc.sharedMesh = mesh;
        }
    }

    #region Debug

    /// <summary>
    /// Draw the points used to calculate mesh-plane intersection.
    /// </summary>
    public void DrawCalculationPoints(Color colour, float radius = 0.03f)
    {
        DrawPoints(colour, m_calculationVertices, radius);
    }

    private void DrawPoints(Color colour, IList<Vector3> points, float radius = 0.03f)
    {
        for (int i = 0; i < points.Count; i++)
            DrawPoint(colour, points[i]);
    }

    private void DrawPoint(Color colour, Vector3 point, float radius = 0.03f)
    {
        Gizmos.color = colour;
        Gizmos.DrawSphere(point, radius);
    }

    #endregion
}