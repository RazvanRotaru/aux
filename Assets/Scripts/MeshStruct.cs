using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;

public struct MeshStruct
{
    public Vector3[] VertexPosition;
    public Vector4[] VertexColors;
    public Vector3[] VertexNormals;
    public List<HalfEdge> HalfEdges;

    public int[] indices;
}