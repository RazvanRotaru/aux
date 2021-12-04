using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shapes;
using UnityEngine;
using Object = UnityEngine.Object;
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

    public static bool SphereVsSphere(SphereCollider a, SphereCollider b)
    {
        Vector3 aCenter = a.Center;
        float aRadius = a.Radius;

        Vector3 bCenter = b.Center;
        float bRadius = b.Radius;

        // Calculate squared distance between centers
        Vector3 distance = aCenter - bCenter;
        float dist2 = Vector3.Dot(distance, distance);

        // Spheres intersect if squared distance is less than squared sum of radii
        float radiusSum = aRadius + bRadius;
        return dist2 <= radiusSum * radiusSum;
    }

    public static bool SphereVsOOB(SphereCollider a, PolygonCollider b)
    {
        Vector3 localForward = b.transform.InverseTransformDirection(b.transform.forward);
        Vector3 localRight = b.transform.InverseTransformDirection(b.transform.right);
        Vector3 localUp = b.transform.InverseTransformDirection(b.transform.up);

        float minX = (b.Center - (localRight * b.HalfSize.x)).x;
        float maxX = (b.Center + (localRight * b.HalfSize.x)).x;
        float minY = (b.Center - (localUp * b.HalfSize.y)).y;
        float maxY = (b.Center + (localUp * b.HalfSize.y)).y;
        float minZ = (b.Center - (localForward * b.HalfSize.z)).z;
        float maxZ = (b.Center + (localForward * b.HalfSize.z)).z;

        float x = Mathf.Max(minX, Mathf.Min(a.Center.x, maxX));
        float y = Mathf.Max(minY, Mathf.Min(a.Center.y, maxY));
        float z = Mathf.Max(minZ, Mathf.Min(a.Center.z, maxZ));

        float distance = Vector3.Distance(new Vector3(x, y, z), a.Center);

        return distance < a.Radius;
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
        var bxa = ba.normalized; //Vector3.Cross(b, a).normalized; //ab
        var dxc = dc.normalized; //Vector3.Cross(d, c).normalized; //dc

        var cba = Vector3.Dot(c, bxa);
        var dba = Vector3.Dot(d, bxa);
        var adc = Vector3.Dot(a, dxc);
        var bdc = Vector3.Dot(b, dxc);

        return (cba * dba < 0f) && (adc * bdc < 0f) && (cba * bdc > 0f);
    }

    private static float Distance(HalfEdge halfEdgeA, HalfEdge halfEdgeB, PolygonCollider cubeA)
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

    private static (HalfEdge a, HalfEdge b, float distance) QueryEdgeDirection(PolygonCollider cubeA,
        PolygonCollider cubeB)
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
                            {
                                var center = (halfEdgeA.Vertex + halfEdgeA.Twin.Vertex) * 0.5f;
                                var n = Vector3.Cross(halfEdgeA.Edge, -halfEdgeB.Edge);

                                Debug.DrawLine(center, center + n, Color.magenta, 0.02f, false);
                            }
                            best.a.Draw(Color.Lerp(Color.red, Color.green, 1f / (separation * 10f + 1f)));
                            best.b.Draw(Color.Lerp(Color.red, Color.green, 1f / (separation * 10f + 1f)));
                            return best;
                        }
                    }
                }
            }
        }

        // Debug.LogWarning($"maximum separation: {best.separation.ToString(CultureInfo.InvariantCulture)}");
        return best;
    }


    private static (Face face, float distance) QueryFaceDirection(PolygonCollider cubeA,
        PolygonCollider cubeB)
    {
        var facesA = cubeA.Shape.Faces;

        (Face face, float separation) best = (default, float.MinValue);

        foreach (var faceA in facesA)
        {
            faceA.Draw(Color.cyan);
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

    public static bool OOBVsOOB(PolygonCollider cubeA, PolygonCollider cubeB, out List<Vector3> contactPoints)
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

        // // TODO Solve issues
        var EdgeQuery = QueryEdgeDirection(cubeA, cubeB);
        if (EdgeQuery.distance > 0f)
        {
            Debug.Log($"Edge Detection distance: {EdgeQuery.distance}");
            return false;
        }


        // Debug.Log($"best separation {EdgeQuery.distance}");
        contactPoints = new List<Vector3>();

        var x = Mathf.Abs(faceQueryAb.distance) < Mathf.Abs(faceQueryBa.distance);

        // var distance = x ? Mathf.Abs(faceQueryAb.distance) : Mathf.Abs(faceQueryBa.distance);
        // if (Mathf.Abs(EdgeQuery.distance) < Mathf.Abs(distance))
        // {
        //     Debug.Log($"Edge Detection on edges: {EdgeQuery.a}, {EdgeQuery.b}");
        //     
        //     if (Intersect(EdgeQuery.a.Vertex, EdgeQuery.a.Twin.Vertex, EdgeQuery.b.Vertex, EdgeQuery.b.Twin.Vertex,
        //         out var contactPoint))
        //     {
        //         contactPoints.Add(contactPoint);
        //     }
        // }
        // else
        {
            var referenceFace = x ? faceQueryAb.face : faceQueryBa.face;
            var incidentFace = MostAntiParallelFace(x ? cubeB : cubeA, referenceFace);

            {
                referenceFace.Draw(Color.green);
                // incidentFace.Draw();
            }

            foreach (var sidePlane in referenceFace.SidePlanes)
            {
                sidePlane.Draw(Color.yellow);

                contactPoints.AddRange(GenerateContactPoints(sidePlane, incidentFace)
                    .Where(p => Vector3.Dot(p, referenceFace.Normal) > 0f &&
                                Intersect(p, referenceFace))); // && Intersect(p, referenceFace)
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
        if (mua < 0f || mua >= 1f)
        {
            return false;
        }

        var mub = (d1343 + d4321 * mua) / d4343;
        if (mub < 0f || mub >= 1f)
        {
            return false;
        }

        // TODO use ContactPoint struct 
        contactPoint.x = a.x + mua * ab.x;
        contactPoint.y = a.y + mua * ab.y;
        contactPoint.z = a.z + mua * ab.z;

        return true;
    }

    private static bool Intersect(Vector3 point, Face face)
    {
        if (Vector3.Dot((face.Center - point).normalized, -face.Normal) > Eps)
        {
            return false;
        }

        {
            var cp = Instantiate(Instance.DebugPoint, point, Quaternion.identity);
            cp.GetComponent<MeshRenderer>().material.color = Color.green;
            cp.transform.localScale *= 0.75f;
            Destroy(cp, 0.05f);
        }

        var intersections = 0;

        point -= Vector3.Dot(point - face.Center, face.Normal) * face.Normal;
        {
            var cp = Instantiate(Instance.DebugPoint, point, Quaternion.identity);
            cp.GetComponent<MeshRenderer>().material.color = Color.blue;
            cp.transform.localScale *= 0.5f;
            Destroy(cp, 0.05f);
        }
        var dir = (face.Center - point).normalized;


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

        Debug.Log($"point {point} has {intersections} intersections");

        return (intersections & 1) == 1;
    }

    // Create Plane class and use it here
    private static bool Intersect(Vector3 a, Vector3 b, Plane plane, out Vector3 point)
    {
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

    static Face MostAntiParallelFace(PolygonCollider polygon, Face refFace)
    {
        var faces = polygon.Shape.Faces;
        // Debug.Log($"number of faces for {polygon}: {faces.Count}");
        var mapf = (face: faces[0], dot: float.MinValue);

        foreach (var face in faces)
        {
            var dot = Vector3.Dot(face.Normal, -refFace.Normal);
            if (dot >= mapf.dot)
            {
                mapf = (face, dot);
            }
        }

        return mapf.face;
    }

    #endregion

    /// Firstly, implement lowest detail level, afterwards, we can try raising it.
    /// (return the bounding vertexes on creation)
    [SerializeField] private List<Collider> colliders;

    private void Start()
    {
        // colliders = new List<Collider>(FindObjectsOfType<PolygonCollider>());
        var polygonColliders = FindObjectsOfType<PolygonCollider>();
        colliders.AddRange(polygonColliders);

        var sphereColliders = FindObjectsOfType<SphereCollider>().Where(sphereCollider =>
            polygonColliders.All(col => col.gameObject.GetInstanceID() != sphereCollider.gameObject.GetInstanceID()));
        colliders.AddRange(sphereColliders);
    }

    private void Update()
    {
        var others = new List<PolygonCollider>();
        foreach (var a in colliders)
        {
            var isColliding = false;
            others.Clear();
            foreach (var b in colliders)
            {
                if (a.GetInstanceID() != b.GetInstanceID())
                {
                    if (a.IsColliding(b))
                    {
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