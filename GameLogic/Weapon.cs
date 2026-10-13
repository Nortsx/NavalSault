namespace NavalSault.GameLogic;

public class Weapon
{
    public string Name { get; }
    public int Damage { get; }
    // Range in hex tiles.
    public int Range { get; }

    public Weapon(string name, int damage, int range)
    {
        Name = name;
        Damage = damage;
        Range = range;
    }
}
