using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Utils;

// Draws paths as line strips through tile centers, slightly above the tiles.
public static class PathLine
{
    private const float YOffset = 0.1f;

    public static void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, IReadOnlyList<Vector3> points, Color color)
    {
        if (points == null || points.Count < 2)
            return;

        var vertices = new VertexPositionColor[points.Count];
        for (int i = 0; i < points.Count; i++)
            vertices[i] = new VertexPositionColor(points[i] + new Vector3(0f, YOffset, 0f), color);

        effect.World = Matrix.Identity;
        effect.LightingEnabled = false;
        effect.VertexColorEnabled = true;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, vertices.Length - 1);
        }

        effect.LightingEnabled = true;
        effect.VertexColorEnabled = false;
    }
}
