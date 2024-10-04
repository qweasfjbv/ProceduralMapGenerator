using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
    public class Triangle
    {
        private readonly Vertex a;
        private readonly Vertex b;
        private readonly Vertex c;

        public readonly HashSet<Edge> edges;

        private readonly float circumCenterX;
        private readonly float circumCenterY;
        private readonly float circumRadius2;

        private void SwapVertex(ref Vertex a, ref Vertex b)
        {
            var c = a;
            a = b;
            b = c;
        }

        // 항상 a<b<c가 되도록
        public Triangle(Vertex a, Vertex b, Vertex c)
        {
            this.a = a;
            this.b = b;
            this.c = c;

            // SORT
            if (a > b) SwapVertex(ref a, ref b);
            if (a > c) SwapVertex(ref a, ref c);
            if (b > c) SwapVertex(ref b, ref c);

            edges = new HashSet<Edge> 
            {
                new Edge(this.a, this.b),
                new Edge(this.b, this.c),
                new Edge(this.a, this.c)
            };

            // https://en.wikipedia.org/wiki/Circumcircle
            // Cartesian coordinates 참고.

            float D = (a.x * (b.y - c.y) +
                     b.x * (c.y - a.y) +
                     c.x * (a.y - b.y)) * 2;


            circumCenterX = ((a.x * a.x + a.y * a.y) * (b.y - c.y) +
                    (b.x * b.x + b.y * b.y) * (c.y - a.y) +
                    (c.x * c.x + c.y * c.y) * (a.y - b.y)) / D;
            circumCenterY = ((a.x * a.x + a.y * a.y) * (c.x - b.x) +
                    (b.x * b.x + b.y * b.y) * (a.x - c.x) +
                    (c.x * c.x + c.y * c.y) * (b.x - a.x)) / D;
            float dx = a.x - circumCenterX;
            float dy = a.y - circumCenterY;

            circumRadius2 = dx * dx + dy * dy;
        }
        public override bool Equals(object obj)
        {
            return obj is Triangle t && a == t.a && b == t.b && c == t.c;
        }


        public override string ToString()
        {
            return " (" + a + ", " + b + ", " + c + ")";
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(a, b, c);
        }

        public bool HasEdge(Edge edge)
        {
            return edges.Contains(edge);
        }

        private bool HasVertex(Vertex v)
        {
            return a == v || b == v || c == v;
        }

        public bool HasSameVertex(Triangle t)
        {
            return HasVertex(t.a) || HasVertex(t.b) || HasVertex(t.c);
        }

        // 반지름(제곱)과 중심에서 점까지의 거리 비교
        public bool IsInCircumCircle(Vertex v)
        {
            float dx = v.x - circumCenterX;
            float dy = v.y - circumCenterY;

            float dis = dx * dx + dy * dy;
            return dis< circumRadius2;
        }

        List<GameObject> visaulLines = new List<GameObject>();

        public void SetVisualLines(List<GameObject> gameObjects)
        {
            visaulLines = gameObjects;
        }

        public List<GameObject> GetVisualLines()
        {
            return visaulLines;
        }

    }

}