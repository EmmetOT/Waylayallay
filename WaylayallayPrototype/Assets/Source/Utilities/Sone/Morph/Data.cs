using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Simplex
{

    /// <summary>
    /// This class allows quickly checking which objects (edges, triangles, faces) contain certain points.
    /// </summary>
    [System.Serializable]
    public class Data : ISerializationCallbackReceiver
    {
        private Dictionary<int, Point> m_pointsByID = new Dictionary<int, Point>();
        private Dictionary<int, Edge> m_edgesByID = new Dictionary<int, Edge>();
        private Dictionary<int, Triangle> m_trianglesByID = new Dictionary<int, Triangle>();
        private Dictionary<int, Face> m_facesByID = new Dictionary<int, Face>();

        private Dictionary<int, HashSet<int>> m_edgesByPointID = new Dictionary<int, HashSet<int>>();
        private Dictionary<int, HashSet<int>> m_trianglesByPointID = new Dictionary<int, HashSet<int>>();
        private Dictionary<int, HashSet<int>> m_facesByPointID = new Dictionary<int, HashSet<int>>();

        //private Dictionary<int, HashSet<int>> m_pointsByLocation = new Dictionary<int, HashSet<int>>();

        private Dictionary<int, HashSet<int>> m_connectedPoints = new Dictionary<int, HashSet<int>>();
        private Dictionary<int, HashSet<int>> m_samePoints = new Dictionary<int, HashSet<int>>();

        private int m_highestPointIndex = 0;
        private int m_highestEdgeIndex = 0;
        private int m_highestTriangleIndex = 0;
        private int m_highestFaceIndex = 0;

        #region Serialization

        [SerializeField]
        private Point[] m_serializedPoints;

        [SerializeField]
        private Edge[] m_serializedEdges;

        [SerializeField]
        private Triangle[] m_serializedTriangles;

        [SerializeField]
        private Face[] m_serializedFaces;

        [SerializeField]
        private Serialization.IntSet[] m_s_edgesByPointID;

        [SerializeField]
        private Serialization.IntSet[] m_s_trianglesByPointID;

        [SerializeField]
        private Serialization.IntSet[] m_s_facesByPointID;

        //[SerializeField]
        //private Serialization.IntSet[] m_s_pointsByLocation;

        [SerializeField]
        private Serialization.IntSet[] m_s_connectedPoints;

        [SerializeField]
        private Serialization.IntSet[] m_s_samePoints;

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying &&
                !EditorApplication.isUpdating &&
                !EditorApplication.isCompiling)
                return;
#endif

            int i = 0;
            m_serializedPoints = new Point[m_pointsByID.Count];
            foreach (KeyValuePair<int, Point> kvp in m_pointsByID)
                m_serializedPoints[i++] = kvp.Value;

            i = 0;
            m_serializedEdges = new Edge[m_edgesByID.Count];
            foreach (KeyValuePair<int, Edge> kvp in m_edgesByID)
                m_serializedEdges[i++] = kvp.Value;//

            i = 0;
            m_serializedTriangles = new Triangle[m_trianglesByID.Count];
            foreach (KeyValuePair<int, Triangle> kvp in m_trianglesByID)
                m_serializedTriangles[i++] = kvp.Value;

            i = 0;
            m_serializedFaces = new Face[m_facesByID.Count];
            foreach (KeyValuePair<int, Face> kvp in m_facesByID)
                m_serializedFaces[i++] = kvp.Value;

            m_s_edgesByPointID = m_edgesByPointID.ToIntSets();
            m_s_trianglesByPointID = m_trianglesByPointID.ToIntSets();
            m_s_facesByPointID = m_facesByPointID.ToIntSets();
            //m_s_pointsByLocation = m_pointsByLocation.ToIntSets();
            m_s_connectedPoints = m_connectedPoints.ToIntSets();
            m_s_samePoints = m_samePoints.ToIntSets();
        }

        public void OnAfterDeserialize()
        {
            if (m_serializedPoints == null)
                return;

            m_pointsByID.Clear();
            for (int i = 0; i < m_serializedPoints.Length; i++)
            {
                m_serializedPoints[i].SetData(this);
                m_pointsByID.Add(m_serializedPoints[i].ID, m_serializedPoints[i]);
            }

            m_edgesByID.Clear();
            for (int i = 0; i < m_serializedEdges.Length; i++)
            {
                m_serializedEdges[i].SetData(this);
                m_edgesByID.Add(m_serializedEdges[i].ID, m_serializedEdges[i]);
            }

            m_trianglesByID.Clear();
            for (int i = 0; i < m_serializedTriangles.Length; i++)
            {
                m_serializedTriangles[i].SetData(this);
                m_trianglesByID.Add(m_serializedTriangles[i].ID, m_serializedTriangles[i]);
            }

            m_facesByID.Clear();
            for (int i = 0; i < m_serializedFaces.Length; i++)
            {
                m_serializedFaces[i].SetData(this);
                m_facesByID.Add(m_serializedFaces[i].ID, m_serializedFaces[i]);
            }

            m_edgesByPointID = m_s_edgesByPointID.ToDictionary();
            m_trianglesByPointID = m_s_trianglesByPointID.ToDictionary();
            m_facesByPointID = m_s_facesByPointID.ToDictionary();
            m_connectedPoints = m_s_connectedPoints.ToDictionary();
            m_samePoints = m_s_samePoints.ToDictionary();
        }

        #endregion

        public void AddEdge(Edge edge)
        {
            // Prevent adding duplicate edges
            // not necessarym but might be useful if we want to start using collections which don't have restrictions
            // on duplicates
            if (m_edgesByPointID.ContainsKey(edge.A.ID) && m_edgesByPointID[edge.A.ID].Contains(edge.ID))
                return;

            if (edge.ID == -1)
            {
                edge.SetID(m_highestEdgeIndex++);
                m_edgesByID.Add(edge.ID, edge);
            }

            AddConnection(edge.A, edge.B);

            if (!m_edgesByPointID.ContainsKey(edge.A.ID))
                m_edgesByPointID.Add(edge.A.ID, new HashSet<int>());

            m_edgesByPointID[edge.A.ID].Add(edge.ID);

            if (!m_edgesByPointID.ContainsKey(edge.B.ID))
                m_edgesByPointID.Add(edge.B.ID, new HashSet<int>());

            m_edgesByPointID[edge.B.ID].Add(edge.ID);
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
                return m_edgesByID.Count;
            }
        }

        public IEnumerable<Edge> Edges
        {
            get
            {
                foreach (KeyValuePair<int, Edge> kvp in m_edgesByID)
                    yield return kvp.Value;
            }
        }

        public int TriangleCount
        {
            get
            {
                return m_trianglesByID.Count;
            }
        }

        public IEnumerable<Triangle> Triangles
        {
            get
            {
                foreach (KeyValuePair<int, Triangle> kvp in m_trianglesByID)
                    yield return kvp.Value;
            }
        }

        public int FaceCount
        {
            get
            {
                return m_facesByID.Count;
            }
        }

        public IEnumerable<Face> Faces
        {
            get
            {
                foreach (KeyValuePair<int, Face> kvp in m_facesByID)
                    yield return kvp.Value;
            }
        }

        public IEnumerable<Edge> GetEdgesContaining(int id)
        {
            if (!m_edgesByPointID.ContainsKey(id))
                yield break;

            foreach (int edge in m_edgesByPointID[id])
                yield return m_edgesByID[edge];
        }

        public Edge GetEdge(int edgeID)
        {
            return m_edgesByID[edgeID];
        }

        public Edge GetEdge(int a, int b)
        {
            foreach (Edge edge in GetEdgesContaining(a))
                if (edge.Contains(b))
                    return edge;

            return null;
        }

        public void RemoveEdge(int edgeID)
        {
            Edge edge = m_edgesByID[edgeID];
            m_edgesByID.Remove(edgeID);

            if (m_connectedPoints.ContainsKey(edge.A.ID))
                m_connectedPoints[edge.A.ID].Remove(edge.B.ID);

            if (m_connectedPoints.ContainsKey(edge.B.ID))
                m_connectedPoints[edge.B.ID].Remove(edge.A.ID);
        }

        public bool HasEdge(Edge edge)
        {
            return m_edgesByID.ContainsKey(edge.ID);
        }

        public void AddTriangle(Triangle triangle)
        {
            if (triangle.ID == -1)
            {
                triangle.SetID(m_highestTriangleIndex++);
                m_trianglesByID.Add(triangle.ID, triangle);
            }

            foreach (Point point in triangle.Points)
            {
                if (!m_trianglesByPointID.ContainsKey(point.ID))
                    m_trianglesByPointID.Add(point.ID, new HashSet<int>());

                m_trianglesByPointID[point.ID].Add(triangle.ID);
            }

            foreach (Edge edge in triangle.Edges)
            {
                AddEdge(edge);
            }

            ConnectTriangleWithFace(triangle);
        }

        /// <summary>
        /// Attempt to merge the triangle with whichever face it belongs to.
        /// </summary>
        private void ConnectTriangleWithFace(Triangle triangle)
        {
            bool addedToExistingFace = false;
            foreach (Face face in Faces)
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
                AddFace(new Face(triangle.ID, this));
            }
        }

        /// <summary>
        /// Called whenever a face adds a new triangle.
        /// </summary>
        public void OnAddedTriangleToFace(int faceID, int triangleID)
        {
            Triangle triangle = m_trianglesByID[triangleID];
            foreach (Point point in triangle.Points)
            {
                if (!m_facesByPointID.TryGetValue(point.ID, out HashSet<int> faces))
                    m_facesByPointID.Add(point.ID, new HashSet<int>() { faceID });
                else
                    faces.Add(faceID);
            }
        }

        /// <summary>
        /// Called whenever a face removes a triangle.
        /// </summary>
        public void OnRemovedTriangleFromFace(int faceID, int triangleID)
        {
            Triangle triangle = m_trianglesByID[triangleID];
            foreach (Point point in triangle.Points)
            {
                if (m_facesByPointID.TryGetValue(point.ID, out HashSet<int> faces))
                    faces.Remove(faceID);
            }
        }

        public void RemoveTriangle(Triangle triangle)
        {
            RemoveTriangle(triangle.ID);
        }

        public void RemoveTriangle(int triangleID)
        {
            Triangle triangle = m_trianglesByID[triangleID];
            m_trianglesByID.Remove(triangleID);

            foreach (Point point in triangle.Points)
                m_trianglesByPointID[point.ID].Remove(triangle.ID);

            foreach (Edge edge in triangle.Edges)
                RemoveEdge(edge.ID);

            foreach (Point point in triangle.Points)
                RemovePoint(point.ID);
        }

        public Triangle GetTriangle(int id)
        {
            if (m_trianglesByID.TryGetValue(id, out Triangle triangle))
                return triangle;

            return null;
        }

        public bool HasTriangle(Triangle triangle)
        {
            return m_trianglesByID.ContainsKey(triangle.ID);
        }

        public void AddFace(Face face)
        {
            if (face.ID == -1)
            {
                face.SetID(m_highestFaceIndex++);
                m_facesByID.Add(face.ID, face);
            }

            foreach (Triangle triangle in face.Triangles)
            {
                foreach (Point point in triangle.Points)
                {
                    if (!m_facesByPointID.TryGetValue(point.ID, out HashSet<int> faces))
                        m_facesByPointID.Add(point.ID, new HashSet<int>() { face.ID });
                    else
                        faces.Add(face.ID);
                }
            }
        }

        public void RemoveFace(int faceId)
        {
            if (!m_facesByID.TryGetValue(faceId, out Face face))
                return;

            m_facesByID.Remove(faceId);

            foreach (Triangle triangle in face.Triangles)
            {
                RemoveTriangle(triangle);

                foreach (Point point in triangle.Points)
                {
                    if (m_facesByPointID.ContainsKey(point.ID))
                        m_facesByPointID.Remove(point.ID);
                }
            }
        }

        /// <summary>
        /// Get the face by the given face ID, if it exists.
        /// </summary>
        public Face GetFace(int id)
        {
            if (m_facesByID.TryGetValue(id, out Face face))
                return face;

            return null;
        }

        public void AddPoint(Point point)
        {
            if (point.ID == -1)
            {
                point.SetID(m_highestPointIndex++);
                m_pointsByID.Add(point.ID, point);
            }

            int locationID = point.GetLocationID();

            //if (!m_pointsByLocation.TryGetValue(locationID, out HashSet<int> points))
            //    m_pointsByLocation.Add(locationID, new HashSet<int>() { point.ID });
            //else
            //    points.Add(point.ID);

            // make a note of any points which share a vertex (for pathing)
            ConnectPointsInSameLocation(point);
        }

        public void RemovePoint(int id)
        {
            if (!m_pointsByID.TryGetValue(id, out Point point))
                return;

            m_pointsByID.Remove(id);

            //m_pointsByLocation[point.GetLocationID()].Remove(id);

            if (m_connectedPoints.ContainsKey(id))
            {
                foreach (int connectedPoint in m_connectedPoints[id].ToList())
                    m_connectedPoints[connectedPoint].Remove(id);

                m_connectedPoints.Remove(id);
            }

            if (m_samePoints.ContainsKey(id))
            {
                foreach (int samePoint in m_samePoints[id].ToList())
                    m_samePoints[samePoint].Remove(id);

                m_samePoints.Remove(id);
            }

            foreach (int edge in m_edgesByPointID[id].ToList())
                RemoveEdge(edge);

            m_edgesByPointID.Remove(id);

            foreach (int triangle in m_trianglesByPointID[id].ToList())
                RemoveTriangle(triangle);

            m_trianglesByPointID.Remove(id);

            if (m_facesByPointID.ContainsKey(id))
                m_facesByPointID.Remove(id);
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
        /// If we already have a point object in the exact same location as the given point object,
        /// return it. Else return the given point back.
        /// 
        /// If the given point has data such as UV, Tangent, or Normal, you can optionally make the returned point
        /// (if it already existed after all) copy this data onto itself. This defaults to true.
        /// </summary>
        public void ConnectPointsInSameLocation(Point point)
        {
            int locationID = point.GetLocationID();

            foreach (KeyValuePair<int, Point> kvp in m_pointsByID)
            {
                if (point.ID != kvp.Key && kvp.Value.GetLocationID() == locationID && !m_connectedPoints[kvp.Key].Contains(point.ID))
                {
                    AddConnection(m_pointsByID[kvp.Key], point, isSamePoint: true);
                }
            }
        }

        /// <summary>
        /// If we already have a point object in the exact same location as the given vector,
        /// return it. Else return the given point back.
        /// </summary>
        public Point GetExistingPointInSameLocation(Vector3 vec)
        {
            int locationID = vec.HashVector3();

            //// points already exist at given location. return one arbitrarily.
            //if (m_pointsByLocation.ContainsKey(locationID) && !m_pointsByLocation[locationID].IsNullOrEmpty())
            //    return m_pointsByID[m_pointsByLocation[locationID].First()];

            foreach (KeyValuePair<int, Point> kvp in m_pointsByID)
                if (kvp.Value.GetLocationID() == locationID)
                    return kvp.Value;

            // point is at a new location so it's ok to use
            return new Point(this, vec);
        }

        /// <summary>
        /// For when a point changes location, update faces.
        /// </summary>
        public void OnRelocatePoint(int pointID)
        {
            if (m_facesByPointID.TryGetValue(pointID, out HashSet<int> faces))
            {
                foreach (int faceID in faces.ToList())
                {
                    Face face = m_facesByID[faceID];

                    foreach (Triangle triangle in face.Triangles.ToList())
                    {
                        if (face.Contains(triangle) && face.TriangleCount > 1 && !face.IsSameFace(triangle))
                        {
                            face.RemoveTriangle(triangle);
                            ConnectTriangleWithFace(triangle);
                        }
                    }
                }
            }
        }
    }
}