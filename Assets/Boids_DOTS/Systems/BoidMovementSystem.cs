using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
[UpdateAfter(typeof(BoidFlockingSystem))]
public partial struct BoidMovementSystem : ISystem
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
        float deltaTime = SystemAPI.Time.DeltaTime;
        float elapsedTime = (float)SystemAPI.Time.ElapsedTime;

        new MovementJob
        {
            Config = config,
            DeltaTime = deltaTime,
            ElapsedTime = elapsedTime
        }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct MovementJob : IJobEntity
    {
        public BoidConfigComponent Config;
        public float DeltaTime;
        public float ElapsedTime;

        void Execute(
            ref LocalTransform transform,
            in BoidComponent boid,
            in BoidTargetComponent target)
        {
            float3 currentPosition = transform.Position;
            quaternion currentRotation = transform.Rotation;

            float velocity = BoidFlockingMath.CalculateVelocityWithNoiseDOTS(
                Config.BaseVelocity,
                Config.VelocityVariation,
                ElapsedTime,
                boid.NoiseOffset);

            quaternion newRotation = BoidFlockingMath.CalculateRotationDOTS(
                target.Separation,
                target.Alignment,
                target.Cohesion,
                currentRotation,
                Config.RotationCoeff,
                DeltaTime);

            transform.Rotation = newRotation;

            float3 forward = math.forward(transform.Rotation);
            transform.Position = BoidFlockingMath.CalculateNewPositionDOTS(
                currentPosition,
                forward,
                velocity,
                DeltaTime);
        }
    }
}
