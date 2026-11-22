namespace Arena.Core;

internal interface IBattleState
{
    BattlePhase Phase { get; }
    ActionResult UseSkill(Battle battle, string actorId, bool isSpecial, string? targetId);
    ActionResult StepEnemy(Battle battle);
}
