using System.Collections.Generic;
using System.Linq;
using Shapes;
using UnityEngine;

public static class MeshGenerator
{
    private static List<Vector3> Vertices = new List<Vector3>();
    private static List<int> Indices = new List<int>();
    private static float Size = 1;

    // Start is called before the first frame update
    public static MeshStruct GenerateMesh(ShapeType Type, DetailLevel Details)
    {
        MeshStruct MeshInfo = new MeshStruct();
        Vertices.Clear();
        Indices.Clear();

        GenerateShape(Type, Details);

        MeshInfo.VertexPosition = Vertices.ToArray();
        MeshInfo.indices = Indices.ToArray();

        var halfEdges = GenerateHalfEdges();
        MeshInfo.HalfEdges = halfEdges;

        return MeshInfo;
    }

    private static MeshStruct GenerateShape(ShapeType Type, DetailLevel Details)
    {
        MeshStruct CurrentStruct = new MeshStruct();

        switch (Type)
        {
            case ShapeType.CUBE:
                GenerateCube(Details);
                break;
            case ShapeType.CYLINDER:
                GenerateCylinder(Details);
                break;
            case ShapeType.CONE:
                GenerateCone(Details);
                break;
            case ShapeType.SPHERE:
                GenerateSphere(Details);
                break;
            default:
                break;
        }

        return CurrentStruct;
    }

    private static void GenerateCone(DetailLevel Details)
    {
        float DistanceBetweenRings = DistanceBetweenElements(Details);
        int Height = (int) (1.0f / DistanceBetweenRings) + 1;
        float Angle = 0;
        float AngleStep = Mathf.PI / (Height * 2);
        int Counter = 0;
        float Radius = 1.0f;

        for (int HeightIndex = 0; HeightIndex < Height - 1; HeightIndex++)
        {
            Angle = 0;
            Counter = 0;
            while (Angle < 2 * Mathf.PI)
            {
                Vertices.Add(ComputeCircleVertexPosition(HeightIndex, Angle, DistanceBetweenRings,
                    Radius - (HeightIndex * DistanceBetweenRings)));
                Angle += AngleStep;
                Counter++;
            }
        }

        AddCenterPoints(ShapeType.CONE);

        int PointsPerRing = Counter;

        GenerateTriangles(Height, PointsPerRing, ShapeType.CONE);
    }

    private static void GenerateCylinder(DetailLevel Details)
    {
        float DistanceBetweenRings = DistanceBetweenElements(Details);
        int Height = (int) (1.0f / DistanceBetweenRings) + 1;
        float Angle = 0;
        float AngleStep = Mathf.PI / (Height * 2);
        int Counter = 0;
        float Radius = 1.0f;

        for (int HeightIndex = 0; HeightIndex < Height; HeightIndex++)
        {
            Angle = 0;
            Counter = 0;
            while (Angle < 2 * Mathf.PI)
            {
                Vertices.Add(ComputeCircleVertexPosition(HeightIndex, Angle, DistanceBetweenRings, Radius));
                Angle += AngleStep;
                Counter++;
            }
        }

        AddCenterPoints(ShapeType.CYLINDER);

        int PointsPerRing = Counter;

        GenerateTriangles(Height, PointsPerRing, ShapeType.CYLINDER);
    }

    private static void GenerateSphere(DetailLevel Details)
    {
        float DistanceBetweenRings = DistanceBetweenElements(Details);
        int Height = (int) (1.0f / DistanceBetweenRings) + 1;
        float Alpha = 0;
        float Theta = Mathf.PI - 0.05f * Mathf.PI;
        float AngleAlphaStep = Mathf.PI / (Height * 2);
        float AngleThetaStep = Mathf.PI / (Height * 4);
        int Counter = 0;
        int SphereLayers = 0;
        float Radius = 1.0f;

        while (Theta >= 0)
        {
            Counter = 0;
            Alpha = 0;

            while (Alpha < 2 * Mathf.PI)
            {
                Vertices.Add(ComputeSphereVertexPosition(Alpha, Theta, Radius));
                Alpha += AngleAlphaStep;
                Counter++;
            }

            Theta -= AngleThetaStep;
            SphereLayers++;
        }

        AddCenterPoints(ShapeType.SPHERE);

        int PointsPerRing = Counter;

        GenerateTriangles(SphereLayers, PointsPerRing, ShapeType.SPHERE);
    }

