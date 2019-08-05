﻿using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Assertions;
using System.Text;

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
        private Data m_data;

        public Morph()
        {
            m_data = new Data();
        }

        public Morph(params MeshFilter[] meshes) : this()
        {
            for (int i = 0; i < meshes.Length; i++)
                AddMesh(meshes[i]);
        }

        public void AddMesh(MeshFilter meshFilter)
        {
            AddMesh(meshFilter.sharedMesh, meshFilter.transform.localToWorldMatrix);
        }

        public void AddMesh(Mesh mesh, Matrix4x4 matrix = default)
        {
            if (matrix == default)
                matrix = Matrix4x4.identity;

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

                // create the three new candidate points
                a = new Point(matrix.MultiplyPoint(vertices[indexA]), (indexA >= uvs.Length) ? default : uvs[indexA], (indexA >= normals.Length) ? default : matrix.MultiplyVector(normals[indexA]), (indexA >= tangents.Length) ? default : matrix.MultiplyVector(tangents[indexA]));
                b = new Point(matrix.MultiplyPoint(vertices[indexB]), (indexB >= uvs.Length) ? default : uvs[indexB], (indexB >= normals.Length) ? default : matrix.MultiplyVector(normals[indexB]), (indexB >= tangents.Length) ? default : matrix.MultiplyVector(tangents[indexB]));
                c = new Point(matrix.MultiplyPoint(vertices[indexC]), (indexC >= uvs.Length) ? default : uvs[indexC], (indexC >= normals.Length) ? default : matrix.MultiplyVector(normals[indexC]), (indexC >= tangents.Length) ? default : matrix.MultiplyVector(tangents[indexC]));

                m_data.AddPoint(a);
                m_data.AddPoint(b);
                m_data.AddPoint(c);

                AddTriangle(a, b, c);
            }
        }

        /// <summary>
        /// Generate a Unity Mesh object from this Morph.
        /// </summary>
        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh();

            int pointCount = m_data.PointCount;
            Point[] points = new Point[pointCount];
            Vector3[] vertices = new Vector3[pointCount];
            Vector2[] uvs = new Vector2[pointCount];
            Vector3[] normals = new Vector3[pointCount];
            Vector4[] tangents = new Vector4[pointCount];

            int i = 0;
            foreach (Point point in m_data.Points)
            {
                points[i] = point;
                vertices[i] = point.LocalPosition;
                uvs[i] = point.UV;
                normals[i] = point.Normal;
                tangents[i] = point.Tangent;

                ++i;
            }

            int[] triangles = new int[m_data.TriangleCount * 3];
            i = 0;

            foreach (Triangle triangle in m_data.Triangles)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < points.Length; k++)
                    {
                        if (points[k].Equals(triangle[j]))
                        {
                            triangles[i++] = k;
                            break;
                        }
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.triangles = triangles;

            return mesh;
        }

        /// <summary>
        /// Toggles whether this Morph will 'collapse colocated points' - meaing that Points in the same
        /// position in space will get treated as single vertices of a resulting mesh. For meshes with sharp angles such as cubes,
        /// this will likely result in weird shading because there'll only be one normal per vertex. However, the mesh will also be more compact,
        /// and this effect is not noticable for smooth meshes such as spheres.
        /// </summary>
        //public void SetCollapseColocatedPoints(bool collapseColocatedPoints)
        //{
        //    m_collapseColocatedPoints = collapseColocatedPoints;
        //}

        #region Points

        public IEnumerable<Point> Points
        {
            get
            {
                foreach (Point point in m_data.Points)
                    yield return point;
            }
        }

        public int PointCount
        {
            get
            {
                return m_data == null ? 0 : m_data.PointCount;
            }
        }

        /// <summary>
        /// Get the point with the given index.
        /// </summary>
        public Point GetPoint(int pointIndex)
        {
            return m_data.GetPoint(pointIndex);
        }

        /// <summary>
        /// Set the point with the given index to the given position, as well as all attached points.
        /// </summary>
        public void SetPoint(int pointIndex, Vector3 localPosition)
        {
            m_data.GetPoint(pointIndex).LocalPosition = localPosition;

            HashSet<int> colocated = m_data.GetColocatedPoints(pointIndex);

            if (colocated != null)
            {
                foreach (int id in colocated)
                    m_data.GetPoint(id).LocalPosition = localPosition;
            }
        }

        /// <summary>
        /// Set the given point to the given position, as well as all attached points.
        /// </summary>
        public void SetPoint(Point point, Vector3 localPosition)
        {
            SetPoint(point.ID, localPosition);
        }

        /// <summary>
        /// Get a collection of all the points to which the given point is connected.
        /// </summary>
        public IEnumerable<Point> GetConnectedPoints(Point point)
        {
            foreach (int connected in m_data.ConnectedPoints(point.ID))
                yield return m_data.GetPoint(connected);
        }

        /// <summary>
        /// Get a collection of all the points to which the given point is connected.
        /// </summary>
        public IEnumerable<Point> GetConnectedPoints(int pointIndex)
        {
            Point point = m_data.GetPoint(pointIndex);

            if (point == null)
                yield break;

            foreach (Point connected in GetConnectedPoints(point))
                yield return connected;
        }

        /// <summary>
        /// Returns true if the points with the given IDs share an edge.
        /// </summary>
        public bool IsDirectlyConnected(Point a, Point b)
        {
            return IsDirectlyConnected(a.ID, b.ID);
        }

        /// <summary>
        /// Returns true if the given points share an edge.
        /// </summary>
        public bool IsDirectlyConnected(int a, int b)
        {
            Debug.Assert(m_data.HasPoint(a) && m_data.HasPoint(b), "Both points A and B must be in the Morph.");

            if (a == b)
                return true;

            if (m_data.IsDirectlyConnected(a, b))
                return true;

            return false;
        }

        /// <summary>
        /// Tries to determine whether it's possible to draw a path from point A to 
        /// point B, using a breadth-first-search.
        /// </summary>
        public bool HasPath(Point a, Point b)
        {
            if (a == b)
                return true;

            return HasPath(a.ID, b.ID);
        }

        /// <summary>
        /// Tries to determine whether it's possible to draw a path from point A to 
        /// point B, using a breadth-first-search.
        /// </summary>
        public bool HasPath(int a, int b)
        {
            Debug.Assert(m_data.HasPoint(a) && m_data.HasPoint(b), "Both points A and B must be in the Morph.");

            if (a == b)
                return true;

            if (m_data.GetDirectlyConnectedPoints(a).Contains(b))
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
                foreach (int connected in m_data.ConnectedPoints(current))
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

        #endregion

        #region Edges

        /// <summary>
        /// Add a new edge connecting point A to point B.
        /// </summary>
        public void AddEdge(Edge edge, bool findNewTriangles = true)
        {
            if (m_data.HasEdge(edge))
                return;

            m_data.AddPoint(edge.A);
            m_data.AddPoint(edge.B);

            m_data.AddEdge(edge);

            if (findNewTriangles)
            {
                // adding an edge may create a new triangle
                // 1) get all the points connected to A
                // 2) get all the points connected to B
                // 3) compare - if any overlap points P, A-B-P forms a triangle

                HashSet<int> pointsFromB = m_data.GetDirectlyConnectedPoints(edge.B.ID);

                foreach (int pointFromA in m_data.GetDirectlyConnectedPoints(edge.A.ID))
                {
                    if (pointsFromB.Contains(pointFromA))
                    {
                        Internal_AddTriangle(new Triangle(edge, m_data.GetEdge(edge.B.ID, pointFromA), m_data.GetEdge(pointFromA, edge.A.ID)));
                    }
                }
            }
        }

        /// <summary>
        /// Add a new edge connecting point A to point B.
        /// </summary>
        public Edge AddEdge(Point a, Point b, bool findNewTriangles = true)
        {
            Assert.AreNotEqual(a.LocalPosition, b.LocalPosition);

            Edge edge = new Edge(a, b);
            AddEdge(edge, findNewTriangles);

            return edge;
        }

        /// <summary>
        /// Add a new edge connecting the points at the given indices.
        /// </summary>
        public Edge AddEdge(int aIndex, int bIndex, bool findNewTriangles = true)
        {
            Debug.Assert(m_data.HasPoint(aIndex) && m_data.HasPoint(bIndex), "Both points A and B must be in the Morph.");

            return AddEdge(m_data.GetPoint(aIndex), m_data.GetPoint(bIndex), findNewTriangles);
        }

        /// <summary>
        /// Get the edge connecting the points with the given idnex.
        /// </summary>
        public Edge GetEdge(int aIndex, int bIndex)
        {
            Debug.Assert(m_data.HasPoint(aIndex) && m_data.HasPoint(bIndex), "Both points A and B must be in the Morph.");

            Point b = m_data.GetPoint(bIndex);

            foreach (Edge edge in m_data.GetEdgesContaining(aIndex))
                if (edge.Contains(b))
                    return edge;

            return null;
        }

        #endregion

        #region Triangles

        /// <summary>
        /// Add a new triangle connecting points a, b, and c, which are assumed to have a clockwise winding order.
        /// </summary>
        public void AddTriangle(Point a, Point b, Point c)
        {
            if (a.ID == -1)
                m_data.AddPoint(a);

            if (b.ID == -1)
                m_data.AddPoint(b);

            if (c.ID == -1)
                m_data.AddPoint(c);

            Edge ab = new Edge(a, b);
            Edge bc = new Edge(b, c);
            Edge ca = new Edge(c, a);

            AddEdge(ab, findNewTriangles: false);
            AddEdge(bc, findNewTriangles: false);
            AddEdge(ca, findNewTriangles: false);

            Internal_AddTriangle(new Triangle(ab, bc, ca, a, b, c));
        }

        /// <summary>
        /// Add a new triangle connecting points a, b, and c, which are assumed to have a clockwise winding order.
        /// </summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Point pointA = m_data.GetExistingPointInSameLocation(a);
            Point pointB = m_data.GetExistingPointInSameLocation(b);
            Point pointC = m_data.GetExistingPointInSameLocation(c);

            AddTriangle(pointA, pointB, pointC);
        }

        /// <summary>
        /// Add a new triangle connecting points a, b, and c (given by point ID), which are assumed to have a clockwise winding order.
        /// </summary>
        public void AddTriangle(int a, int b, int c)
        {
            Point pointA = m_data.GetPoint(a);
            Point pointB = m_data.GetPoint(b);
            Point pointC = m_data.GetPoint(c);

            Debug.Assert(pointA != null && pointB != null && pointC != null, "All three points used to make a triangle must be in the Morph.");

            AddTriangle(pointA, pointB, pointC);
        }

        /// <summary>
        /// This method implicitly accepts the given triangle,
        /// and determines if the new triangle belongs to any faces.
        /// </summary>
        private void Internal_AddTriangle(Triangle triangle)
        {
            m_data.AddTriangle(triangle);

            bool addedToExistingFace = false;
            foreach (Face face in m_data.Faces)
            {
                if (face.TryAddTriangle(triangle))
                {
                    addedToExistingFace = true;

                    // TODO - what if this triangle bridges 2 faces?
                    break;
                }
            }

            if (!addedToExistingFace)
            {
                m_data.AddFace(new Face(triangle, m_data));
            }
        }

        /// <summary>
        /// Flip the normals of all triangles of the morph.
        /// </summary>
        public void FlipNormals()
        {
            foreach (Triangle triangle in m_data.Triangles)
                triangle.Flip();
        }

        #endregion

        #region Faces

        public IEnumerable<Face> Faces
        {
            get
            {
                foreach (Face face in m_data.Faces)
                    yield return face;
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        public void DrawGizmo(Transform transform = null)
        {
            //foreach (Point point in m_hashes.Points)
            //    point.DrawNormalGizmo(Color.red, transform);

            foreach (Point point in m_data.Points)
                point.DrawPointLabels(Color.black, transform);

            //DrawFaces(transform);

            //foreach (Triangle triangle in m_hashes.Triangles)
            //    triangle.DrawGizmo(Color.black, Color.black, Color.blue, transform: transform);

            //// orphans :(
            //foreach (Edge edge in m_hashes.Edges)
            //    if (!m_lookups.IsEdgeInTriangle(edge))
            //        edge.DrawGizmo(Color.red, transform: transform);
        }

        public void DrawFaces(Transform transform = null)
        {
            foreach (Face face in m_data.Faces)
            {
                int hash = face.GetHashCode();

                Color col = new Color((Mathf.Abs(hash) % 255f) / 255f, (Mathf.Abs(hash * 3f) % 255f) / 255f, (Mathf.Abs(hash * 5f) % 255f) / 255f, 0.3f);

                face.DrawGizmo(col, transform);
            }
        }
#endif

        #endregion

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
            public Vector3 LocalPosition { get; set; }
            public Vector2 UV { get; private set; }
            public Vector3 Normal { get; private set; }
            public Vector4 Tangent { get; private set; }

            /// <summary>
            /// Used to uniquely identify a particular point object, even after transformations.
            /// </summary>
            public int ID { get; private set; } = -1;

            public bool HasDataOtherThanPosition
            {
                get
                {
                    return UV != Vector2.zero || Normal != Vector3.zero || Tangent != Vector4.zero;
                }
            }

            public Point(Vector3 vector, Vector2 uv = default, Vector3 normal = default, Vector4 tangent = default)
            {
                LocalPosition = vector;
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
                ID = id;
            }

            /// <summary>
            /// Take the normal, uv, and tangent data from the other point.
            /// </summary>
            public void CopyNonPositionData(Point other)
            {
                UV = other.UV;
                Normal = other.Normal;
                Tangent = other.Tangent;
            }

            /// <summary>
            /// Returns an integer which is unique to the point's current location. This ID
            /// is not liable to floating point errors which makes it a very useful way to compare
            /// vectors.
            /// </summary>
            public int GetLocationID()
            {
                return LocalPosition.HashVector3();
            }

            /// <summary>
            /// Transforms this point as if it's on the 2d plane and its z component doesn't matter,
            /// accorsding to the given normal.
            /// </summary>
            public Vector2 To2D(Vector3 normal)
            {
                return RotateBy(Quaternion.FromToRotation(normal, -Vector3.forward));
            }

            public Vector3 GetWorldPosition(Matrix4x4 localToWorldMatrix)
            {
                return localToWorldMatrix * LocalPosition;
            }

            /// <summary>
            /// Transforms this point by the given rotation.
            /// </summary>
            public Vector3 RotateBy(Quaternion rotation)
            {
                return rotation * LocalPosition;
            }

            public override string ToString()
            {
                return "[" + ID + "] " + LocalPosition.ToString();
            }

            public static bool operator ==(Point point1, Point point2)
            {
                if (point1 is null)
                    return point2 is null;
                else if (point2 is null)
                    return false;

                return point1.GetLocationID() == point2.GetLocationID();
            }

            public static bool operator !=(Point point1, Point point2)
            {
                return !(point1 == point2);
            }

            public override bool Equals(object obj)
            {
                Point other = obj as Point;

                if (other == null)
                    return false;

                return ID == other.ID;
            }

            public override int GetHashCode()
            {
                return ID;
            }

#if UNITY_EDITOR

            public void DrawNormalGizmo(Color col, Transform transform = null)
            {
                Matrix4x4 originalGizmoMatrix = Gizmos.matrix;
                Gizmos.matrix = transform == null ? originalGizmoMatrix : transform.localToWorldMatrix;

                Gizmos.color = col;
                Gizmos.DrawLine(LocalPosition, LocalPosition + Normal * 0.6f);

                Gizmos.matrix = originalGizmoMatrix;
            }

            public void DrawPointLabels(Color col, Transform transform = null)
            {
                Matrix4x4 originalHandleMatrix = Handles.matrix;
                Handles.matrix = transform == null ? originalHandleMatrix : transform.localToWorldMatrix;

                GUIStyle handleStyle = new GUIStyle();
                handleStyle.normal.textColor = Color.black;
                handleStyle.fontSize = 10;

                Handles.Label(LocalPosition + Normal * 0.15f + new Vector3(0f, ID * 0.01f, 0f), ID.ToString(), handleStyle);

                Handles.matrix = originalHandleMatrix;
            }

            public void DrawPointGizmo(Color col, float radius = 0.025f, Transform transform = null)
            {
                Matrix4x4 originalGizmoMatrix = Gizmos.matrix;
                Gizmos.matrix = transform == null ? originalGizmoMatrix : transform.localToWorldMatrix;

                Matrix4x4 originalHandleMatrix = Handles.matrix;
                Handles.matrix = transform == null ? originalHandleMatrix : transform.localToWorldMatrix;

                Gizmos.DrawSphere(LocalPosition, radius);

                Gizmos.matrix = originalGizmoMatrix;
                Handles.matrix = originalHandleMatrix;
#endif
            }
        }


        /// <summary>
        /// Represents the connection of 2 points to form an edge of the mesh.
        /// </summary>
        [System.Serializable]
        public class Edge
        {
            private const int TWO_TO_FIFTEENTH_POWER = 32768;

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
                return "[" + A.ID + ", " + B.ID + "]";
            }

            public override int GetHashCode()
            {
                if (A.ID > B.ID)
                {
                    return TWO_TO_FIFTEENTH_POWER * A.ID + B.ID;
                }
                else
                {
                    return TWO_TO_FIFTEENTH_POWER * B.ID + A.ID;
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Edge other))
                    return false;

                return GetHashCode() == other.GetHashCode();
            }

            public static bool operator ==(Edge edge1, Edge edge2)
            {
                if (edge1 is null)
                    return edge2 is null;
                else if (edge2 is null)
                    return false;

                int _1a = edge1.A.GetLocationID();
                int _1b = edge1.B.GetLocationID();

                int _2a = edge2.A.GetLocationID();
                int _2b = edge2.B.GetLocationID();

                return (_1a == _2a && _1b == _2b) || (_1a == _2b && _1b == _2a);
            }

            public static bool operator !=(Edge edge1, Edge edge2)
            {
                return !(edge1 == edge2);
            }

