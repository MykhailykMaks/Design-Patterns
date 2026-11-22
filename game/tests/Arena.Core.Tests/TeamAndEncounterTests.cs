using Arena.Core;

namespace Arena.Core.Tests;

internal static class TeamAndEncounterTests
{
    public static void CatalogHasRequiredRolesAndAuras()
    {
        Check.Equal(5, HeroCatalog.PlayerHeroes.Count, "Каталог союзників.");
        Check.Equal(7, HeroCatalog.EnemyHeroes.Count, "Каталог ворогів.");
        Check.Equal(2, HeroCatalog.PlayerHeroes.Count(hero => hero.Aura != AuraKind.None), "Аури союзників.");
        Check.Equal(3, HeroCatalog.EnemyHeroes.Count(hero => hero.Aura != AuraKind.None), "Аури ворогів.");
        Check.Equal(3, HeroCatalog.EnemyHeroes.Count(hero => hero.Role == HeroRole.Damage), "Бійці ворога.");
        Check.Equal(2, HeroCatalog.EnemyHeroes.Count(hero => hero.Role == HeroRole.Support), "Підтримка ворога.");
        Check.Equal(2, HeroCatalog.EnemyHeroes.Count(hero => hero.Role == HeroRole.Control), "Контроль ворога.");
        var all = HeroCatalog.PlayerHeroes.Concat(HeroCatalog.EnemyHeroes).ToArray();
        Check.Equal(12, all.Select(hero => hero.Id).Distinct().Count(), "Ідентифікатори каталогу унікальні.");
        foreach (var hero in all)
        {
            Check.Equal(hero, HeroCatalog.Get(hero.Id), "Пошук профілю в каталозі.");
            Check.True(hero.MaxHealth > 0 && hero.Attack > 0, "Додатні базові характеристики.");
        }
    }

    public static void BuilderRejectsIncompleteDuplicateAndAuralessTeams()
    {
        var a = BattleFixture.Hero("a", aura: AuraKind.Attack);
        var b = BattleFixture.Hero("b");
        Check.Throws<InvalidOperationException>(() => new TeamBuilder().Add(a).Add(b).Build());
        Check.Throws<InvalidOperationException>(() => new TeamBuilder().Add(a).Add(a).Add(b).Build());
        Check.Throws<InvalidOperationException>(() => new TeamBuilder().Add(b)
            .Add(BattleFixture.Hero("c")).Add(BattleFixture.Hero("d")).Build());
    }

    public static void BuilderRequiresExplicitChoiceForTwoAuras()
    {
        var a = BattleFixture.Hero("a", aura: AuraKind.Attack);
        var b = BattleFixture.Hero("b", aura: AuraKind.Health);
        var c = BattleFixture.Hero("c");
        var builder = new TeamBuilder().Add(a).Add(b).Add(c);
        Check.Throws<InvalidOperationException>(() => builder.Build());
        Check.Equal("b", builder.SetLeader("b").Build().LeaderId, "Явно вибраний лідер.");
        Check.Throws<InvalidOperationException>(() => builder.SetLeader("c").Build());
    }

    public static void BuilderAutoSelectsOnlyAuraAndKeepsBuiltTeamIndependent()
    {
        var a = BattleFixture.Hero("a", aura: AuraKind.Attack);
        var b = BattleFixture.Hero("b");
        var c = BattleFixture.Hero("c");
        var builder = new TeamBuilder().Add(a).Add(b).Add(c);
        var built = builder.Build();
        Check.Equal("a", built.LeaderId, "Єдиний носій аури автоматично стає лідером.");
        builder.Remove("c").Add(BattleFixture.Hero("d"));
        Check.Equal("a,b,c", BattleFixture.TeamKey(built), "Готовий загін не змінюється разом із Builder.");
        Check.Equal("a,b,d", BattleFixture.TeamKey(builder.Build()), "Builder дозволяє створити новий загін.");
        if (built.Members is IList<HeroDefinition> list)
        {
            Check.True(list.IsReadOnly, "Колекцію готового загону не можна редагувати через IList.");
        }
    }

