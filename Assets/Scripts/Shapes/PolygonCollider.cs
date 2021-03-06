using System;
using System.Collections.Generic;
using System.Diagnostics;
using Collisions.GPU;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

        public Vector3 Center => transform.position;

        public Vector3 MinAxes => minAxes;
        public Vector3 MaxAxes => minAxes;

        public Vector3 HalfSize => halfSizes;

        protected override void Awake()
        {
            base.Awake();

            if (meshFilter.mesh.vertices.Length > 0)
            {
                // SphereCollider = GetComponent<SphereCollider>();
                // ComputeProperties(meshFilter.mesh.vertices);
                // isGenerated = true;
            } else
            {
                Debug.LogWarning($"{this} can't generate cube collider from empty mesh!");
            }
        }

        protected override void OnEnable()
        {
            SphereCollider = GetComponent<SphereCollider>();
            // ComputeProperties(meshFilter.mesh.vertices);
            isGenerated = true;
        }

        public override bool IsColliding(Collider other, out List<ContactPoint> contactPoints)
        {
            contactPoints = null;
            if (SphereCollider != null) return false;
            switch (other)
            {
                case SphereCollider sphereCollider:
                    return CollideManager.SphereVsOOB(sphereCollider, this);
                case PolygonCollider polygonCollider
                    when CollideManager.SphereVsSphere(SphereCollider, polygonCollider.SphereCollider, out _):
                    isNear = true;
                    return CollideManager.OOBVsOOB(this, polygonCollider, out contactPoints);
                case HalfPlaneCollider halfPlaneCollider:
                    return CollideManager.HalfPlaneVsObject(halfPlaneCollider.Plane, this, out contactPoints);
                default:
                    return false;
            }
        }

        public override void GenerateCollider()
        {
            base.GenerateCollider();
            if (!isGenerated)
            {
                Debug.Log($"GENERATING CUBE COLLIDER FROM <color=gray> {meshFilter.mesh.vertices.Length} </color> vertices!");
                // SphereCollider = GetComponent<SphereCollider>();
                // SphereCollider.GenerateCollider();
                ComputeProperties(meshFilter.mesh.vertices);
                isGenerated = true;

                // if (CollideManager.Instance != null)
                // {
                //     CollideManager.Instance.AddCollider(gameObject.GetComponent<Shape>());
                // }
                //
                // if (GPUCollideManager.Instance != null)
                // {
                //     GPUCollideManager.Instance.AddCollider(gameObject.GetComponent<Shape>());
                // }
            }
        }

        void ComputeProperties(Vector3[] vertices)
        {
            minAxes = vertices[0];
            maxAxes = vertices[0];

            Debug.Log(vertices[0].y);

            for (int i = 1; i < vertices.Length; ++i)
            {
                // Debug.Log(vertices[i].y);

                minAxes.x = minAxes.x > vertices[i].x ? vertices[i].x : minAxes.x;
                minAxes.y = minAxes.y > vertices[i].y ? vertices[i].y : minAxes.y;
                minAxes.z = minAxes.z > vertices[i].z ? vertices[i].z : minAxes.z;

                maxAxes.x = maxAxes.x < vertices[i].x ? vertices[i].x : maxAxes.x;
                maxAxes.y = maxAxes.y < vertices[i].y ? vertices[i].y : maxAxes.y;
                maxAxes.z = maxAxes.z < vertices[i].z ? vertices[i].z : maxAxes.z;
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

                Gizmos.color = Color.black;
                Vector3 minCorner = transform.position - transform.right * halfSizes.x;
                minCorner -= transform.up * halfSizes.y;
                minCorner -= transform.forward * halfSizes.z;

                Vector3 maxCorner = transform.position + transform.right * halfSizes.x;
                maxCorner += transform.up * halfSizes.y;
                maxCorner += transform.forward * halfSizes.z;

                Gizmos.DrawLine(minCorner, maxCorner);
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(minCorner, 0.2f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(maxCorner, 0.2f);

                Gizmos.color = Color.green;

                Gizmos.DrawLine(minCorner, minCorner + halfSizes.x * transform.right * 2);
                Gizmos.DrawLine(minCorner, minCorner + halfSizes.z * transform.forward * 2);
                Gizmos.DrawLine(minCorner + halfSizes.x * transform.right * 2,
                    minCorner + (halfSizes.x * transform.right + halfSizes.z * transform.forward) * 2);
                Gizmos.DrawLine(minCorner + halfSizes.z * transform.forward * 2,
                    minCorner + (halfSizes.x * transform.right + halfSizes.z * transform.forward) * 2);

                Gizmos.DrawLine(minCorner, minCorner + halfSizes.y * transform.up * 2);
                Gizmos.DrawLine(minCorner + halfSizes.x * transform.right * 2,
                    minCorner + halfSizes.x * transform.right * 2 + halfSizes.y * transform.up * 2);
                Gizmos.DrawLine(minCorner + halfSizes.z * transform.forward * 2,
                    minCorner + halfSizes.z * transform.forward * 2 + halfSizes.y * transform.up * 2);
                Gizmos.DrawLine(minCorner + (halfSizes.x * transform.right + halfSizes.z * transform.forward) * 2,
                    minCorner + (halfSizes.x * transform.right + halfSizes.z * transform.forward) * 2 +
                    halfSizes.y * transform.up * 2);

                Gizmos.DrawLine(maxCorner, maxCorner - halfSizes.x * transform.right * 2);
                Gizmos.DrawLine(maxCorner, maxCorner - halfSizes.z * transform.forward * 2);
                Gizmos.DrawLine(maxCorner - halfSizes.x * transform.right * 2,
                    maxCorner - (halfSizes.x * transform.right + halfSizes.z * transform.forward) * 2);
                Gizmos.DrawLine(maxCorner - halfSizes.z * transform.forward * 2,
                    maxCorner - (halfSizes.x * transform.right + halfSizes.z * transform.forward) * 2);
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