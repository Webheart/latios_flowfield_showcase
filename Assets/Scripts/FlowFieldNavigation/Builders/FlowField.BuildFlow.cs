using System;
using System.Diagnostics;
using Latios;
using Latios.Transforms.Abstract;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Exposed;
using Unity.Jobs;

namespace FlowFieldNavigation
{
    public struct BuildFlowConfig
    {
        internal FlowSettings FlowSettings;
        internal Field Field;
        internal EntityQuery GoalsQuery;
        internal FlowGoalTypeHandles TypeHandles;
    }

    public static partial class FlowField
    {
        public static FluentQuery PatchQueryForBuildingFlowGoals(this FluentQuery fluent)
        {
            return fluent.WithWorldTransformReadOnly().With<Goal>();
        }
        
        public static BuildFlowConfig BuildFlow(in Field field, EntityQuery goalsQuery, in FlowGoalTypeHandles requiredTypeHandles) =>
            new() { Field = field, FlowSettings = FlowSettings.Default, GoalsQuery = goalsQuery, TypeHandles = requiredTypeHandles };

        #region FluentChain

        public static BuildFlowConfig WithSettings(this BuildFlowConfig config, FlowSettings settings)
        {
            config.FlowSettings = settings;
            return config;
        }

        #endregion

        #region Schedulers

        public static JobHandle ScheduleParallel(this BuildFlowConfig config, out Flow flow, AllocatorManager.AllocatorHandle allocator, JobHandle inputDeps = default)
        {
            config.ValidateSettings();

            flow = new Flow(config.Field, config.FlowSettings, allocator);

            var dependency = inputDeps;

            var count = (config.GoalsQuery.HasFilter() || config.GoalsQuery.UsesEnabledFiltering())
                ? config.GoalsQuery.CalculateEntityCount()
                : config.GoalsQuery.CalculateEntityCountWithoutFiltering();
            flow.GoalCells.SetCapacity(count);

            dependency = new FlowFieldInternal.CollectGoalsJob
            {
                Field = config.Field,
                GoalCells = flow.GoalCells.AsParallelWriter(),
                TypeHandles = config.TypeHandles
            }.ScheduleParallel(config.GoalsQuery, dependency);

            dependency = new FlowFieldInternal.CalculateCostsWithPriorityQueueJob
            {
                Field = config.Field,
                Costs = flow.Costs,
                GoalCells = flow.GoalCells
            }.Schedule(dependency);
            
            dependency = new FlowFieldInternal.CalculateDirectionJob
            {
                Settings = config.FlowSettings,
                DirectionMap = flow.DirectionMap,
                CostField = flow.Costs,
                DensityField = config.Field.DensityMap,
                Width = config.Field.Width,
                Height = config.Field.Height,
            }.ScheduleParallel(flow.DirectionMap.Length, 32, dependency);

            return dependency;
        }

        #endregion


        #region Validators

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateSettings(this BuildFlowConfig config)
        {
            if (!config.Field.IsCreated)
                throw new InvalidOperationException("BuildFlow: Field is not created");
            if (config.FlowSettings.PassabilityMultiplier <= 0)
                throw new InvalidOperationException("BuildFlow requires a valid PassabilityMultiplier");
        }

        #endregion
    }
}