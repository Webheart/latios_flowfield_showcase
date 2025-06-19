using Unity.Entities;
using UnityEngine;

public class IntervalSpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float Interval;
    public int Batch;
    public int MaxCount;

    public class IntervalSpawnerBaker : Baker<IntervalSpawnerAuthoring>
    {
        public override void Bake(IntervalSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new IntervalSpawner
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                Interval = authoring.Interval,
                Batch = authoring.Batch,
                MaxCount = authoring.MaxCount
            });
        }
    }
}

public struct IntervalSpawner : IComponentData
{
    public Entity Prefab;
    public float Interval;
    public int Batch;
    public int MaxCount;

    public int Count;
    public float Elapsed;
}