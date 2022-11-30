using System.Collections.Generic;
using UnityEngine;
using Voxelization.DataStructures.Geometry;
using Voxelization.Utils;

namespace Voxelization.DataStructures.Octree
{
    public class VoxelOctree
    {
        /// <summary>
        /// Vertices of the all meshes to voxelize
        /// </summary>
        private List<Vector3> vertices;
        /// <summary>
        /// Indices of the all meshes to voxelize
        /// </summary>
        private List<int> indices;
        /// <summary>
        /// Uvs of the all meshes to voxelize
        /// </summary>
        private List<Vector2> uvs;
        /// <summary>
        /// Bone weights of the all meshes to voxelize
        /// </summary>
        private List<BoneWeight> bws;
        /// <summary>
        /// All faces to voxelize (.Count = indices.Count / 3)
        /// </summary>
        private List<int> allTriangles;

        private float voxelSize;

        private int maxTreeDepth;
        private int innerNodes;
        private int leafsNodes;
        private int freeNodeIndex;

        private IOctreeProcessor octreeProcessor;

        public int[] SubMeshesIndices { get; internal set; }

        public OctreeQuery Query { get; internal set; }
        public List<OctreeNode> Nodes { get; internal set; }
        public List<OctreeNode> Voxels { get; internal set; }
        public Vector3[] Bounds { get; set; }
        public float VoxelSize { get => voxelSize; }


        public VoxelOctree(float voxelSize, IOctreeProcessor octreeProcessor, params Mesh[] sharedMeshes)
        {
            this.octreeProcessor = octreeProcessor;
            int objectsNumber = sharedMeshes.Length;

            this.voxelSize = voxelSize;
            this.vertices = new List<Vector3>();
            this.indices = new List<int>();
            this.uvs = new List<Vector2>();
            this.bws = new List<BoneWeight>();

            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            int indicesOffset = 0;
            var subMeshesIndicesList = new List<int>();
            for (int i = 0; i < objectsNumber; i++)
            {
                Mesh mesh = sharedMeshes[i];

                int[] tmpMeshIndices;
                (indicesOffset, tmpMeshIndices) = MeshPropertiesUtils.MergeMeshProperties(mesh, indicesOffset, vertices, uvs, bws, indices);
                subMeshesIndicesList.AddRange(tmpMeshIndices);

                min = FastMathUtils.Min(mesh.bounds.min, min);
                max = FastMathUtils.Max(mesh.bounds.max, max);
            }
            this.SubMeshesIndices = subMeshesIndicesList.ToArray();

            InitRootAABB(min, max);
        }

        /// <summary>
        /// Build Adjacency matrix, empty - 0, voxel - 1
        /// </summary>
        public static byte[,,] BuildAdjMatrix(List<OctreeNode> nodes, Vector3 minVoxelBox, Vector3 maxVoxelBox, float voxelSize, out Vector3Int voxelsNum)
        {
            voxelsNum = Vector3Int.zero;

            if (nodes.Count == 0)
            {
                return new byte[0, 0, 0];
            }

            byte[,,] voxels;

            int voxelsX = (int)((maxVoxelBox.x - minVoxelBox.x) / voxelSize) + 1;
            int voxelsY = (int)((maxVoxelBox.y - minVoxelBox.y) / voxelSize) + 1;
            int voxelsZ = (int)((maxVoxelBox.z - minVoxelBox.z) / voxelSize) + 1;

            voxels = new byte[voxelsX, voxelsY, voxelsZ];
            foreach (var node in nodes)
            {
                var pos = node.AABB.Min;
                int x = FastMathUtils.TranslatePosition(minVoxelBox.x, pos.x, voxelSize);
                int y = FastMathUtils.TranslatePosition(minVoxelBox.y, pos.y, voxelSize);
                int z = FastMathUtils.TranslatePosition(minVoxelBox.z, pos.z, voxelSize);
                voxels[x, y, z] = 1;
            }

            voxelsNum = new Vector3Int(voxelsX, voxelsY, voxelsZ);

            return voxels;
        }

        private void InitRootAABB(Vector3 min, Vector3 max)
        {
            // Create bounding box (cube) from cuboid
            float width = max.x - min.x;
            float height = max.y - min.y;
            float depth = max.z - min.z;
            float maxValue = FastMathUtils.Max(FastMathUtils.Max(width, height), depth);

            float newTemp = this.voxelSize;
            for (; newTemp < maxValue; newTemp *= 2)
            {
            }
            maxValue = newTemp;

            int voxelsPerSide = (int)(maxValue / this.voxelSize);
            this.maxTreeDepth = FastMathUtils.Log2(voxelsPerSide);

            max = min + (Vector3.one * maxValue);

            OctreeNode root = new OctreeNode();
            root.AABB = new AABB(min, max);

            this.Nodes = new List<OctreeNode>();
            this.Nodes.Add(root);

            // 0 - element of Vector3 array is MIN | 1 - is MAX
            this.Bounds = new Vector3[] { Vector3.positiveInfinity, Vector3.negativeInfinity };

            this.allTriangles = new List<int>(indices.Count / 3);
            for (int i = 0; i < allTriangles.Capacity; i++)
            {
                this.allTriangles.Add(i);
            }
        }

