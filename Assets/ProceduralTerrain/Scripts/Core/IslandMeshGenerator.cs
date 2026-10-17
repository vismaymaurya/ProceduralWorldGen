using System.Collections.Generic;
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

        public IslandMeshGenerator(TerrainGenerationSettings settings, float[,] heightMap)
        {
            this.settings = settings;
            this.heightMap = heightMap;
            this.mapSize = settings.mapSize;
            this.worldSize = settings.worldSize;
            this.cellSize = worldSize / (mapSize - 1);
        }

        public Mesh GenerateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "ProceduralIslandMesh";

            if (settings.flatShaded)
            {
                GenerateFlatShadedMesh(mesh);
            }
            else
            {
                GenerateSmoothMesh(mesh);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private void GenerateFlatShadedMesh(Mesh mesh)
        {
            int quadsCount = (mapSize - 1) * (mapSize - 1);
            int vertexCount = quadsCount * 6;

            // If vertex count exceeds 65535, use 32-bit index format
            if (vertexCount > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Color[] colors = new Color[vertexCount];
            int[] triangles = new int[vertexCount];

            float halfWorld = worldSize * 0.5f;
            int vertIndex = 0;

            for (int y = 0; y < mapSize - 1; y++)
            {
                for (int x = 0; x < mapSize - 1; x++)
                {
                    // 4 corners of current quad
                    Vector3 vBL = new Vector3(x * cellSize - halfWorld, heightMap[x, y], y * cellSize - halfWorld);
                    Vector3 vBR = new Vector3((x + 1) * cellSize - halfWorld, heightMap[x + 1, y], y * cellSize - halfWorld);
                    Vector3 vTL = new Vector3(x * cellSize - halfWorld, heightMap[x, y + 1], (y + 1) * cellSize - halfWorld);
                    Vector3 vTR = new Vector3((x + 1) * cellSize - halfWorld, heightMap[x + 1, y + 1], (y + 1) * cellSize - halfWorld);

                    Vector2 uvBL = new Vector2((float)x / (mapSize - 1), (float)y / (mapSize - 1));
                    Vector2 uvBR = new Vector2((float)(x + 1) / (mapSize - 1), (float)y / (mapSize - 1));
                    Vector2 uvTL = new Vector2((float)x / (mapSize - 1), (float)(y + 1) / (mapSize - 1));
                    Vector2 uvTR = new Vector2((float)(x + 1) / (mapSize - 1), (float)(y + 1) / (mapSize - 1));

                    // Triangle 1: BL -> TL -> BR
                    Vector3 normal1 = Vector3.Cross(vTL - vBL, vBR - vBL).normalized;
                    AddTriangle(vertices, normals, uvs, colors, triangles, ref vertIndex,
                        vBL, vTL, vBR,
                        uvBL, uvTL, uvBR,
                        normal1);

                    // Triangle 2: TL -> TR -> BR
                    Vector3 normal2 = Vector3.Cross(vTR - vTL, vBR - vTL).normalized;
                    AddTriangle(vertices, normals, uvs, colors, triangles, ref vertIndex,
                        vTL, vTR, vBR,
                        uvTL, uvTR, uvBR,
                        normal2);
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
        }

        private void AddTriangle(
            Vector3[] vertices, Vector3[] normals, Vector2[] uvs, Color[] colors, int[] triangles,
            ref int vertIndex,
            Vector3 v0, Vector3 v1, Vector3 v2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2,
            Vector3 faceNormal)
        {
            int i0 = vertIndex++;
            int i1 = vertIndex++;
            int i2 = vertIndex++;

            vertices[i0] = v0;
            vertices[i1] = v1;
            vertices[i2] = v2;

            normals[i0] = faceNormal;
            normals[i1] = faceNormal;
            normals[i2] = faceNormal;

            uvs[i0] = uv0;
            uvs[i1] = uv1;
            uvs[i2] = uv2;

            colors[i0] = CalculateVertexColor(v0, faceNormal);
            colors[i1] = CalculateVertexColor(v1, faceNormal);
            colors[i2] = CalculateVertexColor(v2, faceNormal);

            triangles[i0] = i0;
            triangles[i1] = i1;
            triangles[i2] = i2;
        }

        private void GenerateSmoothMesh(Mesh mesh)
        {
            int totalVerts = mapSize * mapSize;
            if (totalVerts > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            Vector3[] vertices = new Vector3[totalVerts];
            Vector2[] uvs = new Vector2[totalVerts];
            Color[] colors = new Color[totalVerts];
            int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];

            float halfWorld = worldSize * 0.5f;

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int index = y * mapSize + x;
                    float wx = x * cellSize - halfWorld;
                    float wz = y * cellSize - halfWorld;
                    float wy = heightMap[x, y];

                    vertices[index] = new Vector3(wx, wy, wz);
                    uvs[index] = new Vector2((float)x / (mapSize - 1), (float)y / (mapSize - 1));
                }
            }

            int triIndex = 0;
            for (int y = 0; y < mapSize - 1; y++)
            {
                for (int x = 0; x < mapSize - 1; x++)
                {
                    int bl = y * mapSize + x;
                    int br = bl + 1;
                    int tl = (y + 1) * mapSize + x;
                    int tr = tl + 1;

                    triangles[triIndex++] = bl;
                    triangles[triIndex++] = tl;
                    triangles[triIndex++] = br;

                    triangles[triIndex++] = tl;
                    triangles[triIndex++] = tr;
                    triangles[triIndex++] = br;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            Vector3[] normals = mesh.normals;
            for (int i = 0; i < totalVerts; i++)
            {
                colors[i] = CalculateVertexColor(vertices[i], normals[i]);
            }
            mesh.colors = colors;
        }

        public Color CalculateVertexColor(Vector3 pos, Vector3 normal)
        {
            // Fairytale Color Palette:
            Color deepWaterSand = new Color(0.76f, 0.69f, 0.52f); // Golden sand
            Color beachSand = new Color(0.92f, 0.85f, 0.66f);     // Warm shoreline
            Color lushGrass = new Color(0.38f, 0.76f, 0.28f);     // Fairytale vibrant green
            Color meadowGrass = new Color(0.28f, 0.68f, 0.24f);   // Meadow tone
            Color rockCliff = new Color(0.55f, 0.52f, 0.48f);     // Warm stone
            Color darkRock = new Color(0.42f, 0.39f, 0.37f);      // Shadowed rock
            Color snowCap = new Color(0.94f, 0.96f, 1.0f);        // Alpine snow

            float height = pos.y;
            float slope = 1.0f - Mathf.Clamp01(normal.y); // 0 = flat ground, 1 = vertical cliff

            // Sand / Beach transition
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

            // Mountain Snow Cap
            float snowStart = settings.maxHeight * 0.72f;
            if (height > snowStart && slope < 0.6f)
            {
                float snowT = Mathf.InverseLerp(snowStart, settings.maxHeight * 0.88f, height);
                Color baseC = (slope > 0.35f) ? rockCliff : meadowGrass;
                return Color.Lerp(baseC, snowCap, snowT);
            }

            // Steep slope -> Rock cliff face
            if (slope > 0.38f)
            {
                float rockT = Mathf.InverseLerp(0.38f, 0.65f, slope);
                Color grass = (height > settings.maxHeight * 0.4f) ? meadowGrass : lushGrass;
                Color stone = (height > settings.maxHeight * 0.6f) ? darkRock : rockCliff;
                return Color.Lerp(grass, stone, rockT);
            }

            // Lowland to midland meadow with organic height tint
            float midT = Mathf.InverseLerp(settings.beachHeight, settings.maxHeight * 0.6f, height);
            return Color.Lerp(lushGrass, meadowGrass, midT);
        }
    }
}
