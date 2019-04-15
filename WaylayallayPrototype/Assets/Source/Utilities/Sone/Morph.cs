using System.Collections.Generic;
using UnityEngine;

namespace Simplex
{
    public static partial class Graph
    {
        /// <summary>
        /// A mesh representation, like Unity's Mesh class, but with
        /// a bunch of extra features.
        /// </summary>
        [System.Serializable]
        public class Morph
        {
            private Hashes m_hashes = new Hashes();
            private Lookups m_lookups = new Lookups();

            private class Hashes
            {
                private OrderedHashSet<Point> m_points = new OrderedHashSet<Point>();
                private OrderedHashSet<Edge> m_edges = new OrderedHashSet<Edge>();
                private OrderedHashSet<Triangle> m_triangles = new OrderedHashSet<Triangle>();
                private OrderedHashSet<Face> m_faces = new OrderedHashSet<Face>();

                public IEnumerable<Point> Points
                {
                    get
                    {
                        for (int i = 0; i < m_points.Count; i++)
                        {
                            yield return m_points[i];
                        }
                    }
                }

                public void AddPoint(Point point)
                {
                    m_points.Add(point);
                }

                public void RemovePoint(Point point)
                {
                    m_points.Remove(point);
                }

                public bool HasPoint(Point point)
                {
                    return m_points.Contains(point);
                }

                public void AddEdge(Edge edge)
                {
                    m_edges.Add(edge);
                }

                public void RemoveEdge(Edge edge)
                {
                    m_edges.Remove(edge);
                }

                public bool HasEdge(Edge edge)
                {
                    return m_edges.Contains(edge);
                }

                public void AddTriangle(Triangle triangle)
                {
                    m_triangles.Add(triangle);
                }

                public void RemoveTriangle(Triangle triangle)
                {
                    m_triangles.Remove(triangle);
                }

                public bool HasTriangle(Triangle triangle)
                {
                    return m_triangles.Contains(triangle);
                }

                public void AddFace(Face face)
                {
                    m_faces.Add(face);
                }

                public void RemoveFace(Face face)
                {
                    m_faces.Remove(face);
                }

                public bool HasFace(Face face)
                {
                    return m_faces.Contains(face);
                }
            }

            private class Lookups
            {
                private Dictionary<Point, HashSet<Edge>> m_edges = new Dictionary<Point, HashSet<Edge>>();
                private Dictionary<Point, HashSet<Triangle>> m_triangles = new Dictionary<Point, HashSet<Triangle>>();
                private Dictionary<Point, HashSet<Face>> m_faces = new Dictionary<Point, HashSet<Face>>();

                public void AddEdge(Edge edge)
                {
                    foreach (Point point in edge.Points)
                    {
                        if (!m_edges.ContainsKey(point))
                            m_edges.Add(point, new HashSet<Edge>());

                        m_edges[point].Add(edge);
                    }
                }

                public IEnumerable<Edge> EdgesFromPoint(Point point)
                {
                    foreach (Edge edge in m_edges[point])
                        yield return edge;
                }

                public void AddTriangle(Triangle triangle)
                {
                    foreach (Point point in triangle.Points)
                    {
                        if (!m_triangles.ContainsKey(point))
                            m_triangles.Add(point, new HashSet<Triangle>());

                        m_triangles[point].Add(triangle);
                    }

                    foreach (Edge edge in triangle.Edges)
                    {
                        AddEdge(edge);
                    }
                }

                public IEnumerable<Triangle> TrianglesFromPoint(Point point)
                {
                    foreach (Triangle triangle in m_triangles[point])
                        yield return triangle;
                }
            }
            
            public Morph() { }

            public Morph(Mesh mesh)
            {
                int[] triangles = mesh.triangles;
                Vector3[] vertices = mesh.vertices;
                Vector2[] uvs = mesh.uv;
                Vector3[] normals = mesh.normals;
                Vector4[] tangents = mesh.tangents;

                int indexA, indexB, indexC;
                Point a, b, c;

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    indexA = triangles[i];
                    indexB = triangles[i + 1];
                    indexC = triangles[i + 2];

                    a = new Point(vertices[indexA], uvs[indexA], normals[indexA], tangents[indexA]);
                    b = new Point(vertices[indexB], uvs[indexB], normals[indexB], tangents[indexB]);
                    c = new Point(vertices[indexC], uvs[indexC], normals[indexC], tangents[indexC]);

                    AddTriangle(a, b, c);
                }
            }

            public Mesh ToMesh()
            {
                return null;
            }
            
