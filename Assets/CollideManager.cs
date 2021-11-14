using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class CollideManager : MonoBehaviour
{
    private const float Eps = 10e-3f;

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

        float ra, rb;

        // Test axes L = A0, L = A1, L = A2
        for (int i = 0; i < 3; i++)
        {
            ra = a.HalfWidth[i];
            rb = b.HalfWidth[0] * AbsR[i, 0] + b.HalfWidth[1] * AbsR[i, 1] + b.HalfWidth[2] * AbsR[i, 2];
            if (Mathf.Abs(t[i]) > ra + rb) return false;
        }

        // Test axes L = B0, L = B1, L = B2
        for (int i = 0; i < 3; i++)
        {
            ra = a.HalfWidth[0] * AbsR[0, i] + a.HalfWidth[1] * AbsR[1, i] + a.HalfWidth[2] * AbsR[2, i];
            rb = b.HalfWidth[i];
            if (Mathf.Abs(t[0] * R[0, i] + t[1] * R[1, i] + t[2] * R[2, i]) > ra + rb) return false;
        }

        // Test axis L = A0 x B0
        ra = a.HalfWidth[1] * AbsR[2, 0] + a.HalfWidth[2] * AbsR[1, 0];
        rb = b.HalfWidth[1] * AbsR[0, 2] + b.HalfWidth[2] * AbsR[0, 1];
        if (Mathf.Abs(t[2] * R[1, 0] - t[1] * R[2, 0]) > ra + rb) return false;

        // Test axis L = A0 x B1
        ra = a.HalfWidth[1] * AbsR[2, 1] + a.HalfWidth[2] * AbsR[1, 1];
        rb = b.HalfWidth[0] * AbsR[0, 2] + b.HalfWidth[2] * AbsR[0, 0];
        if (Mathf.Abs(t[2] * R[1, 1] - t[1] * R[2, 1]) > ra + rb) return false;

        // Test axis L = A0 x B2
        ra = a.HalfWidth[1] * AbsR[2, 2] + a.HalfWidth[2] * AbsR[1, 2];
        rb = b.HalfWidth[0] * AbsR[0, 1] + b.HalfWidth[1] * AbsR[0, 0];
        if (Mathf.Abs(t[2] * R[1, 2] - t[1] * R[2, 2]) > ra + rb) return false;

        // Test axis L = A1 x B0
        ra = a.HalfWidth[0] * AbsR[2, 0] + a.HalfWidth[2] * AbsR[0, 0];
        rb = b.HalfWidth[1] * AbsR[1, 2] + b.HalfWidth[2] * AbsR[1, 1];
        if (Mathf.Abs(t[0] * R[2, 0] - t[2] * R[0, 0]) > ra + rb) return false;

        // Test axis L = A1 x B1
        ra = a.HalfWidth[0] * AbsR[2, 1] + a.HalfWidth[2] * AbsR[0, 1];
        rb = b.HalfWidth[0] * AbsR[1, 2] + b.HalfWidth[2] * AbsR[1, 0];
        if (Mathf.Abs(t[0] * R[2, 1] - t[2] * R[0, 1]) > ra + rb) return false;

        // Test axis L = A1 x B2
        ra = a.HalfWidth[0] * AbsR[2, 2] + a.HalfWidth[2] * AbsR[0, 2];
        rb = b.HalfWidth[0] * AbsR[1, 1] + b.HalfWidth[1] * AbsR[1, 0];
        if (Mathf.Abs(t[0] * R[2, 2] - t[2] * R[0, 2]) > ra + rb) return false;

        // Test axis L = A2 x B0
        ra = a.HalfWidth[0] * AbsR[1, 0] + a.HalfWidth[1] * AbsR[0, 0];
        rb = b.HalfWidth[1] * AbsR[2, 2] + b.HalfWidth[2] * AbsR[2, 1];
        if (Mathf.Abs(t[1] * R[0, 0] - t[0] * R[1, 0]) > ra + rb) return false;

        // Test axis L = A2 x B1
        ra = a.HalfWidth[0] * AbsR[1, 1] + a.HalfWidth[1] * AbsR[0, 1];
        rb = b.HalfWidth[0] * AbsR[2, 2] + b.HalfWidth[2] * AbsR[2, 0];
        if (Mathf.Abs(t[1] * R[0, 1] - t[0] * R[1, 1]) > ra + rb) return false;

        // Test axis L = A2 x B2
        ra = a.HalfWidth[0] * AbsR[1, 2] + a.HalfWidth[1] * AbsR[0, 2];
        rb = b.HalfWidth[0] * AbsR[2, 1] + b.HalfWidth[1] * AbsR[2, 0];
        if (Mathf.Abs(t[1] * R[0, 2] - t[0] * R[1, 2]) > ra + rb) return false;

        // Since no separating axis is found, the OBBs must be intersecting
        return true;
    }

    /// Firstly, implement lowest detail level, afterwards, we can try raising it.
    /// (return the bounding vertexes on creation)
    [SerializeField] private List<CubeCollider> _cubeColliders;

    private void Start()
    {
        _cubeColliders = FindObjectsOfType<CubeCollider>().ToList();
    }

    private void Update()
    {
        foreach (var a in _cubeColliders)
        {
            foreach (var b in _cubeColliders)
            {
                if (a != b)
                {
                    var collides = IsColliding(a, b);

                    var debugText = collides
                        ? "<color=green>is collinding</color>"
                        : "<color=red>is NOT colldinig</color>";
                    Debug.Log($"{a} {debugText} with {b}: ");
                    a.Collides(collides);
                    b.Collides(collides);
                }
            }
        }
    }
}