using UnityEngine;
using Voxelization.DataStructures.Octree;

namespace Voxelization.Tools
{
    public class VoxelizerMono : MonoBehaviour
    {
        [SerializeField]
        private GameObject objectToVoxelize;

        [SerializeField]
        private float voxelSize;

        [SerializeField]
        private bool isDebugTree;

        private VoxelOctree voxelMeshTree;
        public VoxelOctree VoxelMeshTree { get => voxelMeshTree; }


        [ContextMenu("CreateObject")]
        private void VoxelizeObject()
        {
            Voxelizer voxelizer = new Voxelizer(voxelSize, ProcessingType.Multi_thread);
            voxelizer.VoxelizeToSingleObject(objectToVoxelize, false, MeshType.All);
        }

        [ContextMenu("ReplaceMeshes")]
        private void ReplaceMeshesToVoxelMeshes()
        {
            Voxelizer voxelizer = new Voxelizer(voxelSize, ProcessingType.Multi_thread);
            voxelizer.Voxelize(objectToVoxelize, true, MeshType.All);
        }

        private void OnDrawGizmosSelected()
        {
            if (voxelMeshTree == null)
            {
                return;
            }

            if (isDebugTree)
            {
                var bounds = voxelMeshTree.Nodes;
                for (int i = 0; i < bounds.Count; i++)
                {
                    var bound = bounds[i].AABB;
                    var randomColor = Color.red;
                    Gizmos.color = randomColor;
                    Gizmos.DrawWireCube(bound.Center, bound.Size);
                }
            }
        }
    }
}
