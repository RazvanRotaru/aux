using System.Collections.Generic;
using Shapes;

public interface ICollidable
{
    bool IsColliding(Collider other, out List<ContactPoint> contactPoints);
}