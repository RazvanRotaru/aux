using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class SphereCollider : Collider
{
    private float radius;

    public Vector3 Center => transform.position;

    public float Radius => radius;

    protected override void Awake()
    {
        base.Awake();
        Debug.Assert(meshFilter.mesh.vertices.Length > 0, "Cannot generate collider from empty mesh!");

        ComputeRadiusFromMesh(meshFilter.mesh.vertices);
    }

    private void ComputeRadiusFromMesh(Vector3[] vertices)
    {
        Vector3 centerPoint = new Vector3(0.0f, 0.0f, 0.0f);
        radius = Vector3.Distance(centerPoint, vertices[0]);

        for (int i = 1; i < vertices.Length; i++)
        {
            float candidate = Vector3.Distance(centerPoint, vertices[i]);
            radius = radius < candidate ? candidate : radius;
        }
    }

    public void Collides(bool value)
    {
        meshRenderer.material = value ? red : blue;
    }

    public override bool IsColliding(Collider other)
    {
        return other switch
        {
            SphereCollider sphereCollider => CollideManager.SphereVsSphere(sphereCollider, this),
            PolygonCollider polygonCollider => CollideManager.SphereVsOOB(this, polygonCollider),
            _ => false
        };
    }

    private void OnDrawGizmos()
    {
        if (drawCollider && radius > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}