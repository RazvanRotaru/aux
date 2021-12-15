using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Shapes
{
    public class Face : IDrawable
    {
        [SerializeField] private Vector3 normal;
        [SerializeField] private Vector3 center;
        [SerializeField] private List<Plane> sidePlanes;
        private List<HalfEdge> edges;
        public HalfEdge HalfEdge { get; }

        public List<Plane> SidePlanes => sidePlanes;

        // public List<Face> AdjacentFaces { get; }
        public IEnumerable<HalfEdge> Edges => edges;

        public Vector3 Normal => HalfEdge.Transform.TransformDirection(normal).normalized;
        public Vector3 NormalLocal => normal;

        public Vector3 Center => HalfEdge.Transform.TransformPoint(center);
        public Vector3 CenterLocal => center;

        public List<Vector3> Points => edges.Select(edge => edge.Vertex).ToList();

        public Face(HalfEdge halfEdge, Vector3 a, Vector3 b, Vector3 c)

        {
            var ab = b - a;
            var ac = c - a;

            normal = Vector3.Cross(ab, ac).normalized;
            if (Vector3.Dot(normal, a) < 0) normal *= -1;

            if (normal.magnitude == 0)
            {
                Debug.LogError($"WTF: {a}. {b}. {c}");
            }

            HalfEdge = halfEdge;

            sidePlanes = new List<Plane>();
            edges = new List<HalfEdge> {halfEdge};
        }

        public void AddSidePlane(HalfEdge edge)
        {
            var A = edge.VertexLocal;

            if (edges.All(e => e.Vertex != edge.Vertex))
            {
                edges.Add(edge);
            }

            var N = Vector3.Cross(normal, edge.Next.VertexLocal - edge.VertexLocal).normalized;
            var O = Vector3.zero;
            // try
            // {
            //     O = Points.First(p => p != A);
            // }
            // catch
            // {
            //     O = edges.First(e => e.Next.Vertex != A).Vertex;
            // }

            var OA = A - O;

            if (Vector3.Dot(OA, N) < 0f)
            {
                N *= -1;
            }

            var edgeCenter = (edge.Next.VertexLocal + A) * 0.5f;

            sidePlanes.Add(new Plane(edgeCenter, N, HalfEdge.Transform));
        }

        public void SetCenter()
        {
            center = edges.Aggregate(Vector3.zero, (current, edge) => current + edge.VertexLocal);
            center /= edges.Count;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"N: ({Normal.x:0.000}, {Normal.y:0.000}, {Normal.z:0.000})");
            sb.AppendLine($"Center: {center}");
            // sb.AppendLine("Points: ");
            //
            // foreach (var edge in edges)
            // {
            //     sb.Append($"{edge.Vertex}");
            // }

            return sb.ToString();
        }

        public void Draw(Color color)
        {
            Debug.DrawLine(Center, Center + Normal, color, 0.02f, false);
        }

        public bool Equals(Face other)
        {
            return Vector3.Dot(Normal, other.Normal) > 1f - 1e-6f;
        }
    }
}