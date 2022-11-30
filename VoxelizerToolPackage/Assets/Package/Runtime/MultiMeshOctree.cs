using System.Collections.Generic;
using UnityEngine;
using Voxelization.DataStructures.Octree;

namespace Voxelization
{
    /// <typeparam name="T">SkinnedMeshRenderer or MeshFilter</typeparam>
    public class MultiMeshOctree<T> where T : Component
    {
        public Dictionary<T, VoxelOctree> TreesDict { get; internal set; }
        public List<VoxelOctree> Trees { get; internal set; }


        public MultiMeshOctree()
        {
            this.TreesDict = new Dictionary<T, VoxelOctree>();
            this.Trees = new List<VoxelOctree>();
        }

        public void AddNewTree(T filter, VoxelOctree tree)
        {
            this.TreesDict.Add(filter, tree);
            this.Trees.Add(tree);
        }

        public void RemoveIntersectingVoxels()
        {
            for (int i = 0; i < Trees.Count; i++)
            {
                var tree1 = Trees[i];
                for (int j = i + 1; j < Trees.Count; j++)
                {
                    var tree2 = Trees[j];
                    for (int k = tree2.Voxels.Count - 1; k > -1; k--)
                    {
                        var voxel = tree2.Voxels[k];
                        OctreeNode outVoxel;
                        if (tree1.Query.IsVoxelAtPosition(voxel.AABB.Center, out outVoxel))
                        {
                            tree2.Voxels.RemoveAt(k);
                        }
                    }
                }
            }
        }
    }
}
