namespace Arena.Core;

internal sealed class EnemyPlanner
{
    private readonly IReadOnlyDictionary<HeroRole, IEnemyStrategy> _strategies =
        new Dictionary<HeroRole, IEnemyStrategy>
        {
            [HeroRole.Damage] = new DamageStrategy(),
            [HeroRole.Support] = new SupportStrategy(),
            [HeroRole.Defender] = new SupportStrategy(),
            [HeroRole.Control] = new ControlStrategy()
        };

    internal EnemyIntent Plan(Battle battle, Combatant actor)
    {
        if (actor.IsStunned)
        {
            return new EnemyIntent(actor.Id, false, null, "Пропуск через оглушення");
        }
        if (actor.Hero.SpecialSkill.Target == TargetKind.AllEnemies)
        {
            return _strategies[HeroRole.Damage].Plan(battle, actor);
        }
        return _strategies[actor.Hero.Role].Plan(battle, actor);
    }
}
