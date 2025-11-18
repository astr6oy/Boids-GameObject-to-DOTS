//
// Boids - Flocking behavior simulation.
//
// Copyright (C) 2014 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using UnityEngine;
using System.Collections;

public class BoidBehaviour : MonoBehaviour
{
    // Reference to the controller.
    public BoidController controller;

    // Options for animation playback.
    public float animationSpeedVariation = 0.2f;

    // Random seed.
    float noiseOffset;

    // NOTE: Moved to BoidFlockingMath.GetSeparationVector()
    // Kept as wrapper for backward compatibility
    Vector3 GetSeparationVector(Transform target)
    {
        return BoidFlockingMath.GetSeparationVector(
            transform.position,
            target.transform.position,
            controller.neighborDist);
    }

    void Start()
    {
        noiseOffset = Random.value * 10.0f;

        var animator = GetComponent<Animator>();
        if (animator)
            animator.speed = Random.Range(-1.0f, 1.0f) * animationSpeedVariation + 1.0f;
    }

    void Update()
    {
        var currentPosition = transform.position;
        var currentRotation = transform.rotation;

        // NOTE: Using BoidFlockingMath.CalculateVelocityWithNoise()
        var velocity = BoidFlockingMath.CalculateVelocityWithNoise(
            controller.velocity,
            controller.velocityVariation,
            Time.time,
            noiseOffset);

        // Looks up nearby boids (Physics.OverlapSphere - original method)
        var nearbyBoids = Physics.OverlapSphere(currentPosition, controller.neighborDist, controller.searchLayer);

        // Prepare arrays for BoidFlockingMath
        int nearbyCount = nearbyBoids.Length;
        Vector3[] nearbyPositions = new Vector3[nearbyCount];
        Vector3[] nearbyForwards = new Vector3[nearbyCount];
        int myIndex = -1;

        for (int i = 0; i < nearbyCount; i++)
        {
            var boid = nearbyBoids[i];
            nearbyPositions[i] = boid.transform.position;
            nearbyForwards[i] = boid.transform.forward;

            if (boid.gameObject == gameObject)
            {
                myIndex = i;
            }
        }

        // NOTE: Using BoidFlockingMath.CalculateFlocking()
        Vector3 separation, alignment, cohesion;
        BoidFlockingMath.CalculateFlocking(
            currentPosition,
            controller.transform.forward,
            controller.transform.position,
            nearbyPositions,
            nearbyForwards,
            nearbyCount,
            myIndex,
            controller.neighborDist,
            out separation,
            out alignment,
            out cohesion);

        // NOTE: Using BoidFlockingMath.CalculateRotation()
        var newRotation = BoidFlockingMath.CalculateRotation(
            separation,
            alignment,
            cohesion,
            currentRotation,
            controller.rotationCoeff,
            Time.deltaTime);

        transform.rotation = newRotation;

        // NOTE: Using BoidFlockingMath.CalculateNewPosition()
        var forward = transform.forward;
        transform.position = BoidFlockingMath.CalculateNewPosition(
            currentPosition,
            forward,
            velocity,
            Time.deltaTime);
    }
}
