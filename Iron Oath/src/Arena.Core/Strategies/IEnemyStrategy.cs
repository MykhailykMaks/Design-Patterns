namespace Arena.Core;

internal interface IEnemyStrategy
{
    EnemyIntent Plan(Battle battle, Combatant actor);
}
