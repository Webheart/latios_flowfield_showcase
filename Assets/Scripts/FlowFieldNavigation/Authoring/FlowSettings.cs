using System;
using UnityEngine;

namespace FlowFieldNavigation
{
    [Serializable]
    public struct FlowSettings
    {
        internal const float PassabilityLimit = 50000;
        internal const float HeapCapacityFactor = 0.35f;

        [Range(0, 5)]
        public float DensityMultiplier;
        [Range(0.1f, 5)]
        public float PassabilityMultiplier;

        public static FlowSettings Default => new()
        {
            DensityMultiplier = 1f,
            PassabilityMultiplier = 1f,
        };
    }
}