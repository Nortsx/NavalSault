using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Utils;

public class HexFill
{
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly int _primitiveCount;

    public HexFill(GraphicsDevice graphicsDevice, float radius, Color color)
    {
        var vertices = new VertexPositionColor[7];
        vertices[0] = new VertexPositionColor(Vector3.Zero, color);
        for (int i = 0; i < 6; i++)
        {
            float angle = MathHelper.ToRadians(60f * i);
            var corner = new Vector3(radius * (float)Math.Cos(angle), 0f, radius * (float)Math.Sin(angle));
            vertices[i + 1] = new VertexPositionColor(corner, color);
        }

        var indices = new ushort[18];
        int idx = 0;
        for (int i = 0; i < 6; i++)
        {
            indices[idx++] = 0;
            indices[idx++] = (ushort)(i + 1);
            indices[idx++] = (ushort)(i == 5 ? 1 : i + 2);
        }

        _primitiveCount = 6;

        _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);

        _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect)
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