            /// <summary>
            /// Add a new triangle connecting points a, b, and c.
            /// </summary>
            private void AddTriangle(Point a, Point b, Point c)
            {
                Edge ab = new Edge(a, b);
                Edge bc = new Edge(b, c);
                Edge ca = new Edge(c, a);
                
                AddEdge(ab);
                AddEdge(bc);
                AddEdge(ca);

                m_lookups.AddTriangle(new Triangle(ab, bc, ca));
            }

            /// <summary>
            /// Add a new edge connecting point A to point B.
            /// </summary>
            private void AddEdge(Edge edge)
            {
                m_hashes.AddPoint(edge.A);
                m_hashes.AddPoint(edge.B);

                RefreshIndices();

                m_lookups.AddEdge(edge);
            }
            
            /// <summary>
            /// Get a collection of all the points to which the given point is connected.
            /// </summary>
            public HashSet<Vector3> GetConnectedPoints(Vector3 point)
            {
                throw new System.NotImplementedException();
                //if (m_edges.ContainsKey(point))
                //    return m_edges[point];

                //return null;
            }

            /// <summary>
            /// Tries to determine whether it's possible to draw a path from point A to 
            /// point B.
            /// </summary>
            public bool AreConnected(Point a, Point b)
            {
                Debug.Assert(m_hashes.HasPoint(a) && m_hashes.HasPoint(b), "Both points A and B must be in the Morph.");

                HashSet<Point> seen = new HashSet<Point>();

                return AreConnected_Internal(a, b, ref seen);
            }

            /// <summary>
            /// Tries to determine whether it's possible to draw a path from point A to 
            /// point B.
            /// 
            /// (Internal method which includes the hashset for remembering where you've been.)
            /// </summary>
            private bool AreConnected_Internal(Point a, Point b, ref HashSet<Point> seen)
            {
                foreach (Edge edge in m_lookups.EdgesFromPoint(a))
                    if (edge.Contains(b))
                        return true;

                foreach (Edge edge in m_lookups.EdgesFromPoint(a))
                {
                    Point nextPoint;

                    if (edge.TryGetOpposite(a, out nextPoint))
                    {
                        if (!seen.Contains(a) && AreConnected_Internal(nextPoint, b, ref seen))
                            return true;
                    }
                }

                seen.Add(a);

                return false;
            }

            /// <summary>
            /// Ensure that the indices for each point match their position in the
            /// ordered hashset.
            /// </summary>
            private void RefreshIndices()
            {
                int index = 0;
                foreach (Point point in m_hashes.Points)
                    point.Index = index++;
            }

            /// <summary>
            /// Represents a vertex of the mesh. A class
            /// so a single vertex can be referenced in lots of places without being copied,
            /// and unlike Vector3 it has a hash code which is guaranteed to be the same for points in the same position.
            /// 
            /// This is the building block of all meshes.
            /// </summary>
            [System.Serializable]
            public class Point
            {
                public Vector3 Vector { get; private set; }
                public Vector2 UV { get; private set; }
                public Vector3 Normal { get; private set; }
                public Vector4 Tangent { get; private set; }

                public int Index { get; set; }

                public Point(Vector3 vector) : this(vector, Vector2.zero, Vector3.zero, Vector4.zero) { }

                public Point(Vector3 vector, Vector2 uv, Vector3 normal, Vector4 tangent)
                {
                    Vector = vector;
                    UV = uv;
                    Normal = normal;
                    Tangent = tangent;
                }

                public override int GetHashCode()
                {
                    // this is the part that makes sure
                    // all points in the same position share a hash, which
                    // can't be said of normal vector3s because of floating point inaccuracy.
                    return Vector.HashVector3();
                }

                public override bool Equals(object obj)
                {
                    Point point = obj as Point;

                    if (point != null)
                        return Equals(point);

                    return false;
                }

                public bool Equals(Point other)
                {
                    return GetHashCode() == other.GetHashCode();
                }
            }

            /// <summary>
            /// Represents the connection of 2 points to form an edge of the mesh.
            /// </summary>
            [System.Serializable]
            public class Edge
            {
                public Point A { get; private set; }
                public Point B { get; private set; }

                public IEnumerable<Point> Points
                {
                    get
                    {
                        yield return A;
                        yield return B;
                    }
                }

                public Edge(Point a, Point b)
                {
                    A = a;
                    B = b;
                }

                public override int GetHashCode()
                {
                    return A.GetHashCode() + B.GetHashCode();
                }

                public override bool Equals(object obj)
                {
                    Edge edge = obj as Edge;

                    if (edge != null)
                        return Equals(edge);

                    return false;
                }

