using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class CubeCollider : MonoBehaviour, ICollidable
{
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private Cube _shape;

    [SerializeField] private Material _red;
    [SerializeField] private Material _blue;


    public Shape Shape => _shape;

    public Vector3 Center => transform.position;


    // TODO(): Must optimize!!!!!
    public Vector3[] U => new Vector3[] {-transform.right, -transform.up, -transform.forward};

    // TODO(): Must optimize!!!!!
    public List<float> HalfWidth
    {
        get
        {
            var v3 = transform.localScale;
            var hw = Mathf.Max(Mathf.Max(v3.x, v3.y), v3.z) / 2.0f;

            return new List<float> {hw, hw, hw};
        }
    }

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _shape = GetComponent<Cube>();
    }

    void Update()
    {
    }

    public bool IsColliding(ICollidable other)
    {
        return CollideManager.IsColliding(this, other);
    }

    public void Collides(bool value)
    {
        _meshRenderer.material = value ? _red : _blue;
    }
}