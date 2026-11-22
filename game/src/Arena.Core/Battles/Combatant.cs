namespace Arena.Core;

public sealed class Combatant
{
    internal Combatant(CombatantSeed seed, Side side)
    {
        Hero = seed.Hero;
        Side = side;
        Id = $"{side}:{seed.Hero.Id}";
        MaxHealth = seed.MaxHealth;
        Attack = seed.Attack;
        Health = MaxHealth;
    }

    public string Id { get; }
    public HeroDefinition Hero { get; }
    public Side Side { get; }
    public int MaxHealth { get; }
    public int Attack { get; }
    public int Health { get; private set; }
    public int Shield { get; private set; }
    public bool IsAlive => Health > 0;
    public bool HasActed { get; internal set; }
    public int ReadyOnRound { get; internal set; } = 1;
    public bool IsWeakened { get; internal set; }
    public bool IsEmpowered { get; internal set; }
    public bool IsStunned { get; internal set; }
    public bool HasStunProtection { get; internal set; }
    public int PoisonRounds => Poison?.Rounds ?? 0;
    internal PoisonStatus? Poison { get; set; }

    internal int TakeDamage(int damage)
    {
        var absorbed = Math.Min(Shield, damage);
        Shield -= absorbed;
        var lost = Math.Min(Health, damage - absorbed);
        Health -= lost;
        if (!IsAlive)
        {
            IsStunned = false;
            HasStunProtection = false;
            Poison = null;
            IsWeakened = false;
            IsEmpowered = false;
        }
        return lost;
    }

    internal int Heal(int amount)
    {
        var restored = Math.Min(MaxHealth - Health, amount);
        Health += restored;
        return restored;
    }

    internal void ApplyShield(int amount) => Shield = Math.Max(Shield, amount);

    internal CombatantSnapshot Capture() =>
        new(Id, Health, Shield, HasActed, ReadyOnRound, IsWeakened, IsEmpowered, Poison, IsStunned, HasStunProtection);

    internal void Restore(CombatantSnapshot snapshot)
    {
        Health = snapshot.Health;
        Shield = snapshot.Shield;
        HasActed = snapshot.HasActed;
        ReadyOnRound = snapshot.ReadyOnRound;
        IsWeakened = snapshot.IsWeakened;
        IsEmpowered = snapshot.IsEmpowered;
        Poison = snapshot.Poison;
        IsStunned = snapshot.IsStunned;
        HasStunProtection = snapshot.HasStunProtection;
    }
}
