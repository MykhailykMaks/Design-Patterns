namespace Arena.Core;

public sealed class EncounterGenerator(Random? random = null)
{
    private readonly Random _random = random ?? new Random();

    public EncounterDefinition Generate(Difficulty difficulty, EncounterDefinition? previous = null)
    {
        var options = new List<(HeroDefinition Leader, HeroDefinition[] Members)>();
        foreach (var leader in HeroCatalog.EnemyHeroes.Where(hero => hero.Aura != AuraKind.None))
        {
            var others = HeroCatalog.EnemyHeroes.Where(hero => hero.Id != leader.Id).ToArray();
            for (var first = 0; first < others.Length; first++)
            {
                for (var second = first + 1; second < others.Length; second++)
                {
                    HeroDefinition[] members = [leader, others[first], others[second]];
                    var validRoles = members.Any(hero => hero.Role == HeroRole.Damage)
                        && members.Any(hero => hero.Role != HeroRole.Damage);
                    var repeats = previous is not null && members.Select(hero => hero.Id).Order()
                        .SequenceEqual(previous.Team.Members.Select(hero => hero.Id).Order());
                    if (validRoles && !repeats)
                    {
                        options.Add((leader, members));
                    }
                }
            }
        }
        var leaders = options.Select(option => option.Leader).Distinct().ToArray();
        var chosenLeader = leaders[_random.Next(leaders.Length)];
        var pairs = options.Where(option => option.Leader == chosenLeader).ToArray();
        var selected = pairs[_random.Next(pairs.Length)];
        var builder = new TeamBuilder();
        foreach (var hero in selected.Members)
        {
            builder.Add(hero);
        }
        var team = builder.SetLeader(chosenLeader.Id).Build();
        return new EncounterDefinition(team, difficulty, CombatantFactory.CreateSeeds(team, difficulty));
    }
}
