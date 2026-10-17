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

            // Find candidates for river sources: elevated terrain above mid-height
            List<Vector2Int> candidates = new List<Vector2Int>();
            float minSourceHeight = settings.waterLevel + (settings.maxHeight - settings.waterLevel) * 0.45f;

            int margin = (int)(mapSize * 0.15f);
            for (int x = margin; x < mapSize - margin; x += 3)
            {
                for (int y = margin; y < mapSize - margin; y += 3)
                {
                    if (heightMap[x, y] >= minSourceHeight)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (candidates.Count == 0)
                return waterfalls;

            // Pick well-spaced river sources
            int riverCount = Mathf.Min(settings.riverCount, candidates.Count);
            List<Vector2Int> sources = new List<Vector2Int>();

            for (int r = 0; r < riverCount; r++)
            {
                int attempts = 20;
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

            // Trace each river downhill to waterLevel
            foreach (var src in sources)
            {
                List<Vector3> path = TraceRiverPath(src, waterfalls);
                if (path.Count > 4)
                {
                    riverPaths.Add(path);
                    CarveRiverBed(path);
                }
            }

            return waterfalls;
        }

        private List<Vector3> TraceRiverPath(Vector2Int startCoord, List<WaterfallPoint> waterfalls)
        {
            List<Vector3> path = new List<Vector3>();
            Vector2 currentGridPos = new Vector2(startCoord.x, startCoord.y);

            int maxSteps = mapSize * 2;
            int step = 0;
            float stepSize = 0.8f; // Grid cells per step

            Vector3 prevWorldPos = GridToWorld(currentGridPos);
            path.Add(prevWorldPos);

            HashSet<int> visited = new HashSet<int>();

            while (step < maxSteps)
            {
                int gx = Mathf.Clamp(Mathf.RoundToInt(currentGridPos.x), 0, mapSize - 1);
                int gy = Mathf.Clamp(Mathf.RoundToInt(currentGridPos.y), 0, mapSize - 1);

                float currentHeight = heightMap[gx, gy];

                // If reached ocean water level, terminate
                if (currentHeight <= settings.waterLevel + 0.15f)
                    break;

                int key = gx * mapSize + gy;
                visited.Add(key);

                // Find direction of steepest descent
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
                    // Local depression / flat area - push slightly toward island center-to-edge direction
                    Vector2 toEdge = (currentGridPos - new Vector2(mapSize * 0.5f, mapSize * 0.5f)).normalized;
                    bestDir = toEdge;
                }

                currentGridPos += bestDir * stepSize;
                int nGx = Mathf.Clamp(Mathf.RoundToInt(currentGridPos.x), 0, mapSize - 1);
                int nGy = Mathf.Clamp(Mathf.RoundToInt(currentGridPos.y), 0, mapSize - 1);
                float nextHeight = heightMap[nGx, nGy];

                Vector3 currentWorldPos = GridToWorld(currentGridPos);
                path.Add(currentWorldPos);

                // Waterfall detection: cliff drop
                float heightDrop = prevWorldPos.y - currentWorldPos.y;
                float horizontalDist = Vector2.Distance(
                    new Vector2(prevWorldPos.x, prevWorldPos.z),
                    new Vector2(currentWorldPos.x, currentWorldPos.z)
                );

                if (heightDrop >= settings.minWaterfallHeight && horizontalDist < 4.0f)
                {
                    Vector3 flow = (currentWorldPos - prevWorldPos);
                    flow.y = 0;
                    if (flow.sqrMagnitude > 0.001f)
                    {
                        waterfalls.Add(new WaterfallPoint
                        {
                            startPosition = prevWorldPos,
                            endPosition = currentWorldPos,
                            flowDirection = flow.normalized,
                            width = settings.riverWidth * 0.9f,
                            dropHeight = heightDrop
                        });
                    }
                }

                prevWorldPos = currentWorldPos;
                step++;
            }

            return path;
        }

        private void CarveRiverBed(List<Vector3> path)
        {
            float radiusCells = (settings.riverWidth * 0.5f) / cellSize;
            int rCeil = Mathf.CeilToInt(radiusCells) + 2;

            foreach (var wp in path)
            {
                Vector2 g = WorldToGrid(wp);
                int cx = Mathf.RoundToInt(g.x);
                int cy = Mathf.RoundToInt(g.y);

                for (int dx = -rCeil; dx <= rCeil; dx++)
                {
                    for (int dy = -rCeil; dy <= rCeil; dy++)
                    {
                        int x = cx + dx;
                        int y = cy + dy;

                        if (x < 1 || x >= mapSize - 1 || y < 1 || y >= mapSize - 1)
                            continue;

                        float dist = Vector2.Distance(new Vector2(x, y), g) * cellSize;
                        if (dist < settings.riverWidth * 0.8f)
                        {
                            // Smooth cosine falloff
                            float factor = Mathf.Cos((dist / (settings.riverWidth * 0.8f)) * Mathf.PI * 0.5f);
                            float carveAmount = settings.riverDepth * factor;

                            // Lower height towards river water level
                            float targetH = Mathf.Max(settings.waterLevel - 0.5f, heightMap[x, y] - carveAmount);
                            heightMap[x, y] = targetH;
                        }
                    }
                }
            }
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

        private Vector2 WorldToGrid(Vector3 worldPos)
        {
            float halfWorld = worldSize * 0.5f;
            float gx = ((worldPos.x + halfWorld) / worldSize) * (mapSize - 1);
            float gy = ((worldPos.z + halfWorld) / worldSize) * (mapSize - 1);
            return new Vector2(gx, gy);
        }
    }
}
