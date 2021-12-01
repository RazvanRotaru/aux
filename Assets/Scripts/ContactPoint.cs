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
        var cp = Object.Instantiate(CollideManager.Instance.DebugPoint, position, Quaternion.identity);
        Object.Destroy(cp, 0.1f);
#endif
    }

    public override string ToString()
    {
        return $"{position} penetrated by {penetration}";
    }
}