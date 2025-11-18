using Unity.Entities;
using Unity.Mathematics;

public struct BoidConfigComponent : IComponentData
{
    public float BaseVelocity;
    public float VelocityVariation;
    public float RotationCoeff;
    public float NeighborDist;
    public int SearchLayer;
    public float3 ControllerPosition;
    public float3 ControllerForward;
}

public struct BoidSpawnerComponent : IComponentData
{
    public int SpawnCount;
    public float SpawnRadius;
    public bool HasSpawned;
    public int BoidLayer;
}

public struct BoidRenderingComponent : IComponentData
{
    public Entity RenderMeshEntity;
}
