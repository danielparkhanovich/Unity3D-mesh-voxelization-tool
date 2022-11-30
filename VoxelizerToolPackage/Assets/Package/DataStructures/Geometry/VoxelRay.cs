using UnityEngine;

namespace Voxelization.DataStructures.Geometry
{
    public struct VoxelRay
    {
        private Vector3 origin;
        private Vector3 direction;
        private Vector3 invdir;
        private int[] sign;


        public VoxelRay(Vector3 from, Vector3 direction)
        {
            this.origin = from;
            this.direction = -direction;

            this.invdir = new Vector3(1f / this.direction.x, 1f / this.direction.y, 1f / this.direction.z);
            this.sign = new int[3];
            this.sign[0] = (invdir.x < 0) ? 1 : 0;
            this.sign[1] = (invdir.y < 0) ? 1 : 0;
            this.sign[2] = (invdir.z < 0) ? 1 : 0;
        }

        public bool IntersectRayBox(Vector3 min, Vector3 max)
        {
            float tmax, tmin, tymin, tymax, tzmin, tzmax;

            Vector3[] bounds = new Vector3[2] { min, max };

            tmin = (bounds[sign[0]].x - origin.x) * invdir.x;
            tmax = (bounds[1 - sign[0]].x - origin.x) * invdir.x;
            tymin = (bounds[sign[1]].y - origin.y) * invdir.y;
            tymax = (bounds[1 - sign[1]].y - origin.y) * invdir.y;

            if ((tmin > tymax) || (tymin > tmax))
                return false;

            tmin = tymin > tmin ? tymin : tmin;
            tmax = tymax < tmax ? tymax : tmax;

            tzmin = (bounds[sign[2]].z - origin.z) * invdir.z;
            tzmax = (bounds[1 - sign[2]].z - origin.z) * invdir.z;

            if ((tmin > tzmax) || (tzmin > tmax))
                return false;

            // Check forward intersections only
            tmin = tzmin > tmin ? tzmin : tmin;
            if (tmin >= 0)
            {
                return false;
            }

            return true;
        }
    }
}
