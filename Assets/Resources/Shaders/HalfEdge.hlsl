#ifndef __HALF_EDGE__
#define __HALF_EDGE__

struct HalfEdge
{
    float3 vertex;
    float3 edge;

    float3 local_vertex;

    float3 n1;
    float3 n2;

    // uint twin_index;
    // uint face_index;
};

#endif
