namespace Arena.Core;

public sealed record BattleEvent(BattleEventKind Kind, int Round, string Message,
    string? ActorId = null, string? TargetId = null, int Amount = 0, string? SkillName = null)
{
    public EffectKind? Effect { get; init; }
    public int Absorbed { get; init; }
    public int? TargetHealth { get; init; }
    public int? TargetShield { get; init; }
    public int? TargetPoisonRounds { get; init; }
    public bool? TargetIsWeakened { get; init; }
    public bool? TargetIsEmpowered { get; init; }
    public bool? TargetIsStunned { get; init; }
    public bool? TargetHasStunProtection { get; init; }
}
