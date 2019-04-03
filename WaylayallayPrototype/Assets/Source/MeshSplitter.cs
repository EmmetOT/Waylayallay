using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Simplex;

public static class MeshSplitter
{
    [SerializeField]
    private static BisectionPlane m_bisectionPlane = new BisectionPlane();
    
    public static bool TryBisect(Mesh mesh, BisectionPlane plane, out Mesh[] bisectedMeshes, Transform transform = null)
    {
        if (transform != null)
            m_bisectionPlane.DrawGizmo(transform);

        m_bisectionPlane = plane;

        List<Vector3> vertices = mesh.vertices.ToList();
        List<int> triangles = mesh.triangles.ToList();

        List<Vector3>[] allNewVertices = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>() };
        List<int>[] allNewTriangles = new List<int>[] { new List<int>(), new List<int>() };

        bool result = false;

        //if (true)//LabelOriginalTriangles)
        //    LabelTriangle(Color.black, (startingIndex / 3).ToString(), startingIndex, triangles, vertices);

        //if (true)//LabelOriginalTrianglePoints)
        //    LabelPoints(Color.black, triangles, vertices, startingIndex);

        //if (true)//ShowOriginalTriangles)
        //    ShowTriangles(Color.black, triangles, vertices, startingIndex);


        //for (int i = 0; i < triangles.Count; i += 3)
        //    result |= TryRetriangulate(m_bisectionPlane, i, vertices, triangles, ref allNewVertices, ref allNewTriangles);

        TryRetriangulate(m_bisectionPlane, 0, vertices, triangles, ref allNewVertices, ref allNewTriangles);

        //Debug.Log("Vertices: " + allNewVertices[0].Count + ", " + allNewVertices[1].Count);
        //Debug.Log("Triangles: " + allNewTriangles[0].Count + ", " + allNewTriangles[1].Count);

        if (true)//(LabelNewTrianglePoints)
        {
            LabelPoints(Color.red, allNewTriangles[0], allNewVertices[0]);
            LabelPoints(Color.blue, allNewTriangles[1], allNewVertices[1]);
        }

        //if (m_showNewPolygons)
        //{
        //    ShowPolygons(Color.blue, allNewTriangles[1], allNewVertices[1]);
        //    ShowPolygons(Color.red, allNewTriangles[0], allNewVertices[0]);
        //}

        if (true)//(m_showNewTriangles)
        {
            //ShowTriangles(Color.blue, allNewTriangles[1], allNewVertices[1]);
            ShowTriangles(Color.red, allNewTriangles[0], allNewVertices[0]);
        }

        if (result)
            bisectedMeshes = new Mesh[] { GenerateMesh(allNewTriangles[0], allNewVertices[0]), GenerateMesh(allNewTriangles[1], allNewVertices[1]) };
        else
            bisectedMeshes = null;

