using UnityEngine;

namespace Shapes
{
    [System.Serializable]
    public struct Vertex
    {
        private Vector3 _point;

        public Vector3 Point
        {
            get => HalfEdge.Transform.TransformPoint(_point);
            private set => _point = value;
        }

        public HalfEdge HalfEdge;

        public Vertex(HalfEdge halfEdge, Vector3 point) : this()
        {
            HalfEdge = halfEdge;
            Point = point;
        }

        public override string ToString()
        {
            return $"{Point}";
        }
    }
}