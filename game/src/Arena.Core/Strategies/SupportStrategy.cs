namespace Arena.Core;

internal sealed class SupportStrategy : IEnemyStrategy
{
    public EnemyIntent Plan(Battle battle, Combatant actor)
    {
        if (!battle.CanUseSpecial(actor.Id))
        {
            return EnemyPlanning.Basic(battle, actor);
        }
        var effects = actor.Hero.SpecialSkill.Effects;
        // A shield support switches to offense in a duel to avoid an indefinite shield exchange.
        if (effects.Any(effect => effect.Kind == EffectKind.Shield)
            && battle.Enemies.Count(unit => unit.IsAlive) == 1 && battle.Players.Count(unit => unit.IsAlive) == 1)
        {
            return EnemyPlanning.Basic(battle, actor);
        }
        var targets = battle.GetTargets(actor.Id, true);
        var heals = effects.Any(effect => effect.Kind == EffectKind.Heal);
        var target = heals
            ? targets.OrderByDescending(unit => unit.MaxHealth - unit.Health).FirstOrDefault()
            : targets.OrderBy(unit => unit.Health).FirstOrDefault();
        return target is null ? EnemyPlanning.Basic(battle, actor) : EnemyPlanning.Intent(actor, true, target);
    }
}
