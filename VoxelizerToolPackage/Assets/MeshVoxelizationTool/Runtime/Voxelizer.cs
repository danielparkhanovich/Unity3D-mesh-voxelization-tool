using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Voxelization.DataStructures.Octree;
using Voxelization.Utils;

namespace Voxelization.Tools
{
    public class Voxelizer
    {
        private readonly float voxelSize;
        private readonly float voxelSizeScale;
        private readonly OctreeFactory treeFactory;
        private readonly bool isCustomMesh;
        private readonly Mesh customMesh;

        public UnityAction<int, Voxelizer> OnFinishedModel;
        public int MeshesNum { get; internal set; }
        public List<VoxelOctree> Trees { get; internal set; }
        public List<Mesh> VoxelMeshes { get; internal set; }


        public Voxelizer(float voxelSize, ProcessingType processingType)
        {
            this.voxelSize = voxelSize;
            this.voxelSizeScale = 1f;
            this.Trees = new List<VoxelOctree>();
            this.VoxelMeshes = new List<Mesh>();
            this.treeFactory = new OctreeFactory(voxelSize, processingType);
            this.customMesh = null;
        }

        public Voxelizer(float voxelSize, ProcessingType processingType, float voxelScale, Mesh customMesh) : this(voxelSize, processingType)
        {
            this.voxelSizeScale = voxelScale;
            this.customMesh = customMesh;
            this.isCustomMesh = true;
        }

        private void Dispose()
        {
            this.MeshesNum = 0;
            this.Trees = new List<VoxelOctree>();
            this.VoxelMeshes = new List<Mesh>();
        }

        /// <returns>Voxelized mesh</returns>
        public Mesh VoxelizeMesh(Mesh mesh, bool isRemoveNeigbouringFaces)
        {
            Dispose();

            this.MeshesNum = 1;
            var tree = CreateVoxelOctree(mesh, 0);
            var voxelMesh = VoxelMeshBuilder.CreateMeshFromTree(tree, isRemoveNeigbouringFaces, voxelSizeScale, mesh.bindposes);
            voxelMesh.name = mesh.name;
            VoxelMeshes.Add(voxelMesh);
            return voxelMesh;
        }

        /// <summary>
        /// Creates a copy of object with voxelized meshes
        /// </summary>
        /// <returns>Voxelized object</returns>
        public GameObject Voxelize(GameObject target, bool isRemoveNeigbouringFaces, MeshType rendererType)
        {
            Dispose();

            GameObject copy = GameObject.Instantiate(target);
            copy.transform.position = target.transform.position;
            copy.transform.rotation = target.transform.rotation;
            copy.transform.localScale = target.transform.lossyScale;
            ReplaceToVoxels(copy, isRemoveNeigbouringFaces, rendererType);
            copy.name += $"voxelized, voxel size: {voxelSize}";

            return copy;
        }

        /// <summary>
        /// Creates a new single object with all voxelized meshes
        /// </summary>
        /// <returns>Voxelized object</returns>
        public GameObject VoxelizeToSingleObject(GameObject target, bool isRemoveNeigbouringFaces, MeshType rendererType)
        {
            Dispose();

            this.MeshesNum = 1;
            List<Mesh> sharedMeshes = MeshUtils.GetAllSharedMeshes(target, rendererType);
            var tree = CreateVoxelOctree(sharedMeshes, 0);
             
            var voxelMesh = VoxelMeshBuilder.CreateMeshFromTree(tree, isRemoveNeigbouringFaces, voxelSizeScale, null, customMesh);
            var allMaterials = MeshUtils.GetAllMaterials(target, rendererType).ToArray();

            voxelMesh.name = sharedMeshes[0].name;
            VoxelMeshes.Add(voxelMesh);

            return PlaceObject(target, voxelMesh, allMaterials, rendererType);
        }

        /// <summary>
        /// Creates a new object with all voxels as childs
        /// </summary>
        /// <returns>Voxelized object with child voxel objects</returns>
        public GameObject[] VoxelizeToPrimitives(GameObject target, MeshType rendererType)
        {
            Dispose();

            this.MeshesNum = 1;
            List<Mesh> sharedMeshes = MeshUtils.GetAllSharedMeshes(target, rendererType);
            this.MeshesNum = sharedMeshes.Count;
            var tree = CreateVoxelOctree(sharedMeshes, 0);

            var allMaterials = MeshUtils.GetAllMaterials(target, rendererType);
            GameObject copy = GameObject.Instantiate(target);
            copy.transform.position = target.transform.position;
            copy.transform.rotation = target.transform.rotation;
            copy.transform.localScale = target.transform.lossyScale;

            List<GameObject> primitiveVoxels = new List<GameObject>();
            MeshFilter[] copyMeshFilters = copy.GetComponentsInChildren<MeshFilter>();
            foreach (var filter in copyMeshFilters)
            {
                primitiveVoxels.AddRange(PlacePrimitives(filter.gameObject, allMaterials.ToArray(), tree));
            }
            for (int i = 0; i < copyMeshFilters.Length; i++)
            {
                GameObject.DestroyImmediate(copyMeshFilters[i]);
            }
            return primitiveVoxels.ToArray();
        }

        /// <summary>
        /// Replace object meshes to voxel meshes
        /// </summary>
        private void ReplaceToVoxels(GameObject target, bool isRemoveNeigbouringFaces, MeshType rendererType)
        {
            if (rendererType == MeshType.Animated)
            {
                ReplaceAnimatedMeshes(target, isRemoveNeigbouringFaces);
            }
            else if (rendererType == MeshType.Static)
            {
                ReplaceStaticMeshes(target, isRemoveNeigbouringFaces);
            }
            else
            {
                ReplaceAnimatedMeshes(target, isRemoveNeigbouringFaces);
                ReplaceStaticMeshes(target, isRemoveNeigbouringFaces);
            }
        }