                public bool Equals(Edge other)
                {
                    return GetHashCode() == other.GetHashCode();
                }

                /// <summary>
                /// Returns whether the given point is on this edge.
                /// </summary>
                public bool Contains(Point point)
                {
                    return A == point || B == point;
                }

                /// <summary>
                /// Given a point on this edge, returns true and
                /// assigns the opposite point to the out param.
                /// 
                /// If the given point is not in this edge, returns false.
                /// </summary>
                public bool TryGetOpposite(Point input, out Point output)
                {
                    if (input == A)
                    {
                        output = B;
                        return true;
                    }
                    else if (input == B)
                    {
                        output = A;
                        return true;
                    }

                    output = null;
                    return false;
                }
            }

            /// <summary>
            /// Represents the connection of three edges to form a single triangle
            /// of the mesh. A > B > C in clockwise order.
            /// </summary>
            [System.Serializable]
            public class Triangle
            {
                public Edge AB { get; private set; }
                public Edge BC { get; private set; }
                public Edge CA { get; private set; }

                public Point A { get { return AB.A; } }
                public Point B { get { return AB.B; } }
                public Point C { get { return BC.B; } }

                public Vector3 Normal { get; private set; }

                public IEnumerable<Edge> Edges
                {
                    get
                    {
                        yield return AB;
                        yield return BC;
                        yield return CA;
                    }
                }

                public IEnumerable<Point> Points
                {
                    get
                    {
                        yield return A;
                        yield return B;
                        yield return C;
                    }
                }
                
                public Triangle(Edge ab, Edge bc, Edge ca)
                {
                    AB = ab;
                    BC = bc;
                    CA = ca;

                    Vector3 side1 = B.Vector - A.Vector;
                    Vector3 side2 = C.Vector - A.Vector;

                    Normal = Vector3.Cross(side1, side2).normalized;
                }

                /// <summary>
                /// Returns true if the two triangles are part of the same face - 
                /// that is, they are both conormal (facing the same way) and contiguous
                /// (attached to each other.)
                /// </summary>
                public bool IsSameFace(Triangle triangle)
                {
                    return IsConormal(triangle) && IsContiguous(triangle);
                }

                /// <summary>
                /// Returns true if the two triangles share an edge.
                /// </summary>
                public bool IsContiguous(Triangle triangle)
                {
                    foreach (Edge edge in Edges)
                    {
                        foreach (Edge other in triangle.Edges)
                        {
                            if (edge == other)
                                return true;
                        }
                    }

                    return false;
                }

                /// <summary>
                /// Returns true if the two triangles share a normal.
                /// </summary>
                public bool IsConormal(Triangle triangle)
                {
                    return Normal == triangle.Normal;
                }

                public override int GetHashCode()
                {
                    return AB.GetHashCode() + BC.GetHashCode() + CA.GetHashCode();
                }

                public override bool Equals(object obj)
                {
                    Triangle triangle = obj as Triangle;

                    if (triangle != null)
                        return Equals(triangle);

                    return false;
                }

                public bool Equals(Triangle other)
                {
                    return GetHashCode() == other.GetHashCode();
                }
            }

            /// <summary>
            /// A face represents a number of triangles which are all contiguous 
            /// and conormal.
            /// </summary>
            [System.Serializable]
            public class Face
            {
                private HashSet<Triangle> m_triangles = new HashSet<Triangle>();

                public void AddTriangle(Triangle triangle)
                {
                    m_triangles.Add(triangle);
                }

                public bool IsContiguous(Triangle triangle)
                {
                    throw new System.NotImplementedException();
                }

                public bool IsConormal(Triangle triangle)
                {
                    throw new System.NotImplementedException();
                }

                public bool IsContinguous(Face face)
                {
                    throw new System.NotImplementedException();
                }

                public bool IsConormal(Face face)
                {
                    throw new System.NotImplementedException();
                }

                public void Retriangulate()
                {
                    throw new System.NotImplementedException();
                }

                public override int GetHashCode()
                {
                    int result = 0;

                    foreach (Triangle triangle in m_triangles)
                        result += triangle.GetHashCode();

                    return result;
                }

                public override bool Equals(object obj)
                {
                    Face face = obj as Face;

                    if (face != null)
                        return Equals(face);

                    return false;
                }

                public bool Equals(Face other)
                {
                    return GetHashCode() == other.GetHashCode();
                }
            }
        }
    }

    public class OrderedHashSet<T> : System.Collections.ObjectModel.KeyedCollection<T, T>
    {
        protected override T GetKeyForItem(T item)
        {
            return item;
        }
    }
}