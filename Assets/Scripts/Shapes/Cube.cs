using Shapes;
using UnityEngine;

public class Cube : Shape
{
    // Start is called before the first frame update
    protected override void Reset()
    {
        base.Reset();
        Type = ShapeType.Cube;
        RequestMeshData();
    }

    private void Start()
    {
        // RequestMeshData();
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