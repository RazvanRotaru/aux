using System;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    private static List<Vector3> _vertices = new List<Vector3>();
    private static List<int> _indices = new List<int>();
    private static float _size = 1f;

    public static MeshStruct GenerateMesh(ShapeType type, DetailLevel details)
    {
        var meshInfo = new MeshStruct();
        _vertices.Clear();
        _indices.Clear();

        GenerateShape(type, details);

        meshInfo.vertexPosition = _vertices.ToArray();
        meshInfo.indices = _indices.ToArray();

        return meshInfo;
    }

    private static void GenerateShape(ShapeType type, DetailLevel details)
    {
        switch (type)
        {
            case ShapeType.Cube:
                GenerateCube();
                break;
            case ShapeType.Cylinder:
                GenerateCylinder(details);
                break;
            case ShapeType.Cone:
                GenerateCone(details);
                break;
            case ShapeType.Sphere:
                GenerateSphere(details);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown type!");
        }
    }

    private static void GenerateCone(DetailLevel details)
    {
        var distanceBetweenRings = DistanceBetweenElements(details);
        const int height = 1;
        float angle = 0;
        var angleStep = Mathf.PI * (distanceBetweenRings * 0.25f);
        var counter = 0;
        const float radius = 1.0f;

        while (angle < 2 * Mathf.PI)
        {
            _vertices.Add(ComputeCircleVertexPosition(angle, radius));
            angle += angleStep;
            counter++;
        }

        AddCenterPoints(ShapeType.Cone);

        var pointsPerRing = counter;

        GenerateTriangles(height, pointsPerRing, ShapeType.Cone);
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

        AddCenterPoints(ShapeType.Cylinder);

        var pointsPerRing = counter;

        GenerateTriangles(height, pointsPerRing, ShapeType.Cylinder);
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

        AddCenterPoints(ShapeType.Sphere);

        var pointsPerRing = counter;

        GenerateTriangles(sphereLayers, pointsPerRing, ShapeType.Sphere);
    }

    private static void  GenerateCube()
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

    private static float DistanceBetweenElements(DetailLevel details)
    {
        var distanceBetweenVerts = 0.0f;
        switch (details)
        {
            case DetailLevel.Low:
                distanceBetweenVerts = _size;
                break;
            case DetailLevel.Medium:
                distanceBetweenVerts = _size / 2.0f;
                break;
            case DetailLevel.High:
                distanceBetweenVerts = _size / 4.0f;
                break;
        }

        return distanceBetweenVerts;
    }

    private static Vector3 ComputeCircleVertexPosition(float angle, float radius, float height = 0f)
    {
        var x = radius * Mathf.Cos(angle);
        var y = -_size * 0.5f + height;
        var z = radius * Mathf.Sin(angle);

        var vertexPosition = new Vector3(x, y, z);
        return vertexPosition;
    }

    private static Vector3 ComputeSphereVertexPosition(float alpha, float theta, float radius)
    {
        var x = radius * Mathf.Sin(theta) * Mathf.Cos(alpha);
        var y = radius * Mathf.Cos(theta);
        var z = radius * Mathf.Sin(theta) * Mathf.Sin(alpha);

        var vertexPosition = new Vector3(x, y, z);
        return vertexPosition;
    }

    private static void AddCenterPoints(ShapeType type)
    {
        if (type == ShapeType.Sphere)
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
        var layerLimit = type == ShapeType.Cone ? height - 1 : height;

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