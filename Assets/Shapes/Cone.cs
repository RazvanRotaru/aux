using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cone : Shape
{
    // Start is called before the first frame update
    override protected void Start()
    {
        base.Start();
        Type = ShapeType.CONE;
        RequestMeshData();
    }

    public override void RequestMeshData()
    {
        MeshInfo = MeshGenerator.GenerateMesh(Type, Details, SpawnPoint);
        UpdateMesh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
