using System.Runtime.InteropServices;
using UnityEngine;

namespace Voxelization.DataStructures.Geometry
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AABB
    {
        private static Vector3[] faceNormals = new Vector3[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        public static Vector3[] FaceNormals { get => faceNormals; }

        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public Vector3 Center { get => (Max + Min) / 2f; }
        public Vector3 Size { get => new Vector3(Width, Height, Depth); }
        public Vector3 Extents { get => new Vector3(Width, Height, Depth) / 2f; }

        public float Width { get => Max.x - Min.x; }
        public float Height { get => Max.y - Min.y; }
        public float Depth { get => Max.z - Min.z; }


        public AABB(Vector3 Min, Vector3 Max)
        {
            this.Min = Min;
            this.Max = Max;
        }

        public AABB(Vector3 center, float size)
        {
            this.Min = new Vector3(center.x - size, center.y - size, center.z - size);
            this.Max = new Vector3(center.x + size, center.y + size, center.z + size);
        }

        public void Resize(Vector3 min, Vector3 max)
        {
            this.Min = min;
            this.Max = max;
        }
    }
}
