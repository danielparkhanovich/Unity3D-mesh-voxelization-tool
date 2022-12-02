using System.Collections.Generic;
using UnityEngine;
using Voxelization.DataStructures.Octree;

namespace Voxelization.DataStructures
{
    public struct MeshData
    {
        public List<Vector3> Vertices;
        public List<int>[] Indices;
        public List<Vector2> Uv;
        public List<BoneWeight> BoneWeights;
        public Matrix4x4[] BindPoses;


        public MeshData(VoxelOctree tree, Matrix4x4[] bindPoses = null)
        {
            this.Vertices = new List<Vector3>();
            this.Indices  = new List<int>[tree.SubMeshesIndices.Length];
            for (int i = 0; i < this.Indices.Length; i++)
            {
                this.Indices[i] = new List<int>();
            }
            this.Uv = new List<Vector2>();
            this.BoneWeights = new List<BoneWeight>();
            this.BindPoses = bindPoses;
        }
    }
}
