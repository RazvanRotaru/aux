using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    private static List<Vector3> vertices = new List<Vector3>();
    private static List<int> indices = new List<int>();
    private static Vector3 SpawnPoint;
    private static float Size = 1;

    // Start is called before the first frame update
    public static MeshStruct GenerateMesh(ShapeType Type, DetailLevel Details, Vector3 SpawnPoint_)
    {
        MeshStruct MeshInfo = new MeshStruct();
        vertices.Clear();
        indices.Clear();

        SpawnPoint = SpawnPoint_;

        GenerateShape(Type, Details);

        MeshInfo.VertexPosition = vertices.ToArray();
        MeshInfo.indices = indices.ToArray();

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
        int Height = (int)(1.0f / DistanceBetweenRings) + 1;
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
                vertices.Add(ComputeCircleVertexPosition(HeightIndex, Angle, DistanceBetweenRings, Radius - (HeightIndex * DistanceBetweenRings)));
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
        int Height = (int)(1.0f / DistanceBetweenRings) + 1;
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
                vertices.Add(ComputeCircleVertexPosition(HeightIndex, Angle, DistanceBetweenRings, Radius));
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
        int Height = (int)(1.0f / DistanceBetweenRings) + 1;
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
                vertices.Add(ComputeSphereVertexPosition(Alpha, Theta, Radius));
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
        Length = Height = Width = (int)(1.0f / DistanceBetweenVerts) + 1;

        int Counter = 0;
        // Generate the side planes first
        for (int HeightIndex = 0; HeightIndex < Height; HeightIndex++)
        {
            int WidthIndex = 0;
            int LengthIndex = 0;
            Counter = 0;

            for (;WidthIndex < Width; WidthIndex++)
            {
                Vector3 VertexPosition = ComputeCubeVertexPosition(HeightIndex, LengthIndex, WidthIndex, DistanceBetweenVerts);
                vertices.Add(VertexPosition);
                Counter++;
            }
            WidthIndex--;

            for (LengthIndex = 1; LengthIndex < Length; LengthIndex++)
            {
                Vector3 VertexPosition = ComputeCubeVertexPosition(HeightIndex, LengthIndex, WidthIndex, DistanceBetweenVerts);
                vertices.Add(VertexPosition);
                Counter++;
            }
            LengthIndex--;

            for (WidthIndex = WidthIndex - 1; WidthIndex >= 0; WidthIndex--)
            {
                Vector3 VertexPosition = ComputeCubeVertexPosition(HeightIndex, LengthIndex, WidthIndex, DistanceBetweenVerts);
                vertices.Add(VertexPosition);
                Counter++;
            }
            WidthIndex++;

            for (LengthIndex = LengthIndex - 1; LengthIndex >= 1; LengthIndex--)
            {
                Vector3 VertexPosition = ComputeCubeVertexPosition(HeightIndex, LengthIndex, WidthIndex, DistanceBetweenVerts);
                vertices.Add(VertexPosition);
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
        float X = SpawnPoint.x - Size / 2.0f + W * Distance;
        float Y = SpawnPoint.y - Size / 2.0f + H * Distance;
        float Z = SpawnPoint.z - Size / 2.0f + L * Distance;

        Vector3 VertexPosition = new Vector3(X, Y, Z);
        return VertexPosition;
    }

    private static Vector3 ComputeCircleVertexPosition(int H, float Angle, float Distance, float Radius)
    {
        float X = SpawnPoint.x + Radius * Mathf.Cos(Angle);
        float Y = SpawnPoint.y - Size / 2.0f + H * Distance;
        float Z = SpawnPoint.z + Radius * Mathf.Sin(Angle);

        Vector3 VertexPosition = new Vector3(X, Y, Z);
        return VertexPosition;
    }

    private static Vector3 ComputeSphereVertexPosition(float Alpha, float Theta, float Radius)
    {
        float X = SpawnPoint.x + Radius * Mathf.Sin(Theta) * Mathf.Cos(Alpha);
        float Y = SpawnPoint.y + Radius * Mathf.Cos(Theta);
        float Z = SpawnPoint.z + Radius * Mathf.Sin(Theta) * Mathf.Sin(Alpha);

        Vector3 VertexPosition = new Vector3(X, Y, Z);
        return VertexPosition;
    }

    private static void AddCenterPoints(ShapeType Type)
    {
        if (Type == ShapeType.SPHERE)
        {
            vertices.Add(SpawnPoint + new Vector3(0.0f, -Size, 0.0f));
            vertices.Add(SpawnPoint + new Vector3(0.0f, Size, 0.0f));
        } else
        {
            vertices.Add(SpawnPoint + new Vector3(0.0f, -Size / 2.0f, 0.0f));
            vertices.Add(SpawnPoint + new Vector3(0.0f, Size / 2.0f, 0.0f));
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
                    indices.Add(HeightIndex * PointsPerLayer + InnerIndex);
                    indices.Add((HeightIndex + 1) * PointsPerLayer + InnerIndex);
                    indices.Add((HeightIndex + 1) * PointsPerLayer + InnerIndex + 1);

                    // Second triangle
                    indices.Add((HeightIndex + 1) * PointsPerLayer + InnerIndex + 1);
                    indices.Add(HeightIndex * PointsPerLayer + InnerIndex + 1);
                    indices.Add(HeightIndex * PointsPerLayer + InnerIndex);
                }
                else
                {
                    // First triangle
                    indices.Add(HeightIndex * PointsPerLayer + InnerIndex);
                    indices.Add((HeightIndex + 1) * PointsPerLayer + InnerIndex);
                    indices.Add((HeightIndex + 1) * PointsPerLayer);

                    // Second triangle
                    indices.Add((HeightIndex + 1) * PointsPerLayer);
                    indices.Add(HeightIndex * PointsPerLayer);
                    indices.Add(HeightIndex * PointsPerLayer + InnerIndex);
                }
            }
        }

        // Draw bottom panel
        for (int InnerIndex = 0; InnerIndex < PointsPerLayer - 1; InnerIndex++)
        {
            indices.Add(InnerIndex + 1);
            indices.Add((LayerLimit + 1) * PointsPerLayer);
            indices.Add(InnerIndex);
        }

        indices.Add(0);
        indices.Add((LayerLimit + 1) * PointsPerLayer);
        indices.Add(PointsPerLayer - 1);

        // Draw top panel
        for (int InnerIndex = 0; InnerIndex < PointsPerLayer - 1; InnerIndex++)
        {
            indices.Add(LayerLimit * PointsPerLayer + InnerIndex);
            indices.Add((LayerLimit + 1) * PointsPerLayer + 1);
            indices.Add(LayerLimit * PointsPerLayer + InnerIndex + 1);
        }

        indices.Add((LayerLimit + 1) * PointsPerLayer - 1);
        indices.Add((LayerLimit + 1) * PointsPerLayer + 1);
        indices.Add(LayerLimit * PointsPerLayer);
    }
}
