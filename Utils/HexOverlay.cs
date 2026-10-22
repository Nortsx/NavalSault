using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Utils;

// Marks any set of tiles with a see-through fill and a solid outline, e.g. the area a shell can land in.
public class HexOverlay : IDisposable
{
    private const float YOffset = 0.08f;

    // White meshes, tinted per draw through the effect's diffuse colour.
    private readonly HexFill _fillMesh;
    private readonly HexTile _outlineMesh;

    public HexOverlay(GraphicsDevice graphicsDevice, float hexRadius)
    {
        _fillMesh = new HexFill(graphicsDevice, hexRadius, Color.White);
        _outlineMesh = new HexTile(graphicsDevice, hexRadius, Color.White);
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, HexGrid grid, IEnumerable<(int Q, int R)> tiles,
        Color color, float fillAlpha, bool outline = true)
    {
        var previousBlendState = graphicsDevice.BlendState;
        var previousDepthState = graphicsDevice.DepthStencilState;
        var previousDiffuse = effect.DiffuseColor;
        var previousAlpha = effect.Alpha;

        effect.LightingEnabled = false;
        effect.VertexColorEnabled = true;
        effect.DiffuseColor = color.ToVector3();
        // Overlays may overlap each other, so they blend without hiding one another.
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        foreach (var tile in tiles)
        {
            effect.World = Matrix.CreateTranslation(grid.AxialToWorld(tile) + new Vector3(0f, YOffset, 0f));

            effect.Alpha = fillAlpha;
            _fillMesh.Draw(graphicsDevice, effect);

            if (!outline)
                continue;

            effect.Alpha = 1f;
            _outlineMesh.Draw(graphicsDevice, effect);
        }

        graphicsDevice.BlendState = previousBlendState;
        graphicsDevice.DepthStencilState = previousDepthState;
        effect.DiffuseColor = previousDiffuse;
        effect.Alpha = previousAlpha;
        effect.LightingEnabled = true;
        effect.VertexColorEnabled = false;
    }

    public void Dispose()
    {
        _fillMesh.Dispose();
        _outlineMesh.Dispose();
    }
}
