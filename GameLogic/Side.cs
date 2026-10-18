namespace NavalSault.GameLogic;

// Who a ship fights for. Separate from who commands it: a ship with a Behaviour is run by the AI whatever its side.
public enum Side
{
    Player,
    Enemy
}
