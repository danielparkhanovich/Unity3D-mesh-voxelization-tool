using UnityEngine;
using Voxelization.DataStructures;

namespace Voxelization.DataStructures.Octree
{
    public class OctreeNode : AABBNode
    {
        public bool IsLeaf { get; set; }
        /// <summary>
        /// Indexes of childs
        /// </summary>
        public int[] Childs { get; set; }
        public OctreeNode Parent { get; set; }

        /// <summary>
        /// AABB Min used for faster collision check (during tree traverse)
        /// </summary>
        public Vector3 FilledMin { get; set; }
        /// <summary>
        /// AABB Max used for faster collision check (during tree traverse)
        /// </summary>
        public Vector3 FilledMax { get; set; }

        // For materials
        public Vector2 Uv { get; set; }
        public int SubMeshAssign { get; set; }

        // For animations
        public BoneWeight Bw { get; set; }
        public Matrix4x4 BindPose { get; set; }
    }
}