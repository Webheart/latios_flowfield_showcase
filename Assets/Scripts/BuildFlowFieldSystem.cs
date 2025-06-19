using FlowFieldNavigation;
using Latios;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct BuildFlowFieldSystem : ISystem, ISystemNewScene
{
    LatiosWorldUnmanaged latiosWorld;
    EntityQuery goalsQuery;
    EntityQuery agentsQuery;
    FlowGoalTypeHandles flowGoalHandles;
    FlowFieldAgentsTypeHandles fieldHandles;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        latiosWorld = state.GetLatiosWorldUnmanaged();
        goalsQuery = state.Fluent().PatchQueryForBuildingFlowGoals().Build();
        agentsQuery = state.Fluent().PatchQueryForFlowFieldAgents().Build();
        flowGoalHandles = new FlowGoalTypeHandles(ref state);
        fieldHandles = new FlowFieldAgentsTypeHandles(ref state);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        latiosWorld.sceneBlackboardEntity.RemoveCollectionComponentAndDispose<FieldContainer>();
        latiosWorld.sceneBlackboardEntity.RemoveCollectionComponentAndDispose<FlowContainer>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        flowGoalHandles.Update(ref state);
        fieldHandles.Update(ref state);

        var obstacleLayer = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<EnvironmentCollisionLayer>(true);

        FlowFieldSettings settings;
        if (latiosWorld.sceneBlackboardEntity.HasComponent<FlowFieldSettings>()) settings = latiosWorld.sceneBlackboardEntity.GetComponentData<FlowFieldSettings>();
        else settings = new FlowFieldSettings { FieldSettings = FieldSettings.Default, FlowSettings = FlowSettings.Default };

        state.Dependency = FlowField.BuildField().WithTransform(settings.FlowFieldTransform)
            .WithSettings(settings.FieldSettings)
            .WithObstacles(obstacleLayer.Layer, CollisionLayerSettings.kDefault)
            .WithAgents(agentsQuery, in fieldHandles)
            .ScheduleParallel(out var field, state.WorldUpdateAllocator, state.Dependency);

        state.Dependency = FlowField.BuildFlow(field, goalsQuery, in flowGoalHandles).WithSettings(settings.FlowSettings)
            .ScheduleParallel(out var flow, state.WorldUpdateAllocator, state.Dependency);

#if UNITY_EDITOR
        if (settings.DrawDebugGizmos) state.Dependency = FlowFieldDebug.DrawCells(in field, in flow, state.Dependency);
#endif
        latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new FieldContainer { Field = field });
        latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new FlowContainer { Flow = flow });
    }

    [BurstCompile]
    public void OnNewScene(ref SystemState state)
    {
        latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<FieldContainer>(default);
        latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<FlowContainer>(default);
    }
}