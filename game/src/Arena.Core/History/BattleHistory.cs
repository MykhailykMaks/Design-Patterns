namespace Arena.Core;

internal sealed class BattleHistory
{
    internal BattleMemento? Latest { get; private set; }
    internal void Save(BattleMemento snapshot) => Latest = snapshot;
}
