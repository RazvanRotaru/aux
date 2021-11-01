using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public abstract class Shape : MonoBehaviour
{
    protected Mesh ShapeMesh;
    protected MeshFilter ShapeMeshFilter;
    protected MeshRenderer ShapeMeshRenderer;
    protected ShapeType Type;
    protected MeshStruct MeshInfo;
    protected Vector3 SpawnPoint;
    protected DetailLevel Details;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        ShapeMesh = new Mesh();
        ShapeMeshFilter = GetComponent<MeshFilter>();
        ShapeMeshRenderer = GetComponent<MeshRenderer>();
    }

    public abstract void RequestMeshData();

    public void SetInfo(DetailLevel Details_, Vector3 SpawnPoint_)
    {
        Details = Details_;
        SpawnPoint = SpawnPoint_;
    }

    protected void UpdateMesh()
    {
        ShapeMesh.Clear();
        ShapeMesh.vertices = MeshInfo.VertexPosition;
        ShapeMesh.triangles = MeshInfo.indices;
        ShapeMeshFilter.mesh = ShapeMesh;
    }
}
