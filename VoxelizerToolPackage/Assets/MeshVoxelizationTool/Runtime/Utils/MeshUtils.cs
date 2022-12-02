using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxelization.Tools;

namespace Voxelization.Utils
{
    public static class MeshUtils
    {
        public static List<Mesh> GetAllSharedMeshes(GameObject target, MeshType rendererType)
        {
            List<Mesh> sharedMeshes = new List<Mesh>();
            if (rendererType == MeshType.Static)
            {
                var meshFilters = target.GetComponentsInChildren<MeshFilter>();
                foreach (var filter in meshFilters)
                {
                    if (filter.sharedMesh != null)
                    {
                        sharedMeshes.Add(filter.sharedMesh);
                    }
                }
            }
            else
            {
                var meshFilters = target.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var filter in meshFilters)
                {
                    if (filter.sharedMesh != null)
                    {
                        sharedMeshes.Add(filter.sharedMesh);
                    }
                }
            }
            return sharedMeshes;
        }

        public static List<Material> GetAllMaterials(GameObject target, MeshType rendererType)
        {
            var allMaterials = new List<Material>();
            if (rendererType == MeshType.Static)
            {
                var meshFilters = target.GetComponentsInChildren<MeshFilter>();
                foreach (var filter in meshFilters)
                {
                    if (filter.GetComponent<Renderer>())
                    {
                        foreach (var sharedMaterial in filter.GetComponent<Renderer>().sharedMaterials)
                        {
                            var tempMaterial = sharedMaterial;
                            allMaterials.Add(tempMaterial);
                        }
                    }
                }
            }
            else
            {
                var meshFilters = target.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var filter in meshFilters)
                {
                    if (filter.GetComponent<Renderer>())
                    {
                        foreach (var sharedMaterial in filter.GetComponent<Renderer>().sharedMaterials)
                        {
                            var tempMaterial = new Material(sharedMaterial);
                            allMaterials.Add(tempMaterial);
                        }
                    }
                }
            }
            return allMaterials;
        }

        public static int GetCurrentSubMeshIndex(int triangleIndex, int[] subMeshesIndices)
        {
            int index = 0;
            for (int i = 0; i < subMeshesIndices.Length; i++)
            {
                if (triangleIndex > subMeshesIndices[i])
                {
                    index = i;
                }
            }
            return index;
        }

        public static void GetTriangleVertices<T>(List<T> vertices, List<int> indices, int index, out T a, out T b, out T c)
        {
            a = vertices[indices[index]];
            b = vertices[indices[index + 1]];
            c = vertices[indices[index + 2]];
        }
    }
}
