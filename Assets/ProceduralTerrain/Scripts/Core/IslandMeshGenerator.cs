using UnityEngine;

namespace ProceduralTerrain
{
    public class IslandMeshGenerator
    {
        private readonly TerrainGenerationSettings settings;
        private readonly float[,] heightMap;
        private readonly int mapSize;
        private readonly float worldSize;
        private readonly float cellSize;

        // Reusable buffers to prevent GC allocations
        private Vector3[] cachedVertices;
        private Vector3[] cachedNormals;
        private Vector2[] cachedUVs;
        private Color[] cachedColors;
        private int[] cachedTriangles;

        public IslandMeshGenerator(TerrainGenerationSettings settings, float[,] heightMap)
        {
            this.settings = settings;
            this.heightMap = heightMap;
            this.mapSize = settings.mapSize;
            this.worldSize = settings.worldSize;
            this.cellSize = worldSize / (mapSize - 1);
        }

        public Mesh GenerateOrUpdateMesh(Mesh targetMesh = null, bool forceFullClear = false)
        {
            if (targetMesh == null)
            {
                targetMesh = new Mesh
                {
                    name = "ProceduralIslandMesh",
                    hideFlags = HideFlags.DontSave
                };
                targetMesh.MarkDynamic();
                forceFullClear = true;
            }

            if (forceFullClear)
            {
                targetMesh.Clear();
            }

            if (settings.flatShaded)
            {
                GenerateFlatShadedMesh(targetMesh, forceFullClear);
            }
            else
            {
                GenerateSmoothMesh(targetMesh, forceFullClear);
            }

            targetMesh.RecalculateBounds();
            return targetMesh;
        }

        private void GenerateFlatShadedMesh(Mesh mesh, bool forceFullClear)
        {
            int quadsCount = (mapSize - 1) * (mapSize - 1);
            int vertexCount = quadsCount * 6;

            if (vertexCount > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            bool reallocate = (cachedVertices == null || cachedVertices.Length != vertexCount);
            if (reallocate)
            {
                cachedVertices = new Vector3[vertexCount];
                cachedNormals = new Vector3[vertexCount];
                cachedUVs = new Vector2[vertexCount];
                cachedColors = new Color[vertexCount];
                cachedTriangles = new int[vertexCount];
            }

            float halfWorld = worldSize * 0.5f;
            int vertIndex = 0;

            for (int y = 0; y < mapSize - 1; y++)
            {
                for (int x = 0; x < mapSize - 1; x++)
                {
                    Vector3 vBL = new Vector3(x * cellSize - halfWorld, heightMap[x, y], y * cellSize - halfWorld);
                    Vector3 vBR = new Vector3((x + 1) * cellSize - halfWorld, heightMap[x + 1, y], y * cellSize - halfWorld);
                    Vector3 vTL = new Vector3(x * cellSize - halfWorld, heightMap[x, y + 1], (y + 1) * cellSize - halfWorld);
                    Vector3 vTR = new Vector3((x + 1) * cellSize - halfWorld, heightMap[x + 1, y + 1], (y + 1) * cellSize - halfWorld);

                    Vector2 uvBL = new Vector2((float)x / (mapSize - 1), (float)y / (mapSize - 1));
                    Vector2 uvBR = new Vector2((float)(x + 1) / (mapSize - 1), (float)y / (mapSize - 1));
                    Vector2 uvTL = new Vector2((float)x / (mapSize - 1), (float)(y + 1) / (mapSize - 1));
                    Vector2 uvTR = new Vector2((float)(x + 1) / (mapSize - 1), (float)(y + 1) / (mapSize - 1));

                    bool setTopology = reallocate || forceFullClear;
                    Vector3 normal1 = Vector3.Cross(vTL - vBL, vBR - vBL).normalized;
                    AddTriangle(ref vertIndex, vBL, vTL, vBR, uvBL, uvTL, uvBR, normal1, setTopology);

                    Vector3 normal2 = Vector3.Cross(vTR - vTL, vBR - vTL).normalized;
                    AddTriangle(ref vertIndex, vTL, vTR, vBR, uvTL, uvTR, uvBR, normal2, setTopology);
                }
            }

            mesh.vertices = cachedVertices;
            mesh.normals = cachedNormals;
            mesh.colors = cachedColors;

            if (reallocate || forceFullClear)
            {
                mesh.uv = cachedUVs;
                mesh.triangles = cachedTriangles;
            }
        }

        private void AddTriangle(
            ref int vertIndex,
            Vector3 v0, Vector3 v1, Vector3 v2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2,
            Vector3 faceNormal,
            bool setTopology)
        {
            int i0 = vertIndex++;
            int i1 = vertIndex++;
            int i2 = vertIndex++;

            float floorY = settings.waterLevel - 3.5f;
            if (float.IsNaN(v0.y) || float.IsInfinity(v0.y)) v0.y = floorY;
            if (float.IsNaN(v1.y) || float.IsInfinity(v1.y)) v1.y = floorY;
            if (float.IsNaN(v2.y) || float.IsInfinity(v2.y)) v2.y = floorY;

            cachedVertices[i0] = v0;
            cachedVertices[i1] = v1;
            cachedVertices[i2] = v2;

            cachedNormals[i0] = faceNormal;
            cachedNormals[i1] = faceNormal;
            cachedNormals[i2] = faceNormal;

            cachedColors[i0] = CalculateVertexColor(v0, faceNormal);
            cachedColors[i1] = CalculateVertexColor(v1, faceNormal);
            cachedColors[i2] = CalculateVertexColor(v2, faceNormal);

            if (setTopology)
            {
                cachedUVs[i0] = uv0;
                cachedUVs[i1] = uv1;
                cachedUVs[i2] = uv2;

                cachedTriangles[i0] = i0;
                cachedTriangles[i1] = i1;
                cachedTriangles[i2] = i2;
            }
        }

        private void GenerateSmoothMesh(Mesh mesh, bool forceFullClear)
        {
            int totalVerts = mapSize * mapSize;
            if (totalVerts > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            bool reallocate = (cachedVertices == null || cachedVertices.Length != totalVerts);
            if (reallocate)
            {
                cachedVertices = new Vector3[totalVerts];
                cachedUVs = new Vector2[totalVerts];
                cachedColors = new Color[totalVerts];
                cachedTriangles = new int[(mapSize - 1) * (mapSize - 1) * 6];
            }

            float halfWorld = worldSize * 0.5f;

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int index = y * mapSize + x;
                    float wx = x * cellSize - halfWorld;
                    float wz = y * cellSize - halfWorld;
                    float wy = heightMap[x, y];

                    cachedVertices[index] = new Vector3(wx, wy, wz);
                    if (reallocate || forceFullClear)
                    {
                        cachedUVs[index] = new Vector2((float)x / (mapSize - 1), (float)y / (mapSize - 1));
                    }
                }
            }

            if (reallocate || forceFullClear)
            {
                int triIndex = 0;
                for (int y = 0; y < mapSize - 1; y++)
                {
                    for (int x = 0; x < mapSize - 1; x++)
                    {
                        int bl = y * mapSize + x;
                        int br = bl + 1;
                        int tl = (y + 1) * mapSize + x;
                        int tr = tl + 1;

                        cachedTriangles[triIndex++] = bl;
                        cachedTriangles[triIndex++] = tl;
                        cachedTriangles[triIndex++] = br;

                        cachedTriangles[triIndex++] = tl;
                        cachedTriangles[triIndex++] = tr;
                        cachedTriangles[triIndex++] = br;
                    }
                }
                mesh.triangles = cachedTriangles;
                mesh.uv = cachedUVs;
            }

            mesh.vertices = cachedVertices;
            mesh.RecalculateNormals();

            Vector3[] normals = mesh.normals;
            for (int i = 0; i < totalVerts; i++)
            {
                cachedColors[i] = CalculateVertexColor(cachedVertices[i], normals[i]);
            }
            mesh.colors = cachedColors;
        }

