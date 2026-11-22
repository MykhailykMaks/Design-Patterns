namespace Arena.Core;

public sealed record DifficultyProfile(string Name, double HealthMultiplier, double AttackMultiplier)
{
    public static DifficultyProfile For(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Normal => new("First Blood", 1, 1),
        Difficulty.Hard => new("Trial by Fire", 1.25, 1.15),
        Difficulty.Nightmare => new("Against All Odds", 1.5, 1.3),
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
    };
}
