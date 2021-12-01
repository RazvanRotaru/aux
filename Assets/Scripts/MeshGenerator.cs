using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    private static List<Vector3> _vertices = new List<Vector3>();
    private static List<int> _indices = new List<int>();
    private static float _size = 1f;

    public static MeshStruct GenerateMesh(ShapeType type, DetailLevel details)
    {
        var MeshInfo = new MeshStruct();
        _vertices.Clear();
        _indices.Clear();

        GenerateShape(type, details);

        MeshInfo.VertexPosition = _vertices.ToArray();
        MeshInfo.Indices = _indices.ToArray();

        return MeshInfo;
    }

    private static void GenerateShape(ShapeType type, DetailLevel details)
    {
        var CurrentStruct = new MeshStruct();

        switch (type)
        {
            case ShapeType.CUBE:
                GenerateCube(details);
                break;
            case ShapeType.CYLINDER:
                GenerateCylinder(details);
                break;
            case ShapeType.CONE:
                GenerateCone(details);
                break;
            case ShapeType.SPHERE:
                GenerateSphere(details);
                break;
            default:
                break;
        }
    }

    private static void GenerateCone(DetailLevel details)
    {
        var DistanceBetweenRings = DistanceBetweenElements(details);
        const int height = 1;
        float Angle = 0;
        var AngleStep = Mathf.PI * (DistanceBetweenRings * 0.25f);
        var Counter = 0;
        const float radius = 1.0f;

        while (Angle < 2 * Mathf.PI)
        {
            _vertices.Add(ComputeCircleVertexPosition(Angle, radius));
            Angle += AngleStep;
            Counter++;
        }

        AddCenterPoints(ShapeType.CONE);

        var PointsPerRing = Counter;

        GenerateTriangles(height, PointsPerRing, ShapeType.CONE);
    }

    private static void GenerateCylinder(DetailLevel details)
    {
        var distanceBetweenRings = DistanceBetweenElements(details);
        var height = (int) _size;
        float angle = 0;
        var angleStep = Mathf.PI * (distanceBetweenRings * 0.25f);
        var counter = 0;
        const float radius = 1f;

        while (angle < 2 * Mathf.PI)
        {
            _vertices.Add(ComputeCircleVertexPosition(angle, radius));
            angle += angleStep;
            counter++;
        }

        angle = 0;
        while (angle < 2 * Mathf.PI)
        {
            _vertices.Add(ComputeCircleVertexPosition(angle, radius, height));
            angle += angleStep;
        }

        AddCenterPoints(ShapeType.CYLINDER);

        var PointsPerRing = counter;

        GenerateTriangles(height, PointsPerRing, ShapeType.CYLINDER);
    }

    private static void GenerateSphere(DetailLevel details)
    {
        var distanceBetweenRings = DistanceBetweenElements(details);
        var theta = Mathf.PI - 0.05f * Mathf.PI;
        var angleAlphaStep = Mathf.PI * (distanceBetweenRings * 0.25f);
        var angleThetaStep = Mathf.PI * (distanceBetweenRings * 0.125f);
        var counter = 0;
        var sphereLayers = 0;
        var radius = _size;

        while (theta >= 0)
        {
            counter = 0;
            float alpha = 0;

            while (alpha < 2 * Mathf.PI)
            {
                _vertices.Add(ComputeSphereVertexPosition(alpha, theta, radius));
                alpha += angleAlphaStep;
                counter++;
            }

            theta -= angleThetaStep;
            sphereLayers++;
        }

        AddCenterPoints(ShapeType.SPHERE);

        var pointsPerRing = counter;

        GenerateTriangles(sphereLayers, pointsPerRing, ShapeType.SPHERE);
    }

    private static void GenerateCube(DetailLevel details)
    {
        var height = _size * 0.5f;
        var width = _size * 0.5f;
        var length = _size * 0.5f;

        _vertices.AddRange(new List<Vector3>
        {
            new Vector3(-width, -height, -length),
            new Vector3(width, -height, -length),
            new Vector3(width, height, -length),
            new Vector3(-width, height, -length),
            new Vector3(-width, height, length),
            new Vector3(width, height, length),
            new Vector3(width, -height, length),
            new Vector3(-width, -height, length),
        });

        _indices.AddRange(new List<int>
        {
            0, 2, 1, //face front
            0, 3, 2,
            2, 3, 4, //face top
            2, 4, 5,
            1, 2, 5, //face right
            1, 5, 6,
            0, 7, 4, //face left
            0, 4, 3,
            5, 4, 7, //face back
            5, 7, 6,
            0, 6, 7, //face bottom
            0, 1, 6
        });
    }

    private static float DistanceBetweenElements(DetailLevel Details)
    {
        var DistanceBetweenVerts = 0.0f;
        switch (Details)
        {
            case DetailLevel.LOW:
                DistanceBetweenVerts = _size;
                break;
            case DetailLevel.MEDIUM:
                DistanceBetweenVerts = _size / 2.0f;
                break;
            case DetailLevel.HIGH:
                DistanceBetweenVerts = _size / 4.0f;
                break;
        }

        return DistanceBetweenVerts;
    }

    private static Vector3 ComputeCubeVertexPosition(int H, int L, int W, float Distance)
    {
        var X = -_size * 0.5f + W * Distance;
        var Y = -_size * 0.5f + H * Distance;
        var Z = -_size * 0.5f + L * Distance;

        var VertexPosition = new Vector3(X, Y, Z);
        return VertexPosition;
    }

    private static Vector3 ComputeCircleVertexPosition(float angle, float radius, float height = 0f)
    {
        var X = radius * Mathf.Cos(angle);
        var Y = -_size * 0.5f + height;
        var Z = radius * Mathf.Sin(angle);

        var VertexPosition = new Vector3(X, Y, Z);
        return VertexPosition;
    }

    private static Vector3 ComputeSphereVertexPosition(float Alpha, float Theta, float Radius)
    {
        var X = Radius * Mathf.Sin(Theta) * Mathf.Cos(Alpha);
        var Y = Radius * Mathf.Cos(Theta);
        var Z = Radius * Mathf.Sin(Theta) * Mathf.Sin(Alpha);

        var VertexPosition = new Vector3(X, Y, Z);
        return VertexPosition;
    }

    private static void AddCenterPoints(ShapeType Type)
    {
        if (Type == ShapeType.SPHERE)
        {
            _vertices.Add(new Vector3(0.0f, -_size, 0.0f));
            _vertices.Add(new Vector3(0.0f, _size, 0.0f));
        }
        else
        {
            _vertices.Add(new Vector3(0.0f, -_size / 2.0f, 0.0f));
            _vertices.Add(new Vector3(0.0f, _size / 2.0f, 0.0f));
        }
    }

    private static void GenerateTriangles(int height, int pointsPerLayer, ShapeType type)
    {
        // Compute the number of layers that we have to connect
        var layerLimit = type == ShapeType.CONE ? height - 1 : height;

        // Connect the rings
        for (var heightIndex = 0; heightIndex < layerLimit; heightIndex++)
        {
            for (var innerIndex = 0; innerIndex < pointsPerLayer; innerIndex++)
            {
                if (innerIndex != pointsPerLayer - 1)
                {
                    // First triangle
                    _indices.Add(heightIndex * pointsPerLayer + innerIndex);
                    _indices.Add((heightIndex + 1) * pointsPerLayer + innerIndex);
                    _indices.Add((heightIndex + 1) * pointsPerLayer + innerIndex + 1);

                    // Second triangle
                    _indices.Add((heightIndex + 1) * pointsPerLayer + innerIndex + 1);
                    _indices.Add(heightIndex * pointsPerLayer + innerIndex + 1);
                    _indices.Add(heightIndex * pointsPerLayer + innerIndex);
                }
                else
                {
                    // First triangle
                    _indices.Add(heightIndex * pointsPerLayer + innerIndex);
                    _indices.Add((heightIndex + 1) * pointsPerLayer + innerIndex);
                    _indices.Add((heightIndex + 1) * pointsPerLayer);

                    // Second triangle
                    _indices.Add((heightIndex + 1) * pointsPerLayer);
                    _indices.Add(heightIndex * pointsPerLayer);
                    _indices.Add(heightIndex * pointsPerLayer + innerIndex);
                }
            }
        }

        // Draw bottom panel
        var bottomCenterIndex = (layerLimit + 1) * pointsPerLayer;
        for (var innerIndex = 0; innerIndex < pointsPerLayer - 1; innerIndex++)
        {
            _indices.Add(innerIndex + 1);
            _indices.Add(bottomCenterIndex);
            _indices.Add(innerIndex);
        }

        _indices.Add(0);
        _indices.Add(bottomCenterIndex);
        _indices.Add(pointsPerLayer - 1);

        // Draw top panel
        var topCenterIndex = (layerLimit + 1) * pointsPerLayer + 1;
        for (var innerIndex = 0; innerIndex < pointsPerLayer - 1; innerIndex++)
        {
            _indices.Add(layerLimit * pointsPerLayer + innerIndex);
            _indices.Add(topCenterIndex);
            _indices.Add(layerLimit * pointsPerLayer + innerIndex + 1);
        }

        _indices.Add((layerLimit + 1) * pointsPerLayer - 1);
        _indices.Add(topCenterIndex);
        _indices.Add(layerLimit * pointsPerLayer);
    }

}