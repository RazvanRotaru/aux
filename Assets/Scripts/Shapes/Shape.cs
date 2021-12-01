using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shapes;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public abstract class Shape : MonoBehaviour
{
    protected Mesh ShapeMesh;
    protected MeshFilter ShapeMeshFilter;
    protected MeshRenderer ShapeMeshRenderer;
    protected ShapeType Type;
    [SerializeField] protected MeshStruct MeshInfo;
    [SerializeField] protected Vector3 SpawnPoint;
    [SerializeField] protected DetailLevel Details;

    public List<HalfEdge> HalfEdges => MeshInfo.HalfEdges;
    public List<Face> Faces => HalfEdges.Select(x => x.Face).Distinct().ToList();

    protected virtual void Reset()
    {
        ShapeMesh = new Mesh();
        ShapeMeshFilter = GetComponent<MeshFilter>();
        ShapeMeshRenderer = GetComponent<MeshRenderer>();
    }

    [ContextMenu("Generate Shape")]
    public void RequestMeshData()
    {
        MeshInfo = MeshGenerator.GenerateMesh(Type, Details);
        var halfEdges = GenerateHalfEdges();
        MeshInfo.HalfEdges = halfEdges;

        var s = new StringBuilder();
        halfEdges.ForEach(x => s.Append($"{x}\n"));
        Debug.Log(s);

        UpdateMesh();
    }

    public void SetInfo(DetailLevel Details_, Vector3 SpawnPoint_)
    {
        Details = Details_;
        SpawnPoint = SpawnPoint_;
    }

    public Vector3 GetSupportPoint(Vector3 direction)
    {
        var vert = MeshInfo.VertexPosition;
        var vertices = vert.Length;
        var bestVertex = 0;

        var bestProjection = float.MinValue;

        for (var index = 0; index < vertices; ++index)
        {
            var vertex = transform.TransformDirection(vert[index]);
            var projection = Vector3.Dot(vertex, direction);

            if (projection > bestProjection)
            {
                bestProjection = projection;
                bestVertex = index;
            }
        }

        return transform.TransformPoint(vert[bestVertex]);
    }

    protected void UpdateMesh()
    {
        ShapeMesh.Clear();

        ShapeMesh.vertices = MeshInfo.VertexPosition;
        ShapeMesh.triangles = MeshInfo.Indices;
        ShapeMesh.RecalculateNormals();
        ShapeMesh.Optimize();

        ShapeMeshFilter.mesh = ShapeMesh;
    }

    private List<HalfEdge> GenerateHalfEdges()
    {
        var halfEdges = new Dictionary<(int, int), HalfEdge>();
        var edgeIndices = new List<(int u, int v)> {(0, 1), (1, 2), (2, 0)};

        var _indices = MeshInfo.Indices.ToList();
        var _vertices = MeshInfo.VertexPosition;

        var size = _indices.Count;
        for (var i = 0; i < size; i += 3)
        {
            var A = _indices[i];
            var B = _indices[i + 1];
            var C = _indices[i + 2];
            var triangle = new List<int> {A, B, C};

            foreach (var (U, V) in edgeIndices)
            {
                var edge = (u: triangle[U], v: triangle[V]);
                var halfEdge = new HalfEdge(transform);
                halfEdges[edge] = halfEdge;
                halfEdges[edge].Vertex = new Vertex(halfEdge, _vertices[edge.u]);
                halfEdges[edge].Face = new Face(halfEdge, _vertices[A], _vertices[B], _vertices[C]);
            }

            foreach (var (U, V) in edgeIndices)
            {
                var edge = (u: triangle[U], v: triangle[V]);
                var nextEdge = (u: triangle[(U + 1) % 3], v: triangle[(V + 1) % 3]);
                halfEdges[edge].Next = halfEdges[nextEdge];

                var twinEdge = (edge.v, edge.u);

                if (halfEdges.ContainsKey(twinEdge))
                {
                    halfEdges[edge].Twin = halfEdges[twinEdge];
                    halfEdges[twinEdge].Twin = halfEdges[edge];
                }
            }
        }

        var sortedHalfEdges = new List<HalfEdge>();

        foreach (var halfEdge in halfEdges.Values)
        {
            if (sortedHalfEdges.Contains(halfEdge))
            {
                continue;
            }

            sortedHalfEdges.Add(halfEdge);
            sortedHalfEdges.Add(halfEdge.Twin);
        }

        return sortedHalfEdges.Where(x => x.Face.Normal != Vector3.zero).ToList();
    }
}