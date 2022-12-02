using Voxelization.DataStructures.Geometry;

namespace Voxelization.DataStructures
{
    public class AABBNode
    {
        public AABB AABB { get; set; }
        public int Depth { get; set; }
    }
}