        public void Voxelize()
        {
            var root = Nodes[0];
            BuildTree(root, this.allTriangles);
            this.Query = new OctreeQuery(this.Nodes);
        }

        private void BuildTree(OctreeNode root, List<int> triangles)
        {
            octreeProcessor.StartBuildTree(root, () => BuildNode(root, triangles));

            this.Voxels = new List<OctreeNode>();
            foreach (var node in this.Nodes)
            {
                if (node.IsLeaf)
                {
                    this.Voxels.Add(node);
                }
            }
            Dispose();
        }

        /// <summary>
        /// Delete used data
        /// </summary>
        private void Dispose()
        {
            indices = null;
            vertices = null;
            allTriangles = null;
            bws = null;
            uvs = null;
        }

        private void BuildNode(OctreeNode node, List<int> triangles)
        {
            node.FilledMin = Vector3.positiveInfinity;
            node.FilledMax = Vector3.negativeInfinity;

            int nextDepth = node.Depth + 1;

            // Inner node
            octreeProcessor.IncreaseNodesNumber(ref innerNodes, 1);

            List<OctreeNode> childs = new List<OctreeNode>();
            List<List<int>> childsTriangles = new List<List<int>>();
            DivideToOct(node, triangles, ref childs, ref childsTriangles, nextDepth);

            // Process children
            int num = childs.Count;
            node.Childs = new int[num];
            for (int i = 0; i < num; i++)
            {
                childs[i].Depth = nextDepth;
                childs[i].Parent = node;

                octreeProcessor.CallAddNode(() => {
                    freeNodeIndex += 1;
                    node.Childs[i] = freeNodeIndex;
                    Nodes.Add(childs[i]);
                });

                // No triangles - skip child (no collisions)
                if (childsTriangles[i].Count == 0)
                {
                    continue;
                }

                // Leaf node -> voxel
                if (nextDepth >= maxTreeDepth)
                {
                    ProcessLeaf(node, childs[i], childsTriangles[i][0]);
                    continue;
                }

                var icopy = i;
                octreeProcessor.CallBuildNextNode(() => BuildNode(childs[icopy], childsTriangles[icopy]));
            }

            if (nextDepth >= maxTreeDepth)
            {
                octreeProcessor.CallUpdateBounds(() => {
                    var prevNode = node;
                    var currentParent = node.Parent;
                    while (currentParent != null)
                    {
                        currentParent.FilledMin = FastMathUtils.Min(prevNode.FilledMin, currentParent.FilledMin);
                        currentParent.FilledMax = FastMathUtils.Max(prevNode.FilledMax, currentParent.FilledMax);
                        prevNode = currentParent;
                        currentParent = currentParent.Parent;
                    }
                });
            }
        }

        private void ProcessLeaf(OctreeNode parent, OctreeNode child, int childTriangle)
        {
            child.IsLeaf = true;

            octreeProcessor.IncreaseNodesNumber(ref leafsNodes, 1);

            int index = childTriangle * 3;
            bool isApproximateProperties = (uvs.Count != 0 || bws.Count != 0);
            if (isApproximateProperties)
            {
                Vector3 a, b, c;
                MeshUtils.GetTriangleVertices(vertices, indices, index, out a, out b, out c);

                (float ratio, int closest) = MeshPropertiesUtils.GetRationAndTriIndex(child.AABB.Center, a, b, c);
                int closestIndex = index + closest;

                if (uvs.Count > indices[index])
                {
                    Vector2 uv1, uv2, uv3;
                    MeshUtils.GetTriangleVertices(uvs, indices, index, out uv1, out uv2, out uv3);
                    var centroidUv = MeshPropertiesUtils.GetCentroid2D(uv1, uv2, uv3);
                    var lerped = Vector2.Lerp(uvs[indices[closestIndex]], centroidUv, ratio);
                    child.Uv = lerped;
                }
                if (bws.Count > indices[index])
                {
                    BoneWeight bw1, bw2, bw3;
                    MeshUtils.GetTriangleVertices(bws, indices, index, out bw1, out bw2, out bw3);
                    var centroidBw = MeshPropertiesUtils.GetCentroidBoneWeight(bw1, bw2, bw3);
                    var lerped = MeshPropertiesUtils.LerpBoneWeight(bws[indices[closestIndex]], centroidBw, ratio);
                    child.Bw = bws[indices[closestIndex]];
                }
            }

            parent.FilledMin = FastMathUtils.Min(child.AABB.Min, parent.FilledMin);
            parent.FilledMax = FastMathUtils.Max(child.AABB.Max, parent.FilledMax);

            child.FilledMin = child.AABB.Min;
            child.FilledMax = child.AABB.Max;

            child.SubMeshAssign = MeshUtils.GetCurrentSubMeshIndex(index, SubMeshesIndices);

            octreeProcessor.CallUpdateGlobalBounds(() => {
                Bounds[0] = FastMathUtils.Min(parent.FilledMin, Bounds[0]);
                Bounds[1] = FastMathUtils.Max(parent.FilledMax, Bounds[1]);
            });
        }

