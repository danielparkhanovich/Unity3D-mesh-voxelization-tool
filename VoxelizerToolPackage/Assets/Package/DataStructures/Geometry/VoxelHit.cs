using System.Runtime.InteropServices;
using UnityEngine;
using Voxelization.DataStructures.Octree;

namespace Voxelization.DataStructures.Geometry
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VoxelHit
    {
        public Vector3 Position;
        public int Face;
        public OctreeNode Voxel;
    }
}