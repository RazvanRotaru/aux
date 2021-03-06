using System;
using UnityEngine;
using Object = UnityEngine.Object;

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

        public Vector3 Vertex
        {
            get
            {
                try
                {
                    return Transform.TransformPoint(vertex);
                    // The origin vertex of this half-edge
                }
                catch
                {
                    Debug.Log($"something is fishy with {Transform} and {vertex}");
                    return Vector3.down;
                }
            }
        }

        public Vector3 VertexLocal => vertex;

        public Vector3 Edge => Transform.TransformDirection(EdgeLocal);
        public Vector3 EdgeLocal => Twin.VertexLocal - VertexLocal;

        public HalfEdge(Transform transform, Vector3 vertex)
        {
            Transform = transform;
            if (transform == null)
            {
                Debug.Log("trasnform is null");
            }

            this.vertex = vertex;
        }

        public override string ToString()
        {
            return $"vert: {Vertex}, face: {Face}, twinface: {Twin.Face}";
        }

        public void Draw(Color color)
        {
        
            // var cp = Object.Instantiate(CollideManager.Instance.DebugPoint, Vertex, Quaternion.identity);
            // cp.GetComponent<MeshRenderer>().material.color = color;
            // cp.transform.localScale *= 0.33f;
            // Object.Destroy(cp, 0.05f);
            //
            // var ep = Object.Instantiate(CollideManager.Instance.DebugPoint, Twin.Vertex, Quaternion.identity);
            // ep.GetComponent<MeshRenderer>().material.color = Color.red;
            // ep.transform.localScale *= 0.33f;
            // Object.Destroy(ep, 0.05f);
            //
            // Debug.DrawLine(Vertex, Vertex + EdgeLocal * 1.25f, color, 0.02f, false);
            // Face.Draw(Color.green);
            // Twin.Face.Draw(Color.red);
        }
    }
}