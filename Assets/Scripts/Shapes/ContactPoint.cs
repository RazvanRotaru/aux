using UnityEngine;

namespace Shapes
{
    public struct ContactPoint : IDrawable
    {
        public Vector3 Point;
        public Vector3 Normal;

        public ContactPoint(Vector3 point, Vector3 normal)
        {
            Point = point;
            Normal = normal;
        }

        public void Draw()
        {
            var cp = Object.Instantiate(CollideManager.Instance.DebugPoint, Point, Quaternion.identity);
            Object.Destroy(cp, 0.05f);
        }
    }
}