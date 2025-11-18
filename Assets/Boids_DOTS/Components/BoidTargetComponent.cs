using Unity.Entities;
using Unity.Mathematics;

public struct BoidTargetComponent : IComponentData
{
    public float3 Separation;
    public float3 Alignment;
    public float3 Cohesion;
    public int NeighborCount;
}
