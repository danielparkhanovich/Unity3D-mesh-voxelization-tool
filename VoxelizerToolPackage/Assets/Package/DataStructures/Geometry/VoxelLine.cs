using System.Collections.Generic;
using UnityEngine;
using Voxelization.Utils;

namespace Voxelization.DataStructures.Geometry
{
    public struct VoxelLine
    {
        private Vector3 from;
        private Vector3 to;


        public VoxelLine(Vector3 from, Vector3 to)
        {
            this.from = from;
            this.to = to;
        }

        // returns true if line (L1, L2) intersects with the box (B1, B2)
        // returns intersection point in Hit
        public bool IntersectLineBox(Vector3 B1, Vector3 B2, out Vector3 Hit)
        {
            var L2 = from;
            var L1 = to;

            Hit = Vector3.zero;
            if (L2.x < B1.x && L1.x < B1.x) return false;
            if (L2.x > B2.x && L1.x > B2.x) return false;
            if (L2.y < B1.y && L1.y < B1.y) return false;
            if (L2.y > B2.y && L1.y > B2.y) return false;
            if (L2.z < B1.z && L1.z < B1.z) return false;
            if (L2.z > B2.z && L1.z > B2.z) return false;
            if (L1.x > B1.x && L1.x < B2.x &&
                L1.y > B1.y && L1.y < B2.y &&
                L1.z > B1.z && L1.z < B2.z)
            {
                Hit = L1;
                return true;
            }

            if ((GetBoxIntersection(L1.x - B1.x, L2.x - B1.x, L1, L2, out Hit) && InBox(Hit, B1, B2, 1))
             || (GetBoxIntersection(L1.y - B1.y, L2.y - B1.y, L1, L2, out Hit) && InBox(Hit, B1, B2, 2))
             || (GetBoxIntersection(L1.z - B1.z, L2.z - B1.z, L1, L2, out Hit) && InBox(Hit, B1, B2, 3))
             || (GetBoxIntersection(L1.x - B2.x, L2.x - B2.x, L1, L2, out Hit) && InBox(Hit, B1, B2, 1))
             || (GetBoxIntersection(L1.y - B2.y, L2.y - B2.y, L1, L2, out Hit) && InBox(Hit, B1, B2, 2))
             || (GetBoxIntersection(L1.z - B2.z, L2.z - B2.z, L1, L2, out Hit) && InBox(Hit, B1, B2, 3)))
                return true;

            return false;
        }

        public void GetVoxelIntersection(Vector3 B1, Vector3 B2, Vector3 L1, Vector3 L2, out Vector3 Hit, out int face)
        {
            Hit = Vector3.zero;
            face = 0;

            var normals = AABB.FaceNormals;
            Vector3 center = (B2 + B1) / 2f;
            Vector3 extents = (B2 - B1) / 2f;
            List<Vector3> hits = new List<Vector3>();
            List<Vector3> faceCenters = new List<Vector3>();
            List<int> sides = new List<int>();
            for (int axis = 0; axis < 3; axis++)
            {
                int index = axis * 2;
                if (GetBoxIntersection(L1[axis] - B2[axis], L2[axis] - B2[axis], L1, L2, out Hit) && InBox(Hit, B1, B2, axis + 1))
                {
                    hits.Add(Hit);
                    faceCenters.Add(Vector3.Scale(extents, normals[index]) + center);
                    sides.Add(index);
                }
                if (GetBoxIntersection(L1[axis] - B1[axis], L2[axis] - B1[axis], L1, L2, out Hit) && InBox(Hit, B1, B2, axis + 1))
                {
                    hits.Add(Hit);
                    faceCenters.Add(Vector3.Scale(extents, normals[index + 1]) + center);
                    sides.Add(index + 1);
                }
            }

            if (hits.Count != 0)
            {
                int minIndex = 0;
                for (int i = 1; i < faceCenters.Count; i++)
                {
                    if ((faceCenters[i] - L1).magnitude < (faceCenters[minIndex] - L1).magnitude)
                    {
                        minIndex = i;
                    }
                }
                Hit = hits[minIndex];
                face = sides[minIndex];
            }
        }

        private bool GetBoxIntersection(float fDst1, float fDst2, Vector3 P1, Vector3 P2, out Vector3 Hit)
        {
            Hit = Vector3.zero;

            if ((fDst1 * fDst2) >= 0.0f)
                return false;
            if (fDst1 == fDst2)
                return false;

            Hit = P1 + (P2 - P1) * (-fDst1 / (fDst2 - fDst1));
            return true;
        }

        private bool InBox(Vector3 Hit, Vector3 B1, Vector3 B2, int Axis)
        {
            if (Axis == 1 && (Hit.z > B1.z || FastMathUtils.IsEquals(Hit.z, B1.z, Mathf.Pow(10, -4))) &&
                             (Hit.z < B2.z || FastMathUtils.IsEquals(Hit.z, B2.z, Mathf.Pow(10, -4))) &&
                             (Hit.y > B1.y || FastMathUtils.IsEquals(Hit.y, B1.y, Mathf.Pow(10, -4))) &&
                             (Hit.y < B2.y || FastMathUtils.IsEquals(Hit.y, B2.y, Mathf.Pow(10, -4)))) return true;

            if (Axis == 2 && (Hit.z > B1.z || FastMathUtils.IsEquals(Hit.z, B1.z, Mathf.Pow(10, -4))) &&
                             (Hit.z < B2.z || FastMathUtils.IsEquals(Hit.z, B2.z, Mathf.Pow(10, -4))) &&
                             (Hit.x > B1.x || FastMathUtils.IsEquals(Hit.x, B1.x, Mathf.Pow(10, -4))) &&
                             (Hit.x < B2.x || FastMathUtils.IsEquals(Hit.x, B2.x, Mathf.Pow(10, -4)))) return true;

            if (Axis == 3 && (Hit.x > B1.x || FastMathUtils.IsEquals(Hit.x, B1.x, Mathf.Pow(10, -4))) &&
                             (Hit.x < B2.x || FastMathUtils.IsEquals(Hit.x, B2.x, Mathf.Pow(10, -4))) &&
                             (Hit.y > B1.y || FastMathUtils.IsEquals(Hit.y, B1.y, Mathf.Pow(10, -4))) &&
                             (Hit.y < B2.y || FastMathUtils.IsEquals(Hit.y, B2.y, Mathf.Pow(10, -4)))) return true;
            return false;
        }
    }
}
