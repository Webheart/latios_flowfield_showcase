using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Entities;

[RequireMatchingQueriesForUpdate]
public partial struct IntervalSpawnerSystem : ISystem
{
    LatiosWorldUnmanaged latiosWorld;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        latiosWorld = state.GetLatiosWorldUnmanaged();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new Job
        {
            CommandBuffer = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<WorldTransform>(),
            DeltaTime = state.WorldUnmanaged.Time.DeltaTime,
        }.Schedule();
    }

    [BurstCompile]
    partial struct Job : IJobEntity
    {
        public InstantiateCommandBuffer<WorldTransform> CommandBuffer;
        public float DeltaTime;

        void Execute(ref IntervalSpawner spawner, in WorldTransform transform)
        {
            if (spawner.MaxCount > 0 && spawner.MaxCount < spawner.Count)
                return;

            spawner.Elapsed += DeltaTime;
            if (spawner.Elapsed >= spawner.Interval)
            {
                spawner.Elapsed -= spawner.Interval;

                for (int i = 0; i < spawner.Batch; i++)
                {
                    var spawnedTransform = transform;
                    spawnedTransform.worldTransform.position += i * 0.1f;
                    CommandBuffer.Add(spawner.Prefab, spawnedTransform);

                    spawner.Count++;
                }
            }
        }
    }
}