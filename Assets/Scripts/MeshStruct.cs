using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;

[System.Serializable]
public struct MeshStruct
{
    public Vector3[] VertexPosition;
    public Vector4[] VertexColors;
    public Vector3[] VertexNormals;
    public List<HalfEdge> HalfEdges;

    public int[] Indices;
}