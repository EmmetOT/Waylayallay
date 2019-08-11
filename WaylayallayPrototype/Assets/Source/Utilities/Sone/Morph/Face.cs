using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Simplex
{

    /// <summary>
    /// A face represents a number of triangles which are all contiguous 
    /// and conormal.
    /// 
    /// TODO: Any point, edge, or triangle of this face moving means this face has to be updated.
    /// </summary>
    [System.Serializable]
    public class Face : ISerializationCallbackReceiver
    {
        private HashSet<int> m_triangleIDs = new HashSet<int>();
        private int[] m_s_triangleIDs;

        [System.NonSerialized]
        private Data m_data;

        private int m_initialTriangleID;

        public int ID { get; private set; } = -1;

        public int TriangleCount
        {
            get
            {
                return m_triangleIDs.Count;//
            }
        }

        public IEnumerable<Triangle> Triangles
        {
            get
            {
                foreach (int id in m_triangleIDs)
                    yield return m_data.GetTriangle(id);
            }
        }

        public Vector3 Normal
        {
            get
            {
                return m_data.GetTriangle(m_initialTriangleID).Normal;
            }
        }

        public Vector3 Centroid
        {
            get
            {
                Vector3 centroid = Vector3.zero;
                int count = 0;

                foreach (Triangle triangle in Triangles)
                {
                    centroid += triangle.Centroid;
                    count++;
                }

                return centroid / count;
            }
        }
        
        public Face(Data data, int triangleID)
        {
            SetData(data);
            m_initialTriangleID = triangleID;
            m_triangleIDs.Add(m_initialTriangleID);
        }

        public void SetData(Data data)
        {
            m_data = data;
        }

        /// <summary>
        /// Returns whether this face includes the given triangle.
        /// </summary>
        public bool Contains(Triangle triangle)
        {
            return Contains(triangle.ID);
        }

        /// <summary>
        /// Returns whether this face includes the given triangle.
        /// </summary>
        public bool Contains(int triangleID)
        {
            return m_triangleIDs.Contains(triangleID);
        }

        /// <summary>
        /// Remove the given triangle from this face, if it's in it.
        /// </summary>
        public void RemoveTriangle(Triangle triangle)
        {
            RemoveTriangle(triangle.ID);
        }

        /// <summary>
        /// Remove the given triangle from this face, if it's in it.
        /// </summary>
        public void RemoveTriangle(int triangleID)
        {
            m_triangleIDs.Remove(triangleID);
            m_data.OnRemovedTriangleFromFace(ID, triangleID);

        }

        /// <summary>
        /// If this triangle does not already belong to this face, and belongs as part of it (shares a normal with all triangles in the mesh
        /// and an edge with at least one), then add it and return true. Else return false.
        /// </summary>
        public bool TryAddTriangle(Triangle newTriangle)
        {
            if (!m_triangleIDs.Contains(newTriangle.ID))
            {
                foreach (int triangleID in m_triangleIDs)
                {
                    Triangle triangle = m_data.GetTriangle(triangleID);

                    if (triangle.IsSameFace(newTriangle))
                    {
                        m_triangleIDs.Add(newTriangle.ID);
                        m_data.OnAddedTriangleToFace(ID, newTriangle.ID);

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// If this triangle does not already belong to this face, and belongs as part of it (shares a normal with all triangles in the mesh
        /// and an edge with at least one), then add it and return true. Else return false.
        /// </summary>
        public bool TryAddTriangle(int newTriangleID)
        {
            return TryAddTriangle(m_data.GetTriangle(newTriangleID));
        }

        /// <summary>
        /// Returns true if the given triangle belongs to this face.
        /// </summary>
        public bool IsSameFace(Triangle triangle)
        {
            if (!IsConormal(triangle))
                return false;

            foreach (int existingTriangleID in m_triangleIDs)
                if (m_data.GetTriangle(existingTriangleID).IsContiguous(triangle))
                    return true;

            return false;
        }

        /// <summary>
        /// If this triangle is not already part of this face, and it shares an edge with at least
        /// one triangle of this face, return true. Else return false.
        /// </summary>
        public bool IsContiguous(Triangle newTriangle)
        {
            if (m_triangleIDs.Contains(newTriangle.ID))
                return false;

            foreach (int triangleID in m_triangleIDs)
                if (m_data.GetTriangle(triangleID).IsContiguous(newTriangle))
                    return true;

            return false;
        }

        /// <summary>
        /// If this triangle is not already part of this face, and it shares an edge with at least
        /// one triangle of this face, return true. Else return false.
        /// </summary>
        public bool IsConormal(Triangle newTriangle)
        {
            if (m_triangleIDs.Contains(newTriangle.ID))
                return false;

            foreach (int triangleID in m_triangleIDs)
                if (!m_data.GetTriangle(triangleID).IsConormal(newTriangle))
                    return false;

            return true;
        }

        /// <summary>
        /// If this face has any triangles which share an edge with any triangles in the given face,
        /// they are contiguous.
        /// </summary>
        public bool IsContiguous(Face face)
        {
            foreach (int triangleID in m_triangleIDs)
                if (face.IsContiguous(m_data.GetTriangle(triangleID)))
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
        public List<Point> GetPoints()
        {
            HashSet<Point> points = new HashSet<Point>();

            foreach (Triangle triangle in Triangles)
                foreach (Point point in triangle.Points)
                    points.Add(point);

            return points.ToList();
        }

        /// <summary>
        /// Set a unique ID for this face.
        /// </summary>
        public void SetID(int id)
        {
            ID = id;
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

            return perimeter;
        }

        /// <summary>
        /// Attempt to retriangulate this face, returns true if a better triangulation is possible,
        /// with the out parameter being a list of points where each set of 3 points form a triangle.
        /// </summary>
        public bool Retriangulate(out List<Point> result)
        {
            result = null;

            List<Point> perimeter = GetPerimeter();
            
            // can't get any better
            if (perimeter.Count - m_triangleIDs.Count == 2)
                return false;

            result = Retriangulate(GetPerimeter(), Normal);

            return true;
        }

        /// <summary>
        /// Given a set of points which are assumed to form a closed loop, and a normal, return a
        /// new list of points where each set of 3 points form a triangle.
        /// </summary>
        public static List<Point> Retriangulate(List<Point> perimeter, Vector3 normal)
        {
            int pointCount = perimeter.Count;
            
            Vector2[] vecPerimeter = new Vector2[pointCount];

            for (int i = 0; i < pointCount; i++)
                vecPerimeter[i] = perimeter[i].To2D(normal);

            Triangulator triangulator = new Triangulator(vecPerimeter);

            int[] triangulation = triangulator.Triangulate();

            //// new triangulation is somehow worse, don't bother continuing
            //if ((triangulation.Length / 3) >= triangleCount)
            //{
            //    Debug.Log((triangulation.Length / 3) + " is greater than " + triangleCount);
            //    return false;
            //}

            List<Point> result = new List<Point>(triangulation.Length);

            for (int i = 0; i < triangulation.Length; i++)
                result.Add(perimeter[triangulation[i]]);

            return result;
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

            foreach (int triangleID in m_triangleIDs)
                sb.Append(triangleID + ", ");

            sb.Remove(sb.Length - 2, 2);

            sb.Append("]");

            return sb.ToString();
        }


#if UNITY_EDITOR
        public void DrawGizmo(Color col, Transform transform = null, bool label = false)
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

            GUIStyle handleStyle = new GUIStyle();
            handleStyle.normal.textColor = Color.black;
            handleStyle.fontSize = 20;

            Handles.Label(Centroid, ID.ToString(), handleStyle);

            Handles.matrix = originalHandleMatrix;
#endif
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying &&
                !EditorApplication.isUpdating &&
                !EditorApplication.isCompiling)
                return;
#endif

            m_s_triangleIDs = m_triangleIDs.ToArray();
        }

        public void OnAfterDeserialize()
        {
            m_triangleIDs = new HashSet<int>(m_s_triangleIDs);
        }
    }

}