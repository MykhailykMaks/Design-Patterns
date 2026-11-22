namespace Arena.Core;

internal sealed class FinishedState : IBattleState
{
    public BattlePhase Phase => BattlePhase.Finished;
    public ActionResult UseSkill(Battle battle, string actorId, bool isSpecial, string? targetId) =>
        ActionResult.Fail("Бій завершено.");
    public ActionResult StepEnemy(Battle battle) => ActionResult.Fail("Бій завершено.");
}
