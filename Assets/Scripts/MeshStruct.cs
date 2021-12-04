using System;
using System.Collections.Generic;
using Shapes;
using UnityEngine;

[Serializable]
public struct MeshStruct
{
    public Vector3[] vertexPosition;
    public Vector4[] vertexColors;
    public Vector3[] vertexNormals;
    public List<HalfEdge> halfEdges;

    public int[] indices;
}