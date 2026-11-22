namespace Arena.Core;

public sealed record SkillDefinition(
    string Id, string Name, string Description, TargetKind Target,
    IReadOnlyList<SkillEffect> Effects, int CooldownRounds = 1);
