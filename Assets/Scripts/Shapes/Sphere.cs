public class Sphere : Shape
{
    // Start is called before the first frame update
    protected override void Reset()
    {
        base.Reset();
        Type = ShapeType.Sphere;
        RequestMeshData();
    }
}
