using UnityEngine;

namespace Shapes
{
    [System.Serializable]
    public struct Face
    {
        [SerializeField] private Vector3 _normal;
        private (Vector3 A, Vector3 B, Vector3 C) _points;

        public HalfEdge HalfEdge;

        public Vector3 Normal
        {
            get => HalfEdge.Transform.TransformDirection(_normal).normalized;
            private set => _normal = value;
        }

        public (Vector3 A, Vector3 B, Vector3 C) Points
        {
            get => (HalfEdge.Transform.TransformPoint(_points.A), HalfEdge.Transform.TransformPoint(_points.B),
                HalfEdge.Transform.TransformPoint(_points.C));
            private set => _points = value;
        }


        public Face(HalfEdge halfEdge, Vector3 a, Vector3 b, Vector3 c) : this()
        {
            var ab = b - a;
            var ac = c - a;

            Normal = Vector3.Cross(ab, ac).normalized;
            HalfEdge = halfEdge;
            Points = (a, b, c);
        }

        public override string ToString()
        {
            return $"{Normal}";
        }
    }
}