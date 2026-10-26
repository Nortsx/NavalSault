using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Rendering;

// A plain box filling the footprint; the stand-in for ships without a model.
public class BoxHull : ShipHull
{
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly int _primitiveCount;

    public BoxHull(GraphicsDevice graphicsDevice, BoundingBox bounds)
    {
        Vector3 lo = bounds.Min, hi = bounds.Max;
        Vector3 p000 = new Vector3(lo.X, lo.Y, lo.Z);
        Vector3 p100 = new Vector3(hi.X, lo.Y, lo.Z);
        Vector3 p110 = new Vector3(hi.X, lo.Y, hi.Z);
        Vector3 p010 = new Vector3(lo.X, lo.Y, hi.Z);
        Vector3 p001 = new Vector3(lo.X, hi.Y, lo.Z);
        Vector3 p101 = new Vector3(hi.X, hi.Y, lo.Z);
        Vector3 p111 = new Vector3(hi.X, hi.Y, hi.Z);
        Vector3 p011 = new Vector3(lo.X, hi.Y, hi.Z);

        var vertices = new List<VertexPositionNormalTexture>();
        var indices = new List<ushort>();

        AddFace(vertices, indices, p001, p101, p111, p011, Vector3.Up);
        AddFace(vertices, indices, p010, p110, p100, p000, Vector3.Down);
        AddFace(vertices, indices, p000, p100, p101, p001, Vector3.Backward);
        AddFace(vertices, indices, p110, p010, p011, p111, Vector3.Forward);
        AddFace(vertices, indices, p010, p000, p001, p011, Vector3.Left);
        AddFace(vertices, indices, p100, p110, p111, p101, Vector3.Right);

        _primitiveCount = indices.Count / 3;

        _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertices.Count, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());
    }

    private static void AddFace(List<VertexPositionNormalTexture> vertices, List<ushort> indices, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        ushort baseIndex = (ushort)vertices.Count;

        vertices.Add(new VertexPositionNormalTexture(a, normal, new Vector2(0f, 0f)));
        vertices.Add(new VertexPositionNormalTexture(b, normal, new Vector2(1f, 0f)));
        vertices.Add(new VertexPositionNormalTexture(c, normal, new Vector2(1f, 1f)));
        vertices.Add(new VertexPositionNormalTexture(d, normal, new Vector2(0f, 1f)));

        indices.Add(baseIndex);
        indices.Add((ushort)(baseIndex + 1));
        indices.Add((ushort)(baseIndex + 2));

        indices.Add(baseIndex);
        indices.Add((ushort)(baseIndex + 2));
        indices.Add((ushort)(baseIndex + 3));
    }

    public override void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, Matrix world, Color color)
    {
        effect.World = world;
        effect.LightingEnabled = true;
        effect.VertexColorEnabled = false;
        effect.TextureEnabled = false;
        effect.DiffuseColor = color.ToVector3();

        graphicsDevice.SetVertexBuffer(_vertexBuffer);
        graphicsDevice.Indices = _indexBuffer;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
        }
    }

    public override void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }
}
