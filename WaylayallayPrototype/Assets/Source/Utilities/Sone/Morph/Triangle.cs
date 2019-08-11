using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Simplex
{

    /// <summary>
    /// Represents the connection of three edges to form a single triangle
    /// of the mesh. A > B > C in clockwise order.
    /// </summary>
    [System.Serializable]
    public class Triangle
    {
        [System.NonSerialized]
        private Data m_data;

        [System.NonSerialized]
        private Edge m_ab;
        public Edge AB { get { return m_ab; } }

        [System.NonSerialized]
        private Edge m_bc;
        public Edge BC { get { return m_bc; } }

        [System.NonSerialized]
        private Edge m_ca;
        public Edge CA { get { return m_ca; } }

        private int AB_ID = -1;
        private int BC_ID = -1;
        private int CA_ID = -1;
        
        [System.NonSerialized]
        private Point m_a;
        public Point A { get { return m_a; } }

        [System.NonSerialized]
        private Point m_b;
        public Point B { get { return m_b; } }

        [System.NonSerialized]
        private Point m_c;
        public Point C { get { return m_c; } }

        private int A_ID = -1;
        private int B_ID = -1;
        private int C_ID = -1;

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

        public Triangle(Data data, int ab, int bc, int ca)
        {
            AB_ID = ab;
            BC_ID = bc;
            CA_ID = ca;

            A_ID = AB.A.ID;
            B_ID = AB.B.ID;

            if (BC.A == A || BC.A == B)
                C_ID = BC.B.ID;
            else
                C_ID = BC.A.ID;

            SetData(data);
        }

        public Triangle(Data data, int ab, int bc, int ca, int a, int b, int c)
        {
            AB_ID = ab;
            BC_ID = bc;
            CA_ID = ca;

            A_ID = a;
            B_ID = b;
            C_ID = c;

            SetData(data);
        }

        public void SetData(Data data)
        {
            m_data = data;

            m_a = m_data.GetPoint(A_ID);
            m_b = m_data.GetPoint(B_ID);
            m_c = m_data.GetPoint(C_ID);

            m_ab = m_data.GetEdge(AB_ID);
            m_bc = m_data.GetEdge(BC_ID);
            m_ca = m_data.GetEdge(CA_ID);
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
        /// Returns true if the this triangle has the same normal as the given vector (assumed normalized).
        /// </summary>
        public bool IsConormal(Vector3 normal)
        {
            return Normal == normal;
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
            handleStyle.fontSize = 13;

            Handles.Label(Centroid + Normal * 0.05f, ID.ToString(), handleStyle);

            Gizmos.matrix = originalGizmoMatrix;
            Handles.matrix = originalHandleMatrix;
        }
#endif
    }

}