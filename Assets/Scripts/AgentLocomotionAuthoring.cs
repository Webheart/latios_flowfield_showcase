using System;
using Unity.Entities;
using UnityEngine;

public class AgentLocomotionAuthoring : MonoBehaviour
{
    public AgentLocomotion Settings;

    public void Reset()
    {
        Settings = AgentLocomotion.Default;
    }

    class Baker : Baker<AgentLocomotionAuthoring>
    {
        public override void Bake(AgentLocomotionAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, authoring.Settings);
            AddComponent<Velocity>(entity);
        }
    }
}