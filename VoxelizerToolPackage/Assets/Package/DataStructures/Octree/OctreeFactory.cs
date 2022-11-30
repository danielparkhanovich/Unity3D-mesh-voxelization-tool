using UnityEngine;
using Voxelization.Tools;

namespace Voxelization.DataStructures.Octree
{
    public class OctreeFactory
    {
        private readonly float voxelSize;
        private readonly ProcessingType processingType;


        public OctreeFactory(float voxelSize, ProcessingType processingType)
        {
            this.voxelSize = voxelSize;
            this.processingType = processingType;
        }

        public VoxelOctree GetOctree(params Mesh[] meshes)
        {
            if (processingType == ProcessingType.Single_thread)
            {
                return new VoxelOctree(voxelSize, new OctreeProcessor(), meshes);
            }
            else
            {
                return new VoxelOctree(voxelSize, new OctreeParallelProcessor(), meshes);
            }
        }
    }
}