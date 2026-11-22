namespace Arena.Core;

internal sealed class ControlStrategy : IEnemyStrategy
{
    public EnemyIntent Plan(Battle battle, Combatant actor)
    {
        if (!battle.CanUseSpecial(actor.Id))
        {
            return EnemyPlanning.Basic(battle, actor);
        }
        var targets = battle.GetTargets(actor.Id, true);
        var empowers = actor.Hero.SpecialSkill.Effects.Any(effect => effect.Kind == EffectKind.Empower);
        var target = targets.Where(unit => !empowers || unit.Hero.Role == HeroRole.Damage)
            .OrderByDescending(unit => unit.Attack).FirstOrDefault();
        return target is null ? EnemyPlanning.Basic(battle, actor) : EnemyPlanning.Intent(actor, true, target);
    }
}
