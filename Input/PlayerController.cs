using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NavalSault.GameLogic;
using NavalSault.GameLogic.Orders;
using NavalSault.Utils;

namespace NavalSault.Input;

// Turns the player's keys, clicks and the action buttons into orders for the active ship,
// and keeps what the hovered tile would do as a preview for the renderer.
public class PlayerController
{
    private enum ActionMode
    {
        Move,
        Shoot
    }

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Camera _camera;
    private readonly HexGrid _grid;
    private readonly TurnManager _turns;

    private readonly Button _moveButton = new("Move");
    private readonly Button _shootButton = new("Shoot");
    private readonly Button _endTurnButton = new("End Turn");
    private readonly Button[] _buttons;

    private MouseState _previousMouseState;
    private KeyboardState _previousKeyboardState;
    // End facing picked with Q/E for the next move; null lets the pathfinder choose.
    private HexDirection? _desiredFacing;
    // What a click on a tile does for the active ship.
    private ActionMode _mode;

    public (int Q, int R)? HoveredTile { get; private set; }
    // Path the active ship would sail to the hovered tile, or null.
    public List<(int Q, int R)> PreviewPath { get; private set; }
    // Direction the active ship would face at the end of the previewed path.
    public HexDirection PreviewFacing { get; private set; }
    // Shot the active ship would fire at the hovered tile while aiming, or null.
    public ShootOrder PreviewShot { get; private set; }

    public IReadOnlyList<Button> Buttons => _buttons;
    // Buttons only matter while a ship is taking orders.
    public bool ButtonsVisible => _turns.Phase == TurnPhase.Planning;

    public PlayerController(GraphicsDevice graphicsDevice, Camera camera, HexGrid grid, TurnManager turns)
    {
        _graphicsDevice = graphicsDevice;
        _camera = camera;
        _grid = grid;
        _turns = turns;
        _buttons = [_moveButton, _shootButton, _endTurnButton];
        _previousMouseState = Mouse.GetState();
        _previousKeyboardState = Keyboard.GetState();
    }

    public void Update(KeyboardState keyboard, MouseState mouse)
    {
        if (WasPressed(keyboard, Keys.M))
            _mode = ActionMode.Move;
        if (WasPressed(keyboard, Keys.F))
            _mode = ActionMode.Shoot;
        if (WasPressed(keyboard, Keys.Enter))
            EndActiveShipTurn();
        if (WasPressed(keyboard, Keys.Back))
        {
            _turns.UndoLastOrder();
            _desiredFacing = null;
        }
        if (WasPressed(keyboard, Keys.Q))
            RotateDesiredFacing(-1);
        if (WasPressed(keyboard, Keys.E))
            RotateDesiredFacing(1);
        _previousKeyboardState = keyboard;

        var mousePosition = new Vector2(mouse.X, mouse.Y);
        bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

        if (mouse.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
        {
            _mode = ActionMode.Move;
            _desiredFacing = null;
        }

        UpdateButtons();
        bool overButton = ButtonsVisible && Array.Exists(_buttons, b => b.Contains(mouse.Position));

        if (leftClicked && _moveButton.IsEnabled && _moveButton.Contains(mouse.Position))
            _mode = ActionMode.Move;
        if (leftClicked && _shootButton.IsEnabled && _shootButton.Contains(mouse.Position))
            _mode = ActionMode.Shoot;
        if (leftClicked && _endTurnButton.IsEnabled && _endTurnButton.Contains(mouse.Position))
            EndActiveShipTurn();
        // Switching mode by key or click may have picked one the ship can't use.
        UpdateButtons();
        if (_mode != ActionMode.Move)
            _desiredFacing = null;

        // The buttons sit on top of the map, so the map ignores the mouse while it's over them.
        if (overButton)
        {
            HoveredTile = null;
            PreviewPath = null;
            PreviewShot = null;
        }
        else
        {
            UpdateHover(mousePosition);
        }

        if (leftClicked && !overButton && HoveredTile.HasValue)
        {
            if (_mode == ActionMode.Move)
            {
                if (_turns.TryAddMove(HoveredTile.Value, _desiredFacing))
                    _desiredFacing = null;
            }
            else if (_turns.TryAddShot(HoveredTile.Value))
                _mode = ActionMode.Move;
        }
        _previousMouseState = mouse;
    }

    private bool WasPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
    }

    // The active ship is done taking orders; the next one takes over, or the turn resolves after the last.
    private void EndActiveShipTurn()
    {
        _turns.FinishActiveShip();
        _mode = ActionMode.Move;
        _desiredFacing = null;
    }

    // Turns the end facing for the next move by 60° steps, starting from the facing the
    // previewed path would end with, or from where the ship faces if nothing is previewed.
    private void RotateDesiredFacing(int steps)
    {
        var ship = _turns.ActiveShip;
        if (ship == null || _mode != ActionMode.Move)
            return;

        var current = _desiredFacing ?? (PreviewPath != null ? PreviewFacing : ship.PlannedFacing);
        _desiredFacing = HexDirections.Turn(current, steps);
    }

    // Lays the mode buttons out along the bottom centre and End Turn at the bottom right,
    // and enables only what the active ship can do. Falls back to Move if the ship can't afford a shot.
    private void UpdateButtons()
    {
        const int width = 160, height = 44, gap = 12, margin = 20;

        var viewport = _graphicsDevice.Viewport;
        int left = viewport.Width / 2 - width - gap / 2;
        int top = viewport.Height - height - margin;
        _moveButton.Bounds = new Rectangle(left, top, width, height);
        _shootButton.Bounds = new Rectangle(left + width + gap, top, width, height);
        _endTurnButton.Bounds = new Rectangle(viewport.Width - width - margin, top, width, height);

        var ship = _turns.ActiveShip;
        _moveButton.IsEnabled = ship != null;
        _endTurnButton.IsEnabled = ship != null;
        _shootButton.IsEnabled = ship?.Weapon != null && ship.TurnPoints >= ship.Weapon.ShotCost;
        _shootButton.Label = ship?.Weapon != null ? $"Shoot ({ship.Weapon.ShotCost} TP)" : "Shoot";

        if (!_shootButton.IsEnabled)
            _mode = ActionMode.Move;

        _moveButton.IsSelected = _moveButton.IsEnabled && _mode == ActionMode.Move;
        _shootButton.IsSelected = _shootButton.IsEnabled && _mode == ActionMode.Shoot;
    }

    // Highlights the hovered tile and previews the active ship's path to it, if it can get there,
    // or, while aiming, the shot at it and where the shell may land.
    private void UpdateHover(Vector2 mousePosition)
    {
        HoveredTile = null;
        PreviewPath = null;
        PreviewShot = null;

        var picked = _camera.PickPlane(_graphicsDevice.Viewport, mousePosition, _grid.TileY);
        if (picked == null)
            return;

        var tile = _grid.WorldToAxial(picked.Value);
        if (_mode == ActionMode.Shoot)
        {
            PreviewShot = _turns.PlanShot(tile);
            if (PreviewShot != null)
                HoveredTile = tile;
            return;
        }

        PreviewPath = _turns.PlanMove(tile, _desiredFacing);
        if (PreviewPath != null)
        {
            HoveredTile = tile;
            PreviewFacing = HexDirections.Between(PreviewPath[^2], PreviewPath[^1]);
        }
    }
}
