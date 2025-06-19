using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct AgentsCollisionSystem : ISystem, ISystemShouldUpdate
{
    LatiosWorldUnmanaged latiosWorld;
    ComponentLookup<WorldTransform> transformLookup;
    ComponentLookup<Velocity> velocityLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        latiosWorld = state.GetLatiosWorldUnmanaged();
        transformLookup = SystemAPI.GetComponentLookup<WorldTransform>();
        velocityLookup = SystemAPI.GetComponentLookup<Velocity>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        transformLookup.Update(ref state);
        velocityLookup.Update(ref state);

        var characterLayer = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<AgentCollisionLayer>(true).Layer;
        if (!characterLayer.IsCreated) return;

        var processor = new AgentsCollisionProcessor
        {
            TransformLookup = transformLookup,
            VelocityLookup = velocityLookup,
        };

        state.Dependency = Physics.FindPairs(in characterLayer, in processor).ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    struct AgentsCollisionProcessor : IFindPairsProcessor
    {
        public PhysicsComponentLookup<WorldTransform> TransformLookup;
        public PhysicsComponentLookup<Velocity> VelocityLookup;

        const float Epsilon = 0.001f;
        const float Restitution = 0.1f;
        const float PositionCorrectionFactor = 0.5f;
        const float СorrectionLerpRate = 0.31418f;

        [BurstCompile]
        public void Execute(in FindPairsResult result)
        {
            if (!Physics.DistanceBetween(result.colliderA, result.transformA, result.colliderB, result.transformB, Epsilon, out var hit))
                return;

            if (hit.distance >= 0f) return;
            var penetration = -hit.distance;
            var normal = hit.normalA;
            var correction = normal * (penetration * PositionCorrectionFactor);
            var transformA = TransformLookup.GetRW(result.entityA);
            var transformB = TransformLookup.GetRW(result.entityB);
            var posA = transformA.ValueRO.worldTransform.position;
            var posB = transformB.ValueRO.worldTransform.position;
            transformA.ValueRW.worldTransform.position = math.lerp(posA, posA - correction, СorrectionLerpRate);
            transformB.ValueRW.worldTransform.position = math.lerp(posB, posB + correction, СorrectionLerpRate);

            ref var velA = ref VelocityLookup.GetRW(result.entityA).ValueRW.Value;
            ref var velB = ref VelocityLookup.GetRW(result.entityB).ValueRW.Value;
            var velA2D = new float3(velA.x, 0, velA.z);
            var velB2D = new float3(velB.x, 0, velB.z);
            var closingVelocity = math.dot(velA2D - velB2D, normal);
            if (closingVelocity <= 0f) return;

            var impulse = -(1 + Restitution) * closingVelocity / 2f;
            velA.xz += impulse * normal.xz;
            velB.xz -= impulse * normal.xz;
        }
    }

    [BurstCompile]
    public bool ShouldUpdateSystem(ref SystemState state) => latiosWorld.sceneBlackboardEntity.HasCollectionComponent<AgentCollisionLayer>();
}