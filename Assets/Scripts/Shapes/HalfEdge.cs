using System;
using UnityEngine;

namespace Shapes
{
    [Serializable]
    public class HalfEdge
    {
        public Transform Transform { get; }

        public HalfEdge Twin { get; set; } // The matching twin half-edge of the opposing face 
        public HalfEdge Next; // The next half-edge counter clockwise

        public Face Face; // The face connected to this half-edge
        public Vertex Vertex; // The origin vertex of this half-edge

        public Vector3 Edge => Transform.TransformDirection(Next.Vertex.Point - Vertex.Point);

        public HalfEdge(Transform transform)
        {
            Transform = transform;
        }

        public override string ToString()
        {
            return $"vert: {Vertex}, face: {Face}";
        }
    }
}