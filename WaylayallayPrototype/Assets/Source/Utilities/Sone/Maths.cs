﻿using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Sone.Maths
{
    public static class Maths
    {
        private static Plane m_plane = new Plane(Vector3.up, 0.0f);

        #region General Maths Stuff

        /// <summary>
        /// Same as Unity's default LookRotation method but without the obnoxious debug message.
        /// </summary>
        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            if (forward == Vector3.zero)
                return Quaternion.identity;

            return Quaternion.LookRotation(forward, up);
        }

        /// <summary>
        /// Same as Unity's default LookRotation method but without the obnoxious debug message.
        /// </summary>
        public static Quaternion LookRotation(Vector3 forward)
        {
            return LookRotation(forward, Vector3.up);
        }

        /// <summary>
        /// Acts like lerp at the start and smoothstep at the end.
        /// </summary>
        public static float Sinerp(float from, float to, float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Lerp(from, to, Mathf.Sin(t * Mathf.PI * 0.5f));
        }

        /// <summary>
        /// Acts like smoothstep at the start and lerp at the end.
        /// </summary>
        public static float Coserp(float from, float to, float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Lerp(from, to, 1f - Mathf.Cos(t * Mathf.PI * 0.5f));
        }

        /// <summary>
        /// Short for 'boing-like interpolation', this method will first overshoot, then waver back and forth around the end value before coming to a rest.
        /// </summary>
        public static float Berp(float from, float to, float t)
        {
            t = Mathf.Clamp01(t);
            t = (Mathf.Sin(t * Mathf.PI * (0.2f + 2.5f * t * t * t)) * Mathf.Pow(1f - t, 2.2f) + t) * (1f + (1.2f * (1f - t)));

            return from + (to - from) * t;
        }

        /// <summary>
        /// Compare two floats and return if they're 'equal' within a given margin, which
        /// defaults to +/- 0.001.
        /// </summary>
        public static bool IsApproximatelyEqual(float a, float b, float margin = 0.001f)
        {
            return Mathf.Abs(a - b) < 0.001f;
        }

        /// <summary>
        /// Returns a point on a parabola defined by a , b and c.
        /// </summary>
        public static float GetPointOnParabola(float a, float b, float c, float x, float flattenFactor = 1f)
        {
            //flattenFactor = 1.5f;

            float y = (a * (x * x) + b * x + c) / flattenFactor;

            return y;
        }

        /// <summary>
        /// In this method, we find the equation of the parabola based on 3 points.
        /// The equation we want to find is:  y = a (x*x) + b * x + c . We have a system of 3 equations with 3 unknowns, 
        /// a, b and c. By replacing the 3 points of information in the system, we isolate the unknowns and find their values.
        /// We return an array of parameters, where parameter[0] = a, parameter[1] = a and parameter[2] = c.
        /// </summary>
        public static float[] FindParabolaEquationParameters(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            //Convert  world positions to cartesian  2d coordinates (x,y); 
            Vector2 firstPoint = new Vector2(0f, point1.y);
            Vector2 secondPoint = new Vector2((point2 - point1).Flatten().magnitude, point2.y);
            Vector2 thirdPoint = new Vector2((point3 - point1).Flatten().magnitude, point3.y);

            //Find C easily cause x is equal to 0. Therefore c is equal to firstPoint.y
            float c = firstPoint.y;

            float a = thirdPoint.y / ((thirdPoint.x * thirdPoint.x) - thirdPoint.x * secondPoint.x)
                - secondPoint.y / (secondPoint.x * thirdPoint.x - (secondPoint.x * secondPoint.x)) + c / (secondPoint.x * thirdPoint.x - (secondPoint.x * secondPoint.x))
                - c / ((thirdPoint.x * thirdPoint.x) - thirdPoint.x * secondPoint.x);

            //Find b as we have a
            float b = secondPoint.y / secondPoint.x - a * secondPoint.x - c / secondPoint.x;

            return new float[] { a, b, c };
        }

        /// <summary>
        /// Same as Unity's inverse lerp function, but values are extrapolated outside the range [0, 1].
        /// </summary>
        public static float InverseLerpUnclamped(float a, float b, float value)
        {
            return (value - a) / (b - a);
        }

        #endregion

        #region Circles

        /// <summary>
        /// Generate points equally spaced around a circle, with optional parameters for the radius and position of centre.
        /// 
        /// (Defaults to unit circle.)
        /// </summary>
        public static Vector3[] GenerateCirclePoints(int points, float radius = 1f, Vector3 centre = default(Vector3))
        {
            float angle = 360f / points;

            Vector3[] result = new Vector3[points];

            for (int i = 0; i < points; i++)
                result[i] = centre + radius * new Vector3(Mathf.Cos((angle * i) * (Mathf.PI / 180f)), 0f, Mathf.Sin((angle * i) * (Mathf.PI / 180f)));

            for (int i = 0; i < result.Length; i++)
                result[i] += centre;

            return result;
        }
        
        /// <summary>
        /// Given two circle centers and their radii (in respective order), returns the points of their intersection.
        /// 
        /// 2 important notes:
        /// 
        /// the circles are assumed to be flat on the XZ plane. Y components will be ignored.
        /// 
        /// This method is not safe - assumes circles have already been checked that they do in fact intersect.
        /// (Though this is a very simple fix)
        /// </summary>
        public static void CalculateCircleIntersectionPoints(Vector3 center0, Vector3 center1, float radius0, float radius1, out Vector3 i0, out Vector3 i1)
        {
            float dist = Vector3.Distance(center0, center1);

            float cx0 = center0.x;
            float cy0 = center0.z;

            float cx1 = center1.x;
            float cy1 = center1.z;

            // Find a and h.
            float a = (radius0 * radius0 - radius1 * radius1 + dist * dist) / (2f * dist);
            float h = Mathf.Sqrt(radius0 * radius0 - a * a);

            // Find P2.
            float cx2 = cx0 + a * (cx1 - cx0) / dist;
            float cy2 = cy0 + a * (cy1 - cy0) / dist;

            // Get the points P3.
            i0 = new Vector3((cx2 + h * (cy1 - cy0) / dist), 0f, (cy2 - h * (cx1 - cx0) / dist));
            i1 = new Vector3((cx2 - h * (cy1 - cy0) / dist), 0f, (cy2 + h * (cx1 - cx0) / dist));
        }
        /// <summary>
        /// Given a circle and a line, returns whether they intersect.
        /// </summary>
        public static bool CircleLineIntersect(Vector3 lineStart, Vector3 lineEnd, Vector3 circlePosition, float circleRadius)
        {
            float dx = lineEnd.x - lineStart.x;
            float dy = lineEnd.z - lineStart.z;
            float a = dx * dx + dy * dy;
            float b = 2 * (dx * (lineStart.x - circlePosition.x) + dy * (lineStart.z - circlePosition.z));
            float c = circlePosition.x * circlePosition.x + circlePosition.z * circlePosition.z;

            c += lineStart.x * lineStart.x + lineStart.z * lineStart.z;
            c -= 2 * (circlePosition.x * lineStart.x + circlePosition.z * lineStart.z);
            c -= circleRadius * circleRadius;

            return (b * b - 4 * a * c) < 0f;    // if true -> no collision
        }

        #endregion
        
        #region Vector3

        /// <summary>
        /// Given a point and a centre of rotation, rotate the point around the centre by the given
        /// euler angle.
        /// </summary>
        public static Vector3 RotateAround(Vector3 point, Vector3 centre, Vector3 eulerAngle)
        {
            return (Quaternion.Euler(eulerAngle) * (point - centre)) + centre;
        }

        /// <summary>
        /// Return a new direction which points in the same 'hemisphere' as the given direction.
        /// 
        /// In other words, the dot product of the given vector and the resulting vector is guaranteed to be < 0.
        /// </summary>
        public static Vector3 GetRandomDirectionInSameHemisphere(Vector3 direction)
        {
            Vector3 result = UnityEngine.Random.insideUnitSphere.normalized;

            while (Vector3.Dot(direction, result) < 0f)
            {
                result = UnityEngine.Random.insideUnitSphere.normalized;
            }

            return result;
        }

        /// <summary>
        /// Returns the clockwise angle between two vectors. 
        /// 
        /// The clockwise angle is a non commutative operation
        /// which tells you the angle between v1 and v2 assuming v1 is first on the 'clock.'
        /// I.e. the angle between 3 o'clock and 12 o'clock is 270 degrees,
        /// but the angle between 12 o'clock and 3 o'clock is 90 degrees.
        /// </summary>
        public static float ClockwiseAngle(Vector3 v1, Vector3 v2)
        {
            float angleOne = Mathf.Atan2(v1.z, v1.x);
            float angleTwo = Mathf.Atan2(v2.z, v2.x);

            float result = (180f / Mathf.PI) * (angleOne - angleTwo);

            return (result < 0f) ? (result + 360f) : result;
        }

        /// <summary>
        /// Returns the centroid of all the non-null objects in the given list.
        /// The list must contain objects which inherit from MonoBehaviour.
        /// </summary>
        public static Vector3 GetCentroid<T>(IList<T> list) where T : MonoBehaviour
        {
            if (list.Count == 0)
                return Vector3.zero;

            Vector3 result = Vector3.zero;
            int count = 0;

            foreach (T t in list)
            {
                if (t != null)
                {
                    result += t.transform.position;
                    ++count;
                }
            }

            return result / count;
        }

        /// <summary>
        /// Given an ordered array of vertices and a point, returns whether the point is inside the polygon formed by the vertices.
        /// 
        /// This is the Jordan Curve Theorem. It's taken from https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html.
        /// 
        /// Vertices may be listed clockwise or anticlockwise. 
        /// </summary>
        public static bool IsInside(Vector3[] vertices, Vector3 point)
        {
            bool inside = false;

            for (int i = 1, j = 0; i < vertices.Length - 1; j = i++)
            {
                if (((vertices[i].z > point.z) != (vertices[j].z > point.z)) &&
                 (point.x < (vertices[j].x - vertices[i].x) * (point.z - vertices[i].z) / (vertices[j].z - vertices[i].z) + vertices[i].x))
                    inside = !inside;
            }

            return inside;
        }

        /// <summary>
        /// Given a point, a vertical offset from that point, and a layer, project a raycast
        /// downward and return the point where the raycast hits.
        /// 
        /// If no hit, returns the given point again.
        /// </summary>
        public static Vector3 SnapToLayer(this Vector3 vec, float verticalOffset, int layer)
        {
            Vector3 rayStartPoint = vec.SetY(vec.y + verticalOffset);

            RaycastHit hit;

            if (Physics.Raycast(rayStartPoint, Vector3.down, out hit, 100f, layer, QueryTriggerInteraction.UseGlobal))
                return hit.point;

            return vec;
        }

        /// <summary>
        /// Clamp the vector3 on the plane bounded between the min and max vectors,
        /// as if they're corners of the plane.
        /// </summary>
        public static Vector3 Clamp(this Vector3 vec, Vector3 min, Vector3 max)
        {
            vec.x = Mathf.Clamp(vec.x, min.x, max.x);
            vec.y = Mathf.Clamp(vec.y, min.y, max.y);
            vec.z = Mathf.Clamp(vec.z, min.z, max.z);

            return vec;
        }

        /// <summary>
        /// Converts a viewport point to a point on the plane where y = 0.
        /// </summary>
        public static Vector3 ViewportToGround(Vector2 screenPos)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            float distance = 0f;

            if (m_plane.Raycast(ray, out distance))
                return ray.GetPoint(distance);

            return Vector3.zero;
        }
        
        #endregion
        
        #region Bezier Curves

        public static Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;

            return (oneMinusT * oneMinusT * p0) + (2f * oneMinusT * t * p1) + (t * t * p2);
            // Note - this is mathematically exactly the same as:
            //return Vector3.Lerp(Vector3.Lerp(p0, p1, t), Vector3.Lerp(p1, p2, t), t);
        }

        public static Vector3 GetFirstDerivativeBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return (2f * (1f - t) * (p1 - p0)) + (2f * t * (p2 - p1));
        }

        public static Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;

            return (oneMinusT * oneMinusT * oneMinusT * p0) + (3f * oneMinusT * oneMinusT * t * p1) + (3f * oneMinusT * t * t * p2) + (t * t * t * p3);
        }

        public static Vector3 GetFirstDerivativeBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;

            return (3f * oneMinusT * oneMinusT * (p1 - p0)) + (6f * oneMinusT * t * (p2 - p1)) + (3f * t * t * (p3 - p2));
        }

        #endregion

        #region Physics

        /// <summary>
        /// Determine where the point of a 'step' is.
        /// </summary>
        /// <param name="position">Position of the entity taking a step.</param>
        /// <param name="offset">Direction and magnitude of the step being taken.</param>
        /// <param name="maxHeightDifference">Height at which to start looking, compared to the stepping agent.</param>
        /// <param name="layer">The layer of the step object</param>
        public static float GetStepHeight(Vector3 position, Vector3 offset, float maxHeightDifference, LayerMask layer)
        {
            Debug.Assert(maxHeightDifference > 0f);
            
            Vector3 stepLookDownPoint = position + offset;

            RaycastHit hit;
            
            // check if the step is too high (we detect something directly in front)
            if (Physics.Raycast(position, offset.normalized, out hit, offset.magnitude, layer))
            {
                Debug.DrawLine(position, hit.point, Color.red, Time.deltaTime);
                return 0f;
            }

            Debug.DrawLine(position, stepLookDownPoint, Color.black, Time.deltaTime);

            // check if theres a surface below the lookahead point by raycasting down
            if (Physics.Raycast(stepLookDownPoint, -Vector3.up, out hit, maxHeightDifference, layer))
            {
                Debug.DrawLine(stepLookDownPoint, hit.point, Color.red, Time.deltaTime);
                //Debug.Break();

                return (stepLookDownPoint.y - hit.point.y) / 2f;
            }
            
            Debug.DrawLine(stepLookDownPoint, stepLookDownPoint.AddToY(-maxHeightDifference), Color.black, Time.deltaTime);

            return 0f;
        }

        #endregion

        #region Matrices

        /// <summary>
        /// Extract the rotation matrix from the given transforms local to world matrix.
        /// </summary>
        public static Matrix4x4 GetRotationMatrix(this Transform transform)
        {
            Matrix4x4 main = transform.localToWorldMatrix;

            Quaternion rotation = Quaternion.LookRotation(main.GetColumn(2), main.GetColumn(1));

            return Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
        }

        /// <summary>
        /// Extract the translation matrix from the given transforms local to world matrix.
        /// </summary>
        public static Matrix4x4 GetTranslationMatrix(this Transform transform)
        {
            Matrix4x4 main = transform.localToWorldMatrix;

            Vector3 position = main.GetColumn(3);

            return Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
        }

        #endregion
    }


    /// <summary>
    /// A comparer class for Vector3 which sorts the given points in clockwise order around the origin.
    /// </summary>
    public struct ClockwiseComparer : IComparer<Vector3>
    {
        public int Compare(Vector3 v1, Vector3 v2)
        {
            return Mathf.Atan2(v1.x, v1.z).CompareTo(Mathf.Atan2(v2.x, v2.z));
        }
    }

    public class MeshSimplifier
    {
        private Dictionary<int, HashSet<Vector3>> m_normalToVerticesLookup;

        private Dictionary<int, Vector3> m_normals;

        private Dictionary<int, HashSet<Vector3>> m_connectedCounts;

        private Dictionary<int, Color> m_gizmoColours;

        public MeshSimplifier()
        {
            m_normalToVerticesLookup = new Dictionary<int, HashSet<Vector3>>();
            m_normals = new Dictionary<int, Vector3>();
            m_connectedCounts = new Dictionary<int, HashSet<Vector3>>();
        }

        private void AddVertex(Vector3 referenceNormal, Vector3 vertex)
        {
            int key = referenceNormal.HashableVector3();

            if (!m_normalToVerticesLookup.ContainsKey(key))
                m_normalToVerticesLookup.Add(key, new HashSet<Vector3>());

            if (!m_normals.ContainsKey(key))
                m_normals.Add(key, referenceNormal);

            m_normalToVerticesLookup[key].Add(vertex);
        }

        private void AddConnectedVertices(IList<Vector3> vectors)
        {
            for (int i = 0; i < vectors.Count; i++)
            {
                int iHash = vectors[i].HashableVector3();

                for (int j = 0; j < vectors.Count; j++)
                {
                    if (vectors[i] != vectors[j])
                    {
                        if (!m_connectedCounts.ContainsKey(iHash))
                            m_connectedCounts.Add(iHash, new HashSet<Vector3>());

                        m_connectedCounts[iHash].Add(vectors[j]);
                    }
                }
            }
        }

        public void AddPolygons(IList<Vector3> vectors, Plane plane)
        {
            for (int i = 0; i < vectors.Count; i++)
                AddVertex(plane.normal, vectors[i]);

            AddConnectedVertices(vectors);
        }

        public void AddPolygon(IList<Vector3> vectors)
        {
            Debug.Assert(vectors.Count >= 3, "Polygons must have at least three vertices!");

            AddPolygons(vectors, new Plane(vectors[0], vectors[1], vectors[2]));
        }

        public void Clear()
        {
            m_normalToVerticesLookup.Clear();
            m_normals.Clear();
            m_connectedCounts.Clear();

            if (m_gizmoColours != null)
                m_gizmoColours.Clear();
        }

        public override string ToString()
        {
            if (m_normalToVerticesLookup == null)
                return "Not initialized.";

            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<int, HashSet<Vector3>> kvp in m_normalToVerticesLookup)
            {
                sb.Append(m_normals[kvp.Key] + " [" + kvp.Key + "]:\n");

                foreach (Vector3 vertex in kvp.Value)
                    sb.Append("\t" + vertex.ToString() + " (Connected to " + m_connectedCounts[vertex.HashableVector3()].Count + " other vertices.)\n");

                sb.Append("\n");
            }

            sb.Append(m_normalToVerticesLookup.Count + " normals.");

            return sb.ToString();
        }

        //private Vector3[] FindCandidatePolygonVertices(HashSet<Vector3> vecs)
        //{
        //    Vector3[] vertices = new Vector3[4];
        //    int[] connected = new int[] { -1, -1, -1, -1 };
        //    int verticesFound = 0;

        //    foreach (Vector3 vec in vecs)
        //    {
        //        int vecHash = vec.HashableVector3();

        //        for (int i = 0; i < connected.Length; i++)
        //        {
        //            if (connected[i] == -1)
        //            {
        //                connected[i] = m_connectedCounts[vecHash].Count;
        //                vertices[i] = vec;
        //                verticesFound++;

        //                break;
        //            }
        //        }
        //    }
        //}

        public void DrawGizmos(Vector3 offset = default(Vector3), float magnitude = 1f)
        {
#if UNITY_EDITOR

            if (m_gizmoColours == null || m_gizmoColours.Count != m_normalToVerticesLookup.Count)
            {
                if (m_gizmoColours == null)
                    m_gizmoColours = new Dictionary<int, Color>(m_normalToVerticesLookup.Count);
                else
                    m_gizmoColours.Clear();

                foreach (int key in m_normalToVerticesLookup.Keys)
                    m_gizmoColours.Add(key, Random.ColorHSV());
            }

            foreach (KeyValuePair<int, HashSet<Vector3>> kvp in m_normalToVerticesLookup)
            {
                Gizmos.color = m_gizmoColours[kvp.Key];
                
                foreach (Vector3 vertex in kvp.Value)
                {
                    Gizmos.DrawSphere(vertex + offset, 0.08f);
                    Gizmos.DrawLine(vertex + offset, vertex + offset + m_normals[kvp.Key] * magnitude);
                }
            }

            if (m_normalToVerticesLookup.Count <= 4)
                return;


#endif
        }
    }
}