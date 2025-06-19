using FlowFieldNavigation;
using Latios;
using Latios.Transforms;
using Unity.Entities;
using Unity.Jobs;

public struct FlowFieldSettings : IComponentData
{
    public TransformQvvs FlowFieldTransform;
    public FlowSettings FlowSettings;
    public FieldSettings FieldSettings;
    public bool DrawDebugGizmos;
}
public partial struct FieldContainer : ICollectionComponent
{
    public Field Field;
    public JobHandle TryDispose(JobHandle inputDeps) => Field.IsCreated ? Field.Dispose(inputDeps) : inputDeps;
}
public partial struct FlowContainer : ICollectionComponent
{
    public Flow Flow;
    public JobHandle TryDispose(JobHandle inputDeps) => Flow.IsCreated ? Flow.Dispose(inputDeps) : inputDeps;
}