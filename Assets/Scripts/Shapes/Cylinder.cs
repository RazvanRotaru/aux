using System;
using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;

public class Cylinder : Shape
{
    // Start is called before the first frame update
    protected override void Reset()
    {
        base.Reset();
        Type = ShapeType.Cylinder;
        RequestMeshData();
    }

    private void Start()
    {
        Reset();
        PolygonCollider collider = GetComponent<PolygonCollider>();

        if (collider != null)
        {
            collider.GenerateCollider();
        } else
        {
            Debug.LogWarning("Could not get polygon collider in Cube!");
        }
    }
}
