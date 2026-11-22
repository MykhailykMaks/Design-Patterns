using System.Collections;

namespace Arena.Core;

internal sealed class BattleLog(IReadOnlyList<BattleEvent> events) : IReadOnlyList<string>
{
    public int Count => events.Count;
    public string this[int index] => events[index].Message;
    public IEnumerator<string> GetEnumerator() => events.Select(entry => entry.Message).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
