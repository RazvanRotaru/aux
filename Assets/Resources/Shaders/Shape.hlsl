#ifndef __SHAPE__
#define __SHAPE__

#define SPHERE 1
#define PLANE 2

struct Shape
{
    uint2 pointInfo;
    uint2 halfEdgeInfo;
    uint2 faceInfo;
    int type;
    float radius;
};

#endif
