using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(BoidFlockingSystem))]
public partial struct BoidSpatialHashSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BoidConfigComponent>();
        state.RequireForUpdate<BoidComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<BoidConfigComponent>();
        float cellSize = config.NeighborDist;

        new SpatialHashJob
        {
            CellSize = cellSize
        }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct SpatialHashJob : IJobEntity
    {
        public float CellSize;

        void Execute(ref SpatialHashComponent spatialHash, in LocalTransform transform)
        {
            int3 cell = (int3)math.floor(transform.Position / CellSize);
            spatialHash.CellIndex = cell.x + cell.y * 73856093 + cell.z * 19349663;
        }
    }
}
