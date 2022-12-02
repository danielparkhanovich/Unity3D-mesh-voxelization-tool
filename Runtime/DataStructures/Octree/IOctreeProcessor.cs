
using System;

namespace Voxelization.DataStructures.Octree
{
    public interface IOctreeProcessor
    {
        void StartBuildTree(OctreeNode root, Action processNode);
        void CallBuildNextNode(Action processNode);
        void CallAddNode(Action action);
        void CallUpdateGlobalBounds(Action action);
        void IncreaseNodesNumber(ref int nodes, int number);
    }
}
