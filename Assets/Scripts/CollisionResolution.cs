using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionResolution : MonoBehaviour
{
    [SerializeField] private Vector3 gravity = new Vector3(0.0f, -10.0f, 0.0f);
    [SerializeField] private float drag = 0.995f;
    [SerializeField] private float resitution = 0.95f;
    private int numberOfIterations;
    private List<Collider> allColliders;
    private float frameDrag;
    public const float infiniteMassLimit = 0.0001f;
    public float firstDraftCoefficient;
    public float secondDraftCoefficient;


    public static CollisionResolution Instance { get; private set; }

    public void SetColliders(List<Collider> colliders)
    {
        allColliders = colliders;
        Debug.Log(allColliders.Count);
    }

    void ComputeFrameDrag()
    {
        frameDrag = Mathf.Pow(drag, Time.deltaTime);
    }


    void Integrate(Collider col)
    {
        if (col.InverseMass <= infiniteMassLimit)
        {
            return;
        }

        // Update position
        col.transform.position = col.transform.position + col.Velocity * Time.deltaTime;

        // Update acceleration;
        // Consider gravity the default acceleration
        Vector3 newAcc = gravity;
        newAcc += col.ForceAccumulation * col.InverseMass;

        // Update velocity
        col.SetVelocity((col.Velocity + newAcc * Time.deltaTime) * frameDrag);

        col.ClearForce();
    }


    private float ComputeSeparatingVelocity(Collider a, Collider b, Vector3 normal)
    {
        Vector3 velocity = new Vector3(0, 0, 0);
        if (a.InverseMass > infiniteMassLimit) velocity += a.Velocity;
        if (b.InverseMass > infiniteMassLimit) velocity -= b.Velocity;
        //Debug.Log($"Velocity <color=yellow>{velocity}</color>");
        return Vector3.Dot(velocity, normal);
    }

    void ResolveVelocity(Collider a, Collider b, Shapes.ContactPoint point)
    {
        Vector3 contactNormal = point.Normal;

        //Debug.Log($"Contact normal <color=yellow>{contactNormal}</color>");
        //Debug.Log($"Object {a} Velocity <color=yellow>{a.Velocity}</color>");

        float separatingVelocity = ComputeSeparatingVelocity(a, b, contactNormal);

        if (separatingVelocity > 0)
        {
            return;
        }

        //Debug.Log($"Separating velocity <color=yellow>{separatingVelocity}</color>");

        float newSeparatingVelocity = -separatingVelocity * resitution; // This is the restitution

        Vector3 accelerationBuildUp = new Vector3(0, 0, 0);
        if (a.InverseMass > infiniteMassLimit) accelerationBuildUp += gravity;
        if (b.InverseMass > infiniteMassLimit) accelerationBuildUp -= gravity;

        float velocityCausedByGravity = Vector3.Dot(accelerationBuildUp, contactNormal) * Time.deltaTime;


        if (velocityCausedByGravity < 0)
        {

            newSeparatingVelocity += resitution * velocityCausedByGravity * 2; // The *2 is an error rate; Probably make it a variable with range [1, 2]

            if (newSeparatingVelocity < 0)
            {
                //Debug.Log($"New separating velocity set to 0");
                newSeparatingVelocity = 0;
            }
        }

        float deltaVelocity = newSeparatingVelocity - separatingVelocity;


        Debug.Log($"Separating velocity <color=orange>{separatingVelocity}</color>");
        //Debug.Log($"Velocity on normal <color=yellor>{Vector3.Dot(a.Velocity, contactNormal)}</color>");


        float totalInverseMass = 0.0f;
        if (a.InverseMass > infiniteMassLimit) totalInverseMass += a.InverseMass;
        if (b.InverseMass > infiniteMassLimit) totalInverseMass += b.InverseMass;

        if (totalInverseMass == 0.0f)
        {
            return;
        }

        float impulse = deltaVelocity / totalInverseMass; // Why divide and not multiply???
        Vector3 impulsePerMass = impulse * contactNormal;

        if (a.InverseMass > infiniteMassLimit) a.SetVelocity(a.Velocity + impulsePerMass * a.InverseMass);
        if (b.InverseMass > infiniteMassLimit) b.SetVelocity(b.Velocity - impulsePerMass * b.InverseMass);
    }

    (Vector3 deltaA, Vector3 deltaB) ResolveInterpenetration(Collider a, Collider b, Shapes.ContactPoint point, float _penetration)
    {
        //float penetration = point.Penetration;
        float penetration = _penetration;
        Vector3 contactNormal = point.Normal;

        if (penetration <= 0)
        {
            return (new Vector3(0,0,0), new Vector3(0, 0, 0));
        }

        // Movement is based on the total inverse mass
        float totalInverseMass = 0.0f;
        if (a.InverseMass > infiniteMassLimit) totalInverseMass += a.InverseMass;
        if (b.InverseMass > infiniteMassLimit) totalInverseMass += b.InverseMass;

        if (totalInverseMass == 0.0f)
        {
            return (new Vector3(0, 0, 0), new Vector3(0, 0, 0));
        }

        Vector3 movementPerMass = contactNormal * (penetration / totalInverseMass);
        Vector3 deltaA = new Vector3(0, 0, 0);
        Vector3 deltaB = new Vector3(0, 0, 0);

        if (a.InverseMass > infiniteMassLimit) deltaA += movementPerMass * a.InverseMass;
        if (b.InverseMass > infiniteMassLimit) deltaB -= movementPerMass * b.InverseMass;

        a.transform.position += deltaA;
        b.transform.position += deltaB;

        return (deltaA, deltaB);
    }

    public void ResolveCollision(Collider a, Collider b, List<Shapes.ContactPoint> points)
    {
        Debug.Assert(points != null && points.Count > 0, "Can't resolve collision without contact points!");
        Debug.Log($"<color=red>I'm calling this! {points.Count} collisions!</color>");

        if (points.Count >= 1)
        {
            Debug.Log($"{a} has <color=blue>one collision</color> point!");
            ResolveVelocity(a, b, points[0]);
            ResolveInterpenetration(a, b, points[0], points[0].Penetration);

        } else if (points.Count > 1)
        {
            int debugIteration = 0;
            Debug.Log($"{a} has <color=cyan>{points.Count} collision</color> point!");
            numberOfIterations = points.Count * 2;

            List<float> penetrations = new List<float>();
            
            for (int j = 0; j < points.Count; j++)
            {
                penetrations.Add(points[j].Penetration);
                Debug.Log($"Penetration: {points[j].Penetration}");
            }

            // We first do the resolution for the velocity
            for (int i = 0; i < numberOfIterations; i++)
            {
                float maxVel = Mathf.Infinity;
                int pointIndex = points.Count;

                for (int j = 0; j < points.Count; j++)
                {
                    float sepVel = ComputeSeparatingVelocity(a, b, points[j].Normal);
                    if (sepVel < maxVel && (sepVel < 0 || penetrations[j] > 0))
                    {
                        maxVel = sepVel;
                        pointIndex = j;
                    }
                }

                Debug.Log($"Max velocity {maxVel}");
                Debug.Log($"{pointIndex}");
                if (pointIndex == points.Count)
                {
                    break;
                }
                Debug.Log($"My penetration {penetrations[pointIndex]}");

                ResolveVelocity(a, b, points[pointIndex]);

                Vector3 deltaA;
                Vector3 deltaB;
                (deltaA, deltaB) = ResolveInterpenetration(a, b, points[pointIndex], penetrations[pointIndex]);

                Vector3 resultDelta = deltaA - deltaB;

                //Debug.Log($"DeltaA {deltaA.x} {deltaA.y} {deltaA.z}");
                //Debug.Log($"DeltaB {deltaB.x} {deltaB.y} {deltaB.z}");
                //Debug.Log($"Vector result {resultDelta}");
                //Debug.Log($"Value on collision normal: {Vector3.Dot(resultDelta, points[pointIndex].Normal)}");

                //float displacement = Vector3.Dot(resultDelta, points[pointIndex].Normal);

                //// To not recompute the collision, offset the other penetrations

                for (int j = 0; j < points.Count; j++)
                {
                    penetrations[j] -= (Vector3.Dot(resultDelta, points[pointIndex].Normal));
                }

                debugIteration++;
            }

            Debug.Log($"Broke out after {debugIteration} iteration!");
        }
    }
    

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

    public void UpdateFrameDrag()
    {
        ComputeFrameDrag();
    }

    private void LateUpdate()
    {
        foreach (Collider col in allColliders)
        {
            Integrate(col);
        }
    }
}
