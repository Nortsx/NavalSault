using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault;

// Procedural stand-ins for the DuDv and normal map images. Integer wave frequencies keep both maps seamlessly tileable.
public static class WaterTextures
{
    private const int Size = 256;
    private const int WaveCount = 12;
    private const float NormalSlopeScale = 0.08f;

    private readonly record struct Wave(int Kx, int Ky, float Amp, float Phase);

    public static Texture2D CreateDistortionMap(GraphicsDevice graphicsDevice, int seed)
    {
        var rng = new Random(seed);
        var wavesR = RandomWaves(rng);
        var wavesG = RandomWaves(rng);

        var data = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float u = (float)x / Size;
                float v = (float)y / Size;
                float r = Height(wavesR, u, v, out _, out _);
                float g = Height(wavesG, u, v, out _, out _);
                data[y * Size + x] = new Color(r * 0.5f + 0.5f, g * 0.5f + 0.5f, 0f);
            }
        }

        var texture = new Texture2D(graphicsDevice, Size, Size);
        texture.SetData(data);
        return texture;
    }

    public static Texture2D CreateNormalMap(GraphicsDevice graphicsDevice, int seed)
    {
        var waves = RandomWaves(new Random(seed));

        var data = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Height(waves, (float)x / Size, (float)y / Size, out float dhdu, out float dhdv);
                var n = Vector3.Normalize(new Vector3(-dhdu * NormalSlopeScale, 1f, -dhdv * NormalSlopeScale));
                // Shader decodes (R, B, G) -> (X, Y, Z), with Y stored unsigned.
                data[y * Size + x] = new Color(n.X * 0.5f + 0.5f, n.Z * 0.5f + 0.5f, n.Y);
            }
        }

        var texture = new Texture2D(graphicsDevice, Size, Size);
        texture.SetData(data);
        return texture;
    }

    private static Wave[] RandomWaves(Random rng)
    {
        var waves = new Wave[WaveCount];
        for (int i = 0; i < WaveCount; i++)
        {
            int kx, ky;
            do
            {
                kx = rng.Next(-8, 9);
                ky = rng.Next(-8, 9);
            } while (kx * kx + ky * ky < 4);

            float amp = 1f / MathF.Sqrt(kx * kx + ky * ky);
            waves[i] = new Wave(kx, ky, amp, (float)(rng.NextDouble() * MathHelper.TwoPi));
        }
        return waves;
    }

    private static float Height(Wave[] waves, float u, float v, out float dhdu, out float dhdv)
    {
        float h = 0f, totalAmp = 0f;
        dhdu = 0f;
        dhdv = 0f;

        foreach (var w in waves)
        {
            float phase = MathHelper.TwoPi * (w.Kx * u + w.Ky * v) + w.Phase;
            h += w.Amp * MathF.Sin(phase);
            float slope = w.Amp * MathHelper.TwoPi * MathF.Cos(phase);
            dhdu += slope * w.Kx;
            dhdv += slope * w.Ky;
            totalAmp += w.Amp;
        }

        dhdu /= totalAmp;
        dhdv /= totalAmp;
        return h / totalAmp;
    }
}
