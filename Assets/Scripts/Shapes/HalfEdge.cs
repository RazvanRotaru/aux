using System;
using UnityEngine;

namespace Shapes
{
    [Serializable]
    public class HalfEdge : IDrawable
    {
        [SerializeField] private Face face;
        [SerializeField] private Vector3 vertex;

        public Transform Transform { get; }

        public HalfEdge Twin { get; set; } // The matching twin half-edge of the opposing face 
        public HalfEdge Next { get; set; } // The next half-edge counter clockwise

        public Face Face // The face connected to this half-edge
        {
            get => face;
            set => face = value;
        }

        public Vector3 Vertex => Transform.TransformPoint(vertex); // The origin vertex of this half-edge
        public Vector3 VertexLocal => vertex;

        public Vector3 Edge => Transform.TransformDirection(Next.Vertex - Vertex);

        public HalfEdge(Transform transform, Vector3 vertex)
        {
            Transform = transform;
            this.vertex = vertex;
        }

        public override string ToString()
        {
            return $"vert: {Vertex}, face: {Face}";
        }

        public void Draw()
        {
            Debug.DrawLine(Vertex, 2 * Next.Vertex - Vertex, Color.magenta, 0.02f, false);
            Face.Draw();
        }
    }
}