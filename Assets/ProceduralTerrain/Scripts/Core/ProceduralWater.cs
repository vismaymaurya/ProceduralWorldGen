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
        private Mesh waterPlaneMesh;
        private List<GameObject> waterfallObjects = new List<GameObject>();
        private List<Mesh> waterfallMeshes = new List<Mesh>();
        private List<GameObject> riverObjects = new List<GameObject>();
        private List<Mesh> riverMeshes = new List<Mesh>();

        // Real-time river brush stroke preview
        private GameObject dynamicRiverObj;
        private Mesh dynamicRiverMesh;

        private void OnDestroy()
        {
            ClearWater();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                ClearWater();
            }
        }

        public void BuildWater(
            TerrainGenerationSettings settings,
            List<WaterfallPoint> waterfalls,
            List<List<Vector3>> riverPaths = null)
        {
            ClearWater();

            // 1. Create main ocean / water surface plane
            waterPlaneObj = new GameObject("OceanPlane");
            waterPlaneObj.transform.SetParent(transform, false);
            waterPlaneObj.transform.position = new Vector3(0, settings.waterLevel, 0);

            MeshFilter mf = waterPlaneObj.AddComponent<MeshFilter>();
            MeshRenderer mr = waterPlaneObj.AddComponent<MeshRenderer>();

            float size = settings.worldSize * waterPlaneSizeMultiplier;
            waterPlaneMesh = CreateWaterPlaneMesh(size, 40);
            mf.sharedMesh = waterPlaneMesh;

            if (waterMaterial != null)
            {
                mr.sharedMaterial = waterMaterial;
            }

            // 2. Build River water meshes (mountain to sea ribbons)
            if (riverPaths != null && riverPaths.Count > 0)
            {
                BuildRivers(riverPaths, settings);
            }

            // 3. Build Waterfall meshes
            BuildWaterfalls(waterfalls);
        }

        public void ClearWater()
        {
            ClearDynamicRiverStroke();

            if (waterPlaneMesh != null)
            {
                SafeDestroyMesh(waterPlaneMesh);
                waterPlaneMesh = null;
            }

            if (waterPlaneObj != null)
            {
                SafeDestroyGO(waterPlaneObj);
                waterPlaneObj = null;
            }

            for (int i = 0; i < waterfallMeshes.Count; i++)
            {
                if (waterfallMeshes[i] != null)
                {
                    SafeDestroyMesh(waterfallMeshes[i]);
                }
            }
            waterfallMeshes.Clear();

            for (int i = 0; i < waterfallObjects.Count; i++)
            {
                if (waterfallObjects[i] != null)
                {
                    SafeDestroyGO(waterfallObjects[i]);
                }
            }
            waterfallObjects.Clear();

            for (int i = 0; i < riverMeshes.Count; i++)
            {
                if (riverMeshes[i] != null)
                {
                    SafeDestroyMesh(riverMeshes[i]);
                }
            }
            riverMeshes.Clear();

            for (int i = 0; i < riverObjects.Count; i++)
            {
                if (riverObjects[i] != null)
                {
                    SafeDestroyGO(riverObjects[i]);
                }
            }
            riverObjects.Clear();
        }

        private void BuildRivers(List<List<Vector3>> riverPaths, TerrainGenerationSettings settings)
        {
            for (int i = 0; i < riverPaths.Count; i++)
            {
                List<Vector3> path = riverPaths[i];
                if (path == null || path.Count < 2) continue;

                GameObject riverObj = new GameObject($"River_{i}");
                riverObj.transform.SetParent(transform, false);

                MeshFilter mf = riverObj.AddComponent<MeshFilter>();
                MeshRenderer mr = riverObj.AddComponent<MeshRenderer>();

                Mesh riverMesh = GenerateRiverMesh(path, settings, $"RiverMesh_{i}");
                riverMeshes.Add(riverMesh);
                mf.sharedMesh = riverMesh;

                mr.sharedMaterial = waterMaterial != null ? waterMaterial : waterfallMaterial;
                riverObjects.Add(riverObj);
            }
        }

        public Mesh GenerateRiverMesh(List<Vector3> path, TerrainGenerationSettings settings, string meshName = "RiverWaterMesh")
        {
            Mesh mesh = new Mesh
            {
                name = meshName,
                hideFlags = HideFlags.DontSave
            };

            int count = path.Count;
            int vertCount = count * 3;
            int triCount = (count - 1) * 12;

            Vector3[] verts = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Color[] colors = new Color[vertCount];
            int[] tris = new int[triCount];

            float vCoord = 0f;
            Color bankColor = new Color(0.9f, 0.98f, 1.0f, 0.35f);
            Color centerColor = new Color(0.24f, 0.76f, 0.92f, 0.88f);

            for (int i = 0; i < count; i++)
            {
                Vector3 current = path[i];

                Vector3 tangent;
                if (i == 0)
                    tangent = (path[1] - path[0]).normalized;
                else if (i == count - 1)
                    tangent = (path[count - 1] - path[count - 2]).normalized;
                else
                    tangent = (path[i + 1] - path[i - 1]).normalized;

                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                if (side.sqrMagnitude < 0.001f) side = Vector3.right;

                // River sits smoothly in channel
                float width = settings.riverWidth * 0.78f;
                float halfW = width * 0.5f;

                int iLeft = i * 3;
                int iCenter = i * 3 + 1;
                int iRight = i * 3 + 2;

                verts[iLeft] = current - side * halfW;
                verts[iCenter] = current + Vector3.up * 0.02f; // subtle 0.02f camber
                verts[iRight] = current + side * halfW;

                uvs[iLeft] = new Vector2(0f, vCoord);
                uvs[iCenter] = new Vector2(0.5f, vCoord);
                uvs[iRight] = new Vector2(1f, vCoord);

                normals[iLeft] = Vector3.up;
                normals[iCenter] = Vector3.up;
                normals[iRight] = Vector3.up;

                colors[iLeft] = bankColor;
                colors[iCenter] = centerColor;
                colors[iRight] = bankColor;

                if (i < count - 1)
                {
                    vCoord += Vector3.Distance(current, path[i + 1]) * 0.18f;
                }
            }

            int t = 0;
            for (int i = 0; i < count - 1; i++)
            {
                int c0 = i * 3;
                int c1 = i * 3 + 1;
                int c2 = i * 3 + 2;

                int n0 = (i + 1) * 3;
                int n1 = (i + 1) * 3 + 1;
                int n2 = (i + 1) * 3 + 2;

                // Left Quad
                tris[t++] = c0;
                tris[t++] = n0;
                tris[t++] = c1;

                tris[t++] = c1;
                tris[t++] = n0;
                tris[t++] = n1;

                // Right Quad
                tris[t++] = c1;
                tris[t++] = n1;
                tris[t++] = c2;

                tris[t++] = c2;
                tris[t++] = n1;
                tris[t++] = n2;
            }

            for (int i = 0; i < vertCount; i++)
            {
                if (float.IsNaN(verts[i].x) || float.IsNaN(verts[i].y) || float.IsNaN(verts[i].z) ||
                    float.IsInfinity(verts[i].x) || float.IsInfinity(verts[i].y) || float.IsInfinity(verts[i].z))
                {
                    verts[i] = Vector3.zero;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            return mesh;
        }

        // ================= DYNAMIC RIVER STROKE (LIVE BRUSH PAINTING) =================

        public void UpdateDynamicRiverStroke(List<Vector3> strokePoints, TerrainGenerationSettings settings)
        {
            if (strokePoints == null || strokePoints.Count < 2)
            {
                if (dynamicRiverObj != null) dynamicRiverObj.SetActive(false);
                return;
            }

            if (dynamicRiverObj == null)
            {
                dynamicRiverObj = new GameObject("DynamicRiverStroke");
                dynamicRiverObj.transform.SetParent(transform, false);
                dynamicRiverObj.AddComponent<MeshFilter>();
                MeshRenderer mr = dynamicRiverObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = waterMaterial;
            }

            dynamicRiverObj.SetActive(true);
            MeshFilter mf = dynamicRiverObj.GetComponent<MeshFilter>();
            if (dynamicRiverMesh != null)
            {
                SafeDestroyMesh(dynamicRiverMesh);
                dynamicRiverMesh = null;
            }

            dynamicRiverMesh = GenerateRiverMesh(strokePoints, settings, "DynamicRiverMesh");
            mf.sharedMesh = dynamicRiverMesh;
        }

        public void ClearDynamicRiverStroke()
        {
            if (dynamicRiverObj != null)
            {
                dynamicRiverObj.SetActive(false);
                if (dynamicRiverMesh != null)
                {
                    SafeDestroyMesh(dynamicRiverMesh);
                    dynamicRiverMesh = null;
                }
            }
        }

        // ================= WATER SURFACE & WATERFALLS =================

        private Mesh CreateWaterPlaneMesh(float size, int segments)
        {
            Mesh mesh = new Mesh
            {
                name = "WaterSurfaceMesh",
                hideFlags = HideFlags.DontSave
            };

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

                Mesh wfMesh = GenerateWaterfallMesh(wp);
                waterfallMeshes.Add(wfMesh);
                mf.sharedMesh = wfMesh;

                if (waterfallMaterial != null)
                {
                    mr.sharedMaterial = waterfallMaterial;
                }

                // Add circular foaming splash ring at plunge pool base
                GameObject splashObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                splashObj.name = "SplashFoam";
                splashObj.transform.SetParent(wfObj.transform, false);
                splashObj.transform.position = wp.endPosition + Vector3.up * 0.04f;
                splashObj.transform.rotation = Quaternion.Euler(90, 0, 0);
                splashObj.transform.localScale = Vector3.one * (wp.width * 1.8f);

                Collider c = splashObj.GetComponent<Collider>();
                if (c != null) DestroyImmediate(c);

                MeshFilter smf = splashObj.GetComponent<MeshFilter>();
                if (smf != null && smf.sharedMesh != null)
                {
                    Mesh sm = Object.Instantiate(smf.sharedMesh);
                    sm.hideFlags = HideFlags.DontSave;
                    Color[] radialColors = new Color[sm.vertexCount];
                    for (int k = 0; k < radialColors.Length; k++)
                    {
                        // Color.r > 1.5 flags the shader to draw the circular splash ripple
                        radialColors[k] = new Color(2.0f, 1f, 1f, 0.85f);
                    }
                    sm.colors = radialColors;
                    smf.sharedMesh = sm;
                    waterfallMeshes.Add(sm);
                }

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
            Mesh mesh = new Mesh
            {
                name = "WaterfallMesh",
                hideFlags = HideFlags.DontSave
            };

            int segments = 8;
            Vector3[] verts = new Vector3[(segments + 1) * 2];
            Vector2[] uvs = new Vector2[(segments + 1) * 2];
            Color[] colors = new Color[(segments + 1) * 2];
            int[] tris = new int[segments * 6];

            Vector3 side = Vector3.Cross(Vector3.up, wp.flowDirection).normalized * (wp.width * 0.5f);

            for (int s = 0; s <= segments; s++)
            {
                float t = (float)s / segments;

                Vector3 center = Vector3.Lerp(wp.startPosition, wp.endPosition, t);
                // Subtle 0.08f arc to drape naturally down the cliff face without jump ramps
                center += wp.flowDirection * Mathf.Sin(t * Mathf.PI) * 0.08f;

                int iLeft = s * 2;
                int iRight = s * 2 + 1;

                verts[iLeft] = center - side;
                verts[iRight] = center + side;

                uvs[iLeft] = new Vector2(0, t * 2f);
                uvs[iRight] = new Vector2(1, t * 2f);

                float alpha = (s == 0) ? 0.4f : ((s == segments) ? 0.95f : 0.85f);
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

        private void SafeDestroyMesh(Mesh m)
        {
            if (m == null) return;
            if (Application.isPlaying) Destroy(m);
            else DestroyImmediate(m);
        }

        private void SafeDestroyGO(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
