using System;

namespace NavalSault.GameLogic;

public class Weapon
{
    public string Name { get; }
    public int Damage { get; }
    // Range in hex tiles.
    public int Range { get; }
    // Turn points one shot costs.
    public int ShotCost { get; }
    // Rings of tiles the area a shell can land in widens by per tile of distance, e.g. 0.2 adds a ring every 5 tiles.
    public float SpreadPerTile { get; }

    public Weapon(string name, int damage, int range, int shotCost, float spreadPerTile)
    {
        Name = name;
        Damage = damage;
        Range = range;
        ShotCost = shotCost;
        SpreadPerTile = spreadPerTile;
    }

    // Radius in tiles, around the aimed-at tile, of the area a shell fired over this distance can land in.
    public int SpreadRadius(int distance)
    {
        // The small nudge keeps float error from dropping e.g. 5 * 0.2 just under a whole ring.
        return (int)MathF.Floor(distance * SpreadPerTile + 0.0001f);
    }
}
