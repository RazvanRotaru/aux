using System;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using UnityEngine;
using Object = UnityEngine.Object;

public class CollideManager : MonoBehaviour
{
    private const float Eps = 10e-3f;
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
        debugPoint = Resources.Load("Prefabs/DebugPoint.prefab") as GameObject;
        print(debugPoint);
    }

    public static bool IsColliding(ICollidable a, ICollidable b)
    {
        if (a is CubeCollider A)
        {
            if (b is CubeCollider B)
            {
                return OBBvOBB(A, B);
            }
        }

        return false;
    }

    private static bool OBBvOBB(CubeCollider a, CubeCollider b)
    {
        Func<CubeCollider, CubeCollider, Vector3, Vector3, float> SAT = (a, b, t, axis) =>
        {
            var ra = a.HalfWidth[0] * Mathf.Abs(Vector3.Dot(axis, a.U[0])) +
                     a.HalfWidth[1] * Mathf.Abs(Vector3.Dot(axis, a.U[1])) +
                     a.HalfWidth[2] * Mathf.Abs(Vector3.Dot(axis, a.U[2]));

            var rb = b.HalfWidth[0] * Mathf.Abs(Vector3.Dot(axis, b.U[0])) +
                     b.HalfWidth[1] * Mathf.Abs(Vector3.Dot(axis, b.U[1])) +
                     b.HalfWidth[2] * Mathf.Abs(Vector3.Dot(axis, b.U[2]));

            var dist = Mathf.Abs(Vector3.Dot(t, axis));

            return ra + rb - dist;
        };

        var axes = new[]
        {
            a.U[0], // A0
            a.U[1], // A1
            a.U[2], // A2
            b.U[0], // B0
            b.U[1], // B1
            b.U[2], // B2
            Vector3.Cross(a.U[0], b.U[0]), // A0xB0
            Vector3.Cross(a.U[0], b.U[1]), // A0xB1
            Vector3.Cross(a.U[0], b.U[2]), // A0xB2
            Vector3.Cross(a.U[1], b.U[0]), // A1xB0
            Vector3.Cross(a.U[1], b.U[1]), // A1xB1
            Vector3.Cross(a.U[1], b.U[2]), // A1xB2
            Vector3.Cross(a.U[2], b.U[0]), // A2xB0
            Vector3.Cross(a.U[2], b.U[1]), // A2xB1
            Vector3.Cross(a.U[2], b.U[2]) // A2xB2
        };


        var t = b.Center - a.Center;
        t = new Vector3(Vector3.Dot(t, a.U[0]), Vector3.Dot(t, a.U[1]), Vector3.Dot(t, a.U[2]));

        float[,] R = new float[3, 3];
        float[,] AbsR = new float[3, 3];

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                R[i, j] = Vector3.Dot(a.U[i], b.U[j]);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                AbsR[i, j] = Mathf.Abs(R[i, j]) + Eps;
            }
        }

        var minOverlap = float.MaxValue;
        var index = -1;
        bool useOne = false;

        // foreach (var axis in axes)
        for (int i = 0; i < axes.Length; ++i)
        {
            var overlap = SAT(a, b, t, axes[i]);
            if (overlap < 0) return false;
            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                index = i;
            }

            if (i == 6)
            {
                useOne = index > 2;
            }
        }

        // TODO(): for some reason, the contact point is negative, but kinda correct
        // TODO(RazvanRotaru): draw contact point
        var contact = GetContactInfo(a, b, axes[index], index, minOverlap, useOne);
        Debug.LogWarning($"<color=orange>{contact}</color>");
        // Since no separating axis is found, the OBBs must be intersecting
        return true;
    }

    private static ContactPoint GetContactInfo(CubeCollider a, CubeCollider b, Vector3 axis, int index,
        float penetration, bool useOne)
    {
        Vector3 t = b.Center - a.Center;

        Func<CubeCollider, CubeCollider, Vector3, int, ContactPoint> FacePoint = (a, b, t, index) =>
        {
            Vector3 normal = a.U[index];
            if (Vector3.Dot(a.U[index], t) > 0)
            {
                normal *= -1.0f;
            }

            var vertex = b.HalfWidth;
            if (Vector3.Dot(b.U[0], normal) < 0) vertex[0] = -vertex[0];
            if (Vector3.Dot(b.U[1], normal) < 0) vertex[1] = -vertex[1];
            if (Vector3.Dot(b.U[2], normal) < 0) vertex[2] = -vertex[2];

            var point = new Vector3(vertex[0], vertex[1], vertex[2]);
            var position = b.transform.InverseTransformVector(point);

            return new ContactPoint(normal, penetration, position);
        };

        Func<Vector3, Vector3, float, Vector3, Vector3, float, Vector3> EdgePoint =
            (aPoint, aAxis, aSize, bPoint, bAxis, bSize) =>
            {
                Vector3 toSt, cOne, cTwo;
                float dpStaOne, dpStaTwo, dpOneTwo, smOne, smTwo;
                float denom, mua, mub;

                smOne = aAxis.sqrMagnitude;
                smTwo = bAxis.sqrMagnitude;
                dpOneTwo = Vector3.Dot(bAxis, aAxis);

                toSt = aPoint - bPoint;
                dpStaOne = Vector3.Dot(aAxis, toSt);
                dpStaTwo = Vector3.Dot(bAxis, toSt);

                denom = smOne * smTwo - dpOneTwo * dpOneTwo;

                // Zero denominator indicates parrallel lines
                if (Mathf.Abs(denom) < 10e-4f)
                {
                    return useOne ? aPoint : bPoint;
                }

                mua = (dpOneTwo * dpStaTwo - smTwo * dpStaOne) / denom;
                mub = (smOne * dpStaTwo - dpOneTwo * dpStaOne) / denom;

                // If either of the edges has the nearest point out
                // of bounds, then the edges aren't crossed, we have
                // an edge-face contact. Our point is on the edge, which
                // we know from the useOne parameter.
                if (mua > aSize ||
                    mua < -aSize ||
                    mub > bSize ||
                    mub < -bSize)
                {
                    return useOne ? aPoint : bPoint;
                }
                else
                {
                    cOne = aPoint + aAxis * mua;
                    cTwo = bPoint + bAxis * mub;

                    return cOne * 0.5f + cTwo * 0.5f;
                }
            };

        if (index < 3)
        {
            // // We've got a vertex of box two on a face of box one.
            // fillPointFaceBoxBox(one, two, toCentre, data, best, pen);
            // data->addContacts(1);
            return FacePoint(a, b, t, index);
        }
        else if (index < 6)
        {
            // // We've got a vertex of box one on a face of box two.
            // // We use the same algorithm as above, but swap around
            // // one and two (and therefore also the vector between their
            // // centres).
            return FacePoint(b, a, -t, index);
        }
        else
        {
            index -= 6;
            var aAxis = index / 3;
            var bAxis = index % 3;

            var ra = a.U[aAxis];
            var rb = b.U[bAxis];

            var L = Vector3.Cross(ra, rb).normalized;

            if (Vector3.Dot(L, t) > 0) axis *= -1f;

            // We have the axes, but not the edges: each axis has 4 edges parallel
            // to it, we need to find which of the 4 for each object. We do
            // that by finding the point in the centre of the edge. We know
            // its component in the direction of the box's collision axis is zero
            // (its a mid-point) and we determine which of the extremes in each
            // of the other axes is closest.
            var aPoint = a.HalfWidth;
            var bPoint = b.HalfWidth;

            for (int i = 0; i < 3; i++)
            {
                if (i == aAxis) aPoint[i] = 0;
                else if (Vector3.Dot(a.U[i], axis) > 0) aPoint[i] = -aPoint[i];

                if (i == bAxis) bPoint[i] = 0;
                else if (Vector3.Dot(b.U[i], axis) < 0) bPoint[i] = -bPoint[i];
            }

            // Move them into world coordinates (they are already oriented
            // correctly, since they have been derived from the axes).
            var PA = new Vector3(aPoint[0], aPoint[1], aPoint[2]);
            var PB = new Vector3(bPoint[0], bPoint[1], bPoint[2]);
            PA = a.transform.InverseTransformPoint(PA);
            PB = b.transform.InverseTransformPoint(PB);

            // So we have a point and a direction for the colliding edges.
            // We need to find out point of closest approach of the two
            // line-segments.
            var vertex = -EdgePoint(PA, ra, a.HalfWidth[aAxis], PB, rb, b.HalfWidth[bAxis]);
            return new ContactPoint(L, -penetration, vertex);
        }
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

        return IsMinkowskiFace(normalA1, normalA2, -normalB1, -normalB2, edgeA, -edgeB);
    }

    private static bool IsMinkowskiFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 ab, Vector3 dc)
    {
        var bxa = ab; //Vector3.Cross(b, a); //ab
        var dxc = dc; //Vector3.Cross(d, c); //dc

        var cba = Vector3.Dot(c, bxa);
        var dba = Vector3.Dot(d, bxa);
        var adc = Vector3.Dot(a, dxc);
        var bdc = Vector3.Dot(b, dxc);

        return cba * dba < 0 && adc * bdc < 0 && cba * bdc > 0;
    }

    private static float Distance(HalfEdge halfEdgeA, HalfEdge halfEdgeB, CubeCollider cubeA)
    {
        var edgeA = halfEdgeA.Edge.normalized;
        var pointA = halfEdgeA.Vertex.Point;

        var edgeB = halfEdgeB.Edge.normalized;
        var pointB = halfEdgeB.Vertex.Point;

        if (Math.Abs(Mathf.Abs(Vector3.Dot(edgeA, edgeB)) - 1f) < 1e-3f)
        {
            return float.MinValue;
        }

        var normal = Vector3.Cross(edgeA, edgeB).normalized;
        if (Vector3.Dot(normal, pointA - cubeA.Center) > 0f)
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

        (HalfEdge a, HalfEdge b, float separation) best = (null, null, float.MaxValue);

        for (var indexA = 0; indexA < shapeA.Count; indexA += 2)
        {
            var halfEdgeA = cubeA.Shape.HalfEdges[indexA];

            for (var indexB = 0; indexB < shapeB.Count; indexB += 2)
            {
                var halfEdgeB = cubeB.Shape.HalfEdges[indexB];

                if (BuildMinkowskiFace(halfEdgeA, halfEdgeB))
                {
                    var separation = Distance(halfEdgeA, halfEdgeB, cubeA);
                    if (separation < best.separation)
                    {
                        best = (halfEdgeA, halfEdgeB, separation);
                    }
                }
            }
        }

        if (best.a != null)
        {
            var cp = Instantiate(Instance.DebugPoint,
                (best.a.Vertex.Point + best.a.Next.Vertex.Point) * 0.5f, Quaternion.identity);
            Destroy(cp, 0.1f);
        }

        Debug.Log($"best separation {best.separation}");

        return best;
    }


    private static (Face face, float distance) QueryFaceDirection(CubeCollider cubeA,
        CubeCollider cubeB)
    {
        var facesA = cubeA.Shape.Faces;

        (Face face, float separation) best = (default, float.MinValue);

        foreach (var faceA in facesA)
        {
            var vertexB = cubeB.Shape.GetSupportPoint(-faceA.Normal);
            var distance = Vector3.Dot(faceA.Normal, vertexB - faceA.Points.A);

            if (distance > best.separation)
            {
                best = (faceA, distance);
            }
        }

        return best;
    }

    private static bool Overlap(CubeCollider cubeA, CubeCollider cubeB)
    {
        var faceQueryAB = QueryFaceDirection(cubeA, cubeB);
        if (faceQueryAB.distance > 0f) return false;

        var faceQueryBA = QueryFaceDirection(cubeB, cubeA);
        if (faceQueryBA.distance > 0f) return false;

        var edgeQuery = QueryEdgeDirection(cubeA, cubeB);
        if (edgeQuery.distance > 0f) return false;

        return true;
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
                    // if (IsColliding(a, b))
                    if (Overlap(a, b))
                    {
                        others.Add(b);
                        isColliding = true;
                    }
                }
            }

            var debugText = isColliding
                ? "<color=green>is collinding</color>"
                : "<color=red>is NOT colldinig</color>";
            Debug.Log($"{a} {debugText} with {others}: ");
            a.Collides(isColliding);
        }
    }
}