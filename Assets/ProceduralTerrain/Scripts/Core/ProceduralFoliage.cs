using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class ProceduralFoliage : MonoBehaviour
    {
        [Header("Foliage Density & Rules")]
        [Range(0f, 1f)]
        public float treeDensity = 0.45f;
        [Range(0f, 1f)]
        public float rockDensity = 0.25f;
        [Range(0f, 1f)]
        public float flowerDensity = 0.35f;

        [Header("Shared Materials")]
        public Material trunkMaterial;
        public Material pineNeedleMaterial;
        public Material deciduousCanopyMaterial;
        public Material blossomCanopyMaterial;
        public Material rockMaterial;
        public Material flowerMaterial;

        // Cached procedural prototype meshes (built once, reused forever)
        private static Mesh pineMesh;
        private static Mesh deciduousMesh;
        private static Mesh blossomMesh;
        private static Mesh rockMesh;
        private static Mesh flowerMesh;

        private GameObject foliageRoot;
        private List<GameObject> spawnedFoliage = new List<GameObject>();

        private void Awake()
        {
            EnsurePrototypes();
        }

        private void OnDestroy()
        {
            ClearFoliage();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                ClearFoliage();
            }
        }

        public void EnsurePrototypes()
        {
            if (pineMesh == null)
            {
                pineMesh = BuildPineTreeMesh();
                pineMesh.hideFlags = HideFlags.DontSave;
            }
            if (deciduousMesh == null)
            {
                deciduousMesh = BuildDeciduousTreeMesh();
                deciduousMesh.hideFlags = HideFlags.DontSave;
            }
            if (blossomMesh == null)
            {
                blossomMesh = BuildDeciduousTreeMesh(true);
                blossomMesh.hideFlags = HideFlags.DontSave;
            }
            if (rockMesh == null)
            {
                rockMesh = BuildRockMesh();
                rockMesh.hideFlags = HideFlags.DontSave;
            }
            if (flowerMesh == null)
            {
                flowerMesh = BuildFlowerClumpMesh();
                flowerMesh.hideFlags = HideFlags.DontSave;
            }

            EnsureMaterials();
        }

        public void BuildFoliage(TerrainGenerationSettings settings, float[,] heightMap, Mesh terrainMesh)
        {
            EnsurePrototypes();
            ClearFoliage();

            foliageRoot = new GameObject("FoliageRoot");
            foliageRoot.transform.SetParent(transform, false);

            int mapSize = settings.mapSize;
            float worldSize = settings.worldSize;
            float halfWorld = worldSize * 0.5f;
            float cellSize = worldSize / (mapSize - 1);

            FastNoiseLite forestNoise = new FastNoiseLite(settings.seed + 314);
            forestNoise.SetFrequency(0.045f);
            forestNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            forestNoise.SetFractalOctaves(3);

            System.Random prng = new System.Random(settings.seed + 777);

            // Stride of 3 gives great clustering while keeping tree count modest (150-250 trees max)
            int step = 3;
            int maxTrees = 250;
            int treesSpawned = 0;

            for (int y = 3; y < mapSize - 3 && treesSpawned < maxTrees; y += step)
            {
                for (int x = 3; x < mapSize - 3 && treesSpawned < maxTrees; x += step)
                {
                    float h = heightMap[x, y];

                    if (h < settings.beachHeight + 0.4f)
                        continue;

                    if (h > settings.maxHeight * 0.72f)
                        continue;

                    float hL = heightMap[Mathf.Max(0, x - 1), y];
                    float hR = heightMap[Mathf.Min(mapSize - 1, x + 1), y];
                    float hD = heightMap[x, Mathf.Max(0, y - 1)];
                    float hU = heightMap[x, Mathf.Min(mapSize - 1, y + 1)];

                    Vector3 normal = new Vector3(hL - hR, cellSize * 2f, hD - hU).normalized;
                    if (normal.y < 0.78f)
                    {
                        if ((float)prng.NextDouble() < rockDensity * 0.12f)
                        {
                            Vector3 rPos = new Vector3(x * cellSize - halfWorld, h, y * cellSize - halfWorld);
                            SpawnRock(rPos, prng);
                        }
                        continue;
                    }

                    float wx = x * cellSize - halfWorld + ((float)prng.NextDouble() - 0.5f) * cellSize * 0.9f;
                    float wz = y * cellSize - halfWorld + ((float)prng.NextDouble() - 0.5f) * cellSize * 0.9f;
                    Vector3 worldPos = new Vector3(wx, h, wz);

                    float fNoise = (forestNoise.GetNoise(wx, wz) + 1f) * 0.5f;

                    if (fNoise > (1f - treeDensity * 0.85f))
                    {
                        if ((float)prng.NextDouble() < 0.7f)
                        {
                            if (h > settings.maxHeight * 0.45f)
                            {
                                SpawnTree(worldPos, FoliageType.PineTree, prng);
                            }
                            else
                            {
                                double roll = prng.NextDouble();
                                if (roll < 0.65)
                                    SpawnTree(worldPos, FoliageType.DeciduousTree, prng);
                                else if (roll < 0.88)
                                    SpawnTree(worldPos, FoliageType.BlossomTree, prng);
                                else
                                    SpawnTree(worldPos, FoliageType.PineTree, prng);
                            }
                            treesSpawned++;
                        }
                    }
                    else if (fNoise < 0.42f)
                    {
                        if ((float)prng.NextDouble() < flowerDensity * 0.3f)
                        {
                            SpawnFlower(worldPos, prng);
                        }
                    }
                }
            }
        }

        public void SpawnTreeAt(Vector3 pos, FoliageType type)
        {
            EnsurePrototypes();
            if (foliageRoot == null)
            {
                foliageRoot = new GameObject("FoliageRoot");
                foliageRoot.transform.SetParent(transform, false);
            }
            System.Random prng = new System.Random();
            SpawnTree(pos, type, prng);
        }

        public void RemoveFoliageNear(Vector3 center, float radius)
        {
            float sqrR = radius * radius;
            for (int i = spawnedFoliage.Count - 1; i >= 0; i--)
            {
                var f = spawnedFoliage[i];
                if (f != null && (f.transform.position - center).sqrMagnitude <= sqrR)
                {
                    SafeDestroyGO(f);
                    spawnedFoliage.RemoveAt(i);
                }
            }
        }

        public void ClearFoliage()
        {
            if (foliageRoot != null)
            {
                SafeDestroyGO(foliageRoot);
                foliageRoot = null;
            }
            spawnedFoliage.Clear();
        }

        private void SpawnTree(Vector3 pos, FoliageType type, System.Random prng)
        {
            GameObject tree = new GameObject(type.ToString());
            tree.transform.SetParent(foliageRoot.transform, false);
            tree.transform.position = pos;

            float scale = 0.85f + (float)prng.NextDouble() * 0.45f;
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.rotation = Quaternion.Euler(0, (float)prng.NextDouble() * 360f, 0);

            MeshFilter mf = tree.AddComponent<MeshFilter>();
            MeshRenderer mr = tree.AddComponent<MeshRenderer>();

            switch (type)
            {
                case FoliageType.PineTree:
                    mf.sharedMesh = pineMesh;
                    mr.sharedMaterials = new Material[] { trunkMaterial, pineNeedleMaterial };
                    break;
                case FoliageType.BlossomTree:
                    mf.sharedMesh = blossomMesh;
                    mr.sharedMaterials = new Material[] { trunkMaterial, blossomCanopyMaterial };
                    break;
                case FoliageType.DeciduousTree:
                default:
                    mf.sharedMesh = deciduousMesh;
                    mr.sharedMaterials = new Material[] { trunkMaterial, deciduousCanopyMaterial };
                    break;
            }

            spawnedFoliage.Add(tree);
        }

        private void SpawnRock(Vector3 pos, System.Random prng)
        {
            GameObject rock = new GameObject("RiverRock");
            rock.transform.SetParent(foliageRoot.transform, false);
            rock.transform.position = pos;

            float sx = 0.6f + (float)prng.NextDouble() * 0.8f;
            float sy = 0.4f + (float)prng.NextDouble() * 0.5f;
            float sz = 0.6f + (float)prng.NextDouble() * 0.8f;
            rock.transform.localScale = new Vector3(sx, sy, sz);
            rock.transform.rotation = Quaternion.Euler((float)prng.NextDouble() * 20f, (float)prng.NextDouble() * 360f, (float)prng.NextDouble() * 20f);

            MeshFilter mf = rock.AddComponent<MeshFilter>();
            MeshRenderer mr = rock.AddComponent<MeshRenderer>();
            mf.sharedMesh = rockMesh;
            mr.sharedMaterial = rockMaterial;

            spawnedFoliage.Add(rock);
        }

        private void SpawnFlower(Vector3 pos, System.Random prng)
        {
            GameObject flower = new GameObject("FlowerCluster");
            flower.transform.SetParent(foliageRoot.transform, false);
            flower.transform.position = pos;

            float s = 0.7f + (float)prng.NextDouble() * 0.5f;
            flower.transform.localScale = Vector3.one * s;
            flower.transform.rotation = Quaternion.Euler(0, (float)prng.NextDouble() * 360f, 0);

            MeshFilter mf = flower.AddComponent<MeshFilter>();
            MeshRenderer mr = flower.AddComponent<MeshRenderer>();
            mf.sharedMesh = flowerMesh;
            mr.sharedMaterial = flowerMaterial;

            spawnedFoliage.Add(flower);
        }

        // ================= PROCEDURAL MESH FACTORIES =================

        private Mesh BuildPineTreeMesh()
        {
            Mesh mesh = new Mesh { name = "ProceduralPineMesh", subMeshCount = 2, hideFlags = HideFlags.DontSave };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> trunkTris = new List<int>();
            List<int> foliageTris = new List<int>();

            AddCylinder(verts, normals, colors, trunkTris, new Vector3(0, 0, 0), new Vector3(0, 1.4f, 0), 0.22f, 0.16f, 6, new Color(0.45f, 0.30f, 0.18f));
            AddCone(verts, normals, colors, foliageTris, new Vector3(0, 1.0f, 0), 1.6f, 1.35f, 6, new Color(0.18f, 0.48f, 0.22f));
            AddCone(verts, normals, colors, foliageTris, new Vector3(0, 1.8f, 0), 1.5f, 1.10f, 6, new Color(0.20f, 0.52f, 0.24f));
            AddCone(verts, normals, colors, foliageTris, new Vector3(0, 2.5f, 0), 1.4f, 0.80f, 6, new Color(0.24f, 0.58f, 0.28f));

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(trunkTris, 0);
            mesh.SetTriangles(foliageTris, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh BuildDeciduousTreeMesh(bool blossom = false)
        {
            Mesh mesh = new Mesh { name = blossom ? "ProceduralBlossomMesh" : "ProceduralDeciduousMesh", subMeshCount = 2, hideFlags = HideFlags.DontSave };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> trunkTris = new List<int>();
            List<int> foliageTris = new List<int>();

            AddCylinder(verts, normals, colors, trunkTris, Vector3.zero, new Vector3(0, 1.6f, 0), 0.28f, 0.20f, 6, new Color(0.48f, 0.34f, 0.22f));

            Color canopyCol = blossom ? new Color(0.96f, 0.65f, 0.74f) : new Color(0.32f, 0.72f, 0.22f);
            Color canopyHighlight = blossom ? new Color(1.0f, 0.78f, 0.86f) : new Color(0.42f, 0.82f, 0.28f);

            AddIcoSphere(verts, normals, colors, foliageTris, new Vector3(0, 2.2f, 0), 1.15f, canopyCol);
            AddIcoSphere(verts, normals, colors, foliageTris, new Vector3(0.45f, 2.0f, 0.35f), 0.85f, canopyHighlight);
            AddIcoSphere(verts, normals, colors, foliageTris, new Vector3(-0.4f, 2.1f, -0.3f), 0.90f, canopyCol);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(trunkTris, 0);
            mesh.SetTriangles(foliageTris, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh BuildRockMesh()
        {
            Mesh mesh = new Mesh { name = "ProceduralRockMesh", hideFlags = HideFlags.DontSave };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();

            AddIcoSphere(verts, normals, colors, tris, Vector3.zero, 0.8f, new Color(0.55f, 0.52f, 0.48f));

            System.Random prng = new System.Random(42);
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                v.y *= 0.65f;
                v += new Vector3(
                    ((float)prng.NextDouble() - 0.5f) * 0.15f,
                    ((float)prng.NextDouble() - 0.5f) * 0.12f,
                    ((float)prng.NextDouble() - 0.5f) * 0.15f
                );
                verts[i] = v;
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh BuildFlowerClumpMesh()
        {
            Mesh mesh = new Mesh { name = "ProceduralFlowerClumpMesh", hideFlags = HideFlags.DontSave };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();

            Color[] flowerColors = new Color[] {
                new Color(1f, 0.88f, 0.2f),
                new Color(0.95f, 0.35f, 0.45f),
                new Color(0.75f, 0.5f, 0.95f)
            };

            System.Random prng = new System.Random(99);
            for (int f = 0; f < 5; f++)
            {
                float ang = f * Mathf.PI * 2f / 5f;
                float r = 0.25f + (float)prng.NextDouble() * 0.2f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0.18f, Mathf.Sin(ang) * r);
                Color c = flowerColors[prng.Next(flowerColors.Length)];
                AddIcoSphere(verts, normals, colors, tris, pos, 0.10f, c);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddCylinder(List<Vector3> verts, List<Vector3> normals, List<Color> colors, List<int> tris,
            Vector3 bottom, Vector3 top, float rBottom, float rTop, int sides, Color col)
        {
            int baseIdx = verts.Count;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);

                verts.Add(bottom + new Vector3(cos * rBottom, 0, sin * rBottom));
                normals.Add(new Vector3(cos, 0, sin));
                colors.Add(col);

                verts.Add(top + new Vector3(cos * rTop, 0, sin * rTop));
                normals.Add(new Vector3(cos, 0, sin));
                colors.Add(col * 1.15f);
            }

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int b0 = baseIdx + i * 2;
                int t0 = b0 + 1;
                int b1 = baseIdx + next * 2;
                int t1 = b1 + 1;

                tris.Add(b0); tris.Add(t0); tris.Add(b1);
                tris.Add(b1); tris.Add(t0); tris.Add(t1);
            }
        }

        private void AddCone(List<Vector3> verts, List<Vector3> normals, List<Color> colors, List<int> tris,
            Vector3 baseCenter, float height, float radius, int sides, Color col)
        {
            int baseIdx = verts.Count;
            Vector3 apex = baseCenter + Vector3.up * height;

            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);

                Vector3 b = baseCenter + new Vector3(cos * radius, 0, sin * radius);
                Vector3 normal = Vector3.Cross(apex - b, new Vector3(-sin, 0, cos)).normalized;

                verts.Add(b);
                normals.Add(normal);
                colors.Add(col);
            }

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int b0 = baseIdx + i;
                int b1 = baseIdx + next;

                int apexIdx = verts.Count;
                Vector3 midN = (normals[b0] + normals[b1]).normalized;
                verts.Add(apex);
                normals.Add(midN);
                colors.Add(col * 1.25f);

                tris.Add(b0);
                tris.Add(apexIdx);
                tris.Add(b1);
            }
        }

        private void AddIcoSphere(List<Vector3> verts, List<Vector3> normals, List<Color> colors, List<int> tris,
            Vector3 center, float radius, Color col)
        {
            int startIdx = verts.Count;
            float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

            Vector3[] icosahedronVerts = new Vector3[] {
                new Vector3(-1,  t,  0).normalized,
                new Vector3( 1,  t,  0).normalized,
                new Vector3(-1, -t,  0).normalized,
                new Vector3( 1, -t,  0).normalized,
                new Vector3( 0, -1,  t).normalized,
                new Vector3( 0,  1,  t).normalized,
                new Vector3( 0, -1, -t).normalized,
                new Vector3( 0,  1, -t).normalized,
                new Vector3( t,  0, -1).normalized,
                new Vector3( t,  0,  1).normalized,
                new Vector3(-t,  0, -1).normalized,
                new Vector3(-t,  0,  1).normalized
            };

            int[] icosahedronTris = new int[] {
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            for (int i = 0; i < icosahedronVerts.Length; i++)
            {
                verts.Add(center + icosahedronVerts[i] * radius);
                normals.Add(icosahedronVerts[i]);
                colors.Add(col);
            }

            for (int i = 0; i < icosahedronTris.Length; i++)
            {
                tris.Add(startIdx + icosahedronTris[i]);
            }
        }

        private void SafeDestroyGO(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        private void EnsureMaterials()
        {
            Shader standardShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (trunkMaterial == null)
            {
                trunkMaterial = new Material(standardShader) { name = "Mat_TreeTrunk", hideFlags = HideFlags.DontSave };
                trunkMaterial.color = new Color(0.45f, 0.32f, 0.20f);
            }
            if (pineNeedleMaterial == null)
            {
                pineNeedleMaterial = new Material(standardShader) { name = "Mat_PineNeedles", hideFlags = HideFlags.DontSave };
                pineNeedleMaterial.color = new Color(0.18f, 0.50f, 0.22f);
            }
            if (deciduousCanopyMaterial == null)
            {
                deciduousCanopyMaterial = new Material(standardShader) { name = "Mat_DeciduousCanopy", hideFlags = HideFlags.DontSave };
                deciduousCanopyMaterial.color = new Color(0.35f, 0.74f, 0.25f);
            }
            if (blossomCanopyMaterial == null)
            {
                blossomCanopyMaterial = new Material(standardShader) { name = "Mat_BlossomCanopy", hideFlags = HideFlags.DontSave };
                blossomCanopyMaterial.color = new Color(0.96f, 0.65f, 0.75f);
            }
            if (rockMaterial == null)
            {
                rockMaterial = new Material(standardShader) { name = "Mat_RiverRock", hideFlags = HideFlags.DontSave };
                rockMaterial.color = new Color(0.55f, 0.52f, 0.48f);
            }
            if (flowerMaterial == null)
            {
                flowerMaterial = new Material(standardShader) { name = "Mat_FlowerCluster", hideFlags = HideFlags.DontSave };
                flowerMaterial.color = new Color(0.95f, 0.85f, 0.25f);
            }
        }
    }
}
