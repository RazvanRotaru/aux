using System;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using UnityEngine;
using ContactPoint = Shapes.ContactPoint;
using Plane = Shapes.Plane;

public class CollideManager : MonoBehaviour
{
    private static bool _aux;

    public const float Eps = 1e-3f;
    [SerializeField] private GameObject debugPoint;
    [SerializeField] private bool debugContactPoints;
    private Dictionary<(Collider, Collider), bool> collidingObjects = new Dictionary<(Collider, Collider), bool>();


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

    public static bool HalfPlaneVsObject(Plane plane, PolygonCollider obj, out List<ContactPoint> contactPoints)
    {
        contactPoints = new List<ContactPoint>();

        // Debug.Log($"{plane} collides with {obj}");
        // plane.Draw(Color.green);

        foreach (var face in obj.Shape.Faces)
        {
            contactPoints.AddRange(GenerateContactPoints(plane, face)
                .Select(pos => new ContactPoint(pos, plane.Normal,
                    Mathf.Abs(Vector3.Dot(pos - plane.Point, plane.Normal)))));
        }

        // Debug.Log($"Found {contactPoints.Count} points");
        // foreach (var p in contactPoints)
        // {
        //     var cp = Instantiate(Instance.DebugPoint, p.Point, Quaternion.identity);
        //     cp.GetComponent<MeshRenderer>().material.color = Color.blue;
        //     cp.transform.localScale *= 0.33f;
        //     Destroy(cp, 0.05f);
        // }

        return contactPoints.Count > 0;
    }

    public static bool HalfPlaneVsObject(Plane plane, SphereCollider obj, out List<ContactPoint> contactPoints)
    {
        contactPoints = null;
        var dir = Vector3.Dot(plane.Normal, (plane.Point - obj.Center).normalized) * plane.Normal;
        var p = obj.Center + obj.Radius * dir;

        var OA = obj.Center - plane.Point;
        var OB = p - plane.Point;

        // TODO: optimize or create specific method
        if (Intersect(obj.transform.position, p, plane,
            out var pos)) // Vector3.Dot(OA, plane.Normal) * Vector3.Dot(OB, plane.Normal) < 0f &&
        {
            // Debug.Log($"{plane} collides with {obj}");
            // plane.Draw(Color.green);
            //
            // var cp = Instantiate(Instance.DebugPoint, pos, Quaternion.identity);
            // cp.GetComponent<MeshRenderer>().material.color = Color.blue;
            // cp.transform.localScale *= 0.33f;
            // Destroy(cp, 0.05f);

            contactPoints = new List<ContactPoint>
            {
                new ContactPoint(pos, plane.Normal,
                    Mathf.Abs(Vector3.Dot(pos - plane.Point, plane.Normal)))
            };
            return true;
        }

        return false;
    }


    public static bool SphereVsSphere(SphereCollider a, SphereCollider b, out List<ContactPoint> contactPoints)
    {
        contactPoints = null;

        Vector3 aCenter = a.Center;
        float aRadius = a.Radius;

        Vector3 bCenter = b.Center;
        float bRadius = b.Radius;

        // Calculate squared distance between centers
        Vector3 distance = aCenter - bCenter;
        float dist2 = distance.sqrMagnitude;

        // Spheres intersect if squared distance is less than squared sum of radii
        float radiusSum = aRadius + bRadius;
        var overlaping = dist2 <= radiusSum * radiusSum;

        if (overlaping)
        {
            var point = (aCenter + bCenter) * 0.5f;
            var penetration = Mathf.Abs((a.Center - point).magnitude);
            contactPoints = new List<ContactPoint>
            {
                new ContactPoint(point, aCenter - point, aRadius - (aCenter - point).magnitude)
            };
        }

        return overlaping;
    }

    public static bool SphereVsOOB(SphereCollider a, PolygonCollider b)
    {
        Vector3 center = a.Center;
        Vector3 realCenter = b.transform.InverseTransformPoint(center);

        float dist;
        Vector3 closestPt = new Vector3(0, 0, 0);
        dist = realCenter.x;

        // X axis
        Vector3 halfSize = b.HalfSize;
        if (dist > halfSize.x)
        {
            dist = halfSize.x;
        }

        if (dist < -halfSize.x)
        {
            dist = -halfSize.x;
        }

        closestPt.x = dist;

        // Y axis
        dist = realCenter.y;
        if (dist > halfSize.y)
        {
            dist = halfSize.y;
        }

        if (dist < -halfSize.y)
        {
            dist = -halfSize.y;
        }

        closestPt.y = dist;

        // Z axis
        dist = realCenter.z;
        if (dist > halfSize.z)
        {
            dist = halfSize.z;
        }

        if (dist < -halfSize.z)
        {
            dist = -halfSize.z;
        }

        closestPt.z = dist;

        dist = Vector3.Distance(closestPt, realCenter);

        Debug.Log("Distance: " + dist);

        return dist < a.Radius;
    }