    private static void GenerateCube(DetailLevel Details)
    {
        float DistanceBetweenVerts = DistanceBetweenElements(Details);

        int Height;
        int Width;
        int Length;
        Length = Height = Width = (int) (1.0f / DistanceBetweenVerts) + 1;

        int Counter = 0;
        // Generate the side planes first
        for (int HeightIndex = 0; HeightIndex < Height; HeightIndex++)
        {
            int WidthIndex = 0;
            int LengthIndex = 0;
            Counter = 0;

            for (; WidthIndex < Width; WidthIndex++)
            {
                Vector3 VertexPosition =
                    ComputeCubeVertexPosition(HeightIndex, LengthIndex, WidthIndex, DistanceBetweenVerts);
                Vertices.Add(VertexPosition);
                Counter++;
            }

            WidthIndex--;

            for (LengthIndex = 1; LengthIndex < Length; LengthIndex++)
            {
                Vector3 VertexPosition =
                    ComputeCubeVertexPosition(HeightIndex, LengthIndex, WidthIndex, DistanceBetweenVerts);
                Vertices.Add(VertexPosition);
                Counter++;
            }

            LengthIndex--;

            for (WidthIndex = WidthIndex - 1; WidthIndex >= 0; WidthIndex--)
            {
                Vector3 VertexPosition =
                    ComputeCubeVertexPosition(HeightIndex, LengthIndex, WidthIndex, DistanceBetweenVerts);
                Vertices.Add(VertexPosition);
                Counter++;
            }

            WidthIndex++;

            for (LengthIndex = LengthIndex - 1; LengthIndex >= 1; LengthIndex--)
            {
                Vector3 VertexPosition =
                    ComputeCubeVertexPosition(HeightIndex, LengthIndex, WidthIndex, DistanceBetweenVerts);
                Vertices.Add(VertexPosition);
                Counter++;
            }
        }

        AddCenterPoints(ShapeType.CUBE);

        int InnerSlice = (Length - 2) * (Width - 2) > 0 ? (Length - 2) * (Width - 2) : 0;
        int VertsPerLevel = Length * Width - InnerSlice;

        GenerateTriangles(Height, VertsPerLevel, ShapeType.SPHERE);
    }

    private static float DistanceBetweenElements(DetailLevel Details)
    {
        float DistanceBetweenVerts = 0.0f;
        switch (Details)
        {
            case DetailLevel.LOW:
                DistanceBetweenVerts = Size;
                break;
            case DetailLevel.MEDIUM:
                DistanceBetweenVerts = Size / 2.0f;
                break;
            case DetailLevel.HIGH:
                DistanceBetweenVerts = Size / 4.0f;
                break;
        }

        return DistanceBetweenVerts;
    }

    private static Vector3 ComputeCubeVertexPosition(int H, int L, int W, float Distance)
    {
        float X = -Size / 2.0f + W * Distance;
        float Y = -Size / 2.0f + H * Distance;
        float Z = -Size / 2.0f + L * Distance;

        Vector3 VertexPosition = new Vector3(X, Y, Z);
        return VertexPosition;
    }

    private static Vector3 ComputeCircleVertexPosition(int H, float Angle, float Distance, float Radius)
    {
        float X = Radius * Mathf.Cos(Angle);
        float Y = -Size / 2.0f + H * Distance;
        float Z = +Radius * Mathf.Sin(Angle);

        Vector3 VertexPosition = new Vector3(X, Y, Z);
        return VertexPosition;
    }

    private static Vector3 ComputeSphereVertexPosition(float Alpha, float Theta, float Radius)
    {
        float X = Radius * Mathf.Sin(Theta) * Mathf.Cos(Alpha);
        float Y = Radius * Mathf.Cos(Theta);
        float Z = Radius * Mathf.Sin(Theta) * Mathf.Sin(Alpha);

        Vector3 VertexPosition = new Vector3(X, Y, Z);
        return VertexPosition;
    }

