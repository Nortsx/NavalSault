using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Utils;

public class HexGrid
{
    private const float HighlightFillAlpha = 110f / 255f;
    private const float HighlightYOffset = 0.05f;

    private readonly HexTile _tileMesh;
    private readonly HexTile _highlightTileMesh;
    private readonly HexFill _highlightFillMesh;
    private readonly Dictionary<(int Q, int R), Vector3> _tiles = new();
    private readonly HashSet<(int Q, int R)> _blocked = new();

    public float HexRadius { get; }
    public float TileY { get; }
    // World distance from the map center to the furthest tile center.
    public float WorldRadius { get; }

    public HexGrid(GraphicsDevice graphicsDevice, float hexRadius, int gridRadius, float tileY, Color color, Color highlightColor)
    {
        HexRadius = hexRadius;
        TileY = tileY;
        _tileMesh = new HexTile(graphicsDevice, hexRadius, color);
        _highlightTileMesh = new HexTile(graphicsDevice, hexRadius, highlightColor);
        _highlightFillMesh = new HexFill(graphicsDevice, hexRadius, highlightColor * HighlightFillAlpha);

        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            int rMin = Math.Max(-gridRadius, -q - gridRadius);
            int rMax = Math.Min(gridRadius, -q + gridRadius);
            for (int r = rMin; r <= rMax; r++)
                _tiles[(q, r)] = AxialToWorld((q, r));
        }

        // The corner tiles of a hex-shaped map are the furthest out.
        WorldRadius = hexRadius * (float)Math.Sqrt(3) * gridRadius;
    }

    // Center of a tile, whether it's on the map or not.
    public Vector3 AxialToWorld((int Q, int R) tile)
    {
        float x = HexRadius * 1.5f * tile.Q;
        float z = HexRadius * (float)Math.Sqrt(3) * (tile.R + tile.Q / 2f);
        return new Vector3(x, TileY, z);
    }

    public bool IsOnMap((int Q, int R) tile)
    {
        return _tiles.ContainsKey(tile);
    }

    public bool TryGetTilePosition(int q, int r, out Vector3 position)
    {
        return _tiles.TryGetValue((q, r), out position);
    }

    public void SetBlocked((int Q, int R) tile, bool blocked)
    {
        if (blocked)
            _blocked.Add(tile);
        else
            _blocked.Remove(tile);
    }

    // True if the tile is on the map and nothing blocks ships from entering it.
    public bool IsPassable((int Q, int R) tile)
    {
        return _tiles.ContainsKey(tile) && !_blocked.Contains(tile);
    }

    public (int Q, int R) WorldToAxial(Vector3 worldPosition)
    {
        float qf = (2f / 3f * worldPosition.X) / HexRadius;
        float rf = (-1f / 3f * worldPosition.X + (float)Math.Sqrt(3) / 3f * worldPosition.Z) / HexRadius;
        return AxialRound(qf, rf);
    }

    public static int Distance((int Q, int R) a, (int Q, int R) b)
    {
        int dq = a.Q - b.Q;
        int dr = a.R - b.R;
        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
    }

    // Every tile at most radius tiles from center, center included, whether on the map or not.
    public static IEnumerable<(int Q, int R)> TilesWithin((int Q, int R) center, int radius)
    {
        for (int dq = -radius; dq <= radius; dq++)
        {
            int drMin = Math.Max(-radius, -dq - radius);
            int drMax = Math.Min(radius, -dq + radius);
            for (int dr = drMin; dr <= drMax; dr++)
                yield return (center.Q + dq, center.R + dr);
        }
    }

    private static (int Q, int R) AxialRound(float qf, float rf)
    {
        float x = qf;
        float z = rf;
        float y = -x - z;

        int rx = (int)Math.Round(x, MidpointRounding.AwayFromZero);
        int ry = (int)Math.Round(y, MidpointRounding.AwayFromZero);
        int rz = (int)Math.Round(z, MidpointRounding.AwayFromZero);

        float xDiff = Math.Abs(rx - x);
        float yDiff = Math.Abs(ry - y);
        float zDiff = Math.Abs(rz - z);

        if (xDiff > yDiff && xDiff > zDiff)
            rx = -ry - rz;
        else if (yDiff > zDiff)
            ry = -rx - rz;
        else
            rz = -rx - ry;

        return (rx, rz);
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, (int Q, int R)? highlightedTile = null)
    {
        effect.LightingEnabled = false;
        effect.VertexColorEnabled = true;

        foreach (var kvp in _tiles)
        {
            effect.World = Matrix.CreateTranslation(kvp.Value);
            var mesh = highlightedTile.HasValue && kvp.Key == highlightedTile.Value ? _highlightTileMesh : _tileMesh;
            mesh.Draw(graphicsDevice, effect);
        }

        if (highlightedTile.HasValue && _tiles.TryGetValue(highlightedTile.Value, out var highlightPosition))
        {
            var previousBlendState = graphicsDevice.BlendState;
            graphicsDevice.BlendState = BlendState.AlphaBlend;

            effect.World = Matrix.CreateTranslation(highlightPosition + new Vector3(0f, HighlightYOffset, 0f));
            _highlightFillMesh.Draw(graphicsDevice, effect);

            graphicsDevice.BlendState = previousBlendState;
        }

        effect.LightingEnabled = true;
        effect.VertexColorEnabled = false;
    }
}
