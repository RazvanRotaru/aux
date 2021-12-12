using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;
using ContactPoint = Shapes.ContactPoint;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class SphereCollider : Collider
{
    private float radius;

    public Vector3 Center => transform.position;

    public float Radius => radius;

    protected override void Awake()
    {
        base.Awake();

        if (meshFilter.mesh.vertices.Length > 0)
        {
            ComputeRadiusFromMesh(meshFilter.mesh.vertices);
            isGenerated = true;
        }
        else
        {
            Debug.LogWarning($"{this} cannot generate sphere collider from empty mesh");
        }
        //Shape.HalfEdges.ForEach(he => Debug.Assert(he.Transform != null, "A halfedges transform reference is null"));
    }

    public override void GenerateCollider()
    {
        base.GenerateCollider();
        if (!isGenerated)
        {
            ComputeRadiusFromMesh(meshFilter.mesh.vertices);
            isGenerated = true;
        }
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

    public override bool IsColliding(Collider other, out List<ContactPoint> contactPoints)
    {
        contactPoints = null;
        return other switch
        {
            SphereCollider sphereCollider => CollideManager.SphereVsSphere(sphereCollider, this, out contactPoints),
            PolygonCollider polygonCollider => CollideManager.SphereVsOOB(this, polygonCollider),
            HalfPlaneCollider hP => CollideManager.HalfPlaneVsObject(hP.Plane, this, out contactPoints),
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