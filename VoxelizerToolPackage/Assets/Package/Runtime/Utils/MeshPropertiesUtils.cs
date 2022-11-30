using System.Collections.Generic;
using UnityEngine;

namespace Voxelization.Utils
{
    public static class MeshPropertiesUtils
    {
        /// <returns>(triangles index offset, sub meshes index array)</returns>
        public static (int, int[]) MergeMeshProperties(Mesh mesh, int indicesOffset, List<Vector3> vertices, List<Vector2> uvs, List<BoneWeight> bws, List<int> indices)
        {
            var subMeshesIndices = new int[mesh.subMeshCount];
            for (int j = 0; j < mesh.subMeshCount; j++)
            {
                subMeshesIndices[j] = mesh.GetSubMesh(j).indexStart + indicesOffset;
            }

            var meshVertices = mesh.vertices;
            int verticesLength = meshVertices.Length;
            for (int j = 0; j < verticesLength; j++)
            {
                vertices.Add(meshVertices[j]);
            }
            var meshUvs = mesh.uv;
            int uvsLength = meshUvs.Length;
            for (int j = 0; j < uvsLength; j++)
            {
                uvs.Add(meshUvs[j]);
            }

            var meshBones = mesh.boneWeights;
            int bonesLength = meshBones.Length;
            for (int j = 0; j < bonesLength; j++)
            {
                bws.Add(meshBones[j]);
            }

            var meshIndices = mesh.triangles;
            int indicesLength = meshIndices.Length;
            for (int j = 0; j < indicesLength; j++)
            {
                indices.Add(meshIndices[j] + indicesOffset);
            }
            indicesOffset = vertices.Count;
            return (indicesOffset, subMeshesIndices);
        }

        /// <summary>
        /// Calculates approximation for UV and BoneWeights
        /// </summary>
        /// <returns>
        /// (ratio, triangle index) ratio - how voxel is close to the closest 
        /// vertex in triangle with triangle index
        /// </returns>
        public static (float, int) GetRationAndTriIndex(Vector3 voxelCenter, Vector3 a, Vector3 b, Vector3 c)
        {
            var plane = new Plane(a, b, c);

            var centroidTri = (a + b + c) / 3f;

            centroidTri = Vector3.ProjectOnPlane(centroidTri, plane.normal);
            var projected = Vector3.ProjectOnPlane(voxelCenter, plane.normal);

            a = Vector3.ProjectOnPlane(a, plane.normal);
            b = Vector3.ProjectOnPlane(b, plane.normal);
            c = Vector3.ProjectOnPlane(c, plane.normal);
            Vector3[] points = new Vector3[] { a, b, c };

            float minDist = Mathf.Infinity;
            var minPoint = a;
            int indexPoint = 0;
            for (int i = 0; i < 3; i++)
            {
                var point = points[i];
                float dist = Vector3.Distance(projected, point);
                if (dist < minDist)
                {
                    minDist = dist;
                    minPoint = point;
                    indexPoint = i;
                }
            }
            var fromCenterToCorner = Vector3.Distance(minPoint, centroidTri);

            var ratio = minDist / fromCenterToCorner;
            ratio = Mathf.Clamp(ratio, 0f, 1f);

            return (ratio, indexPoint);
        }

        public static Vector2 GetCentroid2D(Vector2 a, Vector2 b, Vector2 c)
        {
            return (a + b + c) / 3f;
        }

        public static BoneWeight GetCentroidBoneWeight(BoneWeight a, BoneWeight b, BoneWeight c)
        {
            var result = a;

            float w0Avg = (a.weight0 + b.weight0 + c.weight0) / 3f;
            float w1Avg = (a.weight1 + b.weight1 + c.weight1) / 3f;
            float w2Avg = (a.weight2 + b.weight2 + c.weight2) / 3f;
            float w3Avg = (a.weight3 + b.weight3 + c.weight3) / 3f;

            result.weight0 = w0Avg;
            result.weight1 = w1Avg;
            result.weight2 = w2Avg;
            result.weight3 = w3Avg;

            if (result.boneIndex0 == 0)
            {
                result.boneIndex0 = a.boneIndex0;
                result.boneIndex0 = result.boneIndex0 == 0 ? b.boneIndex0 : result.boneIndex0;
                result.boneIndex0 = result.boneIndex0 == 0 ? c.boneIndex0 : result.boneIndex0;
            }
            if (result.boneIndex1 == 0)
            {
                result.boneIndex1 = a.boneIndex1;
                result.boneIndex1 = result.boneIndex1 == 0 ? b.boneIndex1 : result.boneIndex1;
                result.boneIndex1 = result.boneIndex1 == 0 ? c.boneIndex1 : result.boneIndex1;
            }
            if (result.boneIndex2 == 0)
            {
                result.boneIndex2 = a.boneIndex2;
                result.boneIndex2 = result.boneIndex2 == 0 ? b.boneIndex2 : result.boneIndex2;
                result.boneIndex2 = result.boneIndex2 == 0 ? c.boneIndex2 : result.boneIndex2;
            }
            if (result.boneIndex3 == 0)
            {
                result.boneIndex3 = a.boneIndex3;
                result.boneIndex3 = result.boneIndex3 == 0 ? b.boneIndex3 : result.boneIndex3;
                result.boneIndex3 = result.boneIndex3 == 0 ? c.boneIndex3 : result.boneIndex3;
            }

            return result;
        }

        public static BoneWeight LerpBoneWeight(BoneWeight a, BoneWeight b, float ratio)
        {
            BoneWeight result;
            if (ratio < 0.5)
            {
                result = a;
            }
            else
            {
                result = b;
            }

            result.weight0 = Mathf.Lerp(a.weight0, b.weight0, ratio);
            result.weight1 = Mathf.Lerp(a.weight1, b.weight1, ratio);
            result.weight2 = Mathf.Lerp(a.weight2, b.weight2, ratio);
            result.weight3 = Mathf.Lerp(a.weight3, b.weight3, ratio);

            return result;
        }
    }
}
