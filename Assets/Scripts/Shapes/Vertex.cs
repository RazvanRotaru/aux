using UnityEngine;

namespace Shapes
{
    [System.Serializable]
    public struct Vertex
    {
        public Vector3 Point;
        public HalfEdge HalfEdge;
    }
}