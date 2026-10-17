using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralWorldManager : MonoBehaviour
    {
        [Header("Settings")]
        public TerrainGenerationSettings settings = new TerrainGenerationSettings();

        [Header("Materials")]
        public Material terrainMaterial;
        public Material waterMaterial;
        public Material waterfallMaterial;

        [Header("Components")]
        public ProceduralWater waterController;
        public ProceduralFoliage foliageController;

        [Header("Runtime Options")]
        public bool generateOnStart = true;
        public bool autoUpdateInEditor = false;

        // Active state
        private float[,] heightMap;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Mesh currentTerrainMesh;
        private IslandMeshGenerator cachedMeshGen;

        private List<WaterfallPoint> activeWaterfalls = new List<WaterfallPoint>();
        private List<List<Vector3>> activeRiverPaths = new List<List<Vector3>>();

        public float[,] HeightMap => heightMap;
        public Mesh CurrentTerrainMesh => currentTerrainMesh;
        public List<WaterfallPoint> ActiveWaterfalls => activeWaterfalls;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

            EnsureControllers();
        }

        private void Start()
        {
            if (Application.isPlaying && generateOnStart)
            {
                GenerateWorld();
            }
        }

        private void OnDestroy()
        {
            CleanupMesh();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                CleanupMesh();
            }
        }

        private void CleanupMesh()
        {
            if (currentTerrainMesh != null)
            {
                if (meshFilter != null && meshFilter.sharedMesh == currentTerrainMesh)
                {
                    meshFilter.sharedMesh = null;
                }
                if (meshCollider != null && meshCollider.sharedMesh == currentTerrainMesh)
                {
                    meshCollider.sharedMesh = null;
                }

                if (Application.isPlaying)
                {
                    Destroy(currentTerrainMesh);
                }
                else
                {
                    DestroyImmediate(currentTerrainMesh);
                }
                currentTerrainMesh = null;
            }
        }

        public void EnsureControllers()
        {
            if (waterController == null)
            {
                waterController = GetComponentInChildren<ProceduralWater>();
                if (waterController == null)
                {
                    GameObject wObj = new GameObject("ProceduralWater");
                    wObj.transform.SetParent(transform, false);
                    waterController = wObj.AddComponent<ProceduralWater>();
                }
            }
            if (waterMaterial != null) waterController.waterMaterial = waterMaterial;
            if (waterfallMaterial != null) waterController.waterfallMaterial = waterfallMaterial;

            if (foliageController == null)
            {
                foliageController = GetComponentInChildren<ProceduralFoliage>();
                if (foliageController == null)
                {
                    GameObject fObj = new GameObject("ProceduralFoliage");
                    fObj.transform.SetParent(transform, false);
                    foliageController = fObj.AddComponent<ProceduralFoliage>();
                }
            }
        }

        public void GenerateWorld()
        {
            EnsureControllers();

            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

            // 1. Generate base heightmap
            GenerateHeightMap();

            // 2. Generate rivers & carve riverbeds
            RiverGenerator riverGen = new RiverGenerator(settings, heightMap);
            activeWaterfalls = riverGen.GenerateRivers(out activeRiverPaths);

            // 3. Generate or update terrain mesh (REUSING single Mesh instance)
            cachedMeshGen = new IslandMeshGenerator(settings, heightMap);
            currentTerrainMesh = cachedMeshGen.GenerateOrUpdateMesh(currentTerrainMesh);
            meshFilter.sharedMesh = currentTerrainMesh;
            meshCollider.sharedMesh = currentTerrainMesh;

            if (terrainMaterial != null)
            {
                meshRenderer.sharedMaterial = terrainMaterial;
            }

            // 4. Build water & waterfalls
            if (waterController != null)
            {
                waterController.BuildWater(settings, activeWaterfalls);
            }

            // 5. Spawn foliage & nature props
            if (foliageController != null)
            {
                foliageController.BuildFoliage(settings, heightMap, currentTerrainMesh);
            }
        }

        public void RandomizeSeed()
        {
            settings.seed = Random.Range(1000, 999999);
            GenerateWorld();
        }

        private void GenerateHeightMap()
        {
            int size = settings.mapSize;
            heightMap = new float[size, size];

            FastNoiseLite baseNoise = new FastNoiseLite(settings.seed);
            baseNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            baseNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            baseNoise.SetFrequency(settings.mainNoiseFrequency);
            baseNoise.SetFractalOctaves(settings.mainNoiseOctaves);

            FastNoiseLite mountainNoise = new FastNoiseLite(settings.seed + 101);
            mountainNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            mountainNoise.SetFractalType(FastNoiseLite.FractalType.Ridged);
            mountainNoise.SetFrequency(settings.mountainNoiseFrequency);
            mountainNoise.SetFractalOctaves(3);

            FastNoiseLite warpNoise = new FastNoiseLite(settings.seed + 202);
            warpNoise.SetFrequency(0.02f);
            warpNoise.SetDomainWarpAmp(settings.coastlineWarpStrength);

            float halfWorld = settings.worldSize * 0.5f;
            float centerDistMax = halfWorld * settings.islandRadius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float wx = (x / (float)(size - 1)) * settings.worldSize - halfWorld;
                    float wz = (y / (float)(size - 1)) * settings.worldSize - halfWorld;

                    float warpedX = wx;
                    float warpedZ = wz;
                    warpNoise.DomainWarp(ref warpedX, ref warpedZ);

                    float distFromCenter = Mathf.Sqrt(warpedX * warpedX + warpedZ * warpedZ);
                    float edge0 = centerDistMax * (1.0f - settings.islandFalloffSharpness);
                    float edge1 = centerDistMax;
                    float islandMask = 1.0f - Mathf.SmoothStep(edge0, edge1, distFromCenter);

                    float nBase = (baseNoise.GetNoise(warpedX, warpedZ) + 1.0f) * 0.5f;
                    float nMountain = Mathf.Max(0, mountainNoise.GetNoise(warpedX * 1.4f, warpedZ * 1.4f));

                    float elevation = (nBase * 0.6f + nMountain * 0.5f * settings.mountainHeightMultiplier);
                    elevation *= settings.maxHeight * islandMask;
                    elevation += settings.baseHeight * islandMask;

                    float oceanFloor = settings.waterLevel - 3.5f;
                    heightMap[x, y] = Mathf.Max(oceanFloor, elevation);
                }
            }
        }

        // ================= INTERACTIVE BRUSH SCULPTING =================

        public void ApplyBrush(Vector3 hitPoint, float radius, float strength, TerrainBrushMode mode)
        {
            if (heightMap == null) return;

            int size = settings.mapSize;
            float worldSize = settings.worldSize;
            float halfWorld = worldSize * 0.5f;
            float cellSize = worldSize / (size - 1);

            float gx = ((hitPoint.x + halfWorld) / worldSize) * (size - 1);
            float gy = ((hitPoint.z + halfWorld) / worldSize) * (size - 1);

            int cx = Mathf.RoundToInt(gx);
            int cy = Mathf.RoundToInt(gy);
            int radiusCells = Mathf.CeilToInt(radius / cellSize);
            float targetPlateauHeight = hitPoint.y;

            bool modified = false;

            for (int dx = -radiusCells; dx <= radiusCells; dx++)
            {
                for (int dy = -radiusCells; dy <= radiusCells; dy++)
                {
                    int x = cx + dx;
                    int y = cy + dy;

                    if (x < 1 || x >= size - 1 || y < 1 || y >= size - 1)
                        continue;

                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(gx, gy)) * cellSize;
                    if (dist > radius) continue;

                    float falloff = Mathf.Cos((dist / radius) * Mathf.PI * 0.5f);
                    float delta = strength * falloff * Time.deltaTime * 10f;

                    switch (mode)
                    {
                        case TerrainBrushMode.RaiseMountain:
                            heightMap[x, y] = Mathf.Min(settings.maxHeight * 1.4f, heightMap[x, y] + delta);
                            modified = true;
                            break;

                        case TerrainBrushMode.LowerRiver:
                            float minFloor = settings.waterLevel - 2.8f;
                            heightMap[x, y] = Mathf.Max(minFloor, heightMap[x, y] - delta);
                            modified = true;
                            break;

                        case TerrainBrushMode.FlattenPlateau:
                            heightMap[x, y] = Mathf.MoveTowards(heightMap[x, y], targetPlateauHeight, delta);
                            modified = true;
                            break;

                        case TerrainBrushMode.SmoothTerrain:
                            float avg = (heightMap[x - 1, y] + heightMap[x + 1, y] + heightMap[x, y - 1] + heightMap[x, y + 1]) * 0.25f;
                            heightMap[x, y] = Mathf.Lerp(heightMap[x, y], avg, falloff * 0.4f);
                            modified = true;
                            break;
                    }
                }
            }

            if (modified)
            {
                // In-place mesh vertex/color update without reallocating native mesh
                UpdateTerrainMeshVisualsOnly();
            }

            // Foliage brush actions
            if (mode == TerrainBrushMode.PaintFoliage && foliageController != null)
            {
                foliageController.SpawnTreeAt(hitPoint, FoliageType.DeciduousTree);
            }
            else if (mode == TerrainBrushMode.EraseFoliage && foliageController != null)
            {
                foliageController.RemoveFoliageNear(hitPoint, radius);
            }
        }

        /// <summary>
        /// Real-time visual update during brush drag - reuses existing mesh buffer without PhysX bake.
        /// </summary>
        public void UpdateTerrainMeshVisualsOnly()
        {
            if (heightMap == null) return;
            if (cachedMeshGen == null)
            {
                cachedMeshGen = new IslandMeshGenerator(settings, heightMap);
            }

            currentTerrainMesh = cachedMeshGen.GenerateOrUpdateMesh(currentTerrainMesh);
            if (meshFilter != null && meshFilter.sharedMesh != currentTerrainMesh)
            {
                meshFilter.sharedMesh = currentTerrainMesh;
            }
        }

        /// <summary>
        /// Bakes collision mesh when mouse is released or full regeneration occurs.
        /// </summary>
        public void BakeCollisionMesh()
        {
            if (meshCollider != null && currentTerrainMesh != null)
            {
                meshCollider.sharedMesh = null; // Forces PhysX refresh
                meshCollider.sharedMesh = currentTerrainMesh;
            }
        }

        public void RebuildMesh()
        {
            UpdateTerrainMeshVisualsOnly();
            BakeCollisionMesh();
        }
    }
}
