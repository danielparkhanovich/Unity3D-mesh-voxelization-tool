using System;

namespace Voxelization.DataStructures.Octree
{
    public class OctreeProcessor : IOctreeProcessor
    {
        public void StartBuildTree(OctreeNode root, Action processNode)
        {
            root.Depth = 0;
            processNode();
        }

        public void CallBuildNextNode(Action processNode)
        {
            processNode();
        }

        public void IncreaseNodesNumber(ref int nodes, int number)
        {
            nodes += number;
        }

        public void CallAddNode(Action action)
        {
            action();
        }

        public void CallUpdateGlobalBounds(Action action)
        {
            action();
        }
    }
}