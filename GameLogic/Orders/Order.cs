using Microsoft.Xna.Framework;
using NavalSault.Utils;

namespace NavalSault.GameLogic.Orders;

// Something a ship was told to do this turn. Orders are planned first and carried out when the turn resolves.
public abstract class Order
{
    protected Order(Ship ship)
    {
        Ship = ship;
    }

    public Ship Ship { get; }
    public abstract int TurnPointCost { get; }

    // Where the ship stands and faces once this order is done, so the next order is planned from there.
    public abstract (int Q, int R) EndTile { get; }
    public abstract HexDirection EndFacing { get; }

    public abstract void Begin();

    // Called every frame after Begin; returns true once the order has finished.
    public abstract bool Update(GameTime gameTime);
}
