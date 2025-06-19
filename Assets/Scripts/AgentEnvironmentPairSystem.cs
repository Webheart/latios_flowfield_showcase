using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct AgentEnvironmentPairSystem : ISystem, ISystemShouldUpdate
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
    public bool ShouldUpdateSystem(ref SystemState state) => latiosWorld.sceneBlackboardEntity.HasCollectionComponent<EnvironmentCollisionLayer>()
                                                             && latiosWorld.sceneBlackboardEntity.HasCollectionComponent<AgentCollisionLayer>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        transformLookup.Update(ref state);
        velocityLookup.Update(ref state);

        var envLayer = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<EnvironmentCollisionLayer>(true).Layer;
        var agentsLayer = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<AgentCollisionLayer>(true).Layer;
        if (!envLayer.IsCreated || !agentsLayer.IsCreated) return;
        var processor = new AgentEnvironmentProcessor
        {
            TransformLookup = transformLookup,
            VelocityLookup = velocityLookup
        };

        state.Dependency = Physics.FindPairs(in agentsLayer, in envLayer, in processor).ScheduleParallel(state.Dependency);
    }

    struct AgentEnvironmentProcessor : IFindPairsProcessor
    {
        public PhysicsComponentLookup<WorldTransform> TransformLookup;
        public PhysicsComponentLookup<Velocity> VelocityLookup;

        public void Execute(in FindPairsResult result)
        {
            if (!Physics.DistanceBetween(result.colliderA, result.transformA, result.colliderB, result.transformB, 0f, out var hit))
                return;

            var transformA = TransformLookup.GetRW(result.entityA);
            ref var velA = ref VelocityLookup.GetRW(result.entityA).ValueRW.Value;
            var translation = new float3(-hit.distance * hit.normalB);
            transformA.ValueRW.worldTransform.position += new float3(translation.x, 0f, translation.z);

            if (math.dot(velA.xz, hit.normalB.xz) >= 0f) return;

            velA -= (math.dot(velA.xz, hit.normalB.xz) * hit.normalB.xz).x0y();
        }
    }
}