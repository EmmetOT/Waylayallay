using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Simplex
{

    /// <summary>
    /// Represents the connection of 2 points to form an edge of the mesh.
    /// </summary>
    [System.Serializable]
    public class Edge
    {
        private const int TWO_TO_FIFTEENTH_POWER = 32768;

        [System.NonSerialized]
        private Data m_data;

        [System.NonSerialized]
        private Point m_a;
        public Point A { get { return m_a; } }

        [System.NonSerialized]
        private Point m_b;
        public Point B { get { return m_b; } }

        public int A_ID = -1;
        public int B_ID = -1;

        /// <summary>
        /// Used to uniquely identify a particular edge object.
        /// </summary>
        public int ID { get; private set; } = -1;

        public IEnumerable<Point> Points
        {
            get
            {
                yield return A;
                yield return B;
            }
        }

        public Edge(Data data, int a, int b)
        {
            A_ID = a;
            B_ID = b;
            SetData(data);
        }

        public void SetData(Data data)
        {
            m_data = data;

            m_a = m_data.GetPoint(A_ID);
            m_b = m_data.GetPoint(B_ID);
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

        /// <summary>
        /// Set the value used to uniquely identify this edge. Should not change over the lifetime.
        /// </summary>
        public void SetID(int id)
        {
            ID = id;
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

            Gizmos.matrix = originalGizmoMatrix;
            Handles.matrix = originalHandleMatrix;
        }

        public void DrawGizmo(Color edgeCol, float radius = 0.05f, bool label = true, Transform transform = null)
        {
            DrawGizmo(edgeCol, edgeCol, radius, label, transform);
        }
#endif
    }

}