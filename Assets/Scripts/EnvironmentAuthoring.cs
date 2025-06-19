using Unity.Entities;
using UnityEngine;

public class EnvironmentAuthoring : MonoBehaviour
{
    class Baker : Baker<EnvironmentAuthoring>
    {
        public override void Bake(EnvironmentAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Renderable);
            AddComponent<EnvironmentLayerTag>(entity);
        }
    }
}