        public Color CalculateVertexColor(Vector3 pos, Vector3 normal)
        {
            Color deepWaterSand = new Color(0.76f, 0.69f, 0.52f);
            Color beachSand = new Color(0.92f, 0.85f, 0.66f);
            Color lushGrass = new Color(0.38f, 0.76f, 0.28f);
            Color meadowGrass = new Color(0.28f, 0.68f, 0.24f);
            Color rockCliff = new Color(0.55f, 0.52f, 0.48f);
            Color darkRock = new Color(0.42f, 0.39f, 0.37f);
            Color snowCap = new Color(0.94f, 0.96f, 1.0f);

            float height = pos.y;
            float slope = 1.0f - Mathf.Clamp01(normal.y);

            if (height <= settings.beachHeight)
            {
                float t = Mathf.InverseLerp(settings.waterLevel - 2.0f, settings.beachHeight, height);
                Color sandCol = Color.Lerp(deepWaterSand, beachSand, t);

                if (height > settings.waterLevel)
                {
                    float grassTransition = Mathf.InverseLerp(settings.waterLevel, settings.beachHeight, height);
                    return Color.Lerp(sandCol, lushGrass, grassTransition * 0.7f);
                }
                return sandCol;
            }

            float snowStart = settings.maxHeight * 0.72f;
            if (height > snowStart && slope < 0.6f)
            {
                float snowT = Mathf.InverseLerp(snowStart, settings.maxHeight * 0.88f, height);
                Color baseC = (slope > 0.35f) ? rockCliff : meadowGrass;
                return Color.Lerp(baseC, snowCap, snowT);
            }

            if (slope > 0.38f)
            {
                float rockT = Mathf.InverseLerp(0.38f, 0.65f, slope);
                Color grass = (height > settings.maxHeight * 0.4f) ? meadowGrass : lushGrass;
                Color stone = (height > settings.maxHeight * 0.6f) ? darkRock : rockCliff;
                return Color.Lerp(grass, stone, rockT);
            }

            float midT = Mathf.InverseLerp(settings.beachHeight, settings.maxHeight * 0.6f, height);
            return Color.Lerp(lushGrass, meadowGrass, midT);
        }
    }
}
