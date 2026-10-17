// FastNoiseLite - C# version
// MIT License
// Generates OpenSimplex2, Perlin, Cellular, and Fractal noise with domain warping.

using System;
using System.Runtime.CompilerServices;

namespace ProceduralTerrain
{
    public class FastNoiseLite
    {
        public enum NoiseType { OpenSimplex2, OpenSimplex2S, Cellular, Perlin, ValueCubic, Value }
        public enum RotationType3D { None, ImproveXYPlanes, ImproveXZPlanes }
        public enum FractalType { None, FBm, Ridged, PingPong, DomainWarpProgressive, DomainWarpIndependent }
        public enum CellularDistanceFunction { Euclidean, EuclideanSq, Manhattan, Hybrid }
        public enum CellularReturnType { CellValue, Distance, Distance2, Distance2Add, Distance2Sub, Distance2Mul, Distance2Div }
        public enum DomainWarpType { OpenSimplex2, OpenSimplex2Reduced, BasicGrid }

        private int mSeed = 1337;
        private float mFrequency = 0.01f;
        private NoiseType mNoiseType = NoiseType.OpenSimplex2;
        private RotationType3D mRotationType3D = RotationType3D.None;

        private FractalType mFractalType = FractalType.None;
        private int mOctaves = 3;
        private float mLacunarity = 2.0f;
        private float mGain = 0.5f;
        private float mWeightedStrength = 0.0f;
        private float mPingPongStrength = 2.0f;

        private CellularDistanceFunction mCellularDistanceFunction = CellularDistanceFunction.EuclideanSq;
        private CellularReturnType mCellularReturnType = CellularReturnType.Distance;
        private float mCellularJitterModifier = 1.0f;

        private DomainWarpType mDomainWarpType = DomainWarpType.OpenSimplex2;
        private float mDomainWarpAmp = 1.0f;

        public FastNoiseLite(int seed = 1337)
        {
            mSeed = seed;
        }

        public void SetSeed(int seed) => mSeed = seed;
        public void SetFrequency(float frequency) => mFrequency = frequency;
        public void SetNoiseType(NoiseType noiseType) => mNoiseType = noiseType;
        public void SetRotationType3D(RotationType3D rotationType3D) => mRotationType3D = rotationType3D;
        public void SetFractalType(FractalType fractalType) => mFractalType = fractalType;
        public void SetFractalOctaves(int octaves) => mOctaves = octaves;
        public void SetFractalLacunarity(float lacunarity) => mLacunarity = lacunarity;
        public void SetFractalGain(float gain) => mGain = gain;
        public void SetFractalWeightedStrength(float weightedStrength) => mWeightedStrength = weightedStrength;
        public void SetFractalPingPongStrength(float pingPongStrength) => mPingPongStrength = pingPongStrength;
        public void SetCellularDistanceFunction(CellularDistanceFunction cellularDistanceFunction) => mCellularDistanceFunction = cellularDistanceFunction;
        public void SetCellularReturnType(CellularReturnType cellularReturnType) => mCellularReturnType = cellularReturnType;
        public void SetCellularJitter(float cellularJitter) => mCellularJitterModifier = cellularJitter;
        public void SetDomainWarpType(DomainWarpType domainWarpType) => mDomainWarpType = domainWarpType;
        public void SetDomainWarpAmp(float domainWarpAmp) => mDomainWarpAmp = domainWarpAmp;

        public float GetNoise(float x, float y)
        {
            x *= mFrequency;
            y *= mFrequency;

            switch (mFractalType)
            {
                default:
                    return GenNoiseSingle(mSeed, x, y);
                case FractalType.FBm:
                    return GenFractalFBm(x, y);
                case FractalType.Ridged:
                    return GenFractalRidged(x, y);
                case FractalType.PingPong:
                    return GenFractalPingPong(x, y);
            }
        }

        private float GenFractalFBm(float x, float y)
        {
            int seed = mSeed;
            float sum = 0;
            float amp = 1;
            float max = 0;

            for (int i = 0; i < mOctaves; i++)
            {
                float noise = GenNoiseSingle(seed++, x, y);
                sum += noise * amp;
                max += amp;
                amp *= mGain;

                x *= mLacunarity;
                y *= mLacunarity;
            }

            return sum / max;
        }

        private float GenFractalRidged(float x, float y)
        {
            int seed = mSeed;
            float sum = 0;
            float amp = 1;
            float max = 0;

            for (int i = 0; i < mOctaves; i++)
            {
                float noise = Math.Abs(GenNoiseSingle(seed++, x, y));
                sum += (1.0f - noise) * amp;
                max += amp;
                amp *= mGain;

                x *= mLacunarity;
                y *= mLacunarity;
            }

            return sum / max;
        }

