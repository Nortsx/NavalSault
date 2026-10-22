using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NavalSault.GameLogic.AI;
using NavalSault.GameLogic.Orders;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

public class Ship : IDisposable
{
    private const float MoveSpeed = 18f;
    // How long a sunk hull takes to slip under, and how deep and how far over it goes.
    private const float SinkDuration = 2.5f;
    private const float SinkDepth = 7f;
    private static readonly float SinkRoll = MathHelper.ToRadians(35f);
    private static readonly float SinkPitch = MathHelper.ToRadians(12f);

    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly int _primitiveCount;

    private readonly Color _baseColor;
    private readonly Color _activeColor;

    private float _sinkElapsed;

    private ((int Q, int R) Tile, Vector3 Position, HexDirection Facing) _target;
    private readonly Queue<((int Q, int R) Tile, Vector3 Position, HexDirection Facing)> _waypoints = new();

    public string Name { get; }
    public Side Side { get; }
    // Plans the orders of a ship the player doesn't command; null for player ships. Can be swapped mid-battle.
    public ShipBehaviour Behaviour { get; set; }
    public bool IsPlayerControlled => Behaviour == null;
    // Tiles the ship sees around every tile of its hull.
    public int ViewRadius { get; }
    // Tiles the hull covers around its anchor tile.
    public HexFootprint Footprint { get; }
    // Anchor tile the ship stands on; the authoritative position for game rules. Updates as each waypoint is reached.
    public (int Q, int R) Tile { get; private set; }
    // World position for drawing; slides between tiles while moving.
    public Vector3 Position { get; private set; }
    // Bow direction; turns to each leg's direction as the ship moves along a path.
    public HexDirection Facing { get; private set; }
    public bool IsMoving { get; private set; }
    // Set by the turn manager while this ship is the one taking orders.
    public bool IsActive { get; set; }

    public int Initiative { get; }
    public int MaxTurnPoints { get; }
    // Spent on orders while planning; refilled at the start of every turn.
    public int TurnPoints { get; set; }
    public Weapon Weapon { get; set; }

    public int MaxHp { get; }
    public int Hp { get; private set; }
    // A sunk ship is out of the fight at once: it can't be hit, block tiles or carry out orders.
    public bool IsSunk => Hp <= 0;
    // Still slipping under after being sunk; the turn waits for it before moving on.
    public bool IsSinking => IsSunk && _sinkElapsed < SinkDuration;

    public List<Order> PlannedOrders { get; } = new();

    // Where the ship will be once all planned orders are carried out.
    public (int Q, int R) PlannedTile => PlannedOrders.Count > 0 ? PlannedOrders[^1].EndTile : Tile;
    public HexDirection PlannedFacing => PlannedOrders.Count > 0 ? PlannedOrders[^1].EndFacing : Facing;

    // Every tile the hull covers now, and once all planned orders are carried out.
    public IEnumerable<(int Q, int R)> Tiles => Footprint.Tiles(Tile, Facing);
    public IEnumerable<(int Q, int R)> PlannedTiles => Footprint.Tiles(PlannedTile, PlannedFacing);