    #region GaussMap Optimization

    private static bool BuildMinkowskiFace(HalfEdge halfEdgeA, HalfEdge halfEdgeB)
    {
        var normalA2 = halfEdgeA.Face.Normal;
        var normalA1 = halfEdgeA.Twin.Face.Normal;
        var edgeA = halfEdgeA.Edge;

        var normalB2 = halfEdgeB.Face.Normal;
        var normalB1 = halfEdgeB.Twin.Face.Normal;
        var edgeB = halfEdgeB.Edge;

        // var dxc = Vector3.Cross(normalB2, normalB1).normalized; //dc
        // if (Vector3.Cross(dxc, edgeB.normalized).magnitude < Eps)
        // {
        //     if (!aux)
        //     {
        //         Debug.Log($"Normal NOT OK: edge {edgeB} vs {dxc} for {normalB1} || {normalB2} ");
        //         halfEdgeB.Draw(Color.blue);
        //         Debug.DrawLine(halfEdgeB.Transform.position, halfEdgeB.Transform.position + 1.5f * dxc, Color.magenta,
        //             0.02f, false);
        //
        //         Debug.DrawLine(halfEdgeB.Transform.position + halfEdgeB.Transform.right * 0.05f,
        //             halfEdgeB.Transform.position + 1.5f * halfEdgeB.Edge + halfEdgeB.Transform.right * 0.05f,
        //             Color.white, 0.02f, false);
        //
        //         halfEdgeA.Draw(Color.blue);
        //         var bxa = Vector3.Cross(normalA2, normalA1).normalized; //dc
        //         Debug.DrawLine(halfEdgeA.Transform.position, halfEdgeA.Transform.position + 1.5f * bxa, Color.magenta,
        //             0.02f, false);
        //
        //         Debug.DrawLine(halfEdgeA.Transform.position + halfEdgeA.Transform.right * 0.05f,
        //             halfEdgeA.Transform.position + 1.5f * halfEdgeA.Edge, Color.white,
        //             0.02f, false);
        //         aux = true;
        //     }
        // }

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

    private static float Distance(HalfEdge halfEdgeA, HalfEdge halfEdgeB, PolygonCollider cubeA)
    {
        var edgeA = halfEdgeA.Edge;
        var pointA = halfEdgeA.Vertex;

        var edgeB = halfEdgeB.Edge;
        var pointB = halfEdgeB.Vertex;

        if (Mathf.Abs(Vector3.Cross(edgeA, edgeB).magnitude) < Eps)
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
                    // halfEdgeA.Draw(Color.Lerp(Color.red, Color.green, 1f / (separation * 10f + 1f)));
                    // halfEdgeB.Draw(Color.Lerp(Color.red, Color.green, 1f / (separation * 10f + 1f)));

                    if (separation > best.separation)
                    {
                        best = (halfEdgeA, halfEdgeB, separation);

                        if (separation > 0f)
                        {
                            return best;
                        }
                    }
                }
            }
        }

        return best;
    }


    private static (Face face, float distance) QueryFaceDirection(PolygonCollider cubeA,
        PolygonCollider cubeB)
    {
        var facesA = cubeA.Shape.Faces;

        (Face face, float separation) best = (default, float.MinValue);

        foreach (var faceA in facesA)
        {
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

    public static bool OOBVsOOB(PolygonCollider a, PolygonCollider b, out List<ContactPoint> contactPoints)
    {
        contactPoints = null;
        var faceQueryAb = QueryFaceDirection(a, b);
        if (faceQueryAb.distance > 0f)
        {
            return false;
        }

        var faceQueryBa = QueryFaceDirection(b, a);
        if (faceQueryBa.distance > 0f)
        {
            return false;
        }

        var EdgeQuery = QueryEdgeDirection(a, b);
        if (EdgeQuery.distance > 0f)
        {
            return false;
        }

        contactPoints = new List<ContactPoint>();

        var x = Mathf.Abs(faceQueryAb.distance) < Mathf.Abs(faceQueryBa.distance);

        var distance = x ? Mathf.Abs(faceQueryAb.distance) : Mathf.Abs(faceQueryBa.distance);
        if (Mathf.Abs(EdgeQuery.distance) < Mathf.Abs(distance))
        {
            if (Intersect(EdgeQuery.a.Vertex, EdgeQuery.a.Twin.Vertex, EdgeQuery.b.Vertex, EdgeQuery.b.Twin.Vertex,
                out var pos))
            {
                contactPoints.Add(new ContactPoint(pos, a.Center - pos, distance));
            }

            Debug.LogWarning("EDGE CASE");
        }
        else
        {
            var referenceFace = x ? faceQueryAb.face : faceQueryBa.face;
            var incidentFace = MostAntiParallelFace(x ? b : a, referenceFace);

            referenceFace.Draw(Color.green);
            incidentFace.Draw(Color.red);

            Debug.Log($"<color=green>{(x ? a : b)} ref face: {referenceFace}</color>");
            Debug.Log($"<color=red>{(x ? b : a)} inc face: {incidentFace}</color>");

            foreach (var sidePlane in referenceFace.SidePlanes)
            {
                contactPoints.AddRange(GenerateContactPoints(sidePlane, incidentFace)
                    .Where(p => Vector3.Dot(p - referenceFace.Center, referenceFace.Normal) <
                        0f && Intersect(p, referenceFace)) //
                    .Select(pos => new ContactPoint(pos, a.Center - pos, EdgeQuery.distance)));
            }

            Debug.LogWarning("FACE CASE");
        }

        if (contactPoints.Count == 0)
        {
            Debug.LogError($"NO CONTACT POINT DETECTED FOR {a}");
        }

        return true;
    }

    #endregion

    #region Sutherland-Hodgman clipping

    private static bool Intersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        out Vector3 contactPoint)
    {
        // Algorithm is ported from the C algorithm of 
        // Paul Bourke at http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline3d/
        contactPoint = Vector3.negativeInfinity;

        var ca = a - c;
        var cd = d - c;

        if (cd.sqrMagnitude < 0f)
        {
            return false;
        }

        var ab = b - a;
        if (ab.sqrMagnitude < 0f)
        {
            return false;
        }

        var d1343 = ca.x * cd.x + ca.y * cd.y + ca.z * cd.z;
        var d4321 = cd.x * ab.x + cd.y * ab.y + cd.z * ab.z;
        var d1321 = ca.x * ab.x + ca.y * ab.y + ca.z * ab.z;
        var d4343 = cd.x * cd.x + cd.y * cd.y + cd.z * cd.z;
        var d2121 = ab.x * ab.x + ab.y * ab.y + ab.z * ab.z;

        var denom = d2121 * d4343 - d4321 * d4321;
        if (Mathf.Abs(denom) < 1e-8f)
        {
            return false;
        }

        var numer = d1343 * d4321 - d1321 * d4343;

        var mua = numer / denom;
        if (mua < Eps || mua >= 1f + Eps)
        {
            return false;
        }

        var mub = (d1343 + d4321 * mua) / d4343;
        if (mub < Eps || mub >= 1f + Eps)
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
        // if (Vector3.Dot((face.Center - point).normalized, -face.Normal) > Eps)
        // {
        //     return false;
        // }

        // {
        //     var cp = Instantiate(Instance.DebugPoint, point, Quaternion.identity);
        //     cp.GetComponent<MeshRenderer>().material.color = Color.green;
        //     cp.transform.localScale *= 0.33f;
        //     Destroy(cp, 0.05f);
        // }

        var intersections = 0;

        var pc = point - face.Center;
        point -= Vector3.Dot(pc, face.Normal) * face.Normal;
        point -= pc * 0.01f;
        // {
        //     var cp = Instantiate(Instance.DebugPoint, point, Quaternion.identity);
        //     cp.GetComponent<MeshRenderer>().material.color = Color.blue;
        //     cp.transform.localScale *= 0.5f;
        //     Destroy(cp, 0.05f);
        // }
        var dir = (face.Center - point).normalized;


        var p0 = point + dir * 100f;
        // Debug.DrawLine(point, p0, Color.blue, 0.02f, false);

        var a = face.Points.Last();
        foreach (var b in face.Points)
        {
            if (Intersect(point, p0, a, b, out _))
            {
                intersections += 1;
            }

            a = b;
        }

        return (intersections & 1) == 1;
    }

    private static bool Intersect(Vector3 a, Vector3 b, Plane plane, out Vector3 point)
    {
        point = Vector3.negativeInfinity;
        var ab = b - a;
        var dot = Vector3.Dot(plane.Normal, ab.normalized);

        // TODO this not might be correct
        // if (Mathf.Abs(dot) < Eps)
        // {
        //     return false;
        // }

        if (dot == 0f)
        {
            return false;
        }

        var t = (Vector3.Dot(plane.Normal, plane.Point) - Vector3.Dot(plane.Normal, a)) / dot;

        if (t < -Eps || t > 1f + Eps)
        {
            return false;
        }

        point = a + ab.normalized * t;
        return true;
    }

    private static IEnumerable<Vector3> GenerateContactPoints(Plane plane, Face incidentFace)
    {
        const int inFront = 1;
        const int behind = -1;
        const int on = 0;

        Func<Vector3, int> planeSide = point =>
        {
            var dot = Vector3.Dot(point - plane.Point, plane.Normal);
            return Mathf.Abs(dot) < 1e-2f ? on : dot > 0f ? inFront : behind;
        };


        var points = new List<Vector3>();

        var a = incidentFace.Points.Last();
        var aSide = planeSide(a);

        foreach (var b in incidentFace.Points)
        {
            var bSide = planeSide(b);

            switch (aSide)
            {
                case behind when bSide == inFront:
                {
                    if (Intersect(a, b, plane, out var contactPoint))
                    {
                        points.Add(contactPoint);
                    }

                    break;
                }
                case behind when bSide == on:
                    points.Add(b);
                    break;
                case inFront when bSide == behind:
                {
                    if (Intersect(a, b, plane, out var contactPoint))
                    {
                        points.Add(contactPoint);
                    }

                    points.Add(b);
                    break;
                }
                case on when bSide == behind:
                    points.Add(a);
                    points.Add(b);
                    break;
                case behind when bSide == behind:
                    points.Add(b);
                    break;
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

    static CollideManager()
    {
        _aux = false;
    }

    private void Start()
    {
        // colliders = new List<Collider>(FindObjectsOfType<PolygonCollider>());
        var polygonColliders = FindObjectsOfType<PolygonCollider>();
        colliders.AddRange(polygonColliders);

        var sphereColliders = FindObjectsOfType<SphereCollider>().Where(sphereCollider =>
            polygonColliders.All(col => col.gameObject.GetInstanceID() != sphereCollider.gameObject.GetInstanceID()));
        colliders.AddRange(sphereColliders);

        var halfPlanes = FindObjectsOfType<HalfPlaneCollider>();
        colliders.AddRange(halfPlanes);

        Debug.Log($"Added {colliders.Count} colliders!");

        CollisionResolution.Instance.SetColliders(colliders);
    }

    private void Update()
    {
        var others = new List<PolygonCollider>();
        CollisionResolution.Instance.UpdateFrameDrag();

        foreach (var a in colliders)
        {
            var isColliding = false;
            others.Clear();
            foreach (var b in colliders.Where(b => a.GetInstanceID() != b.GetInstanceID()))
            {
                if (collidingObjects.ContainsKey((a, b)) || collidingObjects.ContainsKey((b, a))) continue;
                if (!a.IsColliding(b, out var contactPoints)) continue;

                if (debugContactPoints && contactPoints != null)
                isColliding = true;
 
                collidingObjects.Add((a, b), true);
                CollisionResolution.Instance.ResolveCollision(a, b, contactPoints);

                if (debugContactPoints)
                {
                    foreach (var cp in contactPoints)
                    {
                        cp.Draw(Color.yellow);
                    }
                }
            }

            var debugText = isColliding
                ? "<color=green>is colliding</color>"
                : "<color=red>is NOT colliding</color>";
            Debug.Log($"{a} {debugText} with {others}: ");
            a.Collides(isColliding);
        }

        collidingObjects.Clear();
        _aux = false;
    }

    // TODO: There is something wrong with the contact points when the
    // TODO: first collider is a wall; workaround: add everything before the walls
    public void AddCollider(Shape shape)
    {
        PolygonCollider p = shape.GetComponent<PolygonCollider>();
        SphereCollider s = shape.GetComponent<SphereCollider>();

        if (p != null)
        {
            colliders.Insert(0, p);
        } else if (s != null)
        {
            colliders.Insert(0, s);
        }
    }
}