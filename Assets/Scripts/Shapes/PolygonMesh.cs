using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PolygonMesh : Collider
{
    protected virtual void Awake()
    {
        base.Awake();
    }
}