    public Ship(GraphicsDevice graphicsDevice, HexGrid grid, string name, Side side, HexFootprint footprint, (int Q, int R) tile,
        Color baseColor, Color activeColor, HexDirection facing, int initiative, int maxTurnPoints, int maxHp, int viewRadius, Weapon weapon)
    {
        Name = name;
        Side = side;
        Footprint = footprint;
        Tile = tile;
        grid.TryGetTilePosition(tile.Q, tile.R, out var position);
        Position = position;
        Facing = facing;
        Initiative = initiative;
        MaxTurnPoints = maxTurnPoints;
        TurnPoints = maxTurnPoints;
        Weapon = weapon;
        ViewRadius = viewRadius;
        MaxHp = maxHp;
        Hp = maxHp;
        _baseColor = baseColor;
        _activeColor = activeColor;

        // The hull is a box over the footprint's tiles in model space (bow along -Z), around the anchor at the origin,
        // so it turns with the ship exactly like the footprint does.
        Vector3 anchorWorld = grid.AxialToWorld((0, 0));
        Vector3 min = new Vector3(float.MaxValue), max = new Vector3(float.MinValue);
        foreach (var offset in footprint.Offsets)
        {
            Vector3 local = grid.AxialToWorld(offset) - anchorWorld;
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }

        float hw = (max.X - min.X) / 2f + grid.HexRadius * 0.45f;
        float hl = (max.Z - min.Z) / 2f + grid.HexRadius * (float)Math.Sqrt(3) * 0.4f;
        float height = grid.HexRadius * 0.45f;
        float cx = (min.X + max.X) / 2f;
        float cz = (min.Z + max.Z) / 2f;

        Vector3 p000 = new Vector3(cx - hw, 0f, cz - hl);
        Vector3 p100 = new Vector3(cx + hw, 0f, cz - hl);
        Vector3 p110 = new Vector3(cx + hw, 0f, cz + hl);
        Vector3 p010 = new Vector3(cx - hw, 0f, cz + hl);
        Vector3 p001 = new Vector3(cx - hw, height, cz - hl);
        Vector3 p101 = new Vector3(cx + hw, height, cz - hl);
        Vector3 p111 = new Vector3(cx + hw, height, cz + hl);
        Vector3 p011 = new Vector3(cx - hw, height, cz + hl);

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

    // True if any part of the hull sits on the tile, so it can be hit there.
    public bool Occupies((int Q, int R) tile)
    {
        return !IsSunk && Tiles.Contains(tile);
    }

    // Taking the last hit point sinks the ship and starts it going under.
    public void TakeDamage(int damage)
    {
        if (IsSunk)
            return;

        Hp = Math.Max(0, Hp - damage);
        if (IsSunk)
        {
            _sinkElapsed = 0f;
            _waypoints.Clear();
            IsMoving = false;
        }
    }

    // Each waypoint carries the tile, its world position and the direction of the step into it.
    public void MoveAlong(IEnumerable<((int Q, int R) Tile, Vector3 Position, HexDirection Facing)> waypoints)
    {
        _waypoints.Clear();
        foreach (var (tile, position, facing) in waypoints)
            _waypoints.Enqueue((tile, new Vector3(position.X, Position.Y, position.Z), facing));

        StartNextLeg();
    }

    private void StartNextLeg()
    {
        IsMoving = _waypoints.TryDequeue(out _target);
        if (IsMoving)
            Facing = _target.Facing;
    }

    public void Update(GameTime gameTime)
    {
        if (IsSinking)
        {
            _sinkElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            return;
        }

        if (!IsMoving)
            return;

        Vector3 toTarget = _target.Position - Position;
        float distance = toTarget.Length();
        float step = MoveSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (step >= distance || distance < 0.001f)
        {
            Position = _target.Position;
            Tile = _target.Tile;
            StartNextLeg();
        }
        else
        {
            Position += toTarget / distance * step;
        }
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        if (IsSunk && !IsSinking)
            return;

        // A sinking hull lists to starboard and dips its bow while it settles under the water, slowly at first.
        var sink = Matrix.Identity;
        if (IsSunk)
        {
            float t = _sinkElapsed / SinkDuration;
            sink = Matrix.CreateRotationZ(-SinkRoll * MathF.Sqrt(t)) * Matrix.CreateRotationX(-SinkPitch * t)
                * Matrix.CreateTranslation(0f, -SinkDepth * t * t, 0f);
        }

        effect.World = sink * Matrix.CreateRotationY(HexDirections.ToYaw(Facing)) * Matrix.CreateTranslation(Position);
        effect.LightingEnabled = true;
        effect.VertexColorEnabled = false;
        effect.TextureEnabled = false;
        effect.DiffuseColor = (IsActive ? _activeColor : _baseColor).ToVector3();

        graphicsDevice.SetVertexBuffer(_vertexBuffer);
        graphicsDevice.Indices = _indexBuffer;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
        }
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }
}
