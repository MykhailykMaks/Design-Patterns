namespace Arena.Core;

public enum BattleEventKind
{
    RoundStart, RoundEnd, Ability, Damage, Critical, Heal, Shield,
    Buff, Debuff, Periodic, Defeat, Skip, Result,
    StunSkip,
    StunProtectionEnded
}
