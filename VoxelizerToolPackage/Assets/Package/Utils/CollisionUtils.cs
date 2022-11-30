using UnityEngine;
using Voxelization.DataStructures.Geometry;

namespace Voxelization.Utils
{
    public static class CollisionUtils
    {
        public static bool IntersectsAabbAabb(Vector3 min1, Vector3 max1, Vector3 min2, Vector3 max2)
        {
            return ((min1.x < max2.x || FastMathUtils.IsEquals(min1.x, max2.x)) && (max1.x > min2.x || FastMathUtils.IsEquals(max1.x, min2.x))) &&
                   ((min1.y < max2.y || FastMathUtils.IsEquals(min1.y, max2.y)) && (max1.y > min2.y || FastMathUtils.IsEquals(max1.y, min2.y))) &&
                   ((min1.z < max2.z || FastMathUtils.IsEquals(min1.z, max2.z)) && (max1.z > min2.z || FastMathUtils.IsEquals(max1.z, min2.z)));
        }

        public static bool IsInsideOfAABB(Vector3 minTri, Vector3 maxTri, Vector3 minBox, Vector3 maxBox)
        {
            return (minTri.x > minBox.x && maxTri.x < maxBox.x) &&
                   (minTri.y > minBox.y && maxTri.y < maxBox.y) &&
                   (minTri.z > minBox.z && maxTri.z < maxBox.z);
        }

        public static bool IsPointInsideOfAABB(Vector3 point, Vector3 minBox, Vector3 maxBox)
        {
            return (point.x >= minBox.x && point.x <= maxBox.x) &&
                   (point.y >= minBox.y && point.y <= maxBox.y) &&
                   (point.z >= minBox.z && point.z <= maxBox.z);
        }

        public static void GetMinMax(Vector3 a, Vector3 b, Vector3 c, out Vector3 min, out Vector3 max)
        {
            min = FastMathUtils.Min(a, b);
            min = FastMathUtils.Min(b, min);
            min = FastMathUtils.Min(c, min);

            max = FastMathUtils.Max(a, b);
            max = FastMathUtils.Max(b, max);
            max = FastMathUtils.Max(c, max);
        }

        public static bool IntersectsTriangleAabb(Vector3 a, Vector3 b, Vector3 c, AABB aabb)
        {
            var center = aabb.Center;
            a -= center;
            b -= center;
            c -= center;

            Vector3 ab = FastMathUtils.Normalize(b - a);
            Vector3 bc = FastMathUtils.Normalize(c - b);
            Vector3 ca = FastMathUtils.Normalize(a - c);

            var extents = aabb.Extents;

            if (
                !IntersectsTriangleAabbSat(a, b, c, extents, new Vector3(0.0f, -ab.z, ab.y)) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, new Vector3(0.0f, -bc.z, bc.y)) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, new Vector3(0.0f, -ca.z, ca.y)) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, new Vector3(ab.z, 0.0f, -ab.x)) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, new Vector3(bc.z, 0.0f, -bc.x)) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, new Vector3(ca.z, 0.0f, -ca.x)) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, new Vector3(-ab.y, ab.x, 0.0f)) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, new Vector3(-bc.y, bc.x, 0.0f)) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, new Vector3(-ca.y, ca.x, 0.0f)) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, Vector3.right) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, Vector3.up) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, Vector3.forward) ||
                !IntersectsTriangleAabbSat(a, b, c, extents, FastMathUtils.Cross(ab, bc))
            )
            {
                return false;
            }

            return true;
        }

        private static bool IntersectsTriangleAabbSat(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 aabbExtents, Vector3 axis)
        {
            float p0 = FastMathUtils.Dot(v0, axis);
            float p1 = FastMathUtils.Dot(v1, axis);
            float p2 = FastMathUtils.Dot(v2, axis);

            float r = aabbExtents.x * FastMathUtils.Abs(FastMathUtils.Dot(Vector3.right, axis)) +
                      aabbExtents.y * FastMathUtils.Abs(FastMathUtils.Dot(Vector3.up, axis)) +
                      aabbExtents.z * FastMathUtils.Abs(FastMathUtils.Dot(Vector3.forward, axis));

            return !(FastMathUtils.Max(-FastMathUtils.Max(p0, FastMathUtils.Max(p1, p2)), FastMathUtils.Min(p0, FastMathUtils.Min(p1, p2))) > r);
        }
    }
}
