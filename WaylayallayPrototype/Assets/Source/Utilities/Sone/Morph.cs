using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Assertions;

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
            if (a.ID == -1)
                m_hashes.AddPoint(a);

            if (b.ID == -1)
                m_hashes.AddPoint(b);

            if (c.ID == -1)
                m_hashes.AddPoint(c);
            
            AddEdge(new Edge(a, b));
            AddEdge(new Edge(b, c));
            AddEdge(new Edge(c, a));
        }

        /// <summary>
        /// Add a new triangle connecting points a, b, and c.
        /// </summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Point pointA = m_lookups.GetExistingPointInSameLocation(a.ToPoint());
            Point pointB = m_lookups.GetExistingPointInSameLocation(b.ToPoint());
            Point pointC = m_lookups.GetExistingPointInSameLocation(c.ToPoint());
            
            AddTriangle(pointA, pointB, pointC);
        }

        public void AddTriangle(int a, int b, int c)
        {
            Point pointA = m_hashes.GetPoint(a);
            Point pointB = m_hashes.GetPoint(b);
            Point pointC = m_hashes.GetPoint(c);

            Debug.Assert(pointA != null && pointB != null && pointC != null, "All three points used to make a triangle must be in the Morph.");

            AddTriangle(pointA, pointB, pointC);
        }
        
        /// <summary>
        /// Add a new edge connecting point A to point B.
        /// 
        /// If this
        /// </summary>
        public void AddEdge(Edge edge)
        {
            if (m_hashes.HasEdge(edge))
            {
                Debug.Log("Already have the edge " + edge.ToString());
                return;
            }

            Debug.Log("Adding edge " + edge.ToString());

            m_hashes.AddPoint(edge.A);
            m_hashes.AddPoint(edge.B);
            
            m_hashes.AddEdge(edge);

            m_lookups.AddPoint(edge.A);
            m_lookups.AddPoint(edge.B);

            m_lookups.AddEdge(edge);
            
            // adding an edge may create a new triangle
            // 1) get all the points connected to A
            // 2) get all the points connected to B
            // 3) compare - if any overlap points P, A-B-P forms a triangle

            HashSet<int> pointsFromB = m_lookups.GetConnectedPoints(edge.B.ID);

            foreach (int pointFromA in m_lookups.GetConnectedPoints(edge.A.ID))
            {
                if (pointsFromB.Contains(pointFromA))
                {
                    Triangle triangle = new Triangle(edge, m_lookups.GetEdge(edge.B.ID, pointFromA), m_lookups.GetEdge(pointFromA, edge.A.ID));

                    m_hashes.AddTriangle(triangle);
                    m_lookups.AddTriangle(triangle);
                }
            }
        }
        
        /// <summary>
        /// Add a new edge connecting point A to point B.
        /// </summary>
        public Edge AddEdge(Point a, Point b)
        {
            Assert.AreNotEqual(a.Position, b.Position);

            Edge edge = new Edge(a, b);
            AddEdge(edge);

            return edge;
        }

        /// <summary>
        /// Add a new edge connecting point A to point B.
        /// </summary>
        public Edge AddEdge(Vector3 a, Vector3 b)
        {
            Assert.AreNotEqual(a, b);
            
            Edge edge = new Edge(m_lookups.GetExistingPointInSameLocation(a.ToPoint()), m_lookups.GetExistingPointInSameLocation(b.ToPoint()));
            AddEdge(edge);

            return edge;
        }

        /// <summary>
        /// Add a new edge connecting point A to point B.
        /// </summary>
        public Edge AddEdge(Point a, Vector3 b)
        {
            Assert.AreNotEqual(a.Position, b);

            Edge edge = new Edge(a, m_lookups.GetExistingPointInSameLocation(b.ToPoint()));
            AddEdge(edge);

            return edge;
        }

        /// <summary>
        /// Add a new edge connecting point A to point B.
        /// </summary>
        public Edge AddEdge(Vector3 a, Point b)
        {
            Assert.AreNotEqual(a, b.Position);

            Edge edge = new Edge(m_lookups.GetExistingPointInSameLocation(a.ToPoint()), b);
            AddEdge(edge);

            return edge;
        }

        /// <summary>
        /// Add a new edge connecting the points at the given indices.
        /// </summary>
        public Edge AddEdge(int aIndex, int bIndex)
        {
            Debug.Assert(m_hashes.HasPoint(aIndex) && m_hashes.HasPoint(bIndex), "Both points A and B must be in the Morph.");
            
            return AddEdge(m_hashes.GetPoint(aIndex), m_hashes.GetPoint(bIndex));
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
            
            Point b = m_hashes.GetPoint(bIndex);

            foreach (Edge edge in m_lookups.GetEdgesContaining(aIndex))
                if (edge.Contains(b))
                    return edge;

            return null;
        }

        /// <summary>
        /// Get a collection of all the points to which the given point is connected.
        /// </summary>
        public IEnumerable<Point> GetConnectedPoints(Point point)
        {
            //Point opposite;
            //foreach (Edge edge in m_lookups.ConnectedPoints(point))
            //{
            //    if (edge.TryGetOpposite(point, out opposite))
            //        yield return opposite;
            //}

            foreach (int connected in m_lookups.ConnectedPoints(point.ID))
                yield return m_hashes.GetPoint(connected);
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
        /// Flip the normals of all triangles of the morph.
        /// </summary>
        public void FlipNormals()
        {
            foreach (Triangle triangle in m_hashes.Triangles)
                triangle.Flip();
        }

        /// <summary>
        /// Tries to determine whether it's possible to draw a path from point A to 
        /// point B, using a breadth-first-search.
        /// </summary>
        public bool IsConnected(Point a, Point b)
        {
            if (a == b)
                return true;

            return IsConnected(a.ID, b.ID);
        }

        /// <summary>
        /// Tries to determine whether it's possible to draw a path from point A to 
        /// point B, using a breadth-first-search.
        /// </summary>
        public bool IsConnected(int a, int b)
        {
            Debug.Assert(m_hashes.HasPoint(a) && m_hashes.HasPoint(b), "Both points A and B must be in the Morph.");

            if (a == b)
                return true;
            
            Queue<int> queue = new Queue<int>();
            HashSet<int> seen = new HashSet<int>();

            queue.Enqueue(a);

            while (queue.Count != 0)
            {
                int current = queue.Dequeue();
                
                // Get all connected points of the current point
                // if a connected point has not been seen, then mark it seen 
                // and enqueue it 
                foreach (int connected in m_lookups.ConnectedPoints(current))
                {
                    if (connected == b)
                        return true;

                    if (!seen.Contains(connected))
                    {
                        seen.Add(connected);
                        queue.Enqueue(connected);
                    }
                }
            }

            return false;
        }

#if UNITY_EDITOR
        public void DrawGizmo()
        {
            foreach (Triangle triangle in m_hashes.Triangles)
                triangle.DrawGizmo(Color.black, Color.black);

            foreach (Edge edge in m_hashes.Edges)
                if (!m_lookups.IsEdgeInTriangle(edge))
                    edge.DrawGizmo(Color.red);
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

            /// <summary>
            /// Used to uniquely identify a particular point object, even after transformations.
            /// </summary>
            public int ID { get; private set; } = -1;

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
            /// of the point.
            /// </summary>
            public void SetID(int id)
            {
                Debug.Log("Setting id to " + id);
                ID = id;
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

                    Handles.Label(Position + Vector3.up * 0.2f, ID.ToString(), handleStyle);

                    handleStyle = new GUIStyle();
                    handleStyle.normal.textColor = Color.black;
                    handleStyle.fontSize = 10;

                    Handles.Label(Position + Vector3.up * 0.3f, Position.ToString(), handleStyle);
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
            /// Returns whether the given point index is on this edge.
            /// </summary>
            public bool Contains(int id)
            {
                return A.ID == id || B.ID == id;
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

            public override string ToString()
            {
                return "[" + A.ID + " to " + B.ID + "]";
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash += A.ID * 997;
                hash += B.ID * 997;

                return hash;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Edge other))
                    return false;

                return GetHashCode() == other.GetHashCode();
            }

#if UNITY_EDITOR
            public void DrawGizmo(Color pointCol, Color edgeCol, float radius = 0.05f, bool label = true)
            {
                Gizmos.color = edgeCol;
                Gizmos.DrawLine(A.Position, B.Position);

                A.DrawGizmo(pointCol, radius);
                B.DrawGizmo(pointCol, radius);
            }

            public void DrawGizmo(Color edgeCol, float radius = 0.05f, bool label = true)
            {
                DrawGizmo(edgeCol, edgeCol, radius, label);
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

            private bool m_flippedNormal = false;

            public Vector3 Normal
            {
                get
                {
                    Vector3 side1 = B.Position - A.Position;
                    Vector3 side2 = C.Position - A.Position;

                    return Vector3.Cross(side1, side2).normalized * (m_flippedNormal ? -1 : 1);
                }
            }

            public Vector3 Centroid
            {
                get
                {
                    return (A.Position + B.Position + C.Position) * 0.3333f;
                }
            }

            public IEnumerable<Edge> Edges
            {
                get
                {
                    yield return AB;
                    yield return BC;
                    yield return CA;
                }
            }

            public Point this[int i]
            {
                get
                {
                    if (i < 0 || i >= 3)
                        throw new System.ArgumentOutOfRangeException();

                    if (i == 0)
                        return A;

                    if (B.ID > C.ID)
                    {
                        if (i == 2)
                            return B;
                        else
                            return C;
                    }
                    else
                    {
                        if (i == 2)
                            return C;
                        else
                            return B;
                    }
                }
            }
            
            public IEnumerable<Point> Points
            {
                get
                {
                    if (B.ID > C.ID)
                    {
                        yield return A;
                        yield return B;
                        yield return C;
                    }
                    else
                    {
                        yield return A;
                        yield return C;
                        yield return B;
                    }
                }
            }

            public Triangle(Edge ab, Edge bc, Edge ca)
            {
                AB = ab;
                BC = bc;
                CA = ca;

                Debug.Log("Created triangle connecting " + ab.ToString() + ", " + bc.ToString() + ", " + ca.ToString());
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
                    foreach (Edge other in triangle.Edges)
                        if (edge == other)
                            return true;

                return false;
            }

            /// <summary>
            /// Returns true if the two triangles share a normal.
            /// </summary>
            public bool IsConormal(Triangle triangle)
            {
                return Normal == triangle.Normal;
            }

            /// <summary>
            /// Reverse the normal of this triangle.
            /// </summary>
            public void Flip()
            {
                m_flippedNormal = !m_flippedNormal;
            }

#if UNITY_EDITOR
            public void DrawGizmo(Color pointCol, Color edgeCol, float radius = 0.05f)
            {
                foreach (Point point in Points)
                    point.DrawGizmo(pointCol, radius);

                foreach (Edge edge in Edges)
                    edge.DrawGizmo(edgeCol);

                Gizmos.color = edgeCol;
                Gizmos.DrawLine(Centroid, Centroid + Normal);
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

            public bool IsContiguous(Face face)
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
                    Debug.Log("Adding point " + point.ID + " at " + point.Position);

                    ++m_highestPointIndex;
                    
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

            private Dictionary<int, HashSet<int>> m_connectedPoints = new Dictionary<int, HashSet<int>>();

            private Dictionary<int, HashSet<Edge>> m_edges = new Dictionary<int, HashSet<Edge>>();

            private Dictionary<int, HashSet<Triangle>> m_triangles = new Dictionary<int, HashSet<Triangle>>();
            private Dictionary<int, HashSet<Face>> m_faces = new Dictionary<int, HashSet<Face>>();

            public void AddEdge(Edge edge)
            {
                if (!m_connectedPoints.ContainsKey(edge.A.ID))
                    m_connectedPoints.Add(edge.A.ID, new HashSet<int>());
                
                m_connectedPoints[edge.A.ID].Add(edge.B.ID);
                
                if (!m_connectedPoints.ContainsKey(edge.B.ID))
                    m_connectedPoints.Add(edge.B.ID, new HashSet<int>());
                
                m_connectedPoints[edge.B.ID].Add(edge.A.ID);
                
                if (!m_edges.ContainsKey(edge.A.ID))
                    m_edges.Add(edge.A.ID, new HashSet<Edge>());

                m_edges[edge.A.ID].Add(edge);

                if (!m_edges.ContainsKey(edge.B.ID))
                    m_edges.Add(edge.B.ID, new HashSet<Edge>());

                m_edges[edge.B.ID].Add(edge);
            }
            
            public IEnumerable<int> ConnectedPoints(int id)
            {
                if (!m_connectedPoints.ContainsKey(id))
                    yield break;

                foreach (int connected in m_connectedPoints[id])
                    yield return connected;
            }

            public HashSet<int> GetConnectedPoints(int id)
            {
                if (!m_connectedPoints.ContainsKey(id))
                    return null;

                // TODO: read only hashset?

                return m_connectedPoints[id];
            }

            public IEnumerable<Edge> GetEdgesContaining(int id)
            {
                if (!m_edges.ContainsKey(id))
                    yield break;

                foreach (Edge edge in m_edges[id])
                    yield return edge;
            }

            public IEnumerable<Edge> GetEdgesContaining(Point point)
            {
                return GetEdgesContaining(point.ID);
            }

            public Edge GetEdge(int a, int b)
            {
                foreach (Edge edge in GetEdgesContaining(a))
                    if (edge.Contains(b))
                        return edge;

                return null;
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

            public IEnumerable<Triangle> GetTrianglesContaining(Point point)
            {
                foreach (Triangle triangle in m_triangles[point.ID])
                    yield return triangle;
            }

            /// <summary>
            /// Returns whether there are any triangles in the morph with the given edge.
            /// </summary>
            public bool IsEdgeInTriangle(Edge edge)
            {
                return m_triangles.ContainsKey(edge.A.ID) || m_triangles.ContainsKey(edge.B.ID);
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
            /// TODO: PROBABLY DELETE OR HUGELY OPTIMIZE- Possibly by dirtying points when moved
            /// and then only moving the dirty ones in this method
            /// TODO: This needs to be called whenever a point position is changed
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

                // points already exist at given location. return one arbitrarily.
                if (m_pointsByLocation.ContainsKey(locationID) && !m_pointsByLocation[locationID].IsNullOrEmpty())
                    return m_pointsByLocation[locationID].First();

                // point is at a new location so it's ok to use
                return point;
            }
        }

        #endregion
    }

    public static partial class Graph
    {
        #region Helper

        /// <summary>
        /// Convert a Vector3 instance to a point.
        /// 
        /// Remember that calling this on identical vectors will yield different point
        /// instances. If you want these points to be treated as distinct by the Morph,
        /// that's fine. If you want the Morph to collapse multiple instances of the given Vector3
        /// into one point, then you should cache a reference to the instance produced by this method
        /// for re-use.
        /// </summary>
        public static Morph.Point ToPoint(this Vector3 vec)
        {
            return new Morph.Point(vec);
        }

        #endregion
    }
}