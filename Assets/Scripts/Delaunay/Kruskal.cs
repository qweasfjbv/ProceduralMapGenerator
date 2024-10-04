using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;


namespace Delaunay
{
    public static class Kruskal
    {
        public static List<Edge> MinimumSpanningTree(IEnumerable<Edge> graph)
        {
            List<Edge> ret = new List<Edge>();
            List<Edge> edges = new List<Edge>(graph);
            edges.Sort(Edge.LengthCompare);

            HashSet<Vertex> points = new HashSet<Vertex>();
            foreach (var edge in edges)
            {
                points.Add(edge.a);
                points.Add(edge.b);
            }

            Dictionary<Vertex, Vertex> parents = new Dictionary<Vertex, Vertex>();
            foreach (var point in points)
                parents[point] = point;

            Vertex find(Vertex x)
            {
                if (parents[x] == x) return x;
                parents[x] = find(parents[x]);

                return parents[x];
            }

            void Union(Edge edge)
            {
                var x_par = find(edge.a);
                var y_par = find(edge.b);

                if (x_par == y_par)
                {
                    return;
                }

                ret.Add(edge);

                if (x_par < y_par) parents[y_par] = x_par;
                else parents[x_par] = y_par;
            }

            foreach (var edge in edges) Union(edge);

            return ret;
        }
    }
}