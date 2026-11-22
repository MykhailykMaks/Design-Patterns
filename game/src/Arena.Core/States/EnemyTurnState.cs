namespace Arena.Core;

internal sealed class EnemyTurnState : IBattleState
{
    public BattlePhase Phase => BattlePhase.EnemyTurn;
    public ActionResult UseSkill(Battle battle, string actorId, bool isSpecial, string? targetId) =>
        ActionResult.Fail("Дочекайтеся завершення ходу противників.");
    public ActionResult StepEnemy(Battle battle) => battle.ApplyEnemyStep();
}
