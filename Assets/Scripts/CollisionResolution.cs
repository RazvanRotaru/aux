using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionResolution : MonoBehaviour
{
    [SerializeField] private Vector3 gravity = new Vector3(0.0f, -10.0f, 0.0f);
    [SerializeField] private float drag = 0.995f;
    [SerializeField] private float resitution = 0.95f;
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
        Debug.Log($"Velocity <color=yellow>{velocity}</color>");
        return Vector3.Dot(velocity, normal);
    }

    public void ResolveCollision(Collider a, Collider b, List<Shapes.ContactPoint> points)
    {
        Debug.Log("RESOLVING COLLISION!");

        if (points.Count <= 0)
        {
            return;
        }

        Vector3 contactNormal = points[0].Normal; // For testing

        Debug.Log($"Contact normal <color=yellow>{contactNormal}</color>");
        Debug.Log($"Object {a} Velocity <color=yellow>{a.Velocity}</color>");

        float separatingVelocity = ComputeSeparatingVelocity(a, b, contactNormal);

        if (separatingVelocity > 0)
        {
            return;
        }

      
        Debug.Log($"Separating velocity <color=yellow>{separatingVelocity}</color>");


        float newSeparatingVelocity = -separatingVelocity * resitution; // This is the restitution

        Vector3 accelerationBuildUp = new Vector3(0, 0, 0);
        if (a.InverseMass > infiniteMassLimit) accelerationBuildUp += gravity;
        if (b.InverseMass > infiniteMassLimit) accelerationBuildUp -= gravity;

        float velocityCausedByGravity = Vector3.Dot(accelerationBuildUp, contactNormal) * Time.deltaTime;


        if (velocityCausedByGravity < 0)
        {

            newSeparatingVelocity += resitution * velocityCausedByGravity * 2;
            //a.SetVelocity(new Vector3(a.Velocity.x, 0, a.Velocity.z));

            if (newSeparatingVelocity < 0)
            {
                Debug.Log($"New separating velocity set to 0");
                newSeparatingVelocity = 0;
            }
        }

        float deltaVelocity = newSeparatingVelocity - separatingVelocity;


        Debug.Log($"Separating velocity <color=orange>{separatingVelocity}</color>");
        Debug.Log($"Velocity on normal <color=yellor>{Vector3.Dot(a.Velocity, contactNormal)}</color>");


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

        ResolveInterpenetration(a, b, points);
    }

    void ResolveInterpenetration(Collider a, Collider b, List<Shapes.ContactPoint> points)
    {
        float penetration = 0;
        Vector3 contactNormal = points[0].Normal;
        //penetration = points[0].Penetration;

        if (penetration <= 0)
        {
            return;
        }

        // Movement is based on the total inverse mass
        float totalInverseMass = 0.0f;
        if (a.InverseMass > infiniteMassLimit) totalInverseMass += a.InverseMass;
        if (b.InverseMass > infiniteMassLimit) totalInverseMass += b.InverseMass;

        if (totalInverseMass == 0.0f)
        {
            return;
        }

        Vector3 movementPerMass = contactNormal * (penetration / totalInverseMass);
        Vector3 deltaA = new Vector3(0, 0, 0);
        Vector3 deltaB = new Vector3(0, 0, 0);

        if (a.InverseMass > infiniteMassLimit) deltaA += movementPerMass * a.InverseMass;
        if (b.InverseMass > infiniteMassLimit) deltaB -= movementPerMass * b.InverseMass;

        a.transform.position += deltaA;
        b.transform.position += deltaB;
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
