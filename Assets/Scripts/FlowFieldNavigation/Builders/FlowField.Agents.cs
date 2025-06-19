using Latios;
using Latios.Transforms.Abstract;
using Unity.Entities;
using Unity.Jobs;

namespace FlowFieldNavigation
{
    public struct MoveAgentsConfig
    {
        internal EntityQuery AgentsQuery;
        internal FlowFieldAgentsTypeHandles AgentTypeHandles;
        internal float DeltaTime;
    }
    
    public static partial class FlowField
    {
        public static FluentQuery PatchQueryForFlowFieldAgents(this FluentQuery fluent)
        {
            return fluent.WithWorldTransformReadOnly().With<AgentDirection>().With<PrevPosition>().With<Velocity>();
        }

        public static MoveAgentsConfig AgentsDirections(EntityQuery agentsQuery, in FlowFieldAgentsTypeHandles handles, float deltaTime) =>
            new() { AgentsQuery = agentsQuery, AgentTypeHandles = handles, DeltaTime = deltaTime };
        
        
        #region Schedulers

        public static JobHandle ScheduleParallel(this MoveAgentsConfig config, in Field field, in Flow flow, JobHandle inputDeps = default)
        {
            var dependency = inputDeps;

            dependency = new FlowFieldInternal.CalculateAgentsDirectionsJob
            {
                Flow = flow,
                Field = field,
                TypeHandles = config.AgentTypeHandles, DeltaTime = config.DeltaTime
            }.ScheduleParallel(config.AgentsQuery, dependency);
            return dependency;
        }
        
        #endregion
    }
}