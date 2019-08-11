using System.Collections.Generic;
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

        public void Test()
        {
            Debug.Log("Point Count = " + m_data.PointCount);
            Debug.Log("Edge Count = " + m_data.EdgeCount);
            Debug.Log("Triangle Count = " + m_data.TriangleCount);
            Debug.Log("Face Count = " + m_data.FaceCount);

            Debug.Log("Drawing Triangles...");

            foreach (Triangle triangle in m_data.Triangles)
            {
                Debug.Log("Triangle " + triangle.ID);
                foreach (Edge edge in triangle.Edges)
                {
                    Debug.Log("---> Edge " + edge.ID);
                }
            }
        }


        [SerializeField]
        private Data m_data;

        public Morph()
        {
            m_data = new Data();
        }

        public Morph(params MeshFilter[] meshes) : this()
        {
            for (int i = 0; i < meshes.Length; i++)
                if (meshes[i] != null)
                    AddMesh(meshes[i]);
        }

        public Morph(Matrix4x4 matrix = default, params Mesh[] meshes) : this()
        {
            for (int i = 0; i < meshes.Length; i++)
                if (meshes[i] != null)
                    AddMesh(meshes[i], matrix);
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
                a = new Point(m_data, matrix.MultiplyPoint(vertices[indexA]), (indexA >= uvs.Length) ? default : uvs[indexA], (indexA >= normals.Length) ? default : matrix.MultiplyVector(normals[indexA]), (indexA >= tangents.Length) ? default : matrix.MultiplyVector(tangents[indexA]));
                b = new Point(m_data, matrix.MultiplyPoint(vertices[indexB]), (indexB >= uvs.Length) ? default : uvs[indexB], (indexB >= normals.Length) ? default : matrix.MultiplyVector(normals[indexB]), (indexB >= tangents.Length) ? default : matrix.MultiplyVector(tangents[indexB]));
                c = new Point(m_data, matrix.MultiplyPoint(vertices[indexC]), (indexC >= uvs.Length) ? default : uvs[indexC], (indexC >= normals.Length) ? default : matrix.MultiplyVector(normals[indexC]), (indexC >= tangents.Length) ? default : matrix.MultiplyVector(tangents[indexC]));

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
        /// Removes the point with the given ID. Can indirectly destroy triangles, edges, faces, etc.
        /// </summary>
        public void RemovePoint(int pointId)
        {
            m_data.RemovePoint(pointId);
        }

        /// <summary>
        /// Returns true if a point with the given index exists.
        /// </summary>
        public bool HasPoint(int pointIndex)
        {
            return m_data.HasPoint(pointIndex);
        }

        /// <summary>
        /// Set the point with the given index to the given position, as well as all attached points.
        /// </summary>
        public void SetPoint(int pointIndex, Vector3 localPosition)
        {
            Point point = m_data.GetPoint(pointIndex);

            if (point == null)
                return;

            SetPoint(point, localPosition);
        }

        /// <summary>
        /// Set the given point to the given position, as well as all attached points.
        /// </summary>
        public void SetPoint(Point point, Vector3 localPosition)
        {
            int oldHash = point.GetLocationID();

            point.LocalPosition = localPosition;
            
            HashSet<int> colocated = m_data.GetColocatedPoints(point.ID);

            if (colocated != null)
            {
                foreach (int id in colocated)
                {
                    point = m_data.GetPoint(id);
                    point.LocalPosition = localPosition;
                }
            }
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
        /// Return the edge with the given ID, if it exists.
        /// </summary>
        public Edge GetEdge(int edgeId)
        {
            return m_data.GetEdge(edgeId);
        }

        /// <summary>
        /// Remove the edge with the given ID, if it exists.
        /// </summary>
        public void RemoveEdge(int edgeId)
        {
            m_data.RemoveEdge(edgeId);
        }

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
                        Internal_AddTriangle(new Triangle(m_data, edge.ID, m_data.GetEdge(edge.B.ID, pointFromA).ID, m_data.GetEdge(pointFromA, edge.A.ID).ID));
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

            Edge edge = new Edge(m_data, a.ID, b.ID);
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
        /// Return the triangle with the given ID, if it exists.
        /// </summary>
        public Triangle GetTriangle(int triangleId)
        {
            return m_data.GetTriangle(triangleId);
        }

        /// <summary>
        /// Remove the triangle with the given ID, if it exists.
        /// </summary>
        public void RemoveTriangle(int triangleId)
        {
            m_data.RemoveTriangle(triangleId);
        }
        
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

            Edge ab = new Edge(m_data, a.ID, b.ID);
            Edge bc = new Edge(m_data, b.ID, c.ID);
            Edge ca = new Edge(m_data, c.ID, a.ID);

            AddEdge(ab, findNewTriangles: false);
            AddEdge(bc, findNewTriangles: false);
            AddEdge(ca, findNewTriangles: false);

            Internal_AddTriangle(new Triangle(m_data, ab.ID, bc.ID, ca.ID, a.ID, b.ID, c.ID));
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

        /// <summary>
        /// Return the face with the given ID, if it exists.
        /// </summary>
        public Face GetFace(int faceId)
        {
            return m_data.GetFace(faceId);
        }

        /// <summary>
        /// Remove the face with the given ID, if it exists.
        /// </summary>
        public void RemoveFace(int faceId)
        {
            m_data.RemoveFace(faceId);
        }

        /// <summary>
        /// Given a face ID, try to retriangulate the face to a simpler set of triangles. 
        /// 
        /// Doesn't have much regard for things like UVs and normals.
        /// </summary>
        public bool TryRetriangulateFace(int faceId)
        {
            Face face = m_data.GetFace(faceId);

            if (face == null)
                return false;

            if (!face.Retriangulate(out List<Point> points))
                return false;

            m_data.RemoveFace(faceId);

            for (int i = 0; i < points.Count; i += 3)
            {
                AddTriangle(points[i], points[i + 1], points[i + 2]);
            }

            return true;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        public void DrawGizmo(Transform transform = null)
        {
            //foreach (Point point in m_data.Points)
            //    point.DrawNormalGizmo(Color.red, transform);

            foreach (Point point in m_data.Points)
                point.DrawPointLabels(Color.black, transform);

            //DrawFaces(transform);

            foreach (Triangle triangle in m_data.Triangles)
                triangle.DrawGizmo(Color.black, Color.black, Color.blue, transform: transform);

            //// orphans :(
            //foreach (Edge edge in m_data.Edges)
            //    if (!m_lookups.IsEdgeInTriangle(edge))
            //        edge.DrawGizmo(Color.red, transform: transform);
        }

        public void DrawPoints(Transform transform = null)
        {
            foreach (Point point in m_data.Points)
                point.DrawPointLabels(Color.black, transform);
        }

        public void DrawTriangles(Transform transform = null, bool label = false)
        {
            foreach (Triangle triangle in m_data.Triangles)
                triangle.DrawGizmo(Color.black, Color.black, Color.blue, transform: transform, label: label);
        }

        public void DrawFaces(Transform transform = null, bool label = false)
        {
            foreach (Face face in m_data.Faces)
                face.DrawGizmo(face.HashToColour(), transform, label);//
        }
#endif

        #endregion
    }
}