namespace Arena.Core;

public sealed class BattleMemento
{
    internal BattleMemento(Guid ownerId, int round, BattlePhase phase, BattleResult result,
        int enemyIndex, IEnumerable<CombatantSnapshot> combatants,
        IEnumerable<EnemyIntent> intents, IEnumerable<BattleEvent> events, uint randomState,
        BattleMemento? roundStart = null)
    {
        OwnerId = ownerId;
        Round = round;
        Phase = phase;
        Result = result;
        EnemyIndex = enemyIndex;
        Combatants = combatants.ToArray();
        Intents = intents.ToArray();
        Events = events.ToArray();
        RandomState = randomState;
        RoundStart = roundStart;
    }

    internal Guid OwnerId { get; }
    internal int Round { get; }
    internal BattlePhase Phase { get; }
    internal BattleResult Result { get; }
    internal int EnemyIndex { get; }
    internal CombatantSnapshot[] Combatants { get; }
    internal EnemyIntent[] Intents { get; }
    internal BattleEvent[] Events { get; }
    internal uint RandomState { get; }
    internal BattleMemento? RoundStart { get; }
}