#if UNITY_EDITOR
            public void DrawGizmo(Color pointCol, Color edgeCol, float radius = 0.05f, bool label = true, Transform transform = null)
            {
                Matrix4x4 originalGizmoMatrix = Gizmos.matrix;
                Gizmos.matrix = transform == null ? originalGizmoMatrix : transform.localToWorldMatrix;

                Matrix4x4 originalHandleMatrix = Handles.matrix;
                Handles.matrix = transform == null ? originalHandleMatrix : transform.localToWorldMatrix;

                Gizmos.color = edgeCol;
                Gizmos.DrawLine(A.LocalPosition, B.LocalPosition);

                A.DrawPointGizmo(pointCol, radius, transform);
                B.DrawPointGizmo(pointCol, radius, transform);

                Gizmos.matrix = originalGizmoMatrix;
                Handles.matrix = originalHandleMatrix;
            }

            public void DrawGizmo(Color edgeCol, float radius = 0.05f, bool label = true, Transform transform = null)
            {
                DrawGizmo(edgeCol, edgeCol, radius, label, transform);
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

            public Point A { get; private set; }
            public Point B { get; private set; }
            public Point C { get; private set; }

            private bool m_flippedNormal = false;

            public int ID { get; private set; } = -1;

            public Vector3 Normal
            {
                get
                {
                    Vector3 side1 = B.LocalPosition - A.LocalPosition;
                    Vector3 side2 = C.LocalPosition - A.LocalPosition;

                    return Vector3.Cross(side1, side2).normalized * (m_flippedNormal ? -1 : 1);
                }
            }

            public Vector3 Centroid
            {
                get
                {
                    return (A.LocalPosition + B.LocalPosition + C.LocalPosition) * 0.3333f;
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
                    if (i < 0 || i > 2)
                        throw new System.ArgumentOutOfRangeException();

                    if (i == 0)
                        return A;
                    else if (i == 1)
                        return m_flippedNormal ? C : B;

                    return m_flippedNormal ? B : C;
                }
            }

            public IEnumerable<Point> Points
            {
                get
                {
                    if (m_flippedNormal)
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

                A = AB.A;
                B = AB.B;

                if (BC.A == A || BC.A == B)
                    C = BC.B;
                else
                    C = BC.A;

                Debug.Assert((A.LocalPosition != B.LocalPosition) && (B.LocalPosition != C.LocalPosition) && (C.LocalPosition != A.LocalPosition), "Can't have a triangle where two or more points are equal!");
            }

            public Triangle(Edge ab, Edge bc, Edge ca, Point a, Point b, Point c)
            {
                AB = ab;
                BC = bc;
                CA = ca;

                A = a;
                B = b;
                C = c;

                Debug.Assert((A.LocalPosition != B.LocalPosition) && (B.LocalPosition != C.LocalPosition) && (C.LocalPosition != A.LocalPosition), "Can't have a triangle where two or more points are equal!");
            }

            /// <summary>
            /// Set this triangle's unique ID.
            /// </summary>
            public void SetID(int id)
            {
                ID = id;
            }

            /// <summary>
            /// Generate an array of vectors representing this triangle's vertices.
            /// </summary>
            public Vector3[] ToVectorArray()
            {
                return new Vector3[] { this[0].LocalPosition, this[1].LocalPosition, this[2].LocalPosition };
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

                foreach (Point point in Points)
                    foreach (Point other in triangle.Points)
                        if (point == other)
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

            public override string ToString()
            {
                return "[" + A.ID + ", " + B.ID + ", " + C.ID + "]";
            }

#if UNITY_EDITOR
            public void DrawGizmo(Color pointCol, Color edgeCol, Color normalCol, bool label = true, float normalScale = 0.1f, float radius = 0.025f, Transform transform = null)
            {
                foreach (Edge edge in Edges)
                    edge.DrawGizmo(edgeCol, radius: radius, label: false, transform: transform);

                Matrix4x4 originalGizmoMatrix = Gizmos.matrix;
                Gizmos.matrix = transform == null ? originalGizmoMatrix : transform.localToWorldMatrix;

                Matrix4x4 originalHandleMatrix = Handles.matrix;
                Handles.matrix = transform == null ? originalHandleMatrix : transform.localToWorldMatrix;

                Gizmos.color = normalCol;
                Gizmos.DrawLine(Centroid, Centroid + Normal * normalScale);

                GUIStyle handleStyle = new GUIStyle();
                handleStyle.normal.textColor = Color.black;
                handleStyle.fontSize = 20;

                Handles.Label(Centroid + Normal * 0.15f, ID.ToString(), handleStyle);

                Gizmos.matrix = originalGizmoMatrix;
                Handles.matrix = originalHandleMatrix;
            }
#endif
        }

        /// <summary>
        /// A face represents a number of triangles which are all contiguous 
        /// and conormal.
        /// 
        /// TODO: Any point, edge, or triangle of this face moving means this face has to be updated.
        /// </summary>
        [System.Serializable]
        public class Face
        {
            private HashSet<Triangle> m_triangles = new HashSet<Triangle>();

            private Data m_data;

            private Triangle m_initialTriangle;

            public IEnumerable<Triangle> Triangles
            {
                get
                {
                    foreach (Triangle triangle in m_triangles)
                        yield return triangle;
                }
            }

            public Vector3 Normal
            {
                get
                {
                    return m_initialTriangle.Normal;
                }
            }

            public Face(Triangle triangle, Data lookup)
            {
                m_data = lookup;
                m_initialTriangle = triangle;
                m_triangles.Add(m_initialTriangle);
            }

            /// <summary>
            /// If this triangle does not already belong to this face, and belongs as part of it (shares a normal with all triangles in the mesh
            /// and an edge with at least one), then add it and return true. Else return false.
            /// </summary>
            public bool TryAddTriangle(Triangle newTriangle)
            {
                if (!m_triangles.Contains(newTriangle))
                {
                    foreach (Triangle triangle in m_triangles)
                    {
                        if (triangle.IsSameFace(newTriangle))
                        {
                            m_triangles.Add(newTriangle);
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// If this triangle is not already part of this face, and it shares an edge with at least
            /// one triangle of this face, return true. Else return false.
            /// </summary>
            public bool IsContiguous(Triangle newTriangle)
            {
                if (m_triangles.Contains(newTriangle))
                    return false;

                foreach (Triangle triangle in m_triangles)
                    if (triangle.IsContiguous(newTriangle))
                        return true;

                return false;
            }

            /// <summary>
            /// If this triangle is not already part of this face, and it shares an edge with at least
            /// one triangle of this face, return true. Else return false.
            /// </summary>
            public bool IsConormal(Triangle newTriangle)
            {
                if (m_triangles.Contains(newTriangle))
                    return false;

                foreach (Triangle triangle in m_triangles)
                    if (!triangle.IsConormal(newTriangle))
                        return false;

                return true;
            }

            /// <summary>
            /// If this face has any triangles which share an edge with any triangles in the given face,
            /// they are contiguous.
            /// </summary>
            public bool IsContiguous(Face face)
            {
                foreach (Triangle triangle in m_triangles)
                    if (face.IsContiguous(triangle))
                        return true;

                return false;
            }

            /// <summary>
            /// If the normal of this face is the same as the normal of the given face, they are conormal.
            /// </summary>
            public bool IsConormal(Face face)
            {
                return Normal == face.Normal;
            }

            /// <summary>
            /// Return a list of all the points in this face.
            /// </summary>
            public List<Point> GetPoints(Matrix4x4 matrix = default)
            {
                HashSet<Point> points = new HashSet<Point>();

                foreach (Triangle triangle in Triangles)
                    foreach (Point point in triangle.Points)
                        points.Add(point);

                return points.ToList();
            }

            /// <summary>
            /// Generate a list of Points which defines the perimeter of this face in clockwise winding order.
            /// 
            /// This may be problematic on a Morph which is not collapse colocated points.
            /// </summary>
            public List<Point> GetPerimeter()
            {
                List<Point> points = GetPoints();

                List<Point> perimeter = new List<Point>();

                // to make maths easier, this quaternion turns our maths 2D
                Quaternion normalizingRotation = Quaternion.FromToRotation(Normal, -Vector3.forward);

                // STEP 1: Find the leftmost (and if necessary, bottommost) point and start there

                Point leftMost = null;
                float leastX = Mathf.Infinity;
                float leastY = Mathf.Infinity;

                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 _2dPos = points[i].RotateBy(normalizingRotation);
                    if (_2dPos.x == leastX)
                    {
                        if (_2dPos.y < leastY)
                        {
                            leftMost = points[i];
                            leastY = _2dPos.y;
                        }
                    }
                    else if (_2dPos.x < leastX)
                    {
                        leftMost = points[i];
                        leastX = _2dPos.x;
                        leastY = _2dPos.y;
                    }
                }

                if (leftMost == null)
                {
                    Debug.LogError("Couldn't find a start point for perimeter calculation! Does your face have any points?");
                    return null;
                }

                perimeter.Add(leftMost);

                //Debug.Log("LeftMost point is " + leftMost.ID);

                // STEP 2: Now that we have an algorithm, the tracing part can begin. Essentially, take the last point,
                // 'swoop' around anticlockwise from the bottom until we hit the next point, and then use that. keep going until
                // our current point matches our starting point

                Vector2 referenceDirection = Vector2.down;
                Point start = leftMost;
                Point current = start;
                int previousLocation = -1;

                int tempSafety = 0;

                while (++tempSafety < 1000)
                {
                    float bestAngle = -Mathf.Infinity;
                    float bestDistance = Mathf.Infinity;
                    Point bestPoint = null;

                    for (int i = 0; i < points.Count; i++)
                    {
                        // don't ever look back!
                        if (points[i] == current || !m_data.IsDirectlyConnected(current.ID, points[i].ID))
                            continue;

                        if (previousLocation != -1 && points[i].GetLocationID() == previousLocation)
                            continue;

                        float angle = Vector2.SignedAngle(referenceDirection, (points[i].RotateBy(normalizingRotation) - current.RotateBy(normalizingRotation)).normalized);

                        //Debug.Log($"Considering {points[i].ID} for best point after {current.ID} with an angle of {angle}.");

                        if (angle > bestAngle)
                        {
                            bestAngle = angle;
                            bestPoint = points[i];
                            bestDistance = (points[i].LocalPosition - current.LocalPosition).sqrMagnitude;
                        }
                        else if (angle == bestAngle)
                        {
                            float sqrDist = (points[i].LocalPosition - current.LocalPosition).sqrMagnitude;

                            if (sqrDist < bestDistance)
                            {
                                bestPoint = points[i];
                                bestDistance = sqrDist;
                            }
                        }
                    }

                    referenceDirection = (bestPoint.RotateBy(normalizingRotation) - current.RotateBy(normalizingRotation)).normalized;

                    previousLocation = current.GetLocationID();
                    current = bestPoint;

                    if (current == start)
                        break;

                    perimeter.Add(current);
                }

                perimeter.Add(start);

                return perimeter;
            }


            public bool Retriangulate(List<Triangle> result)
            {
                List<Point> perimeter = GetPerimeter();
                int triangleCount = m_triangles.Count;
                int n = perimeter.Count;


                throw new System.NotImplementedException();
            }

            public float CalculateArea(List<Point> perimeter = null)
            {
                return CalculateArea(perimeter ?? GetPerimeter(), Normal);
            }

            public static float CalculateArea(List<Point> perimeter, Vector3 normal)
            {
                int n = perimeter.Count;
                float area = 0f;

                for (int p = n - 1, q = 0; q < n; p = q++)
                {
                    Vector2 pval = perimeter[p].To2D(normal);
                    Vector2 qval = perimeter[q].To2D(normal);
                    area += pval.x * qval.y - qval.x * pval.y;
                }

                return area * 0.5f;
            }

            public override string ToString()
            {
                StringBuilder sb = new System.Text.StringBuilder();

                sb.Append("[");

                foreach (Triangle triangle in m_triangles)
                    sb.Append(triangle.ToString() + ", ");

                sb.Remove(sb.Length - 2, 2);

                sb.Append("]");

                return sb.ToString();
            }


#if UNITY_EDITOR
            public void DrawGizmo(Color col, Transform transform = null)
            {
                Matrix4x4 originalHandleMatrix = Handles.matrix;
                Handles.matrix = transform == null ? originalHandleMatrix : transform.localToWorldMatrix;

                Handles.color = col;

                //// TODO: transform this by the matrix above
                //Vector3 normal = Normal;

                foreach (Triangle triangle in Triangles)
                {
                    Handles.DrawAAConvexPolygon(triangle.ToVectorArray());
                }

                Handles.matrix = originalHandleMatrix;
#endif
            }
        }
            #endregion

            #region Data Class

            /// <summary>
            /// This class allows quickly checking which objects (edges, triangles, faces) contain certain points.
            /// </summary>
            [System.Serializable]
            public class Data
            {
                private Dictionary<int, Point> m_pointsByID = new Dictionary<int, Point>();
                private Dictionary<int, HashSet<Edge>> m_edgesByID = new Dictionary<int, HashSet<Edge>>();
                private Dictionary<int, HashSet<Triangle>> m_trianglesByID = new Dictionary<int, HashSet<Triangle>>();

                private HashSet<Edge> m_edges = new HashSet<Edge>();
                private HashSet<Triangle> m_triangles = new HashSet<Triangle>();
                private HashSet<Face> m_faces = new HashSet<Face>();

                private Dictionary<int, HashSet<Point>> m_pointsByLocation = new Dictionary<int, HashSet<Point>>();

                private Dictionary<int, HashSet<int>> m_connectedPoints = new Dictionary<int, HashSet<int>>();
                private Dictionary<int, HashSet<int>> m_samePoints = new Dictionary<int, HashSet<int>>();

                private int m_highestPointIndex = 0;
                private int m_highestTriangleIndex = 0;

                public void AddEdge(Edge edge)
                {
                    // Prevent adding duplicate edges
                    // not necessarym but might be useful if we want to start using collections which don't have restrictions
                    // on duplicates
                    if (m_edgesByID.ContainsKey(edge.A.ID) && m_edgesByID[edge.A.ID].Contains(edge))
                        return;

                    m_edges.Add(edge);

                    AddConnection(edge.A, edge.B);

                    if (!m_edgesByID.ContainsKey(edge.A.ID))
                        m_edgesByID.Add(edge.A.ID, new HashSet<Edge>());

                    m_edgesByID[edge.A.ID].Add(edge);

                    if (!m_edgesByID.ContainsKey(edge.B.ID))
                        m_edgesByID.Add(edge.B.ID, new HashSet<Edge>());

                    m_edgesByID[edge.B.ID].Add(edge);
                }

                public void AddConnection(Point a, Point b, bool isSamePoint = false)
                {
                    AddConnection(a.ID, b.ID, isSamePoint || a == b);
                }

                public void AddConnection(int a, int b, bool isSamePoint = false)
                {
                    bool debug = false;

                    if (debug)
                        Debug.Log("Intentionally connecting " + a + " to " + b);

                    if (!m_connectedPoints.ContainsKey(a))
                        m_connectedPoints.Add(a, new HashSet<int>());

                    m_connectedPoints[a].Add(b);

                    if (!m_connectedPoints.ContainsKey(b))
                        m_connectedPoints.Add(b, new HashSet<int>());

                    m_connectedPoints[b].Add(a);

                    if (isSamePoint)
                    {
                        if (!m_samePoints.ContainsKey(a))
                            m_samePoints.Add(a, new HashSet<int>());

                        m_samePoints[a].Add(b);

                        if (!m_samePoints.ContainsKey(b))
                            m_samePoints.Add(b, new HashSet<int>());

                        m_samePoints[b].Add(a);

                        if (debug)
                            Debug.Log($"{a} and {b} are the same point");

                        foreach (int sameAsA in m_samePoints[a])
                        {
                            if (debug)
                                Debug.Log($"Checking {sameAsA} for new connections");

                            m_samePoints[sameAsA].Add(b);
                            m_samePoints[b].Add(sameAsA);

                            foreach (int connectedToSameAsA in m_connectedPoints[sameAsA])
                            {
                                if (a == connectedToSameAsA)
                                    continue;

                                if (!m_connectedPoints.ContainsKey(connectedToSameAsA))
                                    m_connectedPoints.Add(connectedToSameAsA, new HashSet<int>());

                                m_connectedPoints[connectedToSameAsA].Add(a);
                                m_connectedPoints[a].Add(connectedToSameAsA);

                                if (debug)
                                    Debug.Log($"Virtually connecting {a} and {connectedToSameAsA}");
                            }
                        }

                        foreach (int sameAsB in m_samePoints[b])
                        {
                            if (debug)
                                Debug.Log($"Checking {sameAsB} for new connections");

                            m_samePoints[sameAsB].Add(a);
                            m_samePoints[a].Add(sameAsB);

                            foreach (int connectedToSameAsB in m_connectedPoints[sameAsB])
                            {
                                if (b == connectedToSameAsB)
                                    continue;

                                if (!m_connectedPoints.ContainsKey(connectedToSameAsB))
                                    m_connectedPoints.Add(connectedToSameAsB, new HashSet<int>());

                                m_connectedPoints[connectedToSameAsB].Add(b);
                                m_connectedPoints[b].Add(connectedToSameAsB);

                                if (debug)
                                    Debug.Log($"Virtually connecting {b} and {connectedToSameAsB}");
                            }
                        }
                    }
                    else
                    {
                        if (debug)
                            Debug.Log($"{a} and {b} are NOT the same point");

                        if (m_samePoints.ContainsKey(a))
                        {
                            if (debug)
                                Debug.Log($"{a} has some same points...");

                            foreach (int sameAsA in m_samePoints[a])
                            {
                                if (!m_connectedPoints.ContainsKey(sameAsA))
                                    m_connectedPoints.Add(sameAsA, new HashSet<int>());

                                m_connectedPoints[sameAsA].Add(b);
                                m_connectedPoints[b].Add(sameAsA);

                                if (debug)
                                    Debug.Log($"Virtually connecting {b} and {sameAsA}");
                            }
                        }

                        if (m_samePoints.ContainsKey(b))
                        {
                            if (debug)
                                Debug.Log($"{b} has some same points...");

                            foreach (int sameAsB in m_samePoints[b])
                            {
                                if (!m_connectedPoints.ContainsKey(sameAsB))
                                    m_connectedPoints.Add(sameAsB, new HashSet<int>());

                                m_connectedPoints[sameAsB].Add(a);
                                m_connectedPoints[a].Add(sameAsB);

                                if (debug)
                                    Debug.Log($"Virtually connecting {a} and {sameAsB}");
                            }
                        }
                    }
                }

                public IEnumerable<int> ConnectedPoints(int id)
                {
                    if (!m_connectedPoints.ContainsKey(id))
                        yield break;

                    foreach (int connected in m_connectedPoints[id])
                        yield return connected;
                }

                /// <summary>
                /// Returns whether the two points with the given IDs exist and either both map to the same point,
                /// or are connected by one edge.
                /// </summary>
                public bool IsDirectlyConnected(int a, int b)
                {
                    if ((m_connectedPoints.ContainsKey(a) && m_connectedPoints[a].Contains(b)) || (m_connectedPoints.ContainsKey(b) && m_connectedPoints[b].Contains(a)))
                        return true;

                    //if (m_samePoints.ContainsKey(a))
                    //{
                    //    foreach (int samePointAsA in m_samePoints[a])
                    //        if ((m_connectedPoints.ContainsKey(samePointAsA) && m_connectedPoints[samePointAsA].Contains(b)) || (m_connectedPoints.ContainsKey(b) && m_connectedPoints[b].Contains(samePointAsA)))
                    //            return true;
                    //}

                    //if (m_samePoints.ContainsKey(b))
                    //{
                    //    foreach (int samePointAsB in m_samePoints[b])
                    //        if ((m_connectedPoints.ContainsKey(samePointAsB) && m_connectedPoints[samePointAsB].Contains(a)) || (m_connectedPoints.ContainsKey(a) && m_connectedPoints[a].Contains(samePointAsB)))
                    //            return true;
                    //}

                    return false;
                }

                public HashSet<int> GetDirectlyConnectedPoints(int id)
                {
                    if (!m_connectedPoints.ContainsKey(id))
                        return null;

                    return m_connectedPoints[id];
                }

                public HashSet<int> GetColocatedPoints(int id)
                {
                    if (!m_samePoints.ContainsKey(id))
                        return null;

                    return m_samePoints[id];
                }

                public int PointCount
                {
                    get
                    {
                        return m_pointsByID.Count;
                    }
                }

                public IEnumerable<Point> Points
                {
                    get
                    {
                        foreach (KeyValuePair<int, Point> kvp in m_pointsByID)
                            yield return kvp.Value;
                    }
                }

                public int EdgeCount
                {
                    get
                    {
                        return m_edges.Count;
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

                public int TriangleCount
                {
                    get
                    {
                        return m_triangles.Count;
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

                public int FaceCount
                {
                    get
                    {
                        return m_faces.Count;
                    }
                }

                public IEnumerable<Face> Faces
                {
                    get
                    {
                        foreach (Face face in m_faces)
                            yield return face;
                    }
                }
                public IEnumerable<Edge> GetEdgesContaining(int id)
                {
                    if (!m_edgesByID.ContainsKey(id))
                        yield break;

                    foreach (Edge edge in m_edgesByID[id])
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
                    if (triangle.ID == -1)
                        triangle.SetID(m_highestTriangleIndex++);

                    m_triangles.Add(triangle);

                    foreach (Point point in triangle.Points)
                    {
                        if (!m_trianglesByID.ContainsKey(point.ID))
                            m_trianglesByID.Add(point.ID, new HashSet<Triangle>());

                        m_trianglesByID[point.ID].Add(triangle);
                    }

                    foreach (Edge edge in triangle.Edges)
                    {
                        AddEdge(edge);
                    }
                }

                public IEnumerable<Triangle> GetTrianglesContaining(Point point)
                {
                    foreach (Triangle triangle in m_trianglesByID[point.ID])
                        yield return triangle;
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

                /// <summary>
                /// Returns whether there are any triangles in the morph with the given edge.
                /// </summary>
                public bool IsEdgeInTriangle(Edge edge)
                {
                    return m_trianglesByID.ContainsKey(edge.A.ID) || m_trianglesByID.ContainsKey(edge.B.ID);
                }

                public void AddPoint(Point point)
                {
                    if (point.ID == -1)
                        point.SetID(m_highestPointIndex);

                    if (!m_pointsByID.ContainsKey(point.ID))
                    {
                        ++m_highestPointIndex;

                        m_pointsByID.Add(point.ID, point);
                    }

                    int locationID = point.GetLocationID();

                    if (!m_pointsByLocation.ContainsKey(locationID))
                        m_pointsByLocation.Add(locationID, new HashSet<Point>());

                    m_pointsByLocation[locationID].Add(point);

                    // make a note of any points which share a vertex (for pathing)
                    ConnectPointsInSameLocation(point);
                }


                public void RemovePoint(Point point)
                {
                    m_pointsByID.Remove(point.ID);
                }

                public bool HasPoint(Point point)
                {
                    return HasPoint(point.ID);
                }

                public bool HasPoint(int id)
                {
                    return m_pointsByID.ContainsKey(id);
                }

                public Point GetPoint(int id)
                {
                    if (!m_pointsByID.ContainsKey(id))
                        return null;

                    return m_pointsByID[id];
                }


                /// <summary>
                /// This method needs to be called whenever a point moves in the mesh.
                /// 
                /// TODO: PROBABLY DELETE OR HUGELY OPTIMIZE- Possibly by dirtying points when moved
                /// and then only moving the dirty ones in this method
                /// TODO: This needs to be called whenever a point position is changed
                /// </summary>
                //public void RegeneratePointByLocationLookup()
                //{
                //    List<Point> points = new List<Point>(m_pointsByLocation.Count);

                //    foreach (KeyValuePair<int, HashSet<Point>> kvp in m_pointsByLocation)
                //        foreach (Point point in kvp.Value)
                //            points.Add(point);

                //    m_pointsByLocation.Clear();

                //    foreach (Point point in points)
                //        AddPoint(point);
                //}

                /// <summary>
                /// If we already have a point object in the exact same location as the given point object,
                /// return it. Else return the given point back.
                /// 
                /// If the given point has data such as UV, Tangent, or Normal, you can optionally make the returned point
                /// (if it already existed after all) copy this data onto itself. This defaults to true.
                /// </summary>
                public void ConnectPointsInSameLocation(Point point)
                {
                    int locationID = point.GetLocationID();

                    if (!m_pointsByLocation.TryGetValue(locationID, out HashSet<Point> pointsAtSameLocation) || pointsAtSameLocation.IsNullOrEmpty())
                        return;

                    foreach (Point existing in pointsAtSameLocation)
                    {
                        if (existing.ID == point.ID || m_connectedPoints[existing.ID].Contains(point.ID))
                            continue;

                        AddConnection(existing, point, isSamePoint: true);
                    }
                }

                /// <summary>
                /// If we already have a point object in the exact same location as the given vector,
                /// return it. Else return the given point back.
                /// </summary>
                public Point GetExistingPointInSameLocation(Vector3 vec)
                {
                    int locationID = vec.HashVector3();

                    // points already exist at given location. return one arbitrarily.
                    if (m_pointsByLocation.ContainsKey(locationID) && !m_pointsByLocation[locationID].IsNullOrEmpty())
                        return m_pointsByLocation[locationID].First();

                    // point is at a new location so it's ok to use
                    return vec.ToPoint();
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