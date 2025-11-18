using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BoidSpatialHashSystem))]
public partial struct BoidFlockingSystem : ISystem
{
    private NativeParallelMultiHashMap<int, EntityData> _spatialMap;

    struct EntityData
    {
        public Entity Entity;
        public float3 Position;
        public float3 Forward;
        public int Layer;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BoidConfigComponent>();
        state.RequireForUpdate<BoidComponent>();
        _spatialMap = new NativeParallelMultiHashMap<int, EntityData>(1024, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        _spatialMap.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<BoidConfigComponent>();

        _spatialMap.Clear();

        var query = SystemAPI.QueryBuilder().WithAll<BoidComponent>().Build();
        int boidCount = query.CalculateEntityCount();

        if (_spatialMap.Capacity < boidCount)
        {
            _spatialMap.Capacity = boidCount;
        }

        var spatialMapParallel = _spatialMap.AsParallelWriter();

        state.Dependency = new BuildSpatialMapJob
        {
            SpatialMap = spatialMapParallel
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new FlockingJob
        {
            Config = config,
            SpatialMap = _spatialMap
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    partial struct BuildSpatialMapJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, EntityData>.ParallelWriter SpatialMap;

        void Execute(Entity entity, in SpatialHashComponent hash, in LocalTransform transform, in BoidComponent boid)
        {
            SpatialMap.Add(hash.CellIndex, new EntityData
            {
                Entity = entity,
                Position = transform.Position,
                Forward = math.forward(transform.Rotation),
                Layer = boid.Layer
            });
        }
    }

    [BurstCompile]
    partial struct FlockingJob : IJobEntity
    {
        [ReadOnly] public BoidConfigComponent Config;
        [ReadOnly] public NativeParallelMultiHashMap<int, EntityData> SpatialMap;

        void Execute(
            Entity entity,
            ref BoidTargetComponent target,
            in BoidComponent boid,
            in SpatialHashComponent hash,
            in LocalTransform transform)
        {
            float3 currentPosition = transform.Position;

            float cellSize = Config.NeighborDist;
            int3 currentCell = (int3)math.floor(currentPosition / cellSize);

            NativeList<EntityData> nearbyBoids = new NativeList<EntityData>(Allocator.Temp);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        int3 neighborCell = currentCell + new int3(x, y, z);
                        int neighborHash = neighborCell.x + neighborCell.y * 73856093 + neighborCell.z * 19349663;

                        if (SpatialMap.TryGetFirstValue(neighborHash, out var neighborData, out var iterator))
                        {
                            do
                            {
                                int layerMask = 1 << neighborData.Layer;
                                if ((Config.SearchLayer & layerMask) == 0)
                                    continue;

                                float3 diff = currentPosition - neighborData.Position;
                                float distSq = math.lengthsq(diff);

                                if (distSq <= Config.NeighborDist * Config.NeighborDist)
                                {
                                    nearbyBoids.Add(neighborData);
                                }
                            }
                            while (SpatialMap.TryGetNextValue(out neighborData, ref iterator));
                        }
                    }
                }
            }

            int nearbyCount = nearbyBoids.Length;
            NativeArray<float3> nearbyPositions = new NativeArray<float3>(nearbyCount, Allocator.Temp);
            NativeArray<float3> nearbyForwards = new NativeArray<float3>(nearbyCount, Allocator.Temp);
            int myIndex = -1;

            for (int i = 0; i < nearbyCount; i++)
            {
                var boidData = nearbyBoids[i];
                nearbyPositions[i] = boidData.Position;
                nearbyForwards[i] = boidData.Forward;

                if (boidData.Entity == entity)
                {
                    myIndex = i;
                }
            }

            float3 separation, alignment, cohesion;
            BoidFlockingMath.CalculateFlockingDOTS(
                currentPosition,
                Config.ControllerForward,
                Config.ControllerPosition,
                nearbyPositions,
                nearbyForwards,
                nearbyCount,
                myIndex,
                Config.NeighborDist,
                out separation,
                out alignment,
                out cohesion);

            target.Separation = separation;
            target.Alignment = alignment;
            target.Cohesion = cohesion;
            target.NeighborCount = nearbyCount;
            nearbyBoids.Dispose();
            nearbyPositions.Dispose();
            nearbyForwards.Dispose();
        }
    }
}
