using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class Collider : MonoBehaviour
{
    [SerializeField] protected bool drawCollider;
    [SerializeField] protected MeshRenderer meshRenderer;
    [SerializeField] protected MeshFilter meshFilter;
    [SerializeField] protected Shape shape;

    [SerializeField] protected Material red;
    [SerializeField] protected Material blue;

    protected virtual void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        shape = GetComponent<Shape>();
        red = shape.GetContactMaterial();
        blue = shape.GetNormalMaterial();
    }
}
