using System.Collections.Generic;
using UnityEngine;
using static Sone.Graph.Graph;

namespace Sone.Graph
{
    public static class Graph
    {
        //public class Node
        //{
        //    [SerializeField]
        //    private Vector3 m_position;

        //    [SerializeField]
        //    private HashSet<Node> m_neighbours;

        //    public Node(Vector3 position, HashSet<Node> neighbours = null)
        //    {
        //        m_position = position;

        //        if (neighbours != null)
        //            m_neighbours = neighbours;
        //        else
        //            m_neighbours = new HashSet<Node>();
        //    }

        //    public void AddNeighbour(Node node)
        //    {
        //        m_neighbours.Add(node);
        //    }

        //    public bool IsNeighbour(Node node)
        //    {
        //        return m_neighbours.Contains(node);
        //    }

        //    public int CalculatePathLength(Node to)
        //    {
        //        if (IsNeighbour(to))
        //            return 0;

        //        int shortestPath = int.MaxValue;
        //        foreach (Node neighbour in m_neighbours)
        //        {
        //            int pathLength = neighbour.CalculatePathLength(this, to, 1);

        //            if (pathLength < shortestPath)
        //                shortestPath = pathLength;
        //        }

        //        return shortestPath;
        //    }

        //    private int CalculatePathLength(Node from, Node to, int current)
        //    {
        //        if (IsNeighbour(to))
        //            return current;

        //        int shortestPath = int.MaxValue;
        //        foreach (Node neighbour in m_neighbours)
        //        {
        //            if (neighbour == from)
        //                continue;

        //            int pathLength = neighbour.CalculatePathLength(this, to, current + 1);

        //            if (pathLength > shortestPath)
        //                shortestPath = pathLength;
        //        }

        //        return current + shortestPath;
        //    }
        //}
        
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
        
    }
    
    //public struct Grapher
    //{
    //    private Dictionary<Vector3, HashSet<Vector3>> m_edges;

    //    public Grapher(Mesh mesh) : this(mesh.vertices, mesh.triangles) { }

    //    public Grapher(Vector3[] vertices, int[] triangles)
    //    {
    //        m_edges = new Dictionary<Vector3, HashSet<Vector3>>();

    //        for (int i = 0; i < triangles.Length; i += 3)
    //        {
    //            AddEdge(vertices[triangles[i]], vertices[triangles[i + 1]]);
    //            AddEdge(vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
    //            AddEdge(vertices[triangles[i + 2]], vertices[triangles[i]]);
    //        }
    //    }

    //    public List<Edge> GetEdges()
    //    {
    //        List<Edge> edges = new List<Edge>(m_edges.Count * 2);

    //        foreach (KeyValuePair<Vector3, HashSet<Vector3>> kvp in m_edges)
    //        {
    //            foreach (Vector3 b in kvp.Value)
    //            {
    //                edges.Add(new Edge(kvp.Key, b));
    //            }
    //        }

    //        return edges;
    //    }

    //    private void AddEdge(Vector3 a, Vector3 b)
    //    {
    //        if (m_edges.ContainsKey(a))
    //        {
    //            if (m_edges[a] == null)
    //                m_edges[a] = new HashSet<Vector3>();

    //            m_edges[a].Add(b);
    //        }
    //        else
    //        {
    //            if (!m_edges.ContainsKey(b))
    //                m_edges.Add(b, new HashSet<Vector3>());

    //            if (m_edges[b] == null)
    //                m_edges[b] = new HashSet<Vector3>();

    //            m_edges[b].Add(a);
    //        }
    //    }
    //}

    public struct ColouredVertexClockwiseComparer : IComparer<ColouredVertex>
    {
        public int Compare(ColouredVertex v1, ColouredVertex v2)
        {
            return Mathf.Atan2(v1.Vertex.x, v1.Vertex.z).CompareTo(Mathf.Atan2(v2.Vertex.x, v2.Vertex.z));
        }
    }
}