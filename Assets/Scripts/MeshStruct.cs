using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct MeshStruct
{
    [FormerlySerializedAs("VertexPosition")] public Vector3[] vertexPosition;
    [FormerlySerializedAs("VertexColors")] public Vector4[] vertexColors;
    [FormerlySerializedAs("VertexNormals")] public Vector3[] vertexNormals;
    [FormerlySerializedAs("HalfEdges")] public List<HalfEdge> halfEdges;

    [FormerlySerializedAs("Indices")] public int[] indices;
}