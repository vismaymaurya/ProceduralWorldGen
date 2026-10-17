using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class ProceduralWater : MonoBehaviour
    {
        [Header("Water Settings")]
        public Material waterMaterial;
        public Material waterfallMaterial;
        public float waterPlaneSizeMultiplier = 1.35f;

        private GameObject waterPlaneObj;
        private List<GameObject> waterfallObjects = new List<GameObject>();

        public void BuildWater(TerrainGenerationSettings settings, List<WaterfallPoint> waterfalls)
        {
            ClearWater();

            // 1. Create main ocean / water surface plane
            waterPlaneObj = new GameObject("OceanPlane");
            waterPlaneObj.transform.SetParent(transform, false);
            waterPlaneObj.transform.position = new Vector3(0, settings.waterLevel, 0);

            MeshFilter mf = waterPlaneObj.AddComponent<MeshFilter>();
            MeshRenderer mr = waterPlaneObj.AddComponent<MeshRenderer>();

            float size = settings.worldSize * waterPlaneSizeMultiplier;
            mf.sharedMesh = CreateWaterPlaneMesh(size, 40);

            if (waterMaterial != null)
            {
                mr.sharedMaterial = waterMaterial;
            }

            // 2. Build Waterfall meshes
            BuildWaterfalls(waterfalls);
        }

        public void ClearWater()
        {
            if (waterPlaneObj != null)
            {
                DestroyImmediate(waterPlaneObj);
            }

            foreach (var wf in waterfallObjects)
            {
                if (wf != null) DestroyImmediate(wf);
            }
            waterfallObjects.Clear();
        }

        private Mesh CreateWaterPlaneMesh(float size, int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "WaterSurfaceMesh";

            int vertCount = (segments + 1) * (segments + 1);
            Vector3[] verts = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            int[] tris = new int[segments * segments * 6];

            float half = size * 0.5f;
            float step = size / segments;

            for (int y = 0; y <= segments; y++)
            {
                for (int x = 0; x <= segments; x++)
                {
                    int i = y * (segments + 1) + x;
                    verts[i] = new Vector3(x * step - half, 0, y * step - half);
                    uvs[i] = new Vector2((float)x / segments * 8f, (float)y / segments * 8f);
                    normals[i] = Vector3.up;
                }
            }

            int t = 0;
            for (int y = 0; y < segments; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int bl = y * (segments + 1) + x;
                    int br = bl + 1;
                    int tl = (y + 1) * (segments + 1) + x;
                    int tr = tl + 1;

                    tris[t++] = bl;
                    tris[t++] = tl;
                    tris[t++] = br;

                    tris[t++] = tl;
                    tris[t++] = tr;
                    tris[t++] = br;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void BuildWaterfalls(List<WaterfallPoint> waterfalls)
        {
            if (waterfalls == null || waterfalls.Count == 0)
                return;

            for (int i = 0; i < waterfalls.Count; i++)
            {
                WaterfallPoint wp = waterfalls[i];
                GameObject wfObj = new GameObject($"Waterfall_{i}");
                wfObj.transform.SetParent(transform, false);

                MeshFilter mf = wfObj.AddComponent<MeshFilter>();
                MeshRenderer mr = wfObj.AddComponent<MeshRenderer>();

                mf.sharedMesh = GenerateWaterfallMesh(wp);
                if (waterfallMaterial != null)
                {
                    mr.sharedMaterial = waterfallMaterial;
                }

                // Add subtle foam splash ring at base
                GameObject splashObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                splashObj.name = "SplashFoam";
                splashObj.transform.SetParent(wfObj.transform, false);
                splashObj.transform.position = wp.endPosition + Vector3.up * 0.08f;
                splashObj.transform.rotation = Quaternion.Euler(90, 0, 0);
                splashObj.transform.localScale = new Vector3(wp.width * 1.6f, wp.width * 1.6f, 1f);

                // Remove collider
                Collider c = splashObj.GetComponent<Collider>();
                if (c != null) DestroyImmediate(c);

                MeshRenderer smr = splashObj.GetComponent<MeshRenderer>();
                if (waterfallMaterial != null)
                {
                    smr.sharedMaterial = waterfallMaterial;
                }

                waterfallObjects.Add(wfObj);
            }
        }

        private Mesh GenerateWaterfallMesh(WaterfallPoint wp)
        {
            Mesh mesh = new Mesh();
            mesh.name = "WaterfallMesh";

            int segments = 8;
            Vector3[] verts = new Vector3[(segments + 1) * 2];
            Vector2[] uvs = new Vector2[(segments + 1) * 2];
            Color[] colors = new Color[(segments + 1) * 2];
            int[] tris = new int[segments * 6];

            Vector3 side = Vector3.Cross(Vector3.up, wp.flowDirection).normalized * (wp.width * 0.5f);

            for (int s = 0; s <= segments; s++)
            {
                float t = (float)s / segments;

                // Parabolic curved arc as water plunges over cliff
                Vector3 center = Vector3.Lerp(wp.startPosition, wp.endPosition, t);
                center += wp.flowDirection * Mathf.Sin(t * Mathf.PI) * 0.4f;

                int iLeft = s * 2;
                int iRight = s * 2 + 1;

                verts[iLeft] = center - side;
                verts[iRight] = center + side;

                uvs[iLeft] = new Vector2(0, t * 3f);
                uvs[iRight] = new Vector2(1, t * 3f);

                // Alpha gradient for foam fade
                float alpha = (s == 0 || s == segments) ? 0.3f : 0.95f;
                colors[iLeft] = new Color(1, 1, 1, alpha);
                colors[iRight] = new Color(1, 1, 1, alpha);
            }

            int tri = 0;
            for (int s = 0; s < segments; s++)
            {
                int bl = s * 2;
                int br = s * 2 + 1;
                int tl = (s + 1) * 2;
                int tr = (s + 1) * 2 + 1;

                // Front face
                tris[tri++] = bl;
                tris[tri++] = tl;
                tris[tri++] = br;

                tris[tri++] = tl;
                tris[tri++] = tr;
                tris[tri++] = br;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
