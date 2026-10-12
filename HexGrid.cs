using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault;

public class HexGrid
{
    private readonly HexTile _tileMesh;
    private readonly List<Vector3> _tilePositions = new();

    public HexGrid(GraphicsDevice graphicsDevice, float hexRadius, int gridRadius, float tileY, Color color)
    {
        _tileMesh = new HexTile(graphicsDevice, hexRadius, color);

        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            int rMin = Math.Max(-gridRadius, -q - gridRadius);
            int rMax = Math.Min(gridRadius, -q + gridRadius);
            for (int r = rMin; r <= rMax; r++)
            {
                float x = hexRadius * 1.5f * q;
                float z = hexRadius * (float)Math.Sqrt(3) * (r + q / 2f);
                _tilePositions.Add(new Vector3(x, tileY, z));
            }
        }
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        effect.LightingEnabled = false;
        effect.VertexColorEnabled = true;

        foreach (var position in _tilePositions)
        {
            effect.World = Matrix.CreateTranslation(position);
            _tileMesh.Draw(graphicsDevice, effect);
        }

        effect.LightingEnabled = true;
        effect.VertexColorEnabled = false;
    }
}
