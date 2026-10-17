using System;
using UnityEngine;

namespace ProceduralTerrain
{
    [Serializable]
    public class TerrainGenerationSettings
    {
        [Header("Map Dimensions")]
        [Tooltip("Grid resolution (number of vertices per side)")]
        public int mapSize = 96;

        [Tooltip("World size in Unity units")]
        public float worldSize = 120f;

        [Header("Seed & Elevation")]
        public int seed = 42;
        public float baseElevation = 5.8f;
        public float baseHeight = 5.8f;
        public float maxHeight = 28f;

        [Header("Island Shape & Masking")]
        [Range(0.2f, 0.95f)]
        public float islandRadius = 0.74f;
        [Range(0.01f, 0.5f)]
        public float islandFalloffSharpness = 0.28f;
        public float coastlineWarpStrength = 15f;

        [Header("Noise Layers")]
        public float mainNoiseFrequency = 0.015f;
        [Range(1, 6)]
        public int mainNoiseOctaves = 4;
        public float mountainNoiseFrequency = 0.035f;
        public float mountainHeightMultiplier = 1.6f;
        public float detailNoiseFrequency = 0.08f;
        public float detailNoiseStrength = 1.2f;

        [Header("Water & Shorelines")]
        public float waterLevel = 4.0f;
        public float beachHeight = 5.2f;

        [Header("Rivers & Lakes")]
        public bool generateRivers = true;
        [Range(1, 8)]
        public int riverCount = 4;
        [Range(0.1f, 2.0f)]
        public float riverDepth = 0.8f;
        [Range(1.5f, 6.0f)]
        public float riverWidth = 2.8f;
        public float minWaterfallHeight = 2.2f;

        [Header("Mesh Styling")]
        [Tooltip("True for flat-shaded low-poly faceted look; False for smooth fairytale shading")]
        public bool flatShaded = true;
    }

    public enum TerrainBrushMode
    {
        RaiseMountain,
        LowerRiver,
        FlattenPlateau,
        SmoothTerrain,
        PaintFoliage,
        EraseFoliage
    }

    [Serializable]
    public struct WaterfallPoint
    {
        public Vector3 startPosition;
        public Vector3 endPosition;
        public Vector3 flowDirection;
        public float width;
        public float dropHeight;
    }

    [Serializable]
    public struct RiverNode
    {
        public Vector3 position;
        public float width;
        public bool isWaterfall;
    }

    public enum FoliageType
    {
        PineTree,
        DeciduousTree,
        BlossomTree,
        Bush,
        FlowerCluster,
        RiverRock
    }
}
