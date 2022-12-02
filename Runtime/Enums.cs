
namespace Voxelization
{
    public enum MeshType
    {
        /// <summary>
        /// Mesh Filters selected only
        /// </summary>
        Static,
        /// <summary>
        /// Skinned Mesh Renderers selected only
        /// </summary>
        Animated,
        /// <summary>
        /// Mesh Filters & Skinned Mesh Renderers
        /// </summary>
        All
    }

    public enum VoxelizeUnits
    {
        Voxel_size,
        /// <summary>
        /// Number of voxels for the longest side of the model
        /// </summary>
        Subdivision_level
    }

    public enum VoxelizationType
    {
        /// <summary>
        /// Exact copy of the object with voxelized meshes
        /// </summary>
        Create_Voxelized_Copy = 0,
        /// <summary>
        /// Single object with merged voxel meshes into one mesh
        /// </summary>
        Create_Voxelized_Mesh = 1,
        /// <summary>
        /// Object with meshes replaced by primitive gameObjects - cubes.
        /// Unsafe due to long processing time and RAM consumption
        /// </summary>
        Create_Voxelized_Mesh_With_Primitives = 2
    }

    public enum ProcessingType
    {
        Single_thread,
        /// <summary>
        /// TPL C# library
        /// </summary>
        Multi_thread,
        /// <summary>
        /// Compute shader, not implemented yet
        /// </summary>
        GPU
    }
}

