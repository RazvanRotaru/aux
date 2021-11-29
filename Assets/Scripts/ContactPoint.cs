using UnityEngine;

public struct ContactPoint
{
    Vector3 normal;
    float penetration;
    Vector3 position;

    public ContactPoint(Vector3 normal, float penetration, Vector3 position)
    {
        this.normal = normal;
        this.penetration = penetration;
        this.position = position;

#if UNITY_EDITOR
        Object.Instantiate(CollideManager.Instance.DebugPoint, position, Quaternion.identity);
#endif
    }

    public override string ToString()
    {
        return $"{position} penetrated by {penetration}";
    }
}