        return result;
    }

    private static bool TryRetriangulate(Plane bisectionPlane, int startingIndex, IList<Vector3> vertices, IList<int> triangles, ref List<Vector3>[] allNewVertices, ref List<int>[] allNewTriangles)
    {
        Plane trianglePlane = new Plane(vertices[triangles[startingIndex]], vertices[triangles[startingIndex + 1]], vertices[triangles[startingIndex + 2]]);

        bool result = false;

        List<Vector3>[] newVertices = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>() };
        List<int>[] newTriangles = new List<int>[] { new List<int>(), new List<int>() };

        Graph.Partition partition;
        if (TryBisectTriangle(ref newVertices, ref newTriangles, startingIndex, bisectionPlane, triangles, vertices, out partition))
        {
            Triangulator triangulator;
            for (int i = 0; i < newTriangles.Length; i++)
            {
                if (newVertices[i].Count > 3)
                {
                    triangulator = new Triangulator(newVertices[i], trianglePlane.normal);
                    newTriangles[i] = new List<int>(triangulator.Triangulate());

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

            if (partition == Graph.Partition.BOTH_POSITIVE)
            {
                AddToVectorSet(ref newVertices[0], ref newTriangles[0], vertices[triangles[startingIndex]]);
                AddToVectorSet(ref newVertices[0], ref newTriangles[0], vertices[triangles[startingIndex + 1]]);
                AddToVectorSet(ref newVertices[0], ref newTriangles[0], vertices[triangles[startingIndex + 2]]);
            }
            else
            {
                AddToVectorSet(ref newVertices[1], ref newTriangles[1], vertices[triangles[startingIndex]]);
                AddToVectorSet(ref newVertices[1], ref newTriangles[1], vertices[triangles[startingIndex + 1]]);
                AddToVectorSet(ref newVertices[1], ref newTriangles[1], vertices[triangles[startingIndex + 2]]);
            }
        }

        //if (true)//LabelNewTrianglePoints)
        //{
        //    LabelPoints(Color.red, allNewTriangles[0], allNewVertices[0]);
        //    LabelPoints(Color.blue, allNewTriangles[1], allNewVertices[1]);
        //}

        //if (true)//ShowNewTriangles)
        //{
        //    ShowTriangles(Color.blue, newTriangles[1], newVertices[1]);
        //    ShowTriangles(Color.red, newTriangles[0], newVertices[0]);
        //}

        if (startingIndex == 0)
        {
            for (int i = 0; i < newVertices[0].Count; i++)
            {
                allNewVertices[0].Add(newVertices[0][i]);
                allNewTriangles[0].Add(allNewVertices[0].Count - 1);
            }

            LabelPoints(Color.black, newTriangles[0], newVertices[0], offset: Vector3.right * 0.2f);
        }

        Debug.Log("--------------");
        newVertices[0].Print();
        newTriangles[0].Print();

        //for (int i = 0; i < newVertices[1].Count; i++)
        //{
        //    allNewVertices[1].Add(newVertices[1][i]);
        //    allNewTriangles[1].Add(allNewVertices[1].Count - 1);
        //}

        //allNewTriangles[0].AddRange(newTriangles[0]);
        //allNewTriangles[1].AddRange(newTriangles[1]);

        //allNewVertices[0].AddRange(newVertices[0]);
        //allNewVertices[1].AddRange(newVertices[1]);

        return result;
    }

    private static void Print<T>(this IList<T> array)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.Append("[");

        for (int i = 0; i < array.Count; i++)
        {
            sb.Append(array[i].ToString());

            if (i < array.Count - 1)
                sb.Append(", ");
        }

        sb.Append("]");

        Debug.Log(sb.ToString());
    }

    private static Mesh GenerateMesh(IList<int> triangles, IList<Vector3> vertices)
    {
        Mesh mesh = new Mesh();
        mesh.triangles = triangles as int[];
        mesh.vertices = vertices as Vector3[];

        return mesh;
    }

    private static bool TryRetriangulate(Plane bisectionPlane, int startingIndex, IList<Vector3> vertices, IList<int> triangles)
    {
        if (LabelOriginalTriangles)
            LabelTriangle(Color.black, (startingIndex / 3).ToString(), startingIndex, triangles, vertices);

        if (LabelOriginalTrianglePoints)
            LabelPoints(Color.black, triangles, vertices, startingIndex);

        if (ShowOriginalTriangles)
            ShowTriangles(Color.black, triangles, vertices, startingIndex);

        Plane trianglePlane = new Plane(vertices[triangles[startingIndex]], vertices[triangles[startingIndex + 1]], vertices[triangles[startingIndex + 2]]);
        List<Vector3>[] newVertices = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>() };
        List<int>[] newTriangles = new List<int>[] { new List<int>(), new List<int>() };

        bool result = false;

        Graph.Partition partition;
        if (TryBisectTriangle(ref newVertices, ref newTriangles, startingIndex, bisectionPlane, triangles, vertices, out partition))
        {
            Triangulator triangulator;
            for (int i = 0; i < newTriangles.Length; i++)
            {
                if (newVertices[i].Count > 3)
                {
                    triangulator = new Triangulator(newVertices[i], trianglePlane.normal);
                    newTriangles[i] = new List<int>(triangulator.Triangulate());

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

            if (partition == Graph.Partition.BOTH_POSITIVE)
            {
                AddToVectorSet(ref newVertices[0], ref newTriangles[0], vertices[triangles[startingIndex]]);
                AddToVectorSet(ref newVertices[0], ref newTriangles[0], vertices[triangles[startingIndex + 1]]);
                AddToVectorSet(ref newVertices[0], ref newTriangles[0], vertices[triangles[startingIndex + 2]]);
            }
            else
            {
                AddToVectorSet(ref newVertices[1], ref newTriangles[1], vertices[triangles[startingIndex]]);
                AddToVectorSet(ref newVertices[1], ref newTriangles[1], vertices[triangles[startingIndex + 1]]);
                AddToVectorSet(ref newVertices[1], ref newTriangles[1], vertices[triangles[startingIndex + 2]]);
            }
        }

        if (LabelNewTrianglePoints)
        {
            LabelPoints(Color.red, newTriangles[0], newVertices[0]);
            LabelPoints(Color.blue, newTriangles[1], newVertices[1]);
        }
        
        if (ShowNewTriangles)
        {
            ShowTriangles(Color.blue, newTriangles[1], newVertices[1], offset: Vector3.one);
            ShowTriangles(Color.red, newTriangles[0], newVertices[0], offset: -Vector3.one);
        }

        return result;
    }

    private static Graph.Partition BisectEdge(Vector3 a, Vector3 b, ref List<Vector3>[] newVertices, ref List<int>[] newTriangles, Plane plane)
    {
        Vector3 intersection;
        Graph.Partition partition = TryBisectEdge(a, b, plane, out intersection);

        // this edge is actually bisected!
        if (partition == Graph.Partition.A_POSITIVE || partition == Graph.Partition.B_POSITIVE)
        {
            if (partition == Graph.Partition.A_POSITIVE)
            {
                // the first point given to the method is the 'upper' point

                AddToVectorSet(ref newVertices[0], ref newTriangles[0], intersection);
                AddToVectorSet(ref newVertices[0], ref newTriangles[0], a);
                AddToVectorSet(ref newVertices[1], ref newTriangles[1], intersection);
                AddToVectorSet(ref newVertices[1], ref newTriangles[1], b);
            }
            else if (partition == Graph.Partition.B_POSITIVE)
            {
                // the first point is the 'lower' point

                AddToVectorSet(ref newVertices[1], ref newTriangles[1], a);
                AddToVectorSet(ref newVertices[1], ref newTriangles[1], intersection);
                AddToVectorSet(ref newVertices[0], ref newTriangles[0], b);
                AddToVectorSet(ref newVertices[0], ref newTriangles[0], intersection);
            }

            if (ShowPointsOfIntersection)
                ShowPoint(Color.red, intersection);
        }

        return partition;
    }

    private static void AddToVectorSet(ref List<Vector3> vertices, ref List<int> triangles, Vector3 point)
    {
        if (vertices.Contains(point))
            return;

        vertices.Add(point);
        triangles.Add(vertices.Count - 1);
    }

    /// <summary>
    /// Try to bisect a triangle with a plane, splitting the result into two pairs of vertices and triangles.
    /// 
    /// Returns whether the triangle is bisected by the plane. If not bisected, the triangle must be on one side of the plane.
    /// Use the returned partition to determine which.
    /// </summary>
    private static bool TryBisectTriangle(ref List<Vector3>[] newVertices, ref List<int>[] newTriangles, int startIndex, Plane plane, IList<int> triangles, IList<Vector3> vertices, out Graph.Partition partition)
    {
        bool bisected = false;

        partition = BisectEdge(vertices[triangles[startIndex]], vertices[triangles[startIndex + 1]], ref newVertices, ref newTriangles, plane);
        bisected |= (partition == Graph.Partition.A_POSITIVE || partition == Graph.Partition.B_POSITIVE);

        partition = BisectEdge(vertices[triangles[startIndex + 1]], vertices[triangles[startIndex + 2]], ref newVertices, ref newTriangles, plane);
        bisected |= (partition == Graph.Partition.A_POSITIVE || partition == Graph.Partition.B_POSITIVE);

        partition = BisectEdge(vertices[triangles[startIndex + 2]], vertices[triangles[startIndex]], ref newVertices, ref newTriangles, plane);
        bisected |= (partition == Graph.Partition.A_POSITIVE || partition == Graph.Partition.B_POSITIVE);

        return bisected;


        // unity has a weird problem with this more succinct expression of the above
        //bisected |= (partition = BisectEdge(vertices[triangles[startIndex]], vertices[triangles[startIndex + 1]], ref newVertices, ref newTriangles, plane)).IsBisection();
        //bisected |= (partition = BisectEdge(vertices[triangles[startIndex + 1]], vertices[triangles[startIndex + 2]], ref newVertices, ref newTriangles, plane)).IsBisection();
        //bisected |= (partition = BisectEdge(vertices[triangles[startIndex + 2]], vertices[triangles[startIndex]], ref newVertices, ref newTriangles, plane)).IsBisection();
    }

    /// <summary>
    /// Returns whether the given plane bisects the line from a to b, and the point of intersection. 
    /// </summary>
    public static Graph.Partition TryBisectEdge(Vector3 a, Vector3 b, Plane plane, out Vector3 intersection)
    {
        intersection = default(Vector3);

        bool isAPositive = plane.GetSide(a);
        bool isBPositive = plane.GetSide(b);

        if (isAPositive != isBPositive)
        {
            float enter;
            Ray ray = new Ray(a, (b - a).normalized);

            if (plane.Raycast(ray, out enter))
                intersection = ray.GetPoint(enter);

            return isAPositive ? Graph.Partition.A_POSITIVE : Graph.Partition.B_POSITIVE;
        }
        else if (isAPositive)
        {
            return Graph.Partition.BOTH_POSITIVE;
        }
        else
        {
            return Graph.Partition.BOTH_NEGATIVE;
        }
    }

    #region Debugging

    public static bool ShowBisectionPlane { get; set; }

    public static bool ShowPointsOfIntersection { get; set; }

    public static bool LabelOriginalTriangles { get; set; }

    public static bool ShowOriginalTriangles { get; set; }
    public static bool ShowNewTriangles { get; set; }

    public static bool LabelOriginalTrianglePoints { get; set; }
    public static bool LabelNewTrianglePoints { get; set; }

    public static void LabelPoints(Color colour, IList<int> triangles, IList<Vector3> vertices, int startingIndex = -1, Vector3 offset = default(Vector3))
    {
        GUIStyle handleStyle = new GUIStyle();
        handleStyle.normal.textColor = colour;

        Gizmos.color = colour;

        if (startingIndex == -1)
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                Gizmos.DrawSphere(vertices[triangles[i]], 0.04f);
                Handles.Label(vertices[triangles[i]] + offset, i.ToString(), handleStyle);
            }
        }
        else
        {
            Gizmos.DrawSphere(vertices[triangles[startingIndex]], 0.04f);
            Handles.Label(vertices[triangles[startingIndex]] + offset, startingIndex.ToString(), handleStyle);

            Gizmos.DrawSphere(vertices[triangles[startingIndex + 1]], 0.04f);
            Handles.Label(vertices[triangles[startingIndex + 1]] + offset, startingIndex.ToString(), handleStyle);

            Gizmos.DrawSphere(vertices[triangles[startingIndex + 2]], 0.04f);
            Handles.Label(vertices[triangles[startingIndex + 2]] + offset, startingIndex.ToString(), handleStyle);
        }
    }

    public static void LabelTriangle(Color colour, string label, int startingIndex, IList<int> triangles, IList<Vector3> vertices)
    {
        Vector3 centroid = Vector3.zero;
        centroid += vertices[triangles[startingIndex]];
        centroid += vertices[triangles[startingIndex + 1]];
        centroid += vertices[triangles[startingIndex + 2]];
        centroid /= 3f;

        GUIStyle handleStyle = new GUIStyle();
        handleStyle.normal.textColor = colour;

        Handles.Label(centroid, label, handleStyle);
    }

    public static void ShowPolygons(Color colour, IList<int> triangles, IList<Vector3> vertices)
    {
        Gizmos.color = colour;

        for (int i = 0; i < triangles.Count; i++)
        {
            Gizmos.DrawLine(vertices[triangles[i]], vertices[triangles[(i + 1) % triangles.Count]]);
        }
    }

    public static void ShowTriangles(Color colour, IList<int> triangles, IList<Vector3> vertices, int startingIndex = -1, Vector3 offset = default(Vector3))
    {
        Gizmos.color = colour;

        if (startingIndex == -1)
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Gizmos.DrawLine(vertices[triangles[i]] + offset, vertices[triangles[i + 1]] + offset);
                Gizmos.DrawLine(vertices[triangles[i + 1]] + offset, vertices[triangles[i + 2]] + offset);
                Gizmos.DrawLine(vertices[triangles[i + 2]] + offset, vertices[triangles[i]] + offset);
            }
        }
        else
        {
            Gizmos.DrawLine(vertices[triangles[startingIndex]] + offset, vertices[triangles[startingIndex + 1]] + offset);
            Gizmos.DrawLine(vertices[triangles[startingIndex + 1]] + offset, vertices[triangles[startingIndex + 2]] + offset);
            Gizmos.DrawLine(vertices[triangles[startingIndex + 2]] + offset, vertices[triangles[startingIndex]] + offset);
        }
    }

    public static void ShowPoints(Color colour, IList<Vector3> points, Vector3 offset = default(Vector3))
    {
        for (int i = 0; i < points.Count; i++)
            ShowPoint(colour, points[i], offset);
    }

    public static void ShowPoint(Color colour, Vector3 point, Vector3 offset = default(Vector3))
    {
        Gizmos.color = colour;
        Gizmos.DrawSphere(point + offset, 0.06f);
    }


    #endregion
}