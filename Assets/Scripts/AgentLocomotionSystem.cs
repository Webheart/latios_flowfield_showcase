using FlowFieldNavigation;
using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct AgentLocomotionSystem : ISystem, ISystemShouldUpdate
{
    LatiosWorldUnmanaged latiosWorld;
    EntityQuery agentsQuery;
    FlowFieldAgentsTypeHandles handles;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        latiosWorld = state.GetLatiosWorldUnmanaged();
        agentsQuery = state.Fluent().PatchQueryForFlowFieldAgents().Build();
        handles = new FlowFieldAgentsTypeHandles(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        handles.Update(ref state);

        var flowContainer = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<FlowContainer>(true);
        var fieldContainer = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<FieldContainer>(true);
        if (!flowContainer.Flow.IsCreated || !fieldContainer.Field.IsCreated) return;

        state.Dependency = FlowField.AgentsDirections(agentsQuery, handles, SystemAPI.Time.DeltaTime).ScheduleParallel(fieldContainer.Field, flowContainer.Flow, state.Dependency);
        new AgentLocomotionJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct AgentLocomotionJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(ref Velocity velocity, in AgentLocomotion agent, in FlowField.AgentDirection input, TransformAspect transform)
        {
            var angularSpeed = math.radians(agent.AngularSpeed);
            velocity.Value = Physics.StepVelocityWithInput(input.Value.x0y(),
                velocity.Value,
                agent.Acceleration,
                agent.Deceleration,
                agent.MaxSpeed,
                agent.Acceleration,
                agent.Deceleration,
                agent.MaxSpeed,
                DeltaTime);

            var normalizedVelocity = math.normalizesafe(velocity.Value);
            if (normalizedVelocity.Equals(float3.zero)) return;
            var angle = math.atan2(normalizedVelocity.x, normalizedVelocity.z);
            transform.worldRotation = math.slerp(transform.worldRotation, quaternion.RotateY(angle), DeltaTime * angularSpeed);
            transform.TranslateWorld(DeltaTime * velocity.Value);
        }
    }

    [BurstCompile]
    public bool ShouldUpdateSystem(ref SystemState state) => latiosWorld.sceneBlackboardEntity.HasCollectionComponent<FieldContainer>()
                                                             && latiosWorld.sceneBlackboardEntity.HasCollectionComponent<FlowContainer>();
}