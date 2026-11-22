namespace Arena.Core;

public sealed record SkillEffect(EffectKind Kind, double Multiplier = 0, double PoisonedTargetBonus = 0,
    int ChancePercent = 100)
{
    public int ChancePercent { get; } = ChancePercent is >= 0 and <= 100
        ? ChancePercent
        : throw new ArgumentOutOfRangeException(nameof(ChancePercent));
}
