using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
    public class DelaunayTriangulation : MonoBehaviour
    {

        private static Triangle CalcSuperTriangle(IEnumerable<Vertex> vertices)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach(var v in vertices)
            {
                minX = Mathf.Min(minX, v.x);
                maxX = Mathf.Max(maxX, v.x);
                minY = Mathf.Min(minY, v.y);
                maxY = Mathf.Max(maxY, v.y);
            }

            int dx = (maxX - minX); int dy = (maxY - minY);
            int dmax = Mathf.Max(dx, dy);

            Vertex v1 = new Vertex(minX - 3 * dmax, minY - dmax);
            Vertex v2 = new Vertex((minX + maxX)/2, maxY + 3 * dmax);
            Vertex v3 = new Vertex(maxX + 3 * dmax, minY - dmax);

            return new Triangle(v1, v2, v3);
        }

        public static HashSet<Triangle> RetTriangles = new HashSet<Triangle>();
        public static IEnumerator TriangulateCoroutine(IEnumerable<Vertex> vertices, bool visualize, Color color, float width = 0, float gapTime = 0)
        {
            Triangle superTriangle = CalcSuperTriangle(vertices);
            HashSet<Triangle> triangulation = new HashSet<Triangle>
            {
                superTriangle
            };

            RetTriangles.Clear();

            foreach (var vertex in vertices)
            {
                HashSet<Triangle> badTriangles = new HashSet<Triangle>();
                foreach (var triangle in triangulation)
                {
                    if (triangle.IsInCircumCircle(vertex))
                        badTriangles.Add(triangle);
                }

                HashSet<Edge> polygon = new HashSet<Edge>();
                foreach (var badTriangle in badTriangles)
                {
                    foreach (var edge in badTriangle.edges)
                    {
                        bool isShared = false;
                        foreach (var otherTriangle in badTriangles)
                        {
                            if (badTriangle == otherTriangle)
                                continue;
                            if (otherTriangle.HasEdge(edge))
                            {
                                isShared = true;
                            }
                        }
                        if (!isShared)
                        {
                            polygon.Add(edge);
                        }
                    }
                }

                if(visualize)
                {
                    foreach(var bad in badTriangles)
                    {
                        foreach (var k in triangulation)
                        {
                            if (k != bad) continue;

                            var lines = k.GetVisualLines();
                            foreach (var line in lines) Destroy(line);
                        }

                        yield return new WaitForSeconds(gapTime);
                    }
                }

                triangulation.ExceptWith(badTriangles);

                foreach (var edge in polygon)
                {
                    var newTri = new Triangle(vertex, edge.a, edge.b);

                    if(visualize)
                    {
                        List<GameObject> lines = new List<GameObject>();
                        foreach (var link in newTri.edges)
                        {
                            Debug.Log("SPAWN LINE");
                            lines.Add(Visualization.Instance.SpawnLineRenderer(link.a.ToVector3(), link.b.ToVector3(), width, color));
                        }
                        newTri.SetVisualLines(lines);

                        yield return new WaitForSeconds(gapTime);
                    }
                    triangulation.Add(newTri);

                }
            }

            if (visualize)
            {
                foreach (var triangle in triangulation)
                {
                    if (triangle.HasSameVertex(superTriangle))
                    {
                        var lines = triangle.GetVisualLines();
                        foreach (var line in lines) Destroy(line);
                        yield return new WaitForSeconds(gapTime);
                    }
                }
            }
            triangulation.RemoveWhere((Triangle t) => t.HasSameVertex(superTriangle));

            RetTriangles = triangulation;
            yield return null;
        }

    }

}