    public static void OnlyLeaderAuraAppliesOnce()
    {
        var attacker = BattleFixture.Hero("a", 100, 20, aura: AuraKind.Attack);
        var defender = BattleFixture.Hero("b", 100, 20, aura: AuraKind.Health);
        var third = BattleFixture.Hero("c", 100, 20);
        var builder = new TeamBuilder().Add(attacker).Add(defender).Add(third);
        var attackBattle = BattleFixture.Create(builder.SetLeader("a").Build());
        var healthBattle = BattleFixture.Create(builder.SetLeader("b").Build());
        foreach (var hero in attackBattle.Players)
        {
            Check.Equal(23, hero.Attack, "Аура атаки +15%.");
            Check.Equal(100, hero.MaxHealth, "Неактивна аура здоров'я не застосовується.");
        }

        foreach (var hero in healthBattle.Players)
        {
            Check.Equal(20, hero.Attack, "Неактивна аура атаки не застосовується.");
            Check.Equal(120, hero.MaxHealth, "Аура здоров'я +20%.");
            Check.Equal(120, hero.Health, "Початкове здоров'я дорівнює збільшеному максимуму.");
        }
    }

    public static void GeneratedTeamsAlwaysSatisfyRoleConstraints()
    {
        var generator = new EncounterGenerator(new Random(173));
        EncounterDefinition? previous = null;
        for (var index = 0; index < 350; index++)
        {
            var encounter = generator.Generate((Difficulty)(index % 3), previous);
            var team = encounter.Team;
            Check.Equal(3, team.Members.Select(hero => hero.Id).Distinct().Count(), "Унікальна трійка ворогів.");
            Check.True(team.Members.All(hero => HeroCatalog.EnemyHeroes.Any(profile => profile.Id == hero.Id)),
                "Вороги походять із ворожого каталогу.");
            Check.True(team.Members.Single(hero => hero.Id == team.LeaderId).Aura != AuraKind.None,
                "Лідер ворогів має ауру.");
            Check.True(team.Members.Any(hero => hero.Role == HeroRole.Damage), "У трійці є боєць.");
            Check.True(team.Members.Any(hero => hero.Role is HeroRole.Support or HeroRole.Control),
                "У трійці є підтримка або контроль.");
            if (previous is not null)
            {
                Check.True(BattleFixture.TeamKey(previous.Team) != BattleFixture.TeamKey(team),
                    "Попередня неупорядкована трійка не повторюється.");
            }

            previous = encounter;
        }
    }

    public static void SeededGenerationIsReproducible()
    {
        var first = new EncounterGenerator(new Random(2026));
        var second = new EncounterGenerator(new Random(2026));
        EncounterDefinition? firstPrevious = null;
        EncounterDefinition? secondPrevious = null;
        for (var index = 0; index < 20; index++)
        {
            firstPrevious = first.Generate(Difficulty.Hard, firstPrevious);
            secondPrevious = second.Generate(Difficulty.Hard, secondPrevious);
            Check.Equal(BattleFixture.EncounterState(firstPrevious), BattleFixture.EncounterState(secondPrevious),
                "Фіксоване зерно відтворює зустрічі.");
        }
    }

    public static void DifficultyThenSingleAuraUsesSpecifiedRounding()
    {
        foreach (var difficulty in Enum.GetValues<Difficulty>())
        {
            var generator = new EncounterGenerator(new Random(19));
            for (var index = 0; index < 25; index++)
            {
                var encounter = generator.Generate(difficulty);
                var aura = encounter.Team.Members.Single(hero => hero.Id == encounter.Team.LeaderId).Aura;
                foreach (var hero in encounter.Combatants)
                {
                    var health = Round(hero.Hero.MaxHealth * (difficulty switch
                    {
                        Difficulty.Hard => 1.25,
                        Difficulty.Nightmare => 1.5,
                        _ => 1.0
                    }));
                    var attack = Round(hero.Hero.Attack * (difficulty switch
                    {
                        Difficulty.Hard => 1.15,
                        Difficulty.Nightmare => 1.3,
                        _ => 1.0
                    }));
                    Check.Equal(aura == AuraKind.Health ? Round(health * 1.20) : health,
                        hero.MaxHealth, "Здоров'я: складність, округлення, одна аура.");
                    Check.Equal(aura == AuraKind.Attack ? Round(attack * 1.15) : attack,
                        hero.Attack, "Атака: складність, округлення, одна аура.");
                }

                var battle = new Battle(BattleFixture.Players(), encounter);
                Check.Equal(string.Join(",", encounter.Combatants.Select(hero => $"{hero.MaxHealth}/{hero.Attack}")),
                    string.Join(",", battle.Enemies.Select(hero => $"{hero.MaxHealth}/{hero.Attack}")),
                    "Battle використовує остаточні параметри зустрічі без повторного множення.");
            }
        }
    }

    private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
