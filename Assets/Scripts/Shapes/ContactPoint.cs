using UnityEngine;

namespace Shapes
{
    public struct ContactPoint : IDrawable
    {
        public Vector3 Point;
        public Vector3 Normal;
        public float Penetration;

        public ContactPoint(Vector3 point, Vector3 normal, float penetration)
        {
            Point = point;
            Normal = normal;
            Penetration = penetration;
        }

        public void Draw(Color color)
        {
            var cp = Object.Instantiate(DebugManager.Instance.DebugPoint, Point, Quaternion.identity);
            cp.GetComponent<MeshRenderer>().material.color = color;
            Object.Destroy(cp, 0.05f);
        }
    }
}