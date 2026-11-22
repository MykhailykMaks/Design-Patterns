namespace Arena.Core;

public sealed class BattleFacade(Battle battle)
{
    public Battle Battle { get; } = battle ?? throw new ArgumentNullException(nameof(battle));
    public ActionResult UseSkill(string actorId, bool isSpecial, string? targetId) => Battle.UseSkill(actorId, isSpecial, targetId);
    public ActionResult StepEnemy() => Battle.StepEnemy();
    public ActionResult RestoreRound() => Battle.RestoreRound();
    public IReadOnlyList<Combatant> GetTargets(string actorId, bool isSpecial) => Battle.GetTargets(actorId, isSpecial);
    public bool CanUseSpecial(string actorId) => Battle.CanUseSpecial(actorId);
}
