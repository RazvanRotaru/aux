using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cone : Shape
{
    // Start is called before the first frame update
    protected override void Reset()
    {
        base.Reset();
        Type = ShapeType.CONE;
        RequestMeshData();
    }
}
