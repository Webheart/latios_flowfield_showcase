using System;
using System.Diagnostics;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Physics = Latios.Psyshock.Physics;

namespace FlowFieldNavigation
{
    public struct BuildFieldConfig
    {
        internal FieldSettings FieldSettings;
        internal TransformQvvs Transform;
        internal CollisionLayer ObstaclesLayer;
        internal CollisionLayerSettings ObstaclesLayerSettings;
        internal BuildAgentsConfig AgentsConfig;

        internal bool HasObstaclesLayer;
        internal bool HasAgentsQuery;
    }

    public struct BuildAgentsConfig
    {
        internal EntityQuery AgentsQuery;
        internal FlowFieldAgentsTypeHandles AgentTypeHandles;
    }

    public static partial class FlowField
    {
        public static BuildFieldConfig BuildField() => new()
        {
            FieldSettings = FieldSettings.Default,
        };

        #region FluentChain

        public static BuildFieldConfig WithSettings(this BuildFieldConfig config, FieldSettings settings)
        {
            config.FieldSettings = settings;
            return config;
        }

        public static BuildFieldConfig WithTransform(this BuildFieldConfig config, TransformQvvs transform)
        {
            config.Transform = transform;
            return config;
        }

        public static BuildFieldConfig WithObstacles(this BuildFieldConfig config, in CollisionLayer obstaclesLayer, CollisionLayerSettings obstaclesLayerSettings)
        {
            config.HasObstaclesLayer = true;
            config.ObstaclesLayer = obstaclesLayer;
            config.ObstaclesLayerSettings = obstaclesLayerSettings;
            return config;
        }

        public static BuildFieldConfig WithAgents(this BuildFieldConfig config, EntityQuery agentsQuery, in FlowFieldAgentsTypeHandles agentsHandles)
        {
            config.HasAgentsQuery = true;
            config.AgentsConfig = new BuildAgentsConfig
            {
                AgentsQuery = agentsQuery,
                AgentTypeHandles = agentsHandles,
            };
            return config;
        }

        #endregion

        #region Schedulers

        public static JobHandle ScheduleParallel(this BuildFieldConfig config, out Field field, AllocatorManager.AllocatorHandle allocator, JobHandle inputDeps = default)
        {
            config.ValidateSettings();
            field = new Field(config.FieldSettings, config.Transform, allocator);

            var dependency = inputDeps;
            dependency = new FlowFieldInternal.BuildCellsBodiesJob { Field = field }.ScheduleParallel(field.CellColliders.Length, 32, dependency);
            dependency = config.ProcessObstaclesLayer(in field, dependency);
            if (!config.HasAgentsQuery) return dependency;
            dependency = ScheduleParallel(config.AgentsConfig, in field, dependency);
            return dependency;
        }

        public static JobHandle ScheduleParallel(this BuildAgentsConfig config, in Field field, JobHandle inputDeps = default)
        {
            var dependency = inputDeps;
            var agentsCount = config.AgentsQuery.CalculateEntityCount();
            var capacity = agentsCount * 4;
            var densityHashMap = new NativeParallelMultiHashMap<int, float3>(capacity, Allocator.TempJob);

            dependency = new FlowFieldInternal.AgentsInfluenceJob
            {
                DensityHashMap = densityHashMap.AsParallelWriter(),
                Field = field,
                TypeHandles = config.AgentTypeHandles
            }.ScheduleParallel(config.AgentsQuery, dependency);

            dependency = new FlowFieldInternal.AgentsPostProcessJob
            {
                DensityHashMap = densityHashMap,
                DensityMap = field.DensityMap,
                MeanVelocityMap = field.MeanVelocityMap,
            }.ScheduleParallel(field.DensityMap.Length, 32, dependency);

            dependency = densityHashMap.Dispose(dependency);
            return dependency;
        }

        static JobHandle ProcessObstaclesLayer(this BuildFieldConfig config, in Field field, JobHandle dependency)
        {
            if (!config.HasObstaclesLayer) return dependency;

            var cellsHandle = Physics.BuildCollisionLayer(field.CellColliders).WithSettings(config.ObstaclesLayerSettings).ScheduleParallel(out var cells, Allocator.TempJob, dependency);
            var obstaclesJob = Physics.FindPairs(in config.ObstaclesLayer, in cells, new FlowFieldInternal.ObstaclesProcessor { Field = field }).ScheduleParallel(cellsHandle);
            return cells.Dispose(obstaclesJob);
        }

        #endregion

        #region Validators

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateSettings(this BuildFieldConfig config)
        {
            if (math.any(config.FieldSettings.FieldSize <= 0))
                throw new InvalidOperationException("BuildField requires a valid field size");
            if (math.any(config.FieldSettings.CellSize <= 0))
                throw new InvalidOperationException("BuildField requires a valid cell size");
        }

        #endregion
    }
}