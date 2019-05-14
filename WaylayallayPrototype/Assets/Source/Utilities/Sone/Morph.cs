﻿using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Simplex
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

        public IEnumerable<Point> Points
        {
            get
            {
                foreach (Point point in m_hashes.Points)
                    yield return point;
            }
        }

        /// <summary>
        /// Add a new triangle connecting points a, b, and c.
        /// </summary>
        public void AddTriangle(Point a, Point b, Point c)
        {
            Edge ab = new Edge(a, b);
            Edge bc = new Edge(b, c);
            Edge ca = new Edge(c, a);

            AddEdge(ab);
            AddEdge(bc);
            AddEdge(ca);

            Triangle triangle = new Triangle(ab, bc, ca);

            m_hashes.AddTriangle(triangle);
            m_lookups.AddTriangle(triangle);
        }

        /// <summary>
        /// Add a new triangle connecting points a, b, and c.
        /// </summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            AddTriangle(
                m_lookups.GetExistingPointInSameLocation(a),
                m_lookups.GetExistingPointInSameLocation(b),
                m_lookups.GetExistingPointInSameLocation(c));
        }

        public bool DebugMode = false;

        public void DebugPrint()
        {
            m_lookups.DebugPrint();
        }

        /// <summary>
        /// Add a new edge connecting point A to point B.
        /// </summary>
        public void AddEdge(Edge edge)
        {
            if (DebugMode)
                Debug.Log("Adding an edge connecting " + edge.A + " and " + edge.B);

            m_hashes.AddPoint(edge.A);
            m_hashes.AddPoint(edge.B);

            m_hashes.AddEdge(edge);

            m_lookups.AddPoint(edge.A);
            m_lookups.AddPoint(edge.B);

            m_lookups.AddEdge(edge);

            // TODO: we must determine what new triangles this creates and add those also

        }

        /// <summary>
        /// Add a new edge connecting point A to point B.
        /// </summary>
        public void AddEdge(Point a, Point b)
        {
            AddEdge(new Edge(a, b));
        }

        /// <summary>
        /// Add a new edge connecting the points at the given indices.
        /// </summary>
        public void AddEdge(int aIndex, int bIndex)
        {
            if (DebugMode)
                Debug.Log("Trying to get points " + aIndex + " and " + bIndex);

            Debug.Assert(m_hashes.HasPoint(aIndex) && m_hashes.HasPoint(bIndex), "Both points A and B must be in the Morph.");

            Point a = m_hashes.GetPoint(aIndex);
            Point b = m_hashes.GetPoint(bIndex);

            AddEdge(a, b);
        }

        /// <summary>
        /// Get the point with the given index.
        /// </summary>
        public Point GetPoint(int pointIndex)
        {
            return m_hashes.GetPoint(pointIndex);
        }

        /// <summary>
        /// Get the edge connecting the points with the given idnex.
        /// </summary>
        public Edge GetEdge(int aIndex, int bIndex)
        {
            Debug.Assert(m_hashes.HasPoint(aIndex) && m_hashes.HasPoint(bIndex), "Both points A and B must be in the Morph.");

            Point a = m_hashes.GetPoint(aIndex);
            Point b = m_hashes.GetPoint(bIndex);

            foreach (Edge edge in m_lookups.EdgesFromPoint(a))
                if (edge.Contains(b))
                    return edge;

            return null;
        }

        /// <summary>
        /// Get a collection of all the points to which the given point is connected.
        /// </summary>
        public IEnumerable<Point> GetConnectedPoints(Point point)
        {
            Point opposite;
            foreach (Edge edge in m_lookups.EdgesFromPoint(point))
            {
                if (edge.TryGetOpposite(point, out opposite))
                    yield return opposite;
            }
        }

        /// <summary>
        /// Get a collection of all the points to which the given point is connected.
        /// </summary>
        public IEnumerable<Point> GetConnectedPoints(int pointIndex)
        {
            Point point = m_hashes.GetPoint(pointIndex);

            if (point == null)
                yield break;

            foreach (Point connected in GetConnectedPoints(point))
                yield return connected;
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

#if UNITY_EDITOR
        public void DrawGizmo()
        {
            foreach (Triangle triangle in m_hashes.Triangles)
                triangle.DrawGizmo(Color.black, Color.black);
        }
#endif

        #region Primitive Classes

        /// <summary>
        /// Represents a vertex of the mesh. A class
        /// so a single vertex can be referenced in lots of places without being copied,
        /// and unlike Vector3 it has a hash code which is guaranteed to be the same for points in the same position.
        /// 
        /// This is the building block of all morphs.
        /// </summary>
        [System.Serializable]
        public class Point
        {
            public Vector3 Position { get; set; }
            public Vector2 UV { get; private set; }
            public Vector3 Normal { get; private set; }
            public Vector4 Tangent { get; private set; }

            private int m_id = -1;

            /// <summary>
            /// Used to uniquely identify a particular point object, even after transformations.
            /// </summary>
            public int ID
            {
                get
                {
                    return m_id;
                }
            }

            public Point(Vector3 vector) : this(vector, Vector2.zero, Vector3.zero, Vector4.zero) { }

            public Point(Vector3 vector, Vector2 uv, Vector3 normal, Vector4 tangent)
            {
                Position = vector;
                UV = uv;
                Normal = normal;
                Tangent = tangent;
            }

            /// <summary>
            /// Set the value used to uniquely identify this point. Should not change over the lifetime
            /// of the object.
            /// </summary>
            public void SetID(int id)
            {
                m_id = id;
            }

            /// <summary>
            /// Returns an integer which is unique to the point's current location. This ID
            /// is not liable to floating point errors which makes it a very useful way to compare
            /// vectors.
            /// </summary>
            public int GetLocationID()
            {
                return Position.HashVector3();
            }

            public static implicit operator Point(Vector3 vec)
            {
                return new Point(vec);
            }

            public override string ToString()
            {
                return "[" + ID + "] " + Position.ToString();
            }

#if UNITY_EDITOR
            public void DrawGizmo(Color col, float radius = 0.05f, bool label = true)
            {
                Gizmos.color = col;
                Gizmos.DrawSphere(Position, radius);

                if (label)
                {
                    GUIStyle handleStyle = new GUIStyle();
                    handleStyle.normal.textColor = Color.white;
                    handleStyle.fontSize = 30;

                    Handles.Label(Position + Vector3.one * 0.2f, ID.ToString(), handleStyle);
                }
#endif
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
            
#if UNITY_EDITOR
            public void DrawGizmo(Color pointCol, Color edgeCol, float radius = 0.05f)
            {
                Gizmos.color = edgeCol;
                Gizmos.DrawLine(A.Position, B.Position);

                A.DrawGizmo(pointCol, radius);
                B.DrawGizmo(pointCol, radius);
            }

            public void DrawGizmo(Color edgeCol)
            {
                Gizmos.color = edgeCol;
                Gizmos.DrawLine(A.Position, B.Position);
            }
#endif
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

                Vector3 side1 = B.Position - A.Position;
                Vector3 side2 = C.Position - A.Position;

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

#if UNITY_EDITOR
            public void DrawGizmo(Color pointCol, Color edgeCol, float radius = 0.05f)
            {
                foreach (Point point in Points)
                    point.DrawGizmo(pointCol, radius);

                foreach (Edge edge in Edges)
                    edge.DrawGizmo(edgeCol);

            }
#endif
        }

        /// <summary>
        /// A face represents a number of triangles which are all contiguous 
        /// and conormal.
        /// </summary>
        [System.Serializable]
        public class Face
        {
            private HashSet<Triangle> m_triangles = new HashSet<Triangle>();

            public IEnumerable<Triangle> Triangles
            {
                get
                {
                    foreach (Triangle triangle in m_triangles)
                        yield return triangle;
                }
            }

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
        }

        #endregion

        #region Organization Classes

        /// <summary>
        /// This class stores all objects which make up this morph.
        /// </summary>
        [System.Serializable]
        private class Hashes
        {
            private Dictionary<int, Point> m_points = new Dictionary<int, Point>();

            private HashSet<Edge> m_edges = new HashSet<Edge>();
            private HashSet<Triangle> m_triangles = new HashSet<Triangle>();
            private HashSet<Face> m_faces = new HashSet<Face>();

            private int m_highestPointIndex = 0;

            public IEnumerable<Point> Points
            {
                get
                {
                    foreach (KeyValuePair<int, Point> kvp in m_points)
                        yield return kvp.Value;
                }
            }

            public IEnumerable<Edge> Edges
            {
                get
                {
                    foreach (Edge edge in m_edges)
                        yield return edge;
                }
            }

            public IEnumerable<Triangle> Triangles
            {
                get
                {
                    foreach (Triangle triangle in m_triangles)
                        yield return triangle;
                }
            }

            // TODO: need to check whether we already have this point,
            // even though we want to use IDs to track points and the point may not have an ID yet...
            public void AddPoint(Point point)
            {
                if (point.ID == -1)
                    point.SetID(m_highestPointIndex);

                if (!m_points.ContainsKey(point.ID))
                {
                    ++m_highestPointIndex;

                    Debug.Log("Adding " + point.ID + " | " + point.ToString());
                    m_points.Add(point.ID, point);
                }
            }

            public void RemovePoint(Point point)
            {
                m_points.Remove(point.ID);
            }

            public bool HasPoint(Point point)
            {
                return HasPoint(point.ID);
            }

            public bool HasPoint(int id)
            {
                return m_points.ContainsKey(id);
            }

            public Point GetPoint(int id)
            {
                if (!m_points.ContainsKey(id))
                    return null;

                return m_points[id];
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

        /// <summary>
        /// This class allows quickly checking which objects (edges, triangles, faces) contain certain points.
        /// </summary>
        [System.Serializable]
        private class Lookups
        {
            private Dictionary<int, HashSet<Point>> m_pointsByLocation = new Dictionary<int, HashSet<Point>>();

            private Dictionary<int, HashSet<Edge>> m_edges = new Dictionary<int, HashSet<Edge>>();
            private Dictionary<int, HashSet<Triangle>> m_triangles = new Dictionary<int, HashSet<Triangle>>();
            private Dictionary<int, HashSet<Face>> m_faces = new Dictionary<int, HashSet<Face>>();

            public void AddEdge(Edge edge)
            {
                foreach (Point point in edge.Points)
                {
                    if (!m_edges.ContainsKey(point.ID))
                        m_edges.Add(point.ID, new HashSet<Edge>());

                    foreach (Edge addedEdge in m_edges[point.ID])
                    {

                    }

                    m_edges[edge.A.ID].Add(edge);
                }
            }

            public void DebugPrint()
            {
                foreach (KeyValuePair<int, HashSet<Edge>> kvp in m_edges)
                {
                    Debug.Log(kvp.Key);

                    foreach (Edge edge in kvp.Value)
                    {
                        Debug.Log("-----> [" + edge.A.ID + ", " + edge.B.ID + "]");
                    }
                }
            }

            public IEnumerable<Edge> EdgesFromPoint(Point point)
            {
                foreach (Edge edge in m_edges[point.ID])
                    yield return edge;
            }

            public void AddTriangle(Triangle triangle)
            {
                foreach (Point point in triangle.Points)
                {
                    if (!m_triangles.ContainsKey(point.ID))
                        m_triangles.Add(point.ID, new HashSet<Triangle>());

                    m_triangles[point.ID].Add(triangle);
                }

                foreach (Edge edge in triangle.Edges)
                {
                    AddEdge(edge);
                }
            }

            public IEnumerable<Triangle> TrianglesFromPoint(Point point)
            {
                foreach (Triangle triangle in m_triangles[point.ID])
                    yield return triangle;
            }

            public void AddPoint(Point point)
            {
                int locationID = point.GetLocationID();

                if (!m_pointsByLocation.ContainsKey(locationID))
                    m_pointsByLocation.Add(locationID, new HashSet<Point>());

                m_pointsByLocation[locationID].Add(point);
            }

            /// <summary>
            /// This method needs to be called whenever a point moves in the mesh.
            /// 
            /// TODO: PROBABLY DELETE OR HUGELY OPTIMIZE
            /// </summary>
            public void RegeneratePointByLocationLookup()
            {
                List<Point> points = new List<Point>(m_pointsByLocation.Count);

                foreach (KeyValuePair<int, HashSet<Point>> kvp in m_pointsByLocation)
                    foreach (Point point in kvp.Value)
                        points.Add(point);

                m_pointsByLocation.Clear();

                foreach (Point point in points)
                    AddPoint(point);
            }

            /// <summary>
            /// If we already have a point object in the exact same location as the given point object,
            /// return it. Else return the given point back.
            /// </summary>
            public Point GetExistingPointInSameLocation(Point point)
            {
                int locationID = point.GetLocationID();

                if (m_pointsByLocation.ContainsKey(locationID) && !m_pointsByLocation[locationID].IsNullOrEmpty())
                {
                    return m_pointsByLocation[locationID].First();
                }

                return point;
            }
        }

        #endregion
    }

    public class OrderedHashSet<T> : System.Collections.ObjectModel.KeyedCollection<T, T>
    {
        protected override T GetKeyForItem(T item)
        {
            return item;
        }
    }
}