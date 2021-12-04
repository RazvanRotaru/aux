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
    }
}