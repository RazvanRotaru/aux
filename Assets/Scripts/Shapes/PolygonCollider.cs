using UnityEngine;

namespace Shapes
{
    [RequireComponent(typeof(SphereCollider))]
    public class PolygonCollider : Collider
    {
        private Vector3 minAxes;
        private Vector3 maxAxes;
        private Vector3 halfSizes;
        private bool isNear;

        public SphereCollider SphereCollider { get; private set; }

        public Shape Shape => shape;

        public Vector3 Center => transform.position;

        public Vector3 MinAxes => minAxes;
        public Vector3 MaxAxes => minAxes;

        public Vector3 HalfSize => halfSizes;

        protected override void Awake()
        {
            base.Awake();
            Debug.Assert(meshFilter.mesh.vertices.Length > 0, "Cannot create cube collider from 0 vertices!");

            SphereCollider = GetComponent<SphereCollider>();
            ComputeProperties(meshFilter.mesh.vertices);
        }

        public override bool IsColliding(Collider other)
        {
            if (other is SphereCollider sphereCollider)
            {
                return CollideManager.SphereVsOOB(sphereCollider, this);
            }
            else if (other is PolygonCollider polygonCollider)
            {
                if (CollideManager.SphereVsSphere(SphereCollider, polygonCollider.SphereCollider))
                {
                    isNear = true;
                    return CollideManager.OOBVsOOB(this, polygonCollider, out var contactPoints);
                }
            }

            return false;
        }

        void ComputeProperties(Vector3[] vertices)
        {
            minAxes = vertices[0];
            maxAxes = vertices[0];

            for (int i = 1; i < vertices.Length; ++i)
            {
                minAxes.x = minAxes.x > vertices[i].x ? vertices[i].x : minAxes.x;
                minAxes.y = minAxes.y > vertices[i].y ? vertices[i].y : minAxes.y;
                minAxes.z = minAxes.z > vertices[i].z ? vertices[i].z : minAxes.z;
                maxAxes.x = maxAxes.x < vertices[i].x ? vertices[i].x : maxAxes.x;
                maxAxes.y = maxAxes.y < vertices[i].y ? vertices[i].z : maxAxes.y;
                maxAxes.z = maxAxes.z < vertices[i].z ? vertices[i].y : maxAxes.z;
            }

            halfSizes.x = (maxAxes.x - minAxes.x) / 2.0f;
            halfSizes.y = (maxAxes.y - minAxes.y) / 2.0f;
            halfSizes.z = (maxAxes.z - minAxes.z) / 2.0f;
        }

        private void OnDrawGizmos()
        {
            if (drawCollider)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position, halfSizes * 2);
            }
        }

        public override void Collides(bool value)
        {
            if (!value && isNear)
            {
                meshRenderer.material = yellow;
                isNear = false;
            }
            else
            {
                base.Collides(value);
            }
        }
    }
}