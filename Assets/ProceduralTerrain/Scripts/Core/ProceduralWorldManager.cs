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
        private List<List<Vector3>> customRiverPaths = new List<List<Vector3>>();
        private List<Vector3> activeRiverStroke = new List<Vector3>();

        public float[,] HeightMap => heightMap;
        public Mesh CurrentTerrainMesh => currentTerrainMesh;
        public List<WaterfallPoint> ActiveWaterfalls => activeWaterfalls;
        public List<List<Vector3>> CustomRiverPaths => customRiverPaths;

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

        public void SanitizeSettings()
        {
            if (settings == null) settings = new TerrainGenerationSettings();
            if (settings.baseHeight < 5.0f) settings.baseHeight = 5.8f;
            if (settings.maxHeight < 24.0f) settings.maxHeight = 28.0f;
            if (settings.islandRadius < 0.70f) settings.islandRadius = 0.74f;
            if (settings.islandFalloffSharpness < 0.20f) settings.islandFalloffSharpness = 0.28f;
            if (settings.coastlineWarpStrength < 5.0f) settings.coastlineWarpStrength = 15.0f;
            if (settings.waterLevel < 1.0f) settings.waterLevel = 4.0f;
            if (settings.beachHeight < 5.0f) settings.beachHeight = 5.2f;
        }

        public void GenerateWorld()
        {
            SanitizeSettings();
            EnsureControllers();

            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

            customRiverPaths.Clear();
            activeRiverStroke.Clear();

            // 1. Generate multi-tiered base heightmap with guaranteed mountains and plains
            GenerateHeightMap();

            // 2. Generate rivers & carve riverbeds
            RiverGenerator riverGen = new RiverGenerator(settings, heightMap);
            activeWaterfalls = riverGen.GenerateRivers(out activeRiverPaths);

            // 3. Generate or update terrain mesh (forcing clean topology on seed / world regeneration)
            cachedMeshGen = new IslandMeshGenerator(settings, heightMap);
            currentTerrainMesh = cachedMeshGen.GenerateOrUpdateMesh(currentTerrainMesh, forceFullClear: true);
            meshFilter.sharedMesh = null;
            meshFilter.sharedMesh = currentTerrainMesh;

            // Force PhysX to re-bake collision with new mesh data
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = currentTerrainMesh;

            if (terrainMaterial != null)
            {
                meshRenderer.sharedMaterial = terrainMaterial;
            }

            // 4. Build ocean plane, rivers & waterfalls
            if (waterController != null)
            {
                waterController.BuildWater(settings, activeWaterfalls, GetAllRiverPaths());
            }

            // 5. Spawn foliage & nature props
            if (foliageController != null)
            {
                foliageController.BuildFoliage(settings, heightMap, currentTerrainMesh);
            }
        }

        public List<List<Vector3>> GetAllRiverPaths()
        {
            List<List<Vector3>> all = new List<List<Vector3>>();
            if (activeRiverPaths != null) all.AddRange(activeRiverPaths);
            if (customRiverPaths != null) all.AddRange(customRiverPaths);
            return all;
        }

        public void RandomizeSeed()
        {
            SanitizeSettings();
            settings.seed = Random.Range(1000, 999999);
            Debug.Log($"<color=#44FF88><b>[Fairytale World]</b> Generated new random island with Seed: {settings.seed}</color>");
            GenerateWorld();
        }

        private void GenerateHeightMap()
        {
            SanitizeSettings();

            int size = settings.mapSize;
            heightMap = new float[size, size];

            FastNoiseLite baseNoise = new FastNoiseLite(settings.seed);
            baseNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            baseNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            baseNoise.SetFrequency(0.016f);
            baseNoise.SetFractalOctaves(4);

            FastNoiseLite hillNoise = new FastNoiseLite(settings.seed + 101);
            hillNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            hillNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            hillNoise.SetFrequency(0.028f);
            hillNoise.SetFractalOctaves(3);

            FastNoiseLite ridgeNoise = new FastNoiseLite(settings.seed + 202);
            ridgeNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            ridgeNoise.SetFrequency(0.025f);

            FastNoiseLite warpNoise = new FastNoiseLite(settings.seed + 303);
            warpNoise.SetFrequency(0.018f);
            warpNoise.SetDomainWarpAmp(settings.coastlineWarpStrength > 0 ? settings.coastlineWarpStrength : 15f);

            float halfWorld = settings.worldSize * 0.5f;
            float islandRadiusWorld = halfWorld * settings.islandRadius;

            // Seed-based mountain spine & peak cluster placement:
            // Every seed creates a distinct mountain orientation, positioning, and landscape layout
            System.Random prng = new System.Random(settings.seed);
            float spineAngle = (float)(prng.NextDouble() * Mathf.PI);
            float spineDirX = Mathf.Cos(spineAngle);
            float spineDirZ = Mathf.Sin(spineAngle);
            float spineOffsetX = (float)(prng.NextDouble() * 24.0 - 12.0);
            float spineOffsetZ = (float)(prng.NextDouble() * 24.0 - 12.0);

            // Secondary peak cluster offset (adds side summits and twin peaks)
            float peak2Angle = spineAngle + (float)(Mathf.PI * 0.5f + (prng.NextDouble() - 0.5) * 0.6f);
            float peak2Dist = (float)(14.0 + prng.NextDouble() * 16.0);
            float peak2X = Mathf.Cos(peak2Angle) * peak2Dist;
            float peak2Z = Mathf.Sin(peak2Angle) * peak2Dist;

            float baseElevation = settings.baseHeight;
            float maxMountainHeight = settings.maxHeight * 0.90f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float wx = (x / (float)(size - 1)) * settings.worldSize - halfWorld;
                    float wz = (y / (float)(size - 1)) * settings.worldSize - halfWorld;

                    // Coastline organic domain warp
                    float warpedX = wx;
                    float warpedZ = wz;
                    warpNoise.DomainWarp(ref warpedX, ref warpedZ);

                    float distFromCenter = Mathf.Sqrt(warpedX * warpedX + warpedZ * warpedZ);

                    // Smooth organic falloff into ocean
                    float edge0 = islandRadiusWorld * (1.0f - settings.islandFalloffSharpness);
                    float edge1 = islandRadiusWorld;
                    float t = Mathf.Clamp01((distFromCenter - edge0) / (edge1 - edge0));
                    float islandMask = 1.0f - (t * t * (3f - 2f * t));

                    // Layer 1: Rolling lowland meadows (5.8m to 9.5m)
                    float nBase = (baseNoise.GetNoise(warpedX, warpedZ) + 1f) * 0.5f;
                    float plains = baseElevation + nBase * 3.8f;

                    // Layer 2: Rolling hills (adds 0m to 7.0m)
                    float nHills = (hillNoise.GetNoise(warpedX * 1.2f + 100f, warpedZ * 1.2f + 100f) + 1f) * 0.5f;
                    float hillsElevation = Mathf.Max(0f, (nHills - 0.35f) * 1.54f) * 7.0f;

                    // Layer 3: Mountain Spines & Peaks (adds up to 25m+ with crags and snowy summits)
                    float relX = warpedX - spineOffsetX;
                    float relZ = warpedZ - spineOffsetZ;
                    float distToSpine = Mathf.Abs(-spineDirZ * relX + spineDirX * relZ);
                    float spineInfluence = Mathf.Clamp01(1.0f - (distToSpine / 28.0f));
                    spineInfluence = spineInfluence * spineInfluence * (3f - 2f * spineInfluence);

                    float distToPeak2 = Mathf.Sqrt((warpedX - peak2X) * (warpedX - peak2X) + (warpedZ - peak2Z) * (warpedZ - peak2Z));
                    float peak2Influence = Mathf.Clamp01(1.0f - (distToPeak2 / 20.0f));
                    peak2Influence = peak2Influence * peak2Influence * (3f - 2f * peak2Influence);

                    float combinedMtnZone = Mathf.Clamp01(spineInfluence + peak2Influence * 0.8f);

                    float rawNoise = ridgeNoise.GetNoise(warpedX * 1.6f, warpedZ * 1.6f);
                    float nRidge = Mathf.Clamp01(1.0f - Mathf.Abs(rawNoise));
                    float peakShape = Mathf.Pow(nRidge, 1.4f);
                    float mountainElevation = combinedMtnZone * peakShape * maxMountainHeight;

                    // Combine multi-tier layers and shape island
                    float landHeight = plains + hillsElevation + mountainElevation;
                    float elevation = landHeight * islandMask;

                    float oceanFloor = settings.waterLevel - 3.5f;
                    if (float.IsNaN(elevation) || float.IsInfinity(elevation))
                    {
                        elevation = oceanFloor;
                    }
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
                            // River brush records stroke points and avoids continuous erasure during drag.
                            // A shallow, gentle channel indentation (max 0.25 units) is applied on stroke completion.
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

            if (mode == TerrainBrushMode.LowerRiver)
            {
                AddRiverStrokePoint(hitPoint);
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

        public float SampleHeightBilinear(float wx, float wz)
        {
            if (heightMap == null) return 0f;
            int size = settings.mapSize;
            float worldSize = settings.worldSize;
            float halfWorld = worldSize * 0.5f;

            float gx = ((wx + halfWorld) / worldSize) * (size - 1);
            float gy = ((wz + halfWorld) / worldSize) * (size - 1);

            int x0 = Mathf.Clamp(Mathf.FloorToInt(gx), 0, size - 1);
            int x1 = Mathf.Clamp(x0 + 1, 0, size - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(gy), 0, size - 1);
            int y1 = Mathf.Clamp(y0 + 1, 0, size - 1);

            float tx = gx - x0;
            float ty = gy - y0;

            float h00 = heightMap[x0, y0];
            float h10 = heightMap[x1, y0];
            float h01 = heightMap[x0, y1];
            float h11 = heightMap[x1, y1];

            float h0 = Mathf.Lerp(h00, h10, tx);
            float h1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(h0, h1, ty);
        }

        private void AddRiverStrokePoint(Vector3 hitPoint)
        {
            if (activeRiverStroke.Count == 0 || Vector3.Distance(activeRiverStroke[activeRiverStroke.Count - 1], hitPoint) >= 1.0f)
            {
                float surfaceY = SampleHeightBilinear(hitPoint.x, hitPoint.z);

                // Water sits snugly right on top of local terrain (+8cm, never floating in air)
                float waterY = surfaceY + 0.08f;
                waterY = Mathf.Max(settings.waterLevel + 0.02f, waterY);

                if (activeRiverStroke.Count > 0)
                {
                    float prevY = activeRiverStroke[activeRiverStroke.Count - 1].y;
                    waterY = Mathf.Max(surfaceY + 0.04f, Mathf.Min(prevY - 0.005f, surfaceY + 0.12f));
                    waterY = Mathf.Max(settings.waterLevel + 0.02f, waterY);
                }

                activeRiverStroke.Add(new Vector3(hitPoint.x, waterY, hitPoint.z));

                if (waterController != null)
                {
                    waterController.UpdateDynamicRiverStroke(activeRiverStroke, settings);
                }
            }
        }

        public void FinalizeRiverStroke()
        {
            if (activeRiverStroke.Count >= 3)
            {
                // Carve a shallow, gentle channel indentation (max 0.25 units) along the stroke
                CarveShallowRiverStroke(activeRiverStroke, settings.riverWidth, 0.25f);

                // Re-conform river stroke surface snugly to the shallow carved bed
                List<Vector3> finalizedStroke = new List<Vector3>(activeRiverStroke.Count);
                float prevY = float.MaxValue;
                for (int i = 0; i < activeRiverStroke.Count; i++)
                {
                    Vector3 pt = activeRiverStroke[i];
                    float bedY = SampleHeightBilinear(pt.x, pt.z);
                    float waterY = bedY + 0.08f;
                    waterY = Mathf.Max(settings.waterLevel + 0.02f, waterY);
                    if (i > 0)
                    {
                        waterY = Mathf.Max(bedY + 0.04f, Mathf.Min(prevY - 0.005f, bedY + 0.12f));
                        waterY = Mathf.Max(settings.waterLevel + 0.02f, waterY);
                    }
                    prevY = waterY;
                    finalizedStroke.Add(new Vector3(pt.x, waterY, pt.z));
                }

                // Detect waterfalls ONLY on genuine cliff drops (> 50 to 60 degrees slope, drop >= 2.2 units)
                for (int i = 2; i < finalizedStroke.Count; i += 3)
                {
                    Vector3 pStart = finalizedStroke[i - 2];
                    Vector3 pEnd = finalizedStroke[i];
                    float drop = pStart.y - pEnd.y;
                    float dist = Vector2.Distance(new Vector2(pStart.x, pStart.z), new Vector2(pEnd.x, pEnd.z));
                    float slope = drop / Mathf.Max(0.1f, dist);

                    if (drop >= 2.2f && slope >= 1.25f)
                    {
                        Vector3 flow = (pEnd - pStart);
                        flow.y = 0;
                        activeWaterfalls.Add(new WaterfallPoint
                        {
                            startPosition = pStart,
                            endPosition = pEnd,
                            flowDirection = flow.sqrMagnitude > 0.01f ? flow.normalized : Vector3.forward,
                            width = settings.riverWidth * 0.85f,
                            dropHeight = drop
                        });
                    }
                }

                customRiverPaths.Add(finalizedStroke);

                if (waterController != null)
                {
                    waterController.ClearDynamicRiverStroke();
                    waterController.BuildWater(settings, activeWaterfalls, GetAllRiverPaths());
                }
            }
            else if (waterController != null)
            {
                waterController.ClearDynamicRiverStroke();
            }

            activeRiverStroke.Clear();
            UpdateTerrainMeshVisualsOnly();
            BakeCollisionMesh();
        }

        private void CarveShallowRiverStroke(List<Vector3> stroke, float riverWidth, float maxCarveDepth)
        {
            if (heightMap == null || stroke == null || stroke.Count < 2) return;

            int size = settings.mapSize;
            float worldSize = settings.worldSize;
            float halfWorld = worldSize * 0.5f;
            float cellSize = worldSize / (size - 1);
            float radius = riverWidth * 0.5f;

            for (int s = 0; s < stroke.Count - 1; s++)
            {
                Vector3 p0 = stroke[s];
                Vector3 p1 = stroke[s + 1];

                float minX = Mathf.Min(p0.x, p1.x) - radius;
                float maxX = Mathf.Max(p0.x, p1.x) + radius;
                float minZ = Mathf.Min(p0.z, p1.z) - radius;
                float maxZ = Mathf.Max(p0.z, p1.z) + radius;

                int startX = Mathf.Clamp(Mathf.FloorToInt(((minX + halfWorld) / worldSize) * (size - 1)), 1, size - 2);
                int endX = Mathf.Clamp(Mathf.CeilToInt(((maxX + halfWorld) / worldSize) * (size - 1)), 1, size - 2);
                int startY = Mathf.Clamp(Mathf.FloorToInt(((minZ + halfWorld) / worldSize) * (size - 1)), 1, size - 2);
                int endY = Mathf.Clamp(Mathf.CeilToInt(((maxZ + halfWorld) / worldSize) * (size - 1)), 1, size - 2);

                Vector2 segA = new Vector2(p0.x, p0.z);
                Vector2 segB = new Vector2(p1.x, p1.z);
                Vector2 segAB = segB - segA;
                float segLenSq = segAB.sqrMagnitude;

                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        float wx = x * cellSize - halfWorld;
                        float wz = y * cellSize - halfWorld;
                        Vector2 pt = new Vector2(wx, wz);

                        float tParam = segLenSq > 0.0001f ? Mathf.Clamp01(Vector2.Dot(pt - segA, segAB) / segLenSq) : 0f;
                        Vector2 closest = segA + segAB * tParam;
                        float dist = Vector2.Distance(pt, closest);

                        if (dist < radius)
                        {
                            float tNorm = 1.0f - (dist / radius);
                            float factor = Mathf.Sin(tNorm * Mathf.PI * 0.5f);
                            float carve = maxCarveDepth * factor;
                            heightMap[x, y] = Mathf.Max(settings.waterLevel - 0.25f, heightMap[x, y] - carve);
                        }
                    }
                }
            }
        }

        public void ClearCustomRivers()
        {
            customRiverPaths.Clear();
            activeRiverStroke.Clear();
            if (waterController != null)
            {
                waterController.ClearDynamicRiverStroke();
                waterController.BuildWater(settings, activeWaterfalls, GetAllRiverPaths());
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
