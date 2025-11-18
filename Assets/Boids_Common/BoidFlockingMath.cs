using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;

public static class BoidFlockingMath
{
    public static Vector3 GetSeparationVector(Vector3 myPosition, Vector3 targetPosition, float neighborDist)
    {
        var diff = myPosition - targetPosition;
        var diffLen = diff.magnitude;
        var scaler = Mathf.Clamp01(1.0f - diffLen / neighborDist);
        return diff * (scaler / diffLen);
    }

    [BurstCompile]
    public static float3 GetSeparationVector(float3 myPosition, float3 targetPosition, float neighborDist)
    {
        float3 diff = myPosition - targetPosition;
        float diffLen = math.length(diff);
        float scaler = math.saturate(1.0f - diffLen / neighborDist);
        return diff * (scaler / diffLen);
    }

    public static float CalculateVelocityWithNoise(float baseVelocity, float velocityVariation, float time, float noiseOffset)
    {
        var noise = Mathf.PerlinNoise(time, noiseOffset) * 2.0f - 1.0f;
        return baseVelocity * (1.0f + noise * velocityVariation);
    }

    [BurstCompile]
    public static float CalculateVelocityWithNoiseDOTS(float baseVelocity, float velocityVariation, float time, float noiseOffset)
    {
        float noiseValue = noise.snoise(new float2(time, noiseOffset));
        return baseVelocity * (1.0f + noiseValue * velocityVariation);
    }

    public static void CalculateFlocking(
        Vector3 myPosition,
        Vector3 controllerForward,
        Vector3 controllerPosition,
        Vector3[] nearbyPositions,
        Vector3[] nearbyForwards,
        int nearbyCount,
        int myIndex,
        float neighborDist,
        out Vector3 separation,
        out Vector3 alignment,
        out Vector3 cohesion)
    {
        separation = Vector3.zero;
        alignment = controllerForward;
        cohesion = controllerPosition;

        for (int i = 0; i < nearbyCount; i++)
        {
            if (i == myIndex) continue;

            var targetPosition = nearbyPositions[i];
            var targetForward = nearbyForwards[i];

            separation += GetSeparationVector(myPosition, targetPosition, neighborDist);
            alignment += targetForward;
            cohesion += targetPosition;
        }

        if (nearbyCount > 0)
        {
            var avg = 1.0f / nearbyCount;
            alignment *= avg;
            cohesion *= avg;
        }
        cohesion = (cohesion - myPosition).normalized;
    }

    public static void CalculateFlockingDOTS(
        float3 myPosition,
        float3 controllerForward,
        float3 controllerPosition,
        Unity.Collections.NativeArray<float3> nearbyPositions,
        Unity.Collections.NativeArray<float3> nearbyForwards,
        int nearbyCount,
        int myIndex,
        float neighborDist,
        out float3 separation,
        out float3 alignment,
        out float3 cohesion)
    {
        separation = float3.zero;
        alignment = controllerForward;
        cohesion = controllerPosition;

        for (int i = 0; i < nearbyCount; i++)
        {
            if (i == myIndex) continue;

            var targetPosition = nearbyPositions[i];
            var targetForward = nearbyForwards[i];

            separation += GetSeparationVector(myPosition, targetPosition, neighborDist);
            alignment += targetForward;
            cohesion += targetPosition;
        }

        if (nearbyCount > 0)
        {
            var avg = 1.0f / nearbyCount;
            alignment *= avg;
            cohesion *= avg;
        }
        cohesion = math.normalizesafe(cohesion - myPosition);
    }

    public static Quaternion CalculateRotation(
        Vector3 separation,
        Vector3 alignment,
        Vector3 cohesion,
        Quaternion currentRotation,
        float rotationCoeff,
        float deltaTime)
    {
        var direction = separation + alignment + cohesion;
        var rotation = Quaternion.FromToRotation(Vector3.forward, direction.normalized);

        if (rotation != currentRotation)
        {
            var ip = Mathf.Exp(-rotationCoeff * deltaTime);
            return Quaternion.Slerp(rotation, currentRotation, ip);
        }

        return currentRotation;
    }

    [BurstCompile]
    public static quaternion CalculateRotationDOTS(
        float3 separation,
        float3 alignment,
        float3 cohesion,
        quaternion currentRotation,
        float rotationCoeff,
        float deltaTime)
    {
        float3 direction = separation + alignment + cohesion;
        direction = math.normalizesafe(direction);

        quaternion rotation = QuaternionFromToRotation(new float3(0, 0, 1), direction);

        if (math.lengthsq(direction) > 0.001f)
        {
            float ip = math.exp(-rotationCoeff * deltaTime);
            return math.slerp(rotation, currentRotation, ip);
        }

        return currentRotation;
    }

    [BurstCompile]
    private static quaternion QuaternionFromToRotation(float3 from, float3 to)
    {
        float3 normFrom = math.normalize(from);
        float3 normTo = math.normalize(to);

        float dot = math.dot(normFrom, normTo);

        if (dot >= 0.999999f)
        {
            return quaternion.identity;
        }

        if (dot <= -0.999999f)
        {
            float3 orthoAxis = math.abs(normFrom.x) < 0.999f
                ? new float3(1, 0, 0)
                : new float3(0, 1, 0);

            float3 axis = math.normalize(math.cross(normFrom, orthoAxis));
            return new quaternion(new float4(axis.x, axis.y, axis.z, 0));
        }

        float3 cross = math.cross(normFrom, normTo);
        float w = dot + 1.0f;

        quaternion q = new quaternion(cross.x, cross.y, cross.z, w);
        return math.normalize(q);
    }

    public static Vector3 CalculateNewPosition(Vector3 currentPosition, Vector3 forward, float velocity, float deltaTime)
    {
        return currentPosition + forward * (velocity * deltaTime);
    }

    [BurstCompile]
    public static float3 CalculateNewPositionDOTS(float3 currentPosition, float3 forward, float velocity, float deltaTime)
    {
        return currentPosition + forward * (velocity * deltaTime);
    }
}
