using UnityEngine;

namespace Voxelization.DataStructures.Octree
{
    public class OctreeNode : AABBNode
    {
        public bool IsLeaf { get; set; }
        /// <summary>
        /// Indices of childs
        /// </summary>
        public int[] ChildNodes { get; set; }

        // For materials
        public Vector2 Uv { get; set; }
        public int SubMeshAssign { get; set; }

        // For animations
        public BoneWeight Bw { get; set; }
        public Matrix4x4 BindPose { get; set; }
    }
}