        private void DivideToOct(OctreeNode nodeToDivide, List<int> triangles, ref List<OctreeNode> outNodes, ref List<List<int>> childsTriangles, int depth)
        {
            Stack<OctreeNode> nodeStack = new Stack<OctreeNode>();
            Stack<List<int>> trianglesStack = new Stack<List<int>>();
            Stack<int> divideTypeStack = new Stack<int>();

            nodeStack.Push(nodeToDivide);
            trianglesStack.Push(triangles);
            divideTypeStack.Push(0);

            while (nodeStack.Count != 0)
            {
                OctreeNode node = nodeStack.Pop();
                List<int> nodeTriangles = trianglesStack.Pop();
                int divideType = divideTypeStack.Pop();

                OctreeNode leftChild, rightChild;
                List<int> leftTriangles, rightTriangles;
                GroupTriangles(depth, node.AABB, nodeTriangles, divideType, out leftChild, out leftTriangles, out rightChild, out rightTriangles);

                // Leaf
                if (divideType > 1)
                {
                    if (leftTriangles.Count != 0)
                    {
                        childsTriangles.Add(leftTriangles);
                        outNodes.Add(leftChild);
                    }
                    if (rightTriangles.Count != 0)
                    {
                        childsTriangles.Add(rightTriangles);
                        outNodes.Add(rightChild);
                    }
                    continue;
                }

                if (leftTriangles.Count != 0)
                {
                    nodeStack.Push(leftChild);
                    trianglesStack.Push(leftTriangles);
                    divideTypeStack.Push(divideType + 1);
                }

                if (rightTriangles.Count != 0)
                {
                    nodeStack.Push(rightChild);
                    trianglesStack.Push(rightTriangles);
                    divideTypeStack.Push(divideType + 1);
                }
            }
        }

        private void GroupTriangles(int depth, AABB nodeBox, IList<int> polygons, int divideType, out OctreeNode leftChild, out List<int> leftTriangles, out OctreeNode rightChild, out List<int> rightTriangles)
        {
            AABB leftBox, rightBox;
            BinaryDivideAABB(nodeBox, divideType, out leftBox, out rightBox);

            leftChild = new OctreeNode();
            leftChild.AABB = leftBox;

            rightChild = new OctreeNode();
            rightChild.AABB = rightBox;

            leftTriangles = new List<int>();
            rightTriangles = new List<int>();

            // Fast collision check for leafs
            if (depth >= maxTreeDepth && divideType == 2)
            {
                for (int i = 0; i < polygons.Count; i++)
                {
                    int index = polygons[i] * 3;
                    Vector3 a, b, c;
                    MeshUtils.GetTriangleVertices(vertices, indices, index, out a, out b, out c);

                    if (IntersectsLeaf(a, b, c, leftBox))
                    {
                        leftTriangles.Add(polygons[i]);
                    }
                    if (IntersectsLeaf(a, b, c, rightBox))
                    {
                        rightTriangles.Add(polygons[i]);
                    }
                }
            }
            // Other collision check - inner nodes
            else
            {
                for (int i = 0; i < polygons.Count; i++)
                {
                    int index = polygons[i] * 3;
                    Vector3 a, b, c;
                    MeshUtils.GetTriangleVertices(vertices, indices, index, out a, out b, out c);

                    if (IntersectsMixed(a, b, c, leftBox, depth))
                    {
                        leftTriangles.Add(polygons[i]);
                    }
                    if (IntersectsMixed(a, b, c, rightBox, depth))
                    {
                        rightTriangles.Add(polygons[i]);
                    }
                }
            }
        }

        private bool IntersectsMixed(Vector3 a, Vector3 b, Vector3 c, AABB box, int depth)
        {
            Vector3 minTri, maxTri;
            CollisionUtils.GetMinMax(a, b, c, out minTri, out maxTri);

            if (CollisionUtils.IsInsideOfAABB(minTri, maxTri, box.Min, box.Max))
            {
                return true;
            }
            return CollisionUtils.IntersectsAabbAabb(minTri, maxTri, box.Min, box.Max);
        }

        private bool IntersectsLeaf(Vector3 a, Vector3 b, Vector3 c, AABB box)
        {
            return CollisionUtils.IntersectsTriangleAabb(a, b, c, box);
        }

        /// <param name="divideType">Divide types: 0 - split along X axis, 1 - Y axis, 2 - Z axis</param>
        private void BinaryDivideAABB(AABB box, int divideType, out AABB leftBox, out AABB rightBox)
        {
            Vector3 min1 = box.Min, max1 = box.Max;
            Vector3 min2 = box.Min, max2 = box.Max;
            if (divideType == 0)
            {
                float half = box.Width / 2f;
                max1.x -= half;
                min2.x += half;
            }
            else if (divideType == 1)
            {
                float half = box.Height / 2f;
                max1.y -= half;
                min2.y += half;
            }
            else
            {
                float half = box.Depth / 2f;
                max1.z -= half;
                min2.z += half;
            }

            leftBox = new AABB(min1, max1);
            rightBox = new AABB(min2, max2);
        }
    }
}