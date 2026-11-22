namespace Arena.Core;

public sealed class TeamDefinition
{
    internal TeamDefinition(IEnumerable<HeroDefinition> members, string leaderId)
    {
        Members = Array.AsReadOnly(members.ToArray());
        LeaderId = leaderId;
    }

    public IReadOnlyList<HeroDefinition> Members { get; }
    public string LeaderId { get; }
    public AuraKind Aura => Members.Single(hero => hero.Id == LeaderId).Aura;
}
