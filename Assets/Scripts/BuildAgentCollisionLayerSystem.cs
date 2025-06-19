using Latios;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct BuildAgentCollisionLayerSystem : ISystem, ISystemNewScene
{
    LatiosWorldUnmanaged latiosWorld;
    EntityQuery query;
    BuildCollisionLayerTypeHandles handles;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        latiosWorld = state.GetLatiosWorldUnmanaged();
        query = state.Fluent().With<AgentLocomotion>().PatchQueryForBuildingCollisionLayer().Build();
        handles = new BuildCollisionLayerTypeHandles(ref state);
    }

    [BurstCompile]
    public void OnNewScene(ref SystemState state)
    {
        latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<AgentCollisionLayer>(default);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        handles.Update(ref state);

        state.Dependency = Physics.BuildCollisionLayer(query, in handles).WithSettings(CollisionLayerSettings.kDefault)
            .ScheduleParallel(out var layer, state.WorldUpdateAllocator, state.Dependency);

        latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new AgentCollisionLayer { Layer = layer });
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        latiosWorld.sceneBlackboardEntity.RemoveCollectionComponentAndDispose<AgentCollisionLayer>();
    }
}