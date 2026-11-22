namespace Arena.Core;

public sealed class EncounterDefinition
{
    public EncounterDefinition(TeamDefinition team, Difficulty difficulty, IEnumerable<CombatantSeed> combatants)
    {
        ArgumentNullException.ThrowIfNull(team);
        ArgumentNullException.ThrowIfNull(combatants);
        Team = team;
        Difficulty = difficulty;
        var seeds = combatants.ToArray();
        if (seeds.Length != 3 || seeds.Any(seed => seed.MaxHealth <= 0 || seed.Attack <= 0)
            || !seeds.Select(seed => seed.Hero.Id).Order().SequenceEqual(team.Members.Select(hero => hero.Id).Order()))
        {
            throw new ArgumentException("Початкові параметри мають відповідати трьом героям команди.", nameof(combatants));
        }
        Combatants = Array.AsReadOnly(seeds);
    }

    public TeamDefinition Team { get; }
    public Difficulty Difficulty { get; }
    public IReadOnlyList<CombatantSeed> Combatants { get; }
}
