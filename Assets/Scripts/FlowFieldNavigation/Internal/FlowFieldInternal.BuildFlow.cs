﻿using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace FlowFieldNavigation
{
    internal static partial class FlowFieldInternal
    {
        [BurstCompile]
        internal struct CollectGoalsJob : IJobChunk
        {
            [ReadOnly] internal Field Field;
            [ReadOnly] internal FlowGoalTypeHandles TypeHandles;

            internal NativeList<int2>.ParallelWriter GoalCells;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var chunkTransforms = TypeHandles.WorldTransform.Resolve(chunk);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var i))
                {
                    var position = chunkTransforms[i].position;

                    if (!Field.TryWorldToFootprint(position, out var footprint, out _)) continue;
                    GoalCells.AddNoResize(footprint.xy);
                    if (footprint.x != footprint.z && footprint.y != footprint.w)
                    {
                        GoalCells.AddNoResize(footprint.zy);
                        GoalCells.AddNoResize(footprint.xw);
                        GoalCells.AddNoResize(footprint.zw);
                    }
                }
            }
        }
        
        [BurstCompile]
        internal struct CalculateCostsWithPriorityQueueJob : IJob
        {
            [ReadOnly] internal Field Field;
            [ReadOnly] internal NativeList<int2> GoalCells;
            
            internal NativeArray<float> Costs;

            public void Execute()
            {
                int width = Field.Width;
                int height = Field.Height;

                for (var i = 0; i < Costs.Length; i++) Costs[i] = FlowSettings.PassabilityLimit + 1;
                int initialHeapCapacity = math.max(64, (int)(width * height * FlowSettings.HeapCapacityFactor));
                var queue = new NativePriorityQueue<CostEntry, CostComparer>(initialHeapCapacity, Allocator.Temp);
                var visited = new NativeBitArray(width * height, Allocator.Temp);
                
                foreach (var goal in GoalCells)
                {
                    queue.Enqueue(new(goal, 0));
                    Costs[Grid.CellToIndex(width, goal)] = 0;
                }
                
                while (queue.TryDequeue(out var current))
                {
                    var currentCost = Costs[Grid.CellToIndex(width, current.Pos)];

                    for (var dir = Grid.Direction.Up; dir <= Grid.Direction.DownRight; dir++)
                    {
                        if (!Grid.TryGetNeighborCell(width, height, current.Pos, dir, out var neighbor)) continue;

                        var neighborIndex = Grid.CellToIndex(width, neighbor);
                        if (visited.IsSet(neighborIndex)) continue;

                        if (Field.PassabilityMap[neighborIndex] < 0) continue;

                        var moveCost = Field.PassabilityMap[neighborIndex] + (dir > Grid.Direction.Right ? math.SQRT2 : 1);

                        var newCost = currentCost + moveCost;

                        if (newCost < Costs[neighborIndex])
                        {
                            Costs[neighborIndex] = newCost;
                            queue.Enqueue(new(neighbor, newCost));
                            visited.Set(neighborIndex, true);
                        }
                    }
                }

                queue.Dispose();
                visited.Dispose();
            }

            struct CostComparer : IComparer<CostEntry>
            {
                public int Compare(CostEntry x,
                    CostEntry y) => x.Cost.CompareTo(y.Cost);
            }

            readonly struct CostEntry
            {
                public readonly float Cost;
                public readonly int2 Pos;

                public CostEntry(int2 pos, float cost)
                {
                    Pos = pos;
                    Cost = cost;
                }
            }
        }

        [BurstCompile]
        internal struct CalculateDirectionJob : IJobFor
        {
            [ReadOnly] internal NativeArray<float> CostField;
            [ReadOnly] internal NativeArray<float> DensityField;
            
            internal FlowSettings Settings;
            internal NativeArray<float2> DirectionMap;
            internal int Width;
            internal int Height;
        
            public void Execute(int index)
            {
                var cell = Grid.IndexToCell(index, Width);
                var gradient = float2.zero;
                var currentCost = CostField[index];
        
                if (currentCost <= 0)
                {
                    DirectionMap[index] = float2.zero;
                    return;
                }
        
                var current = currentCost * Settings.PassabilityMultiplier + DensityField[index] * Settings.DensityMultiplier;
        
                for (var dir = Grid.Direction.Up; dir <= Grid.Direction.Right; dir++)
                {
                    if (!Grid.TryGetNeighborCell(Width, Height, cell, dir, out var neighbor)) continue;
        
                    var neighborIndex = Grid.CellToIndex(Width, neighbor);
                    var neighborCost = CostField[neighborIndex];
                    if(neighborCost > FlowSettings.PassabilityLimit) continue;
                    var resultCost = neighborCost * Settings.PassabilityMultiplier + DensityField[neighborIndex] * Settings.DensityMultiplier;
        
                    var costDifference = resultCost - current;
                    var addGradient = costDifference * dir.ToVector();
                    gradient += addGradient;
                }
        
                DirectionMap[index] = math.normalizesafe(-gradient);
            }
        }
    }
}