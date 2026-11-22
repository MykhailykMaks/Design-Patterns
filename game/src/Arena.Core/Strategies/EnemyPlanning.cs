namespace Arena.Core;

internal static class EnemyPlanning
{
    internal static EnemyIntent Basic(Battle battle, Combatant actor)
    {
        var target = actor.Hero.BasicSkill.Target == TargetKind.AllEnemies ? null : AttackTarget(battle, actor, false);
        return Intent(actor, false, target);
    }

    internal static Combatant? AttackTarget(Battle battle, Combatant actor, bool special)
    {
        var targets = battle.GetTargets(actor.Id, special);
        return targets.MinBy(target => battle.Difficulty switch
        {
            Difficulty.Normal => (long)target.Health,
            Difficulty.Hard => (long)target.Health + target.Shield,
            // A healer within one basic hit of the weakest target is worth pressuring.
            Difficulty.Nightmare => (long)target.Health + target.Shield
                - (target.Hero.Role == HeroRole.Support ? actor.Attack : 0),
            _ => throw new ArgumentOutOfRangeException(nameof(battle.Difficulty))
        });
    }

    internal static EnemyIntent Intent(Combatant actor, bool isSpecial, Combatant? target)
    {
        var skill = isSpecial ? actor.Hero.SpecialSkill : actor.Hero.BasicSkill;
        var targetName = skill.Target == TargetKind.AllEnemies ? "всю команду" : target?.Hero.Name ?? "немає цілі";
        return new EnemyIntent(actor.Id, isSpecial, target?.Id, $"{skill.Name} → {targetName}");
    }
}
