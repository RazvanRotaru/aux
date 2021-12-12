using System;
using UnityEngine;

public class Sphere : Shape
{
    // Start is called before the first frame update
    protected override void Reset()
    {
        base.Reset();
        Type = ShapeType.Sphere;
        RequestMeshData();
    }

    private void Start()
    {
        Reset();
        SphereCollider collider = GetComponent<SphereCollider>();

        if (collider != null)
        {
            collider.GenerateCollider();
        } else
        {
            Debug.LogWarning("No Sphere collider attached to sphere object");
        }
    }
}
