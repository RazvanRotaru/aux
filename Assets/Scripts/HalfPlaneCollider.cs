using System.Collections.Generic;
using Shapes;
using UnityEngine;
using ContactPoint = Shapes.ContactPoint;
using Plane = Shapes.Plane;

public class HalfPlaneCollider : Collider
{
    private Plane plane;
    public Plane Plane => plane;

    protected override void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        plane = new Plane(Vector3.zero, Vector3.up, transform);
    }

    public override bool IsColliding(Collider other, out List<ContactPoint> contactPoints)
    {
        if (other is SphereCollider sphereCollider)
        {
            return CollideManager.HalfPlaneVsObject(plane, sphereCollider, out contactPoints);
        }
        else if (other is PolygonCollider polyonCollider)
        {
            return CollideManager.HalfPlaneVsObject(plane, polyonCollider, out contactPoints);
        }

        contactPoints = null;
        return false;
    }
}