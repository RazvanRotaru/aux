using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shapes;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Shape : MonoBehaviour
{
    protected Mesh ShapeMesh;
    protected MeshFilter ShapeMeshFilter;
    protected MeshRenderer ShapeMeshRenderer;
    protected ShapeType Type;
    [SerializeField] private Material red;
    [SerializeField] private Material blue;
    [SerializeField] private Material yellow;
    [SerializeField] protected MeshStruct MeshInfo;
    [SerializeField] protected Vector3 spawnPoint;
    [SerializeField] protected DetailLevel details;

    [SerializeField] private bool debugEdges = false;

    public List<HalfEdge> HalfEdges => MeshInfo.halfEdges;
    public List<Face> Faces => HalfEdges.Select(x => x.Face).Distinct().ToList();


    private int index = 0;

    private void Awake()
    {
        if (GetComponents<Collider>().Length == 0)
        {
            gameObject.AddComponent<SphereCollider>();
        }

        index = 0;
    }

    private void Update()
    {
        if (!debugEdges) return;

        HalfEdges[index].Draw(Color.cyan);
        Debug.Log($"{HalfEdges[index]}");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            index = (index + 1) % HalfEdges.Count;
        }
    }

    public Material GetContactMaterial()
    {
        return red;
    }

    public Material GetNormalMaterial()
    {
        return blue;
    }

    public Material NearMaterial => yellow;

    protected virtual void Reset()
    {
        ShapeMesh = new Mesh();
        ShapeMeshFilter = GetComponent<MeshFilter>();
        ShapeMeshRenderer = GetComponent<MeshRenderer>();
    }

    [ContextMenu("Generate Shape")]
    public void RequestMeshData()
    {
        blue = Resources.Load<Material>("Materials/Blue");
        red = Resources.Load<Material>("Materials/Red");
        yellow = Resources.Load<Material>("Materials/Yellow");

        MeshInfo = MeshGenerator.GenerateMesh(Type, details);
        var halfEdges = GenerateHalfEdges();
        MeshInfo.halfEdges = halfEdges;

        var s = new StringBuilder();
        halfEdges.ForEach(x => s.Append($"{x}\n"));
        Debug.Log(s);

        UpdateMesh();
    }

    public void SetInfo(DetailLevel details, Vector3 spawnPoint)
    {
        this.details = details;
        this.spawnPoint = spawnPoint;
    }

    public Vector3 GetSupportPoint(Vector3 direction)
    {
        var vert = MeshInfo.vertexPosition;
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

    private void UpdateMesh()
    {
        ShapeMesh.Clear();

        ShapeMesh.vertices = MeshInfo.vertexPosition;
        ShapeMesh.triangles = MeshInfo.indices;
        ShapeMesh.RecalculateNormals();
        ShapeMesh.Optimize();

        ShapeMeshFilter.mesh = ShapeMesh;
    }

    private List<HalfEdge> GenerateHalfEdges()
    {
        var halfEdges = new Dictionary<(int, int), HalfEdge>();

        // CCO
        // var edgeIndices = new List<(int u, int v)> {(0, 2), (2, 1), (1, 0)};

        var edgeIndices = new List<(int u, int v)> {(0, 1), (1, 2), (2, 0)};
        var faces = new List<Face>();

        var indices = MeshInfo.indices;
        var vertices = MeshInfo.vertexPosition;

        var size = indices.Length;
        for (var i = 0; i < size; i += 3)
        {
            var a = indices[i];
            var b = indices[i + 1];
            var c = indices[i + 2];
            var triangle = new List<int> {a, b, c};

            foreach (var (u, v) in edgeIndices)
            {
                var edge = (u: triangle[u], v: triangle[v]);
                var halfEdge = new HalfEdge(transform, vertices[edge.u]);
                halfEdges[edge] = halfEdge;
                var face = new Face(halfEdge, vertices[a], vertices[b], vertices[c]);
                try
                {
                    var existingFace = faces.First(f => f.Equals(face));
                    halfEdges[edge].Face = existingFace;
                }
                catch
                {
                    faces.Add(face);
                    halfEdges[edge].Face = face;
                }
            }

            foreach (var (u, v) in edgeIndices)
            {
                var edge = (u: triangle[u], v: triangle[v]);
                var nextEdge = (u: triangle[(u + 1) % 3], v: triangle[(v + 1) % 3]);
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

            // remove diagonal edges
            if (halfEdge.Face.Equals(halfEdge.Twin.Face))
            {
                continue;
            }

            sortedHalfEdges.Add(halfEdge);
            sortedHalfEdges.Add(halfEdge.Twin);
        }

        // Add side planes to faces
        foreach (var halfEdge in sortedHalfEdges)
        {
            halfEdge.Face.AddSidePlane(halfEdge.Twin);
        }

        // Set face centers
        for (var i = 0; i < faces.Count; i++)
        {
            faces[i].SetCenter();
        }

        var edges = sortedHalfEdges.Where(x => x.Face.Normal != Vector3.zero).ToList();

        Debug.Log($"Generated {edges.Count} edges and {faces.Count} faces");
        return edges;
    }
}