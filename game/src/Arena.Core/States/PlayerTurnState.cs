namespace Arena.Core;

internal sealed class PlayerTurnState : IBattleState
{
    public BattlePhase Phase => BattlePhase.PlayerTurn;
    public ActionResult UseSkill(Battle battle, string actorId, bool isSpecial, string? targetId) =>
        battle.ApplyPlayerAction(actorId, isSpecial, targetId);
    public ActionResult StepEnemy(Battle battle) => ActionResult.Fail("Зараз хід вашої команди.");
}
