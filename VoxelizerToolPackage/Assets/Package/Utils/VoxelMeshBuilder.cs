using UnityEngine;
using UnityEngine.Rendering;
using Voxelization.DataStructures;
using Voxelization.DataStructures.Octree;

namespace Voxelization.Utils
{
    public static class VoxelMeshBuilder
    {
        private static readonly Vector3[] verticesForBuild = {
            new Vector3 (0, 0, 0),
            new Vector3 (1, 0, 0),
            new Vector3 (1, 1, 0),
            new Vector3 (0, 1, 0),
            new Vector3 (0, 1, 1),
            new Vector3 (1, 1, 1),
            new Vector3 (1, 0, 1),
            new Vector3 (0, 0, 1),
        };

        private static readonly int[,] faceVertexIndex = {
            { 6, 2, 1, 5 }, { 7, 3, 0, 4 }, // right/left
            { 4, 2, 3, 5 }, { 7, 1, 0, 6 }, // up/down
            { 4, 6, 7, 5 }, { 3, 1, 0, 2 }  // front/back
        };

        private static readonly int[,] faceIndicesOffset = {
            { 2, 1, 0, 1, 3, 0 }, { 0, 1, 2, 0, 3, 1 }, // right/left
            { 0, 1, 2, 0, 3, 1 }, { 2, 1, 0, 1, 3, 0 }, // up/down
            { 2, 1, 0, 1, 3, 0 }, { 0, 1, 2, 0, 3, 1 }  // front/back
        };

        /// <returns>Voxel mesh with multiple submeshes</returns>
        public static Mesh GetMesh(MeshData meshData, int subMeshCount, IndexFormat indexFormat)
        {
            Mesh voxelMesh = new Mesh();
            voxelMesh.SetVertices(meshData.Vertices);
            voxelMesh.indexFormat = indexFormat;
            voxelMesh.subMeshCount = subMeshCount;
            for (int i = 0; i < subMeshCount; i++)
            {
                voxelMesh.SetTriangles(meshData.Indices[i], i);
            }
            voxelMesh.SetUVs(0, meshData.Uv);
            voxelMesh.boneWeights = meshData.BoneWeights.ToArray();
            voxelMesh.bindposes = meshData.BindPoses;
            voxelMesh.RecalculateBounds();
            voxelMesh.RecalculateNormals();
            voxelMesh.RecalculateTangents();
            return voxelMesh;
        }

        public static Mesh CreateMeshFromTree(VoxelOctree tree, bool removeNeighbouringFaces, float sizeScale, Matrix4x4[] bindPoses = null, Mesh customMesh = null)
        {
            var voxels = tree.Voxels;
            if (voxels.Count == 0)
            {
                return null;
            }

            MeshData meshData;
            if (customMesh == null)
            {
                if (!removeNeighbouringFaces)
                {
                    meshData = BuildVoxelMeshAllFaces(tree, sizeScale, bindPoses);
                }
                else
                {
                    meshData = BuildVoxelMesh(tree, sizeScale, bindPoses);
                }
            }
            else
            {
                meshData = BuildCustomMesh(tree, sizeScale, customMesh, bindPoses);
            }

            var indexFormat = IndexFormat.UInt16;
            if (meshData.Vertices.Count > 60_000)
            {
                indexFormat = IndexFormat.UInt32;
            }

            var mesh = GetMesh(meshData, tree.SubMeshesIndices.Length, indexFormat);
            return mesh;
        }

        private static MeshData BuildCustomMesh(VoxelOctree tree, float sizeScale, Mesh customMesh, Matrix4x4[] bindPoses = null)
        {
            MeshData meshData = new MeshData(tree, bindPoses);

            foreach (var voxel in tree.Voxels)
            {
                PlaceCustomMesh(voxel, tree.VoxelSize * sizeScale, meshData, customMesh);
            }
            return meshData;
        }

        private static MeshData BuildVoxelMeshAllFaces(VoxelOctree tree, float sizeScale, Matrix4x4[] bindPoses = null)
        {
            MeshData meshData = new MeshData(tree, bindPoses);

            foreach (var voxel in tree.Voxels)
            {
                PlaceVoxel(voxel, tree.VoxelSize * sizeScale, meshData);
            }
            return meshData;
        }

        private static MeshData BuildVoxelMesh(VoxelOctree tree, float sizeScale, Matrix4x4[] bindPoses = null)
        {
            MeshData meshData = new MeshData(tree, bindPoses);

            var minVoxelBox = tree.Bounds[0];
            var maxVoxelBox = tree.Bounds[1];

            Vector3Int voxelsNum;
            var adjMatrix = VoxelOctree.BuildAdjMatrix(tree.Voxels, minVoxelBox, maxVoxelBox, tree.VoxelSize, out voxelsNum);

            foreach (var voxel in tree.Voxels)
            {
                PlaceSimplifiedVoxel(voxel, voxelsNum, adjMatrix, sizeScale, tree, meshData);
            }
            return meshData;
        }

