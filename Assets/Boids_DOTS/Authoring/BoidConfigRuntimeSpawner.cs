using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

public class BoidConfigRuntimeSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject boidPrefab;

    [Header("Spawn Settings")]
    public int spawnCount = 100;
    public float spawnRadius = 4.0f;

    [Header("Boid Behavior")]
    [Range(0.1f, 20.0f)]
    public float velocity = 6.0f;

    [Range(0.0f, 0.9f)]
    public float velocityVariation = 0.5f;

    [Range(0.1f, 20.0f)]
    public float rotationCoeff = 4.0f;

    [Range(0.1f, 10.0f)]
    public float neighborDist = 2.0f;

    public LayerMask searchLayer;

    private Entity configEntity;

    void Start()
    {
        if (boidPrefab == null)
        {
            Debug.LogError("BoidConfigRuntimeSpawner: Boid Prefab is not assigned!");
            return;
        }

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        configEntity = entityManager.CreateEntity();

        entityManager.AddComponentData(configEntity, LocalTransform.FromPositionRotation(
            transform.position,
            transform.rotation));

        entityManager.AddComponentData(configEntity, new BoidConfigComponent
        {
            BaseVelocity = velocity,
            VelocityVariation = velocityVariation,
            RotationCoeff = rotationCoeff,
            NeighborDist = neighborDist,
            SearchLayer = searchLayer.value,
            ControllerPosition = transform.position,
            ControllerForward = transform.forward
        });

        Entity renderMeshEntity = GameObjectEntity.ConvertGameObjectHierarchy(boidPrefab, World.DefaultGameObjectInjectionWorld);

        entityManager.AddComponentData(configEntity, new BoidSpawnerComponent
        {
            SpawnCount = spawnCount,
            SpawnRadius = spawnRadius,
            HasSpawned = false,
            BoidLayer = boidPrefab.layer
        });

        entityManager.AddComponentData(configEntity, new BoidRenderingComponent
        {
            RenderMeshEntity = renderMeshEntity
        });
    }

    void Update()
    {
        if (configEntity != Entity.Null && World.DefaultGameObjectInjectionWorld != null)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (entityManager.Exists(configEntity))
            {
                var config = entityManager.GetComponentData<BoidConfigComponent>(configEntity);
                config.ControllerPosition = transform.position;
                config.ControllerForward = transform.forward;
                entityManager.SetComponentData(configEntity, config);
            }
        }
    }
}

public static class GameObjectEntity
{
    public static Entity ConvertGameObjectHierarchy(GameObject gameObject, World world)
    {
        var entityManager = world.EntityManager;
        var entity = entityManager.CreateEntity();

        entityManager.AddComponentData(entity, LocalTransform.FromPositionRotation(
            float3.zero,
            quaternion.identity));

        entityManager.AddComponentData(entity, new Unity.Transforms.LocalToWorld
        {
            Value = float4x4.identity
        });

        var meshFilter = gameObject.GetComponent<MeshFilter>();
        var meshRenderer = gameObject.GetComponent<MeshRenderer>();

        if (meshFilter != null && meshRenderer != null)
        {
            var renderMeshDescription = new RenderMeshDescription(
                shadowCastingMode: UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows: false);

            var renderMeshArray = new RenderMeshArray(
                new[] { meshRenderer.sharedMaterial },
                new[] { meshFilter.sharedMesh });

            RenderMeshUtility.AddComponents(
                entity,
                entityManager,
                renderMeshDescription,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        }

        return entity;
    }
}
