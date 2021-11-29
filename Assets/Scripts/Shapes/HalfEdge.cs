namespace Shapes
{
    [System.Serializable]
    public class HalfEdge
    {
        public HalfEdge Twin; // The matching twin half-edge of the opposing face 
        public HalfEdge Next; // The next half-edge counter clockwise

        public Face Face; // The face connected to this half-edge
        public Vertex Vertex; // The origin vertex of this half-edge
    }
}