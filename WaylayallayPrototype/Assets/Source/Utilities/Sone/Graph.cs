using System.Collections.Generic;
using UnityEngine;

namespace Simplex
{
    public static partial class Graph
    {
        #region Helper Methods

        /// <summary>
        /// A single point and a colour to draw it. For debug purposes.
        /// </summary>
        public struct ColouredVertex
        {
            [SerializeField]
            private Vector3 m_vertex;
            public Vector3 Vertex { get { return m_vertex; } }

            [SerializeField]
            private Color m_colour;
            public Color Colour { get { return m_colour; } }
            
            public ColouredVertex(Vector3 vertex, Color colour)
            {
                m_vertex = vertex;
                m_colour = colour;
            }
        }

        [System.Flags]
        public enum Partition
        {
            BOTH_NEGATIVE = 0,  // 00
            B_POSITIVE = 1,     // 01
            A_POSITIVE = 2,     // 10
            BOTH_POSITIVE = 3,  // 11
        }

        /// <summary>
        /// Returns whether the given partition is a bisection - i.e., it represents a line
        /// which has been cut so that one vertex is on the positive side and the other is on the negative side.
        /// </summary>
        public static bool IsBisection(this Partition bisection)
        {
            return (bisection == Partition.A_POSITIVE || bisection == Partition.B_POSITIVE);
        }

        #endregion

        public struct ColouredVertexClockwiseComparer : IComparer<Graph.ColouredVertex>
        {
            public int Compare(Graph.ColouredVertex v1, Graph.ColouredVertex v2)
            {
                return Mathf.Atan2(v1.Vertex.x, v1.Vertex.z).CompareTo(Mathf.Atan2(v2.Vertex.x, v2.Vertex.z));
            }
        }
    }
}