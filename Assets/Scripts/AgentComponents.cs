using System;
using Unity.Entities;
using Unity.Mathematics;

public struct Velocity : IComponentData
{
    public float3 Value;
}

[Serializable]
public struct AgentLocomotion : IComponentData
{
    public float Acceleration;
    public float Deceleration;
    public float MaxSpeed;
    public float AngularSpeed;

    public static AgentLocomotion Default => new AgentLocomotion
    {
        Acceleration = 10,
        Deceleration = 10,
        MaxSpeed = 5,
        AngularSpeed = 240,
    };
}