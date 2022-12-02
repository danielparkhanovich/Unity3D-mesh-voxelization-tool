using System.Collections.Generic;
using UnityEngine;
using Voxelization.DataStructures.Geometry;
using Voxelization.Utils;

namespace Voxelization.DataStructures.Octree
{
    public class VoxelOctreeQuery
    {
        private readonly List<OctreeNode> nodes;

        private delegate bool IntersectsDelegate(OctreeNode node, out Vector3 hitPosition);
        private delegate T HitInfoDelegate<T>(OctreeNode node, out Vector3 hitPosition, out int hitFace);


        public VoxelOctreeQuery(List<OctreeNode> nodes)
        {
            this.nodes = nodes;
        }

        public bool IsVoxelAtPosition(Vector3 position, out OctreeNode voxel)
        {
            IntersectsDelegate pointIntersection = delegate (OctreeNode node, out Vector3 hitPosition)
            {
                hitPosition = Vector3.zero;
                return CollisionUtils.IsPointInsideOfAABB(position, node.AABB.Min, node.AABB.Max);
            };

            HitInfoDelegate<OctreeNode> hitInfo = delegate (OctreeNode node, out Vector3 hitPosition, out int hitFace)
            {
                hitPosition = Vector3.zero;
                hitFace = 0;
                return node;
            };

            voxel = null;

            var found = GetIntersectsVoxels<OctreeNode>(pointIntersection, hitInfo, true);
            if (found.Count > 0)
            {
                voxel = found[0];
                return true;
            }
            return false;
        }

        private List<T> GetIntersectsVoxels<T>(IntersectsDelegate intersection, HitInfoDelegate<T> hitInfo, bool isFirst)
        {
            List<T> hits = new List<T>();

            Vector3 hitPosition;

            Stack<OctreeNode> traverseNodes = new Stack<OctreeNode>();
            traverseNodes.Push(nodes[0]);

            // Traverse tree
            while (traverseNodes.Count != 0)
            {
                var traverseNode = traverseNodes.Pop();

                if (intersection(traverseNode, out hitPosition))
                {
                    // Leaf node
                    if (traverseNode.IsLeaf)
                    {
                        int hitFace;
                        var hit = hitInfo(traverseNode, out hitPosition, out hitFace);
                        hits.Add(hit);

                        if (isFirst)
                        {
                            return hits;
                        }
                        continue;
                    }

                    // Inner node
                    for (int i = 0; i < traverseNode.ChildNodes.Length; i++)
                    {
                        traverseNodes.Push(nodes[traverseNode.ChildNodes[i]]);
                    }
                }
            }
            return hits;
        }
    }
}