        private static void PlaceCustomMesh(OctreeNode voxel, float scale, MeshData meshData, Mesh customMesh)
        {
            int submesh = voxel.SubMeshAssign;
            int count = meshData.Vertices.Count;

            var verts = customMesh.vertices;
            var tris = customMesh.triangles;

            for (int i = 0; i < customMesh.vertexCount; i++)
            {
                meshData.Vertices.Add(voxel.AABB.Center + verts[i] * scale);
                meshData.Uv.Add(voxel.Uv);
                meshData.BoneWeights.Add(voxel.Bw);
            }

            for (int i = 0; i < tris.Length; i++)
            {
                meshData.Indices[submesh].Add(count + tris[i]);
            }
        }

        private static void PlaceVoxel(OctreeNode voxel, float scale, MeshData meshData)
        {
            for (int i = 0; i < 6; i++)
            {
                PlaceVoxelFace(voxel.AABB.Min, scale, meshData, voxel.SubMeshAssign, i);
                for (int j = 0; j < 4; j++)
                {
                    meshData.Uv.Add(voxel.Uv);
                    meshData.BoneWeights.Add(voxel.Bw);
                }
            }
        }

        /// <summary>
        /// Place voxel without unnecessary faces
        /// </summary>
        private static void PlaceSimplifiedVoxel(OctreeNode voxel, Vector3Int voxelsNum, byte[,,] voxelAdjMatrix, float sizeScale, VoxelOctree tree, MeshData meshData)
        {
            bool[] faces;

            var minVoxelBox = tree.Bounds[0];

            int count = meshData.Vertices.Count;
            PlaceSimplifiedVoxel(voxel, voxelsNum, voxelAdjMatrix, tree, meshData, sizeScale, out faces);
            for (int i = 0; i < meshData.Vertices.Count - count; i++)
            {
                meshData.Uv.Add(voxel.Uv);
                meshData.BoneWeights.Add(voxel.Bw);
            }
        }

        /// <summary>
        /// Place voxel without unnecessary faces
        /// </summary>
        private static void PlaceSimplifiedVoxel(OctreeNode voxel, Vector3Int voxelsNum, byte[,,] voxelAdjMatrix, VoxelOctree tree, MeshData meshData, float sizeScale, out bool[] placedFaces)
        {
            placedFaces = new bool[6];

            var minVoxelBox = tree.Bounds[0];
            var nodePos     = voxel.AABB.Min;
            var scale       = tree.VoxelSize;
            int submesh = voxel.SubMeshAssign;

            int x = FastMathUtils.TranslatePosition(minVoxelBox.x, nodePos.x, scale);
            int y = FastMathUtils.TranslatePosition(minVoxelBox.y, nodePos.y, scale);
            int z = FastMathUtils.TranslatePosition(minVoxelBox.z, nodePos.z, scale);

            if (x >= voxelsNum.x - 1 || voxelAdjMatrix[x + 1, y, z] == 0)
            {
                PlaceVoxelFace(nodePos, scale * sizeScale, meshData, submesh, 0);
                placedFaces[0] = true;
            }

            if (x == 0 || voxelAdjMatrix[x - 1, y, z] == 0)
            {
                PlaceVoxelFace(nodePos, scale * sizeScale, meshData, submesh, 1);
                placedFaces[1] = true;
            }

            if (y >= voxelsNum.y - 1 || voxelAdjMatrix[x, y + 1, z] == 0)
            {
                PlaceVoxelFace(nodePos, scale * sizeScale, meshData, submesh, 2);
                placedFaces[2] = true;
            }

            if (y == 0 || voxelAdjMatrix[x, y - 1, z] == 0)
            {
                PlaceVoxelFace(nodePos, scale * sizeScale, meshData, submesh, 3);
                placedFaces[3] = true;
            }

            if (z >= voxelsNum.z - 1 || voxelAdjMatrix[x, y, z + 1] == 0)
            {
                PlaceVoxelFace(nodePos, scale * sizeScale, meshData, submesh, 4);
                placedFaces[4] = true;
            }

            if (z == 0 || voxelAdjMatrix[x, y, z - 1] == 0)
            {
                PlaceVoxelFace(nodePos, scale * sizeScale, meshData, submesh, 5);
                placedFaces[5] = true;
            }
        }

        private static void PlaceVoxelFace(Vector3 pos, float scale, MeshData meshData, int submesh, int face)
        {
            int count = meshData.Vertices.Count;

            for (int i = 0; i < 4; i++)
            {
                meshData.Vertices.Add(pos + verticesForBuild[faceVertexIndex[face, i]] * scale);
            }

            for (int i = 0; i < 6; i++)
            {
                meshData.Indices[submesh].Add(count + faceIndicesOffset[face, i]);
            }
        }
    }
}
