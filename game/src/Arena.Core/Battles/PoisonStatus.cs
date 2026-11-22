namespace Arena.Core;

internal sealed record PoisonStatus(string ActorId, string SkillName, int Rounds)
{
    internal const int Duration = 2;
    internal const int Damage = 6;
    internal const double AttackPenalty = .20;
}
