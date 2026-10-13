using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

// A group of tiles ships can't enter, drawn as solid hexes.
public class Island
{
    private const float YOffset = 0.05f;

    private readonly HexFill _fillMesh;

    public IReadOnlyList<(int Q, int R)> Tiles { get; }

    public Island(GraphicsDevice graphicsDevice, float hexRadius, IEnumerable<(int Q, int R)> tiles, Color color)
    {
        Tiles = new List<(int Q, int R)>(tiles);
        _fillMesh = new HexFill(graphicsDevice, hexRadius, color);
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, HexGrid grid)
    {
        effect.LightingEnabled = false;
        effect.VertexColorEnabled = true;

        foreach (var (q, r) in Tiles)
        {
            if (!grid.TryGetTilePosition(q, r, out var position))
                continue;

            effect.World = Matrix.CreateTranslation(position + new Vector3(0f, YOffset, 0f));
            _fillMesh.Draw(graphicsDevice, effect);
        }

        effect.LightingEnabled = true;
        effect.VertexColorEnabled = false;
    }
}
