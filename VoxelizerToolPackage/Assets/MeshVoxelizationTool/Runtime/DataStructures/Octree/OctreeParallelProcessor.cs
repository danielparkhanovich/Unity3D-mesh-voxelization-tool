using System;
using System.Threading;
using System.Threading.Tasks;

namespace Voxelization.DataStructures.Octree
{
    public class OctreeParallelProcessor : IOctreeProcessor
    {
        private object addNodesLock = new object();
        private object updateBoundsLock = new object();

        private readonly TaskFactory tasksFactory;


        public OctreeParallelProcessor()
        {
            tasksFactory = new TaskFactory(
                TaskCreationOptions.AttachedToParent,
                TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Start parallel tree build using TPL
        /// </summary>
        public void StartBuildTree(OctreeNode root, Action processNode)
        {
            root.Depth = 0;
            var task = Task.Factory.StartNew(() => processNode());
            task.Wait();
        }

        public void CallBuildNextNode(Action processNode)
        {
            tasksFactory.StartNew(() => processNode());
        }

        public void IncreaseNodesNumber(ref int nodes, int number)
        {
            Interlocked.Add(ref nodes, number);
        }

        public void CallAddNode(Action action)
        {
            lock (addNodesLock)
            {
                action();
            }
        }

        public void CallUpdateGlobalBounds(Action action)
        {
            lock (updateBoundsLock)
            {
                action();
            }
        }
    }
}