        private float GenFractalPingPong(float x, float y)
        {
            int seed = mSeed;
            float sum = 0;
            float amp = 1;
            float max = 0;

            for (int i = 0; i < mOctaves; i++)
            {
                float noise = PingPong((GenNoiseSingle(seed++, x, y) + 1) * 0.5f * mPingPongStrength);
                sum += (noise - 0.5f) * 2 * amp;
                max += amp;
                amp *= mGain;

                x *= mLacunarity;
                y *= mLacunarity;
            }

            return sum / max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float PingPong(float t)
        {
            t -= (int)(t * 0.5f) * 2;
            return t < 1 ? t : 2 - t;
        }

        private float GenNoiseSingle(int seed, float x, float y)
        {
            switch (mNoiseType)
            {
                default:
                case NoiseType.OpenSimplex2:
                    return SingleSimplex(seed, x, y);
                case NoiseType.Perlin:
                    return SinglePerlin(seed, x, y);
                case NoiseType.Cellular:
                    return SingleCellular(seed, x, y);
            }
        }

        private static readonly float SQUISH_2D = (float)(0.5 * (Math.Sqrt(2 + 1) - 1));
        private static readonly float STRETCH_2D = (float)((1.0 / Math.Sqrt(2 + 1) - 1) * 0.5);

        private float SingleSimplex(int seed, float x, float y)
        {
            float stretchOffset = (x + y) * STRETCH_2D;
            float xs = x + stretchOffset;
            float ys = y + stretchOffset;

            int xsb = FastFloor(xs);
            int ysb = FastFloor(ys);

            float squishOffset = (xsb + ysb) * SQUISH_2D;
            float dx0 = x - (xsb + squishOffset);
            float dy0 = y - (ysb + squishOffset);

            float xins = xs - xsb;
            float yins = ys - ysb;

            float inSum = xins + yins;

            int xsv_ext, ysv_ext;
            float dx_ext, dy_ext;

            if (xins > yins)
            {
                xsv_ext = xsb + 1;
                ysv_ext = ysb;
                dx_ext = dx0 - 1 - SQUISH_2D;
                dy_ext = dy0 - SQUISH_2D;
            }
            else
            {
                xsv_ext = xsb;
                ysv_ext = ysb + 1;
                dx_ext = dx0 - SQUISH_2D;
                dy_ext = dy0 - 1 - SQUISH_2D;
            }

            float value = 0;

            float attn0 = 0.5f - dx0 * dx0 - dy0 * dy0;
            if (attn0 > 0)
            {
                attn0 *= attn0;
                value += attn0 * attn0 * GradCoord(seed, xsb, ysb, dx0, dy0);
            }

            float attn1 = 0.5f - dx_ext * dx_ext - dy_ext * dy_ext;
            if (attn1 > 0)
            {
                attn1 *= attn1;
                value += attn1 * attn1 * GradCoord(seed, xsv_ext, ysv_ext, dx_ext, dy_ext);
            }

            float dx2 = dx0 - 1 - 2 * SQUISH_2D;
            float dy2 = dy0 - 1 - 2 * SQUISH_2D;
            float attn2 = 0.5f - dx2 * dx2 - dy2 * dy2;
            if (attn2 > 0)
            {
                attn2 *= attn2;
                value += attn2 * attn2 * GradCoord(seed, xsb + 1, ysb + 1, dx2, dy2);
            }

            return value * 99.83685446303647f;
        }

        private float SinglePerlin(int seed, float x, float y)
        {
            int x0 = FastFloor(x);
            int y0 = FastFloor(y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            float xs = InterpHermite(x - x0);
            float ys = InterpHermite(y - y0);

            float xd0 = x - x0;
            float yd0 = y - y0;
            float xd1 = xd0 - 1;
            float yd1 = yd0 - 1;

            float xf0 = Lerp(GradCoord(seed, x0, y0, xd0, yd0), GradCoord(seed, x1, y0, xd1, yd0), xs);
            float xf1 = Lerp(GradCoord(seed, x0, y1, xd0, yd1), GradCoord(seed, x1, y1, xd1, yd1), xs);

            return Lerp(xf0, xf1, ys);
        }

        private float SingleCellular(int seed, float x, float y)
        {
            int xr = FastRound(x);
            int yr = FastRound(y);

            float distance = 999999;
            for (int xi = xr - 1; xi <= xr + 1; xi++)
            {
                for (int yi = yr - 1; yi <= yr + 1; yi++)
                {
                    int hash = Hash(seed, xi, yi);
                    float vecX = xi - x + (hash & 0xFF) * (1.0f / 255.0f);
                    float vecY = yi - y + ((hash >> 8) & 0xFF) * (1.0f / 255.0f);

                    float newDist = vecX * vecX + vecY * vecY;
                    if (newDist < distance)
                        distance = newDist;
                }
            }

            return (float)Math.Sqrt(distance) * 2 - 1;
        }

        public void DomainWarp(ref float x, ref float y)
        {
            float vx = x * mFrequency;
            float vy = y * mFrequency;
            float dx = SinglePerlin(mSeed + 1, vx, vy) * mDomainWarpAmp;
            float dy = SinglePerlin(mSeed + 2, vx + 100, vy + 100) * mDomainWarpAmp;
            x += dx;
            y += dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastFloor(float f) => f >= 0 ? (int)f : (int)f - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastRound(float f) => f >= 0 ? (int)(f + 0.5f) : (int)(f - 0.5f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InterpHermite(float t) => t * t * (3 - 2 * t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        private const int PRIME_X = 501125321;
        private const int PRIME_Y = 1136930381;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Hash(int seed, int xPrimed, int yPrimed)
        {
            int hash = seed ^ (xPrimed * PRIME_X) ^ (yPrimed * PRIME_Y);
            hash *= 0x27d4eb2d;
            return hash;
        }

        private static readonly float[] GRADIENTS_2D = {
            -1, -1,  1, -1, -1,  1,  1,  1,
             0, -1, -1,  0,  0,  1,  1,  0,
            -1, -1,  1, -1, -1,  1,  1,  1,
             0, -1, -1,  0,  0,  1,  1,  0
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GradCoord(int seed, int x, int y, float xd, float yd)
        {
            int hash = Hash(seed, x, y);
            hash ^= hash >> 15;
            hash &= 7;
            int index = hash << 1;
            return xd * GRADIENTS_2D[index] + yd * GRADIENTS_2D[index + 1];
        }
    }
}
