using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class CubeCollider : PolygonMesh
{
    private Vector3 minAxes;
    private Vector3 maxAxes;
    private Vector3 halfSizes;

    public Shape Shape => shape;

    public Vector3 Center => transform.position;

    public Vector3 MinAxes => minAxes;
    public Vector3 MaxAxes => minAxes;

    public Vector3 HalfSize => halfSizes;

    protected override void Awake()
    {
        base.Awake();
        Debug.Assert(meshFilter.mesh.vertices.Length > 0, "Cannot create cube collider from 0 vertices!");

        ComputeProperties(meshFilter.mesh.vertices);
    }

    void ComputeProperties(Vector3[] vertices)
    {
        minAxes = vertices[0];
        maxAxes = vertices[0];

        for (int i = 1; i < vertices.Length; ++i)
        {
            minAxes.x = minAxes.x > vertices[i].x ? vertices[i].x : minAxes.x;
            minAxes.y = minAxes.y > vertices[i].y ? vertices[i].y : minAxes.y;
            minAxes.z = minAxes.z > vertices[i].z ? vertices[i].z : minAxes.z;
            maxAxes.x = maxAxes.x < vertices[i].x ? vertices[i].x : maxAxes.x;
            maxAxes.y = maxAxes.y < vertices[i].y ? vertices[i].z : maxAxes.y;
            maxAxes.z = maxAxes.z < vertices[i].z ? vertices[i].y : maxAxes.z;
        }

        halfSizes.x = (maxAxes.x - minAxes.x) / 2.0f;
        halfSizes.y = (maxAxes.y - minAxes.y) / 2.0f;
        halfSizes.z = (maxAxes.z - minAxes.z) / 2.0f;
    }

    public void Collides(bool value)
    {
        meshRenderer.material = value ? red : blue;
    }

    private void OnDrawGizmos()
    {
        if (drawCollider)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, halfSizes * 2);
        }
    }
}