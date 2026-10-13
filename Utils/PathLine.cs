using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Utils;

// Draws a path as a line strip through tile centers, slightly above the tiles.
public class PathLine
{
    private const float YOffset = 0.1f;

    private readonly Color _color;
    private VertexPositionColor[] _vertices = [];

    public PathLine(Color color)
    {
        _color = color;
    }

    public void SetPath(IReadOnlyList<Vector3> points)
    {
        if (points == null || points.Count < 2)
        {
            _vertices = [];
            return;
        }

        _vertices = new VertexPositionColor[points.Count];
        for (int i = 0; i < points.Count; i++)
            _vertices[i] = new VertexPositionColor(points[i] + new Vector3(0f, YOffset, 0f), _color);
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        if (_vertices.Length < 2)
            return;

        effect.World = Matrix.Identity;
        effect.LightingEnabled = false;
        effect.VertexColorEnabled = true;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, _vertices, 0, _vertices.Length - 1);
        }

        effect.LightingEnabled = true;
        effect.VertexColorEnabled = false;
    }
}
