using Latios;
using Latios.Psyshock;
using Unity.Entities;
using Unity.Jobs;

public partial struct EnvironmentCollisionLayer : ICollectionComponent
{
    public CollisionLayer Layer;

    JobHandle ICollectionComponent.TryDispose(JobHandle inputDeps) => Layer.IsCreated ? Layer.Dispose(inputDeps) : inputDeps;
}

public partial struct AgentCollisionLayer : ICollectionComponent
{
    public CollisionLayer Layer;

    JobHandle ICollectionComponent.TryDispose(JobHandle inputDeps) => Layer.IsCreated ? Layer.Dispose(inputDeps) : inputDeps;
}

public struct EnvironmentLayerTag : IComponentData { }