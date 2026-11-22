namespace Arena.Core;

internal sealed class DamageStrategy : IEnemyStrategy
{
    public EnemyIntent Plan(Battle battle, Combatant actor)
    {
        if (!battle.CanUseSpecial(actor.Id))
        {
            return EnemyPlanning.Basic(battle, actor);
        }
        if (actor.Hero.SpecialSkill.Target == TargetKind.AllEnemies)
        {
            return EnemyPlanning.Intent(actor, true, null);
        }
        var target = EnemyPlanning.AttackTarget(battle, actor, true);
        return target is null ? EnemyPlanning.Basic(battle, actor) : EnemyPlanning.Intent(actor, true, target);
    }
}
