namespace Arena.Core;

public sealed class TeamBuilder
{
    private readonly List<HeroDefinition> _members = [];
    private string? _leaderId;

    public TeamBuilder Add(HeroDefinition hero)
    {
        ArgumentNullException.ThrowIfNull(hero);
        if (_members.Count == 3 || _members.Any(member => member.Id == hero.Id))
        {
            throw new InvalidOperationException("Потрібно обрати трьох різних героїв.");
        }
        _members.Add(hero);
        return this;
    }

    public TeamBuilder Remove(string id)
    {
        _members.RemoveAll(hero => hero.Id == id);
        if (_leaderId == id)
        {
            _leaderId = null;
        }
        return this;
    }

    public TeamBuilder SetLeader(string id)
    {
        if (!_members.Any(hero => hero.Id == id && hero.Aura != AuraKind.None))
        {
            throw new InvalidOperationException("Лідером має бути обраний герой з аурою.");
        }
        _leaderId = id;
        return this;
    }

    public TeamDefinition Build()
    {
        if (_members.Count != 3)
        {
            throw new InvalidOperationException("Оберіть рівно трьох героїв.");
        }
        var leaders = _members.Where(hero => hero.Aura != AuraKind.None).ToArray();
        var leaderId = _leaderId ?? (leaders.Length == 1 ? leaders[0].Id : null);
        if (leaderId is null)
        {
            throw new InvalidOperationException("Призначте одного лідера з аурою.");
        }
        return new TeamDefinition(_members, leaderId);
    }
}
