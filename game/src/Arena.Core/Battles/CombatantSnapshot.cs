namespace Arena.Core;

internal sealed record CombatantSnapshot(string Id, int Health, int Shield,
    bool HasActed, int ReadyOnRound, bool IsWeakened, bool IsEmpowered, PoisonStatus? Poison,
    bool IsStunned, bool HasStunProtection);
