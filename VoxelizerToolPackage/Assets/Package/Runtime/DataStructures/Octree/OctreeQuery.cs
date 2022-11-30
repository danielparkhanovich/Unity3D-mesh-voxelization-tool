using System.Collections.Generic;
using UnityEngine;
using Voxelization.DataStructures.Geometry;
using Voxelization.Utils;

namespace Voxelization.DataStructures.Octree
{
    public class OctreeQuery
    {
        private readonly List<OctreeNode> nodes;

        private delegate bool IntersectsDelegate(OctreeNode node, out Vector3 hitPosition);
        private delegate T HitInfoDelegate<T>(OctreeNode node, out Vector3 hitPosition, out int hitFace);


        public OctreeQuery(List<OctreeNode> nodes)
        {
            this.nodes = nodes;
        }

        public VoxelHit GetFirstHit(Vector3 fromPoint, Vector3 toPoint)
        {
            VoxelLine line = new VoxelLine(fromPoint, toPoint);

            IntersectsDelegate lineIntersection = delegate(OctreeNode node, out Vector3 hitPosition)
            {
                return line.IntersectLineBox(node.FilledMin, node.FilledMax, out hitPosition);
            };
            HitInfoDelegate<VoxelHit> hitInfo = delegate(OctreeNode node, out Vector3 hitPosition, out int hitFace)
            {
                line.GetVoxelIntersection(node.FilledMin, node.FilledMax, fromPoint, toPoint, out hitPosition, out hitFace);
                return new VoxelHit() { Voxel = node, Position = hitPosition, Face = hitFace };
            };

            var found = GetIntersectsVoxels<VoxelHit>(lineIntersection, hitInfo, true);
            if (found.Count > 0)
            {
                return found[0];
            }
            return new VoxelHit();
        }

        public bool IsAnyVoxel(Vector3 fromPoint, Vector3 toPoint)
        {
            return GetFirstHit(fromPoint, toPoint).Voxel != null;
        }

        public VoxelHit GetFirstHitOnRay(Vector3 fromPoint, Vector3 direction)
        {
            VoxelRay ray = new VoxelRay(fromPoint, direction);

            IntersectsDelegate rayIntersection = delegate(OctreeNode node, out Vector3 hitPosition)
            {
                // TODO: add out Vector3 to IntersectRayBox method for debugging purposes
                hitPosition = Vector3.zero;
                return ray.IntersectRayBox(node.FilledMin, node.FilledMax);
            };
            HitInfoDelegate<VoxelHit> hitInfo = delegate(OctreeNode node, out Vector3 hitPosition, out int hitFace)
            {
                // TODO: add new method with out hitPosition and out hitFace in VoxelRay class for debugging purposes
                hitPosition = Vector3.zero;
                hitFace = 0;
                return new VoxelHit() { Voxel = node, Position = hitPosition, Face = hitFace };
            };

            var found = GetIntersectsVoxels<VoxelHit>(rayIntersection, hitInfo, true);
            if (found.Count > 0)
            {
                return found[0];
            }
            return new VoxelHit();
        }

        public bool IsAnyVoxelOnRay(Vector3 fromPoint, Vector3 direction)
        {
            return GetFirstHitOnRay(fromPoint, direction).Voxel != null;
        }

        public bool IsVoxelAtPosition(Vector3 position, out OctreeNode voxel)
        {
            IntersectsDelegate pointIntersection = delegate (OctreeNode node, out Vector3 hitPosition)
            {
                hitPosition = Vector3.zero;
                return CollisionUtils.IsPointInsideOfAABB(position, node.FilledMin, node.FilledMax);
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
                    for (int i = 0; i < traverseNode.Childs.Length; i++)
                    {
                        traverseNodes.Push(nodes[traverseNode.Childs[i]]);
                    }
                }
            }
            return hits;
        }

        
    }
}
