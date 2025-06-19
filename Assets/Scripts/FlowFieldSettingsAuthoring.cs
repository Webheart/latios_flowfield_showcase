using FlowFieldNavigation;
using Latios.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FlowFieldSettingsAuthoring : MonoBehaviour
{
    public Vector3 TransformPosition;
    public Vector3 TransformRotation;
    public FieldSettings FieldSettings;
    public FlowSettings FlowSettings;
    public bool DrawDebugGizmos;

    void Reset()
    {
        FieldSettings = FieldSettings.Default;
        FlowSettings = FlowSettings.Default;
        TransformPosition = Vector3.zero;
        TransformRotation = Vector3.zero;
    }

    class Baker : Baker<FlowFieldSettingsAuthoring>
    {
        public override void Bake(FlowFieldSettingsAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new FlowFieldSettings
            {
                FlowSettings = authoring.FlowSettings,
                FieldSettings = authoring.FieldSettings,
                FlowFieldTransform = new TransformQvvs(authoring.TransformPosition, quaternion.Euler(authoring.TransformRotation)),
                DrawDebugGizmos = authoring.DrawDebugGizmos
            });
        }
    }
}