namespace Arena.Core;

public sealed record EnemyIntent(string ActorId, bool IsSpecial, string? TargetId, string Description);
