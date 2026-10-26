using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NavalSault.Utils;

namespace NavalSault.Rendering;

// What a ship looks like on the water. Hulls are built in model space with the bow along -Z and the anchor tile
// at the origin, so the ship's world matrix turns and moves them exactly like its footprint.
public abstract class ShipHull : IDisposable
{
    public abstract void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, Matrix world, Color color);

    public virtual void Dispose()
    {
    }

    // The ship's model if there is one and it loads, otherwise a plain box over the footprint.
    // A missing or broken asset only costs the looks, so scenarios can name models before they're made.
    public static ShipHull Create(GraphicsDevice graphicsDevice, ContentManager content, HexGrid grid, HexFootprint footprint,
        ShipModel model)
    {
        if (model != null)
        {
            try
            {
                return new ModelHull(content.Load<Model>(model.AssetName), model, FootprintBounds(grid, footprint));
            }
            catch (ContentLoadException e)
            {
                Debug.WriteLine($"Ship model '{model.AssetName}' failed to load, drawing a box instead: {e.Message}");
            }
        }
        return new BoxHull(graphicsDevice, FootprintBounds(grid, footprint));
    }

    // The space a hull may fill: the footprint's tiles with a margin, from the waterline up to deck height.
    private static BoundingBox FootprintBounds(HexGrid grid, HexFootprint footprint)
    {
        Vector3 anchorWorld = grid.AxialToWorld((0, 0));
        Vector3 min = new Vector3(float.MaxValue), max = new Vector3(float.MinValue);
        foreach (var offset in footprint.Offsets)
        {
            Vector3 local = grid.AxialToWorld(offset) - anchorWorld;
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }

        var margin = new Vector3(grid.HexRadius * 0.45f, 0f, grid.HexRadius * MathF.Sqrt(3) * 0.4f);
        float height = grid.HexRadius * 0.45f;
        return new BoundingBox(new Vector3(min.X, 0f, min.Z) - margin, new Vector3(max.X, height, max.Z) + margin);
    }
}
