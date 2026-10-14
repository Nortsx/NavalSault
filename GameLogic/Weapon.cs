namespace NavalSault.GameLogic;

public class Weapon
{
    public string Name { get; }
    public int Damage { get; }
    // Range in hex tiles.
    public int Range { get; }
    // Turn points one shot costs.
    public int ShotCost { get; }
    // Every this many tiles of distance widens the area a shell can land in by one ring of tiles.
    public int SpreadStep { get; }

    public Weapon(string name, int damage, int range, int shotCost, int spreadStep)
    {
        Name = name;
        Damage = damage;
        Range = range;
        ShotCost = shotCost;
        SpreadStep = spreadStep;
    }

    // Radius in tiles, around the aimed-at tile, of the area a shell fired over this distance can land in.
    public int SpreadRadius(int distance)
    {
        return distance / SpreadStep;
    }
}
