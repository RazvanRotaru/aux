using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;
using ContactPoint = Shapes.ContactPoint;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public abstract class Collider : MonoBehaviour, ICollidable
{
    [SerializeField] protected bool drawCollider;
    [SerializeField] protected MeshRenderer meshRenderer;
    [SerializeField] protected MeshFilter meshFilter;
    [SerializeField] protected Shape shape;

    [SerializeField] protected Material red;
    [SerializeField] protected Material blue;
    [SerializeField] protected Material yellow;

    public Shape Shape => shape;
    
    protected Vector3 velocity = new Vector3(0, 0, 0);
    protected Vector3 acceleration = new Vector3(0, 0, 0);
    [SerializeField]protected float mass = 1.0f;
    protected float inverseMass; // It is preferred this way to avoid division by 0; Since we can represent infinity it will always crash at 0 since 1/0 -> infinity
    protected Vector3 forceAccumulation = new Vector3(0.0f, 0.0f, 0.0f);

    public Vector3 Velocity => velocity;
    public Vector3 Acceleration => acceleration;
    public float InverseMass => inverseMass;

    public float Mass => mass;

    public Vector3 ForceAccumulation => forceAccumulation;


    public void SetVelocity(Vector3 _velocity)
    {
        velocity = _velocity;
    }

    public void SetAcceleration(Vector3 _acceleration)
    {
        acceleration = _acceleration;
    }

    private void ComputeInverseMass()
    {
        inverseMass = 1.0f / mass;
        Debug.Log($"Computed inverse mass of {this} is {inverseMass}!");
    }

    public void AddForce(Vector3 force)
    {
        Debug.Log($"{this} is accumulating force!");
        forceAccumulation += force;
    }

    public void ClearForce()
    {
        forceAccumulation.x = 0;
        forceAccumulation.y = 0;
        forceAccumulation.z = 0;
    }

    protected virtual void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        shape = GetComponent<Shape>();
        red = shape.GetContactMaterial();
        blue = shape.GetNormalMaterial();
        yellow = shape.NearMaterial;
        ComputeInverseMass();
    }

    private void Update()
    {
        Debug.Log($"Acceleration of {this}: {acceleration}!");
    }

    public virtual void Collides(bool value)
    {
        meshRenderer.material = value ? red : blue;
    }

    public abstract bool IsColliding(Collider other, out List<ContactPoint> contactPoints);
}