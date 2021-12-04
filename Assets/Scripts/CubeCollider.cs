using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class CubeCollider : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Shape shape;

    [SerializeField] private Material red;
    [SerializeField] private Material blue;

    public Shape Shape => shape;

    public Vector3 Center => transform.position;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        shape = GetComponent<Shape>();
    }

    // public bool IsColliding(ICollidable other)
    // {
    //     return collideManager.IsColliding(this, other);
    // }

    public void Collides(bool value)
    {
        meshRenderer.material = value ? red : blue;
    }
}