        private void ReplaceAnimatedMeshes(GameObject target, bool removeNeigbouringFaces)
        {
            var multi = new MultiMeshOctree<SkinnedMeshRenderer>();

            var meshFilters = target.GetComponentsInChildren<SkinnedMeshRenderer>();
            this.MeshesNum = meshFilters.Length;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                var tree = CreateVoxelOctree(meshFilters[i].sharedMesh, i);
                multi.AddNewTree(meshFilters[i], tree);
            }
            multi.RemoveIntersectingVoxels();
            for (int i = 0; i < meshFilters.Length; i++)
            {
                var filter = meshFilters[i];
                var tree = multi.TreesDict[filter];
                var voxelMesh = VoxelMeshBuilder.CreateMeshFromTree(tree, removeNeigbouringFaces, voxelSizeScale, meshFilters[i].sharedMesh.bindposes, this.customMesh);
                if (voxelMesh == null)
                {
                    meshFilters[i].sharedMesh = null;
                    continue;
                }
                voxelMesh.name = meshFilters[i].sharedMesh.name;
                this.VoxelMeshes.Add(voxelMesh);
                meshFilters[i].sharedMesh = voxelMesh;
            }
            
        }

        private void ReplaceStaticMeshes(GameObject target, bool removeNeigbouringFaces)
        {
            var multi = new MultiMeshOctree<MeshFilter>();
            var meshFilters = target.GetComponentsInChildren<MeshFilter>();
            this.MeshesNum = meshFilters.Length;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i].sharedMesh == null)
                {
                    continue;
                }
                var tree = CreateVoxelOctree(meshFilters[i].sharedMesh, i);
                multi.AddNewTree(meshFilters[i], tree);
            }
            multi.RemoveIntersectingVoxels();
            for (int i = 0; i < meshFilters.Length; i++)
            {
                var filter = meshFilters[i];
                if (!multi.TreesDict.ContainsKey(filter))
                {
                    continue;
                }
                var tree = multi.TreesDict[filter];
                var voxelMesh = VoxelMeshBuilder.CreateMeshFromTree(tree, removeNeigbouringFaces, voxelSizeScale, meshFilters[i].sharedMesh.bindposes, this.customMesh);
                if (voxelMesh == null)
                {
                    meshFilters[i].sharedMesh = null;
                    continue;
                }
                voxelMesh.name = meshFilters[i].sharedMesh.name;
                this.VoxelMeshes.Add(voxelMesh);
                meshFilters[i].sharedMesh = voxelMesh;
            }
        }

        private VoxelOctree CreateVoxelOctree(Mesh mesh, int index)
        {
            var tree = treeFactory.GetOctree(mesh);
            tree.Voxelize();
            Trees.Add(tree);

            OnFinishedModel.Invoke(index, this);

            return tree;
        }

        private VoxelOctree CreateVoxelOctree(List<Mesh> meshes, int index)
        {
            var tree = treeFactory.GetOctree(meshes.ToArray());
            tree.Voxelize();
            Trees.Add(tree);

            OnFinishedModel.Invoke(index, this);

            return tree;
        }

        private GameObject PlaceObject(GameObject original, Mesh voxelMesh, Material[] materials, MeshType rendererType)
        {
            var name = string.Format("{0}, (voxelized, voxel size: {1})", original.name, voxelSize);
            var newObject = new GameObject(name);
            newObject.transform.position = original.transform.position;
            newObject.transform.rotation = original.transform.rotation;

            Renderer renderer;
            if (rendererType == MeshType.Animated)
            {
                renderer = newObject.AddComponent<SkinnedMeshRenderer>();
                (renderer as SkinnedMeshRenderer).sharedMesh = voxelMesh;
            }
            else
            {
                renderer = newObject.AddComponent<MeshRenderer>();
                var filter = newObject.AddComponent<MeshFilter>();
                filter.sharedMesh = voxelMesh;
            }
            renderer.sharedMaterials = materials;

            return newObject;
        }

        private GameObject[] PlacePrimitives(GameObject original, Material[] materials, VoxelOctree tree)
        {
            var voxels = tree.Voxels;
            GameObject[] voxelObjects = new GameObject[voxels.Count];

            for (int i = 0; i < voxels.Count; i++)
            {
                var voxel = voxels[i];
                GameObject voxelObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

                if (isCustomMesh)
                {
                    voxelObject.GetComponent<MeshFilter>().sharedMesh = customMesh;
                }

                voxelObject.name = string.Format("Voxel [{0}]", i + 1);
                voxelObject.transform.parent = original.transform;
                voxelObject.transform.localPosition = voxel.AABB.Min;
                voxelObject.transform.localRotation = new Quaternion();
                voxelObject.transform.localScale = Vector3.one * voxelSize * voxelSizeScale;
                var mesh = voxelObject.GetComponent<MeshFilter>().sharedMesh;
                Vector2[] uvs = new Vector2[mesh.vertexCount];
                for (int j = 0; j < mesh.vertexCount; j++)
                {
                    uvs[j] = voxel.Uv;
                }
                mesh.SetUVs(0, uvs);

                var renderer = voxelObject.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = materials[voxel.SubMeshAssign];
            }

            return voxelObjects;
        }
    }
}