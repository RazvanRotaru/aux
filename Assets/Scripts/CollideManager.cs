using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shapes;
using UnityEngine;
using Plane = Shapes.Plane;

public class CollideManager : MonoBehaviour
{
    public const float Eps = 1e-3f;
    [SerializeField] private GameObject debugPoint;

    public static CollideManager Instance { get; private set; }
    public GameObject DebugPoint => debugPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Reset()
    {
        debugPoint = Resources.Load("Prefabs/DebugPoint") as GameObject;
        print(debugPoint);
    }

    #region GaussMap Optimization

    private static bool BuildMinkowskiFace(HalfEdge halfEdgeA, HalfEdge halfEdgeB)
    {
        var normalA1 = halfEdgeA.Face.Normal;
        var normalA2 = halfEdgeA.Twin.Face.Normal;
        var edgeA = halfEdgeA.Edge;

        var normalB1 = halfEdgeB.Face.Normal;
        var normalB2 = halfEdgeB.Twin.Face.Normal;
        var edgeB = halfEdgeB.Edge;

        return IsMinkowskiFace(normalA1, normalA2, -normalB1, -normalB2, edgeA, edgeB);
    }

    private static bool IsMinkowskiFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 ba, Vector3 dc)
    {
        var bxa = ba; //Vector3.Cross(b, a).normalized; //ab
        var dxc = dc; //Vector3.Cross(d, c).normalized; //dc

        var cba = Vector3.Dot(c, bxa);
        var dba = Vector3.Dot(d, bxa);
        var adc = Vector3.Dot(a, dxc);
        var bdc = Vector3.Dot(b, dxc);

        return (cba * dba < 0f) && (adc * bdc < 0f) && (cba * bdc > 0f);
    }

    private static float Distance(HalfEdge halfEdgeA, HalfEdge halfEdgeB, CubeCollider cubeA)
    {
        var edgeA = halfEdgeA.Edge;
        var pointA = halfEdgeA.Vertex;

        var edgeB = halfEdgeB.Edge;
        var pointB = halfEdgeB.Vertex;

        if (Mathf.Abs(Mathf.Abs(Vector3.Dot(edgeA.normalized, edgeB.normalized)) - 1f) < Eps)
        {
            return float.MinValue;
        }

        var normal = Vector3.Cross(edgeA, edgeB).normalized;
        if (Vector3.Dot(normal, pointA - cubeA.Center) < 0f)
        {
            normal = -normal;
        }

        return Vector3.Dot(normal, pointB - pointA);
    }

    private static (HalfEdge a, HalfEdge b, float distance) QueryEdgeDirection(CubeCollider cubeA,
        CubeCollider cubeB)
    {
        var shapeA = cubeA.Shape.HalfEdges;
        var shapeB = cubeB.Shape.HalfEdges;
        // Debug.Log($"half edges for {cubeA}: {shapeA.Count}");

        (HalfEdge a, HalfEdge b, float separation) best = (null, null, float.MinValue);

        for (var indexA = 0; indexA < shapeA.Count; indexA += 2) // += 2
        {
            var halfEdgeA = cubeA.Shape.HalfEdges[indexA];

            for (var indexB = 0; indexB < shapeB.Count; indexB += 2) // +=2
            {
                var halfEdgeB = cubeB.Shape.HalfEdges[indexB];

                if (BuildMinkowskiFace(halfEdgeA, halfEdgeB))
                {
                    var separation = Distance(halfEdgeA, halfEdgeB, cubeA);
                    if (separation > best.separation)
                    {
                        best = (halfEdgeA, halfEdgeB, separation);
                        if (separation > 0f)
                        {
                            best.a.Draw();
                            best.b.Draw();
                            return best;
                        }
                    }
                }
            }
        }

        best.a.Draw();
        best.b.Draw();
        // Debug.LogWarning($"maximum separation: {best.separation.ToString(CultureInfo.InvariantCulture)}");
        return best;
    }


    private static (Face face, float distance) QueryFaceDirection(CubeCollider cubeA,
        CubeCollider cubeB)
    {
        var facesA = cubeA.Shape.Faces;

        (Face face, float separation) best = (default, float.MinValue);

        foreach (var faceA in facesA)
        {
            // faceA.Draw();
            var vertexB = cubeB.Shape.GetSupportPoint(-faceA.Normal);
            var distance = Vector3.Dot(faceA.Normal, vertexB - faceA.Points.First());

            if (distance > best.separation)
            {
                best.separation = distance;
                best.face = faceA;
            }
        }

        return best;
    }

    private static bool Overlap(CubeCollider cubeA, CubeCollider cubeB, out List<Vector3> contactPoints)
    {
        contactPoints = null;
        var faceQueryAb = QueryFaceDirection(cubeA, cubeB);
        if (faceQueryAb.distance > 0f)
        {
            // Debug.Log($"Face Detection A->B <color=red>Failed</color>! Distance: {faceQueryAb.distance}");
            return false;
        }

        var faceQueryBa = QueryFaceDirection(cubeB, cubeA);
        if (faceQueryBa.distance > 0f)
        {
            // Debug.Log($"Face Detection B->A <color=red>Failed</color>! Distance: {faceQueryBa.distance}");
            return false;
        }

        // TODO Solve issues
        var EdgeQuery = QueryEdgeDirection(cubeA, cubeB);
        if (EdgeQuery.distance > 0f)
        {
            Debug.Log($"Edge Detection distance: {EdgeQuery.distance}");
            return false;
        }


        // Debug.Log($"best separation {EdgeQuery.distance}");
        contactPoints = new List<Vector3>();

        var x = Mathf.Abs(faceQueryAb.distance) < Mathf.Abs(faceQueryBa.distance);

        var distance = x ? Mathf.Abs(faceQueryAb.distance) : Mathf.Abs(faceQueryBa.distance);
        if (distance > EdgeQuery.distance)
        {
            if (Intersect(EdgeQuery.a.Vertex, EdgeQuery.a.Twin.Vertex, EdgeQuery.b.Vertex, EdgeQuery.b.Twin.Vertex,
                out var contactPoint))
            {
                contactPoints.Add(contactPoint);
            }
        }
        else
        {
            var referenceFace = x ? faceQueryAb.face : faceQueryBa.face;
            var incidentFace = MostAntiParallelFace(x ? cubeB : cubeA, referenceFace);

            {
                referenceFace.Draw();
                // incidentFace.Draw();
            }

            foreach (var sidePlane in referenceFace.SidePlanes)
            {
                sidePlane.Draw();

                contactPoints.AddRange(GenerateContactPoints(sidePlane, incidentFace)
                    .Where(p => Vector3.Dot(p, referenceFace.Normal) > 0f && Intersect(p, referenceFace)));
            }
        }

        foreach (var point in contactPoints)
        {
            var cp = Instantiate(Instance.DebugPoint, point, Quaternion.identity);
            Destroy(cp, 0.05f);
        }

        return true;
    }

    #endregion

    #region Sutherland-Hodgman clipping

    public static bool Intersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        out Vector3 contactPoint)
    {
        // Algorithm is ported from the C algorithm of 
        // Paul Bourke at http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline3d/
        contactPoint = Vector3.negativeInfinity;

        var ca = a - c;

        var cd = d - c;
        if (cd.sqrMagnitude < Eps)
        {
            return false;
        }

        var ab = b - a;
        if (ab.sqrMagnitude < Eps)
        {
            return false;
        }

        var d1343 = ca.x * cd.x + ca.y * cd.y + ca.z * cd.z;
        var d4321 = cd.x * ab.x + cd.y * ab.y + cd.z * ab.z;
        var d1321 = ca.x * ab.x + ca.y * ab.y + ca.z * ab.z;
        var d4343 = cd.x * cd.x + cd.y * cd.y + cd.z * cd.z;
        var d2121 = ab.x * ab.x + ab.y * ab.y + ab.z * ab.z;

        var denom = d2121 * d4343 - d4321 * d4321;
        if (Mathf.Abs(denom) < Eps)
        {
            return false;
        }

        var numer = d1343 * d4321 - d1321 * d4343;

        var mua = numer / denom;
        if (mua < Eps || mua > 1f - Eps)
        {
            return false;
        }

        var mub = (d1343 + d4321 * mua) / d4343;
        if (mub < Eps || mub > 1f - Eps)
        {
            return false;
        }

        contactPoint.x = a.x + mua * ab.x;
        contactPoint.y = a.y + mua * ab.y;
        contactPoint.z = a.z + mua * ab.z;

        return true;
    }

    private static bool Intersect(Vector3 point, Face face)
    {
        var intersections = 0;
        var dir = (face.Center - point).normalized;
        if (Vector3.Dot(dir, -face.Normal) > Eps)
        {
            return false;
        }

        var p0 = point + dir * 100f;
        Debug.DrawLine(point, p0, Color.blue, 0.02f, false);

        var a = face.Points.Last();
        foreach (var b in face.Points)
        {
            if (Intersect(point, p0, a, b, out var contact))
            {
                intersections += 1;
            }

            a = b;
        }

        return (intersections & 1) == 1;
    }

    // Create Plane class and use it here
    private static bool Intersect(Vector3 a, Vector3 b, Plane plane, out Vector3 point)
    {
        // point = Vector3.negativeInfinity;
        // var u = a - b;
        // var dot = Vector3.Dot(plane.Normal, u);
        //
        // // if (Mathf.Abs(dot) > Eps)
        // {
        //     var planePoint = plane.Point;
        //     var w = a - planePoint;
        //     var fac = -Vector3.Dot(planePoint, w) / dot;
        //
        //     if (fac < 0f || fac > 1f)
        //     {
        //         return false;
        //     }
        //
        //     point = a + u * fac;
        //     return true;
        // }
        //
        // return false;

        // if (planeNormal.dot(lineDirection.normalize()) == 0) {
        //     return null;
        // }
        //
        // double t = (planeNormal.dot(planePoint) - planeNormal.dot(linePoint)) / planeNormal.dot(lineDirection.normalize());
        // return linePoint.plus(lineDirection.normalize().scale(t));

        point = Vector3.negativeInfinity;
        var ab = b - a;
        var dot = Vector3.Dot(plane.Normal, ab.normalized);
        if (Mathf.Abs(dot) < Eps)
        {
            return false;
        }

        var t = (Vector3.Dot(plane.Normal, plane.Point) - Vector3.Dot(plane.Normal, a)) / dot;
        point = a + ab.normalized * t;
        return true;
    }

    private static IEnumerable<Vector3> GenerateContactPoints(Plane plane, Face incidentFace)
    {
        const int inFront = 1;
        const int behind = -1;

        Func<Vector3, int> planeSide = point =>
            Vector3.Dot(point - plane.Point, plane.Normal) > 0 ? inFront : behind;


        var points = new List<Vector3>();

        // Use incident planes instead of faces.
        var a = incidentFace.Points.Last();
        var aSide = planeSide(a);


        foreach (var b in incidentFace.Points)
        {
            var bSide = planeSide(b);

            if (aSide == behind && bSide == inFront)
            {
                if (Intersect(a, b, plane, out var contactPoint))
                {
                    points.Add(contactPoint);
                }
            }
            else if (aSide == inFront && bSide == behind)
            {
                if (Intersect(a, b, plane, out var contactPoint))
                {
                    points.Add(contactPoint);
                }

                points.Add(b);
            }
            else if (aSide == behind && bSide == behind)
            {
                points.Add(b);
            }

            a = b;
            aSide = bSide;
        }

        return points;
    }

    static Face MostAntiParallelFace(CubeCollider polygon, Face refFace)
    {
        var faces = polygon.Shape.Faces;
        // Debug.Log($"number of faces for {polygon}: {faces.Count}");
        var mapf = (face: faces[0], dot: float.MinValue, distance: float.MaxValue);

        foreach (var face in faces)
        {
            var dot = Vector3.Dot(face.Normal, -refFace.Normal);
            if (dot >= mapf.dot)
            {
                // var Distance = Vector3.Distance(Face.HalfEdge.Vertex.Point, refFace.HalfEdge.Vertex.Point);
                // if (Distance < Mapf.distance)
                // {
                //     Mapf = (Face, Dot, Distance);
                // }

                mapf.face = face;
                mapf.dot = dot;
            }
        }

        return mapf.face;
    }

    #endregion

    /// Firstly, implement lowest detail level, afterwards, we can try raising it.
    /// (return the bounding vertexes on creation)
    [SerializeField] private List<CubeCollider> cubeColliders;

    private void Start()
    {
        cubeColliders = FindObjectsOfType<CubeCollider>().ToList();
    }

    private void Update()
    {
        var others = new List<CubeCollider>();
        foreach (var a in cubeColliders)
        {
            var isColliding = false;
            others.Clear();
            foreach (var b in cubeColliders)
            {
                if (a.GetInstanceID() != b.GetInstanceID())
                {
                    if (Overlap(a, b, out var contactPoints))
                    {
                        others.Add(b);
                        isColliding = true;
                    }
                }
            }

            var debugText = isColliding
                ? "<color=green>is colliding</color>"
                : "<color=red>is NOT colliding</color>";
            Debug.Log($"{a} {debugText} with {others}: ");
            a.Collides(isColliding);
        }
    }
}