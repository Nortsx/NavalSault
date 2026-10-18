using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NavalSault.GameLogic;
using NavalSault.GameLogic.Orders;
using NavalSault.Input;
using NavalSault.Utils;

namespace NavalSault.Rendering;

// Draws the battle in 3D: the grid, the island, the ships, their planned orders,
// the player's previews and shells in flight.
public class BattleRenderer
{
    private static readonly Color PreviewPathColor = new(255, 225, 70);
    private static readonly Color PlannedPathColor = new(255, 255, 255);
    private static readonly Color PreviewShotColor = new(255, 90, 60);
    private static readonly Color PlannedShotColor = new(255, 255, 255);
    private static readonly Color ShellColor = new(255, 240, 180);
    private static readonly Color SplashColor = new(200, 230, 255);
    private static readonly Color HitColor = new(255, 50, 30);
    // Shells leave from about deck height above the tile.
    private static readonly Vector3 MuzzleOffset = new(0f, 3f, 0f);
    // Share of the arc drawn behind the shell as a tracer.
    private const float TracerLength = 0.2f;
    private const float SplashHeight = 8f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private readonly HexOverlay _overlay;
    private readonly HexGrid _grid;
    private readonly Island _island;
    private readonly TurnManager _turns;
    private readonly PlayerController _controller;

    public BattleRenderer(GraphicsDevice graphicsDevice, Vector3 lightDirection, HexGrid grid, Island island,
        TurnManager turns, PlayerController controller)
    {
        _graphicsDevice = graphicsDevice;
        _grid = grid;
        _island = island;
        _turns = turns;
        _controller = controller;
        _overlay = new HexOverlay(graphicsDevice, grid.HexRadius);

        _effect = new BasicEffect(graphicsDevice)
        {
            LightingEnabled = true,
            AmbientLightColor = new Vector3(0.35f, 0.35f, 0.4f),
            SpecularColor = Vector3.Zero,
            TextureEnabled = false,
            VertexColorEnabled = false
        };
        _effect.DirectionalLight0.Enabled = true;
        _effect.DirectionalLight0.Direction = lightDirection;
        _effect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 0.95f);
        _effect.DirectionalLight0.SpecularColor = Vector3.Zero;
        _effect.DirectionalLight1.Enabled = false;
        _effect.DirectionalLight2.Enabled = false;
    }

    // Hex tiles are a gameplay overlay, not world geometry, so only ships get reflected.
    public void DrawReflected(Matrix reflectionView, Matrix projection)
    {
        _effect.View = reflectionView;
        _effect.Projection = projection;
        foreach (var ship in _turns.Ships)
            ship.Draw(_graphicsDevice, _effect);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        _effect.View = view;
        _effect.Projection = projection;
        _grid.Draw(_graphicsDevice, _effect, _controller.HoveredTile);
        _island.Draw(_graphicsDevice, _effect, _grid);

        foreach (var ship in _turns.Ships)
        {
            // AI ships keep their plans to themselves until the turn resolves.
            if (!ship.IsPlayerControlled && _turns.Phase == TurnPhase.Planning)
                continue;

            foreach (var order in ship.PlannedOrders)
            {
                if (order is MoveOrder move)
                    PathLine.Draw(_graphicsDevice, _effect, ToWorldPositions(move.Path), PlannedPathColor);
                else if (order is ShootOrder shot && shot != _turns.ResolvingOrder)
                    DrawShotPlan(shot, PlannedShotColor, fillAlpha: 0.12f);
            }
        }

        // The tiles the active ship will cover once its planned orders are done.
        if (_turns.ActiveShip is { } active)
            _overlay.Draw(_graphicsDevice, _effect, _grid, active.PlannedTiles, PlannedPathColor, fillAlpha: 0.08f);

        if (_controller.PreviewPath is { } previewPath)
        {
            PathLine.Draw(_graphicsDevice, _effect, ToWorldPositions(previewPath), PreviewPathColor);
            _overlay.Draw(_graphicsDevice, _effect, _grid,
                _turns.ActiveShip.Footprint.Tiles(previewPath[^1], _controller.PreviewFacing), PreviewPathColor, fillAlpha: 0.25f);
        }
        if (_controller.PreviewShot != null)
            DrawShotPlan(_controller.PreviewShot, PreviewShotColor, fillAlpha: 0.3f);
        if (_turns.ResolvingOrder is ShootOrder firing)
            DrawShotInFlight(firing);

        foreach (var ship in _turns.Ships)
            ship.Draw(_graphicsDevice, _effect);
    }

    private List<Vector3> ToWorldPositions(IReadOnlyList<(int Q, int R)> path)
    {
        var positions = new List<Vector3>(path.Count);
        foreach (var (q, r) in path)
        {
            _grid.TryGetTilePosition(q, r, out var position);
            positions.Add(position);
        }
        return positions;
    }

    // The arc from the firing tile to the aimed-at tile, and every tile the shell may land on.
    private void DrawShotPlan(ShootOrder shot, Color color, float fillAlpha)
    {
        var from = _grid.AxialToWorld(shot.FiringTile) + MuzzleOffset;
        var to = _grid.AxialToWorld(shot.TargetTile);
        PathLine.Draw(_graphicsDevice, _effect, ShotArc.Points(from, to, 0f, 1f, 24), color);

        _overlay.Draw(_graphicsDevice, _effect, _grid, shot.SpreadTiles, color, fillAlpha);
        _overlay.Draw(_graphicsDevice, _effect, _grid, [shot.TargetTile], color, fillAlpha);
    }

    // A tracer following the shell, then a splash column on the tile it landed on.
    private void DrawShotInFlight(ShootOrder shot)
    {
        var from = _grid.AxialToWorld(shot.FiringTile) + MuzzleOffset;
        var to = _grid.AxialToWorld(shot.LandingTile);

        if (!shot.HasLanded)
        {
            float t = shot.FlightProgress;
            PathLine.Draw(_graphicsDevice, _effect, ShotArc.Points(from, to, Math.Max(0f, t - TracerLength), t, 8), ShellColor);
            return;
        }

        var color = shot.HitShip != null ? HitColor : SplashColor;
        _overlay.Draw(_graphicsDevice, _effect, _grid, [shot.LandingTile], color, fillAlpha: 0.6f);
        PathLine.Draw(_graphicsDevice, _effect, [to, to + new Vector3(0f, SplashHeight, 0f)], color);
    }
}
