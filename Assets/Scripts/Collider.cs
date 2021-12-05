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

    protected virtual void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        shape = GetComponent<Shape>();
        red = shape.GetContactMaterial();
        blue = shape.GetNormalMaterial();
        yellow = shape.NearMaterial;
    }


    public virtual void Collides(bool value)
    {
        meshRenderer.material = value ? red : blue;
    }

    public abstract bool IsColliding(Collider other, out List<ContactPoint> contactPoints);
}