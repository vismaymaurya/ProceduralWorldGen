using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class RiverGenerator
    {
        private readonly TerrainGenerationSettings settings;
        private readonly float[,] heightMap;
        private readonly int mapSize;
        private readonly float worldSize;
        private readonly float cellSize;

        public RiverGenerator(TerrainGenerationSettings settings, float[,] heightMap)
        {
            this.settings = settings;
            this.heightMap = heightMap;
            this.mapSize = settings.mapSize;
            this.worldSize = settings.worldSize;
            this.cellSize = worldSize / (mapSize - 1);
        }

        public List<WaterfallPoint> GenerateRivers(out List<List<Vector3>> riverPaths)
        {
            riverPaths = new List<List<Vector3>>();
            List<WaterfallPoint> waterfalls = new List<WaterfallPoint>();

            if (!settings.generateRivers || settings.riverCount <= 0)
                return waterfalls;

            System.Random prng = new System.Random(settings.seed + 999);

            // 1. Find candidates for river sources on upper mountain ridges
            List<Vector2Int> candidates = new List<Vector2Int>();
            float[] heightTiers = new float[] { 0.45f, 0.32f, 0.22f, 0.14f };
            int margin = Mathf.Max(4, (int)(mapSize * 0.15f));

            foreach (float tier in heightTiers)
            {
                float minSourceHeight = settings.waterLevel + (settings.maxHeight - settings.waterLevel) * tier;
                candidates.Clear();

                for (int x = margin; x < mapSize - margin; x += 2)
                {
                    for (int y = margin; y < mapSize - margin; y += 2)
                    {
                        if (heightMap[x, y] >= minSourceHeight)
                        {
                            candidates.Add(new Vector2Int(x, y));
                        }
                    }
                }

                if (candidates.Count >= settings.riverCount * 2)
                    break;
            }

            if (candidates.Count == 0)
                return waterfalls;

            // 2. Pick well-spaced river sources
            int riverCount = Mathf.Min(settings.riverCount, candidates.Count);
            List<Vector2Int> sources = new List<Vector2Int>();

            for (int r = 0; r < riverCount; r++)
            {
                int attempts = 30;
                Vector2Int bestSource = candidates[prng.Next(candidates.Count)];
                float maxMinDist = 0;

                for (int a = 0; a < attempts; a++)
                {
                    Vector2Int sample = candidates[prng.Next(candidates.Count)];
                    float minDist = float.MaxValue;
                    foreach (var s in sources)
                    {
                        float d = Vector2Int.Distance(sample, s);
                        if (d < minDist) minDist = d;
                    }
                    if (sources.Count == 0 || minDist > maxMinDist)
                    {
                        maxMinDist = minDist;
                        bestSource = sample;
                    }
                }

                sources.Add(bestSource);
            }

            // 3. Trace each downhill path from mountain ridge to ocean
            List<List<Vector3>> rawPaths = new List<List<Vector3>>();
            foreach (var src in sources)
            {
                List<Vector3> rawPath = TraceRiverPath(src);
                if (rawPath.Count > 5)
                {
                    rawPaths.Add(rawPath);
                }
            }

            // 4. Carve smooth, uniform riverbeds into terrain without duplicate craters
            CarveRiverBeds(rawPaths);

            // 5. Detect genuine cliff waterfalls & conform water surface snugly to carved bed
            foreach (var path in rawPaths)
            {
                List<Vector3> waterSurfacePath = new List<Vector3>(path.Count);
                float prevWaterY = float.MaxValue;

                for (int i = 0; i < path.Count; i++)
                {
                    float wx = path[i].x;
                    float wz = path[i].z;

                    // Sample exact carved bed height with bilinear interpolation
                    float bedY = SampleHeightBilinear(wx, wz);

                    // Water surface sits snugly right above the carved channel (8cm, never floating in midair)
                    float waterY = bedY + 0.08f;
                    waterY = Mathf.Max(settings.waterLevel + 0.02f, waterY);

                    if (i > 0)
                    {
                        // Ensure downhill flow while staying tightly bounded to local terrain bed
                        waterY = Mathf.Max(bedY + 0.04f, Mathf.Min(prevWaterY - 0.005f, bedY + 0.12f));
                        waterY = Mathf.Max(settings.waterLevel + 0.02f, waterY);
                    }
                    prevWaterY = waterY;

                    waterSurfacePath.Add(new Vector3(wx, waterY, wz));
                }

                riverPaths.Add(waterSurfacePath);

                // Detect dramatic waterfalls on steep cliffs (> 50-60 degrees slope, drop >= 2.2 units)
                DetectCliffWaterfalls(waterSurfacePath, waterfalls, settings);
            }

            return waterfalls;
        }

        private List<Vector3> TraceRiverPath(Vector2Int startCoord)
        {
            List<Vector3> path = new List<Vector3>();
            Vector2 currentGridPos = new Vector2(startCoord.x, startCoord.y);

            int maxSteps = mapSize * 2;
            int step = 0;
            float stepSize = 0.9f;

            Vector3 startWorldPos = GridToWorld(currentGridPos);
            path.Add(startWorldPos);

            while (step < maxSteps)
            {
                int gx = Mathf.Clamp(Mathf.RoundToInt(currentGridPos.x), 0, mapSize - 1);
                int gy = Mathf.Clamp(Mathf.RoundToInt(currentGridPos.y), 0, mapSize - 1);

                float currentHeight = heightMap[gx, gy];

                // Terminate when ocean water level is reached
                if (currentHeight <= settings.waterLevel + 0.2f)
                    break;

                // Search 8 directions for steepest descent
                Vector2 bestDir = Vector2.zero;
                float steepestSlope = 0.001f;

                for (int angle = 0; angle < 8; angle++)
                {
                    float rad = angle * Mathf.PI / 4f;
                    Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                    Vector2 nextPos = currentGridPos + dir * stepSize;

                    int nx = Mathf.Clamp(Mathf.RoundToInt(nextPos.x), 0, mapSize - 1);
                    int ny = Mathf.Clamp(Mathf.RoundToInt(nextPos.y), 0, mapSize - 1);

                    if (nx <= 1 || nx >= mapSize - 2 || ny <= 1 || ny >= mapSize - 2)
                        continue;

                    float nextH = heightMap[nx, ny];
                    float slopeDrop = currentHeight - nextH;

                    if (slopeDrop > steepestSlope)
                    {
                        steepestSlope = slopeDrop;
                        bestDir = dir;
                    }
                }

                if (bestDir == Vector2.zero)
                {
                    // Flat saddle or local depression: push outward toward island edge
                    Vector2 toEdge = (currentGridPos - new Vector2(mapSize * 0.5f, mapSize * 0.5f)).normalized;
                    bestDir = toEdge;
                }

                currentGridPos += bestDir * stepSize;
                Vector3 currentWorldPos = GridToWorld(currentGridPos);
                path.Add(currentWorldPos);

                step++;
            }

            return path;
        }

        private void CarveRiverBeds(List<List<Vector3>> paths)
        {
            if (paths == null || paths.Count == 0) return;

            float halfWorld = worldSize * 0.5f;
            float radius = settings.riverWidth * 0.75f;

            for (int y = 1; y < mapSize - 1; y++)
            {
                for (int x = 1; x < mapSize - 1; x++)
                {
                    float wx = (x / (float)(mapSize - 1)) * worldSize - halfWorld;
                    float wz = (y / (float)(mapSize - 1)) * worldSize - halfWorld;
                    Vector2 pt = new Vector2(wx, wz);

                    float minDistance = float.MaxValue;

                    for (int p = 0; p < paths.Count; p++)
                    {
                        List<Vector3> path = paths[p];
                        for (int i = 0; i < path.Count - 1; i++)
                        {
                            Vector2 a = new Vector2(path[i].x, path[i].z);
                            Vector2 b = new Vector2(path[i + 1].x, path[i + 1].z);
                            float d = DistancePointToSegment(pt, a, b);
                            if (d < minDistance) minDistance = d;
                        }
                    }

                    if (minDistance < radius)
                    {
                        float t = 1.0f - (minDistance / radius);
                        // Gentle shallow U-shaped river channel profile (not digging an abyss)
                        float factor = Mathf.Sin(t * Mathf.PI * 0.5f);
                        float carveAmount = settings.riverDepth * factor * 0.35f;
                        heightMap[x, y] = Mathf.Max(settings.waterLevel - 0.25f, heightMap[x, y] - carveAmount);
                    }
                }
            }
        }

        public static void DetectCliffWaterfalls(List<Vector3> path, List<WaterfallPoint> waterfalls, TerrainGenerationSettings settings)
        {
            if (path == null || path.Count < 4 || settings == null) return;

            int waterfallsFound = 0;
            int maxWaterfallsPerRiver = 2; // Only genuine dramatic drops, not everywhere

            for (int i = 2; i < path.Count - 1 && waterfallsFound < maxWaterfallsPerRiver; i++)
            {
                Vector3 pStart = path[i - 2];
                Vector3 pEnd = path[i];

                float heightDrop = pStart.y - pEnd.y;
                float hDist = Vector2.Distance(new Vector2(pStart.x, pStart.z), new Vector2(pEnd.x, pEnd.z));
                float slope = heightDrop / Mathf.Max(0.1f, hDist);

                // User specification: Waterfall only if slope is steeper than 50 to 60 degrees (slope >= 1.25f, Tan(51.3°))
                if (heightDrop >= 2.2f && slope >= 1.25f)
                {
                    Vector3 flow = (pEnd - pStart);
                    flow.y = 0;

                    waterfalls.Add(new WaterfallPoint
                    {
                        startPosition = pStart,
                        endPosition = pEnd,
                        flowDirection = flow.sqrMagnitude > 0.01f ? flow.normalized : Vector3.forward,
                        width = settings.riverWidth * 0.85f,
                        dropHeight = heightDrop
                    });

                    waterfallsFound++;
                    i += 4; // Skip ahead so we don't spawn duplicate waterfalls on the same cliff
                }
            }

            // Check for sea-cliff waterfall plunging into ocean
            if (waterfallsFound < maxWaterfallsPerRiver && path.Count >= 2)
            {
                Vector3 lastPt = path[path.Count - 1];
                Vector3 secondLastPt = path[path.Count - 2];

                if (secondLastPt.y > settings.waterLevel + 1.8f)
                {
                    Vector3 flow = (lastPt - secondLastPt);
                    flow.y = 0;

                    waterfalls.Add(new WaterfallPoint
                    {
                        startPosition = secondLastPt,
                        endPosition = new Vector3(lastPt.x, settings.waterLevel + 0.05f, lastPt.z),
                        flowDirection = flow.sqrMagnitude > 0.01f ? flow.normalized : Vector3.forward,
                        width = settings.riverWidth * 1.0f,
                        dropHeight = secondLastPt.y - settings.waterLevel
                    });
                }
            }
        }

        public float SampleHeightBilinear(float wx, float wz)
        {
            float halfWorld = worldSize * 0.5f;
            float gx = ((wx + halfWorld) / worldSize) * (mapSize - 1);
            float gy = ((wz + halfWorld) / worldSize) * (mapSize - 1);

            int x0 = Mathf.Clamp(Mathf.FloorToInt(gx), 0, mapSize - 1);
            int x1 = Mathf.Clamp(x0 + 1, 0, mapSize - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(gy), 0, mapSize - 1);
            int y1 = Mathf.Clamp(y0 + 1, 0, mapSize - 1);

            float tx = gx - x0;
            float ty = gy - y0;

            float h00 = heightMap[x0, y0];
            float h10 = heightMap[x1, y0];
            float h01 = heightMap[x0, y1];
            float h11 = heightMap[x1, y1];

            return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), ty);
        }

        private float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f) return Vector2.Distance(p, a);

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            Vector2 projection = a + ab * t;
            return Vector2.Distance(p, projection);
        }

        private Vector3 GridToWorld(Vector2 gridPos)
        {
            float halfWorld = worldSize * 0.5f;
            float wx = (gridPos.x / (mapSize - 1)) * worldSize - halfWorld;
            float wz = (gridPos.y / (mapSize - 1)) * worldSize - halfWorld;

            int gx = Mathf.Clamp(Mathf.RoundToInt(gridPos.x), 0, mapSize - 1);
            int gy = Mathf.Clamp(Mathf.RoundToInt(gridPos.y), 0, mapSize - 1);
            float wy = heightMap[gx, gy];

            return new Vector3(wx, wy, wz);
        }
    }
}
