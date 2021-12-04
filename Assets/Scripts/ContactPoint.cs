using UnityEngine;

public struct ContactPoint
{
    Vector3 _normal;
    float _penetration;
    Vector3 _position;

    public ContactPoint(Vector3 normal, float penetration, Vector3 position)
    {
        this._normal = normal;
        this._penetration = penetration;
        this._position = position;

// #if UNITY_EDITOR
//         var cp = Object.Instantiate(CollideManager.Instance.DebugPoint, position, Quaternion.identity);
//         Object.Destroy(cp, 0.1f);
// #endif
    }

    public override string ToString()
    {
        return $"{_position} penetrated by {_penetration}";
    }
}