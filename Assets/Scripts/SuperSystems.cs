using Latios;
using Latios.Transforms.Systems;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSuperSystem))]
public partial class AgentLocomotionSuperSystem : RootSuperSystem
{
    protected override void CreateSystems()
    {
        GetOrCreateAndAddUnmanagedSystem<IntervalSpawnerSystem>();
        GetOrCreateAndAddUnmanagedSystem<AgentLocomotionSystem>();
    }
}


[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TransformSuperSystem))]
public partial class CollisionLayersSuperSystem : RootSuperSystem
{
    protected override void CreateSystems()
    {
        GetOrCreateAndAddUnmanagedSystem<BuildEnvironmentCollisionLayerSystem>();
        GetOrCreateAndAddUnmanagedSystem<BuildAgentCollisionLayerSystem>();
        GetOrCreateAndAddUnmanagedSystem<AgentEnvironmentPairSystem>();
        GetOrCreateAndAddUnmanagedSystem<AgentsCollisionSystem>();
        GetOrCreateAndAddUnmanagedSystem<BuildFlowFieldSystem>();
    }
}