using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct BoidSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BoidSpawnerComponent>();
        state.RequireForUpdate<BoidConfigComponent>();
        state.RequireForUpdate<BoidRenderingComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var spawnerEntity = SystemAPI.GetSingletonEntity<BoidSpawnerComponent>();
        var spawner = SystemAPI.GetComponent<BoidSpawnerComponent>(spawnerEntity);

        if (spawner.HasSpawned) return;

        var config = SystemAPI.GetComponent<BoidConfigComponent>(spawnerEntity);
        var configTransform = SystemAPI.GetComponent<LocalTransform>(spawnerEntity);
        var rendering = SystemAPI.GetComponent<BoidRenderingComponent>(spawnerEntity);

        var random = Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, 100000));

        float3 spawnCenter = config.ControllerPosition;
        quaternion spawnRotation = configTransform.Rotation;

        var templateEntity = rendering.RenderMeshEntity;

        var entityManager = state.EntityManager;
        var newEntities = new NativeArray<Entity>(spawner.SpawnCount, Allocator.Temp);

        entityManager.Instantiate(templateEntity, newEntities);

        for (int i = 0; i < spawner.SpawnCount; i++)
        {
            var entity = newEntities[i];

            float3 randomDirection = random.NextFloat3Direction();
            float randomRadius = math.pow(random.NextFloat(0f, 1f), 1f / 3f) * spawner.SpawnRadius;
            float3 randomOffset = randomDirection * randomRadius;
            float3 position = spawnCenter + randomOffset;

            quaternion randomRotation = random.NextQuaternionRotation();
            quaternion rotation = math.slerp(spawnRotation, randomRotation, 0.3f);

            entityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(position, rotation));

            entityManager.AddComponentData(entity, new BoidComponent
            {
                NoiseOffset = random.NextFloat(0f, 10f),
                Layer = spawner.BoidLayer
            });
            entityManager.AddComponent<BoidTargetComponent>(entity);
            entityManager.AddComponent<SpatialHashComponent>(entity);
        }

        newEntities.Dispose();

        spawner.HasSpawned = true;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        ecb.SetComponent(spawnerEntity, spawner);
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
