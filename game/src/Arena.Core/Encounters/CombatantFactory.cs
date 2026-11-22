namespace Arena.Core;

public static class CombatantFactory
{
    public static IReadOnlyList<CombatantSeed> CreateSeeds(TeamDefinition team, Difficulty difficulty = Difficulty.Normal)
    {
        var profile = DifficultyProfile.For(difficulty);
        return Array.AsReadOnly(team.Members.Select(hero =>
        {
            var health = Scale(hero.MaxHealth, profile.HealthMultiplier);
            var attack = Scale(hero.Attack, profile.AttackMultiplier);
            if (team.Aura == AuraKind.Health)
            {
                health = Scale(health, 1.20);
            }
            if (team.Aura == AuraKind.Attack)
            {
                attack = Scale(attack, 1.15);
            }
            return new CombatantSeed(hero, health, attack);
        }).ToArray());
    }

    internal static int Scale(int value, double multiplier) =>
        Math.Max(1, (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero));
}
