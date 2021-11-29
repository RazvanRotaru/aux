using UnityEngine;

namespace Shapes
{
    [System.Serializable]
    public struct Face
    {
        public HalfEdge HalfEdge;
        public Vector3 Normal;

        public Face(HalfEdge halfEdge, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;

            Normal = Vector3.Cross(ab, ac).normalized;
            HalfEdge = halfEdge;
        }
    }
}