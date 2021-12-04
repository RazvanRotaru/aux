using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;

namespace Shapes
{
    public class Plane : IDrawable
    {
        private Transform _transform;

        private Vector3 _point;
        private Vector3 _normal;

        public Vector3 Point => _transform.TransformPoint(_point);
        public Vector3 Normal => _transform.TransformDirection(_normal).normalized;

        public Plane(Vector3 point, Vector3 normal, Transform transform)
        {
            _point = point;
            _normal = normal.normalized;
            _transform = transform;
        }

        public void Draw()
        {
            // var cp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // cp.transform.position = Point;
            // cp.transform.up = Normal;
            //
            // // cp.transform.rotation = Quaternion.FromToRotation(Vector3.up, Normal);
            // // transform.rotation = Quaternion.FromToRotation(Vector3.right, hit.normal);
            // cp.transform.localScale = new Vector3(1f, 0.01f, 1f);
            // var color = Color.yellow;
            // color.a = 0.1f;
            //
            // var meshRenderer = cp.GetComponent<MeshRenderer>();
            // // meshRenderer.material.renderQueue = RenderQueue.Transparent;
            // // meshRenderer.material.SetColor("_Color", color);
            // meshRenderer.material.color = color;
            // Object.Destroy(cp, 0.02f);
            Debug.DrawLine(Point, Point + Normal * 0.5f, Color.yellow, 0.02f, false);
        }
    }
}