    private static void AddCenterPoints(ShapeType Type)
    {
        if (Type == ShapeType.SPHERE)
        {
            Vertices.Add(new Vector3(0.0f, -Size, 0.0f));
            Vertices.Add(new Vector3(0.0f, Size, 0.0f));
        }
        else
        {
            Vertices.Add(new Vector3(0.0f, -Size / 2.0f, 0.0f));
            Vertices.Add(new Vector3(0.0f, Size / 2.0f, 0.0f));
        }
    }

    private static void GenerateTriangles(int Height, int PointsPerLayer, ShapeType Type)
    {
        // Compute the number of layers that we have to connect
        int LayerLimit = Type == ShapeType.CONE ? Height - 2 : Height - 1;

        // Connect the rings
        for (int HeightIndex = 0; HeightIndex < LayerLimit; HeightIndex++)
        {
            for (int InnerIndex = 0; InnerIndex < PointsPerLayer; InnerIndex++)
            {
                if (InnerIndex != PointsPerLayer - 1)
                {
                    // First triangle
                    Indices.Add(HeightIndex * PointsPerLayer + InnerIndex);
                    Indices.Add((HeightIndex + 1) * PointsPerLayer + InnerIndex);
                    Indices.Add((HeightIndex + 1) * PointsPerLayer + InnerIndex + 1);

                    // Second triangle
                    Indices.Add((HeightIndex + 1) * PointsPerLayer + InnerIndex + 1);
                    Indices.Add(HeightIndex * PointsPerLayer + InnerIndex + 1);
                    Indices.Add(HeightIndex * PointsPerLayer + InnerIndex);
                }
                else
                {
                    // First triangle
                    Indices.Add(HeightIndex * PointsPerLayer + InnerIndex);
                    Indices.Add((HeightIndex + 1) * PointsPerLayer + InnerIndex);
                    Indices.Add((HeightIndex + 1) * PointsPerLayer);

                    // Second triangle
                    Indices.Add((HeightIndex + 1) * PointsPerLayer);
                    Indices.Add(HeightIndex * PointsPerLayer);
                    Indices.Add(HeightIndex * PointsPerLayer + InnerIndex);
                }
            }
        }

        // Draw bottom panel
        for (int InnerIndex = 0; InnerIndex < PointsPerLayer - 1; InnerIndex++)
        {
            Indices.Add(InnerIndex + 1);
            Indices.Add((LayerLimit + 1) * PointsPerLayer);
            Indices.Add(InnerIndex);
        }

        Indices.Add(0);
        Indices.Add((LayerLimit + 1) * PointsPerLayer);
        Indices.Add(PointsPerLayer - 1);

        // Draw top panel
        for (int InnerIndex = 0; InnerIndex < PointsPerLayer - 1; InnerIndex++)
        {
            Indices.Add(LayerLimit * PointsPerLayer + InnerIndex);
            Indices.Add((LayerLimit + 1) * PointsPerLayer + 1);
            Indices.Add(LayerLimit * PointsPerLayer + InnerIndex + 1);
        }

        Indices.Add((LayerLimit + 1) * PointsPerLayer - 1);
        Indices.Add((LayerLimit + 1) * PointsPerLayer + 1);
        Indices.Add(LayerLimit * PointsPerLayer);
    }

    private static List<HalfEdge> GenerateHalfEdges()
    {
        var halfEdges = new Dictionary<(int, int), HalfEdge>();
        var edges = new List<(int u, int v)> {(0, 1), (1, 2), (2, 0)};

        var size = Indices.Count;
        for (var i = 0; i < size; i += 3)
        {
            var A = Vertices[i];
            var B = Vertices[i + 1];
            var C = Vertices[i + 2];

            foreach (var edge in edges)
            {
                var halfEdge = new HalfEdge();
                halfEdges[edge] = halfEdge;
                halfEdges[edge].Face = new Face(halfEdge, A, B, C);
            }

            foreach (var edge in edges)
            {
                halfEdges[edge].Next = halfEdges[((edge.u + 1) % 3, (edge.v + 1) % 3)];
                var twinEdge = (edge.v, edge.u);

                if (halfEdges.ContainsKey(twinEdge))
                {
                    halfEdges[edge].Twin = halfEdges[twinEdge];
                    halfEdges[twinEdge].Twin = halfEdges[edge];
                }
            }
        }

        return halfEdges.Values.ToList();
    }
}