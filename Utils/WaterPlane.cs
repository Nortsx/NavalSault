using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Utils;

public class WaterPlane
{
    private struct Wave
    {
        public float DirX, DirZ, Freq, Amp, Speed;
    }

    private static readonly Wave[] Waves =
    {
        new Wave { DirX = 0.80f, DirZ = 0.60f, Freq = 0.08f, Amp = 0.6f, Speed = 1.1f },
        new Wave { DirX = -0.50f, DirZ = 0.87f, Freq = 0.14f, Amp = 0.35f, Speed = 0.8f },
        new Wave { DirX = 0.95f, DirZ = -0.31f, Freq = 0.22f, Amp = 0.18f, Speed = 1.7f },
    };

    private readonly DynamicVertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly VertexPositionNormalTexture[] _vertices;
    private readonly float[] _baseX;
    private readonly float[] _baseZ;
    private readonly int _primitiveCount;

    public WaterPlane(GraphicsDevice graphicsDevice, float width, float depth, int segmentsX, int segmentsZ)
    {
        int vertsPerRow = segmentsX + 1;
        int vertsPerCol = segmentsZ + 1;
        _vertices = new VertexPositionNormalTexture[vertsPerRow * vertsPerCol];
        _baseX = new float[_vertices.Length];
        _baseZ = new float[_vertices.Length];

        float cellW = width / segmentsX;
        float cellD = depth / segmentsZ;

        for (int row = 0; row < vertsPerCol; row++)
        {
            for (int col = 0; col < vertsPerRow; col++)
            {
                int idx = row * vertsPerRow + col;
                float x = -width / 2f + col * cellW;
                float z = -depth / 2f + row * cellD;
                _baseX[idx] = x;
                _baseZ[idx] = z;
                var uv = new Vector2((float)col / segmentsX, (float)row / segmentsZ);
                _vertices[idx] = new VertexPositionNormalTexture(new Vector3(x, 0f, z), Vector3.Up, uv);
            }
        }

        var indices = new ushort[segmentsX * segmentsZ * 6];
        int i = 0;
        for (int row = 0; row < segmentsZ; row++)
        {
            for (int col = 0; col < segmentsX; col++)
            {
                int topLeft = row * vertsPerRow + col;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + vertsPerRow;
                int bottomRight = bottomLeft + 1;

                indices[i++] = (ushort)topLeft;
                indices[i++] = (ushort)topRight;
                indices[i++] = (ushort)bottomLeft;

                indices[i++] = (ushort)topRight;
                indices[i++] = (ushort)bottomRight;
                indices[i++] = (ushort)bottomLeft;
            }
        }

        _primitiveCount = segmentsX * segmentsZ * 2;

        _vertexBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), _vertices.Length, BufferUsage.WriteOnly);
        _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);

        UpdateWaves(0f);
    }

    public void UpdateWaves(float time)
    {
        for (int i = 0; i < _vertices.Length; i++)
        {
            float x = _baseX[i];
            float z = _baseZ[i];

            float height = 0f;
            float dHdx = 0f;
            float dHdz = 0f;

            foreach (var wave in Waves)
            {
                float phase = (wave.DirX * x + wave.DirZ * z) * wave.Freq + time * wave.Speed;
                float s = MathF.Sin(phase);
                float c = MathF.Cos(phase);

                height += wave.Amp * s;
                float slope = wave.Amp * wave.Freq * c;
                dHdx += slope * wave.DirX;
                dHdz += slope * wave.DirZ;
            }

            _vertices[i].Position.Y = height;
            _vertices[i].Normal = Vector3.Normalize(new Vector3(-dHdx, 1f, -dHdz));
        }

        _vertexBuffer.SetData(0, _vertices, 0, _vertices.Length, VertexPositionNormalTexture.VertexDeclaration.VertexStride, SetDataOptions.Discard);
    }

    public void Draw(GraphicsDevice graphicsDevice, Effect effect)
    {
        graphicsDevice.SetVertexBuffer(_vertexBuffer);
        graphicsDevice.Indices = _indexBuffer;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
        }
    }
}
