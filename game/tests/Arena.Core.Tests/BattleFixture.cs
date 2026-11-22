using Arena.Core;

namespace Arena.Core.Tests;

internal static class BattleFixture
{
    public static SkillDefinition Skill(TargetKind target, params SkillEffect[] effects) =>
        new("test-skill", "Перевірна навичка", "Навичка для перевірки бойових правил.", target, effects);

    public static HeroDefinition Hero(string id, int health = 500, int attack = 10,
        SkillDefinition? skill = null, AuraKind aura = AuraKind.None,
        HeroRole role = HeroRole.Damage) =>
        new(id, id, role, health, attack,
            skill ?? Skill(TargetKind.Enemy, new SkillEffect(EffectKind.Damage, 1.8)), aura);

    public static TeamDefinition Team(params HeroDefinition[] heroes)
    {
        var builder = new TeamBuilder();
        foreach (var hero in heroes)
        {
            builder.Add(hero);
        }

        builder.SetLeader(heroes.First(hero => hero.Aura != AuraKind.None).Id);
        return builder.Build();
    }

    public static TeamDefinition Players(int health = 500, int attack = 10) => Team(
        Hero("p-one", health, attack, aura: AuraKind.Attack),
        Hero("p-two", health, attack), Hero("p-three", health, attack));

    public static Battle Create(TeamDefinition? players = null, int seed = 42) =>
        new(players ?? Players(), new EncounterGenerator(new Random(seed)).Generate(Difficulty.Normal));

    public static EncounterDefinition Enemies(int health = 500, int attack = 10,
        params HeroDefinition[] heroes)
    {
        if (heroes.Length == 0)
        {
            heroes = [Hero("e-one", aura: AuraKind.Attack), Hero("e-two"), Hero("e-three")];
        }

        var team = Team(heroes);
        return new EncounterDefinition(team, Difficulty.Normal,
            heroes.Select(hero => new CombatantSeed(hero, health, attack)));
    }

    public static Combatant Player(Battle battle, string id) => battle.Players.Single(hero => hero.Hero.Id == id);

    public static void FinishPlayerTurn(BattleFacade facade)
    {
        foreach (var hero in facade.Battle.Players.Where(hero => hero.IsAlive && !hero.HasActed).ToArray())
        {
            if (facade.Battle.Phase != BattlePhase.PlayerTurn)
            {
                break;
            }

            var target = facade.GetTargets(hero.Id, false).First();
            Check.Succeeds(facade.UseSkill(hero.Id, false, target.Id));
        }
    }

    public static void FinishEnemyTurn(BattleFacade facade)
    {
        for (var steps = 0; facade.Battle.Phase == BattlePhase.EnemyTurn; steps++)
        {
            Check.True(steps < 6, "Ворожа черга не завершилася за два раунди.");
            Check.Succeeds(facade.StepEnemy());
        }
    }

    public static void Complete(Battle battle)
    {
        var facade = new BattleFacade(battle);
        for (var rounds = 0; battle.Result == BattleResult.InProgress; rounds++)
        {
            Check.True(rounds < 250, "Бій не завершився за 250 раундів.");
            FinishPlayerTurn(facade);
            FinishEnemyTurn(facade);
        }
    }

    public static string TeamKey(TeamDefinition team) =>
        string.Join(",", team.Members.Select(hero => hero.Id).Order(StringComparer.Ordinal));

    public static string EncounterState(EncounterDefinition encounter) =>
        $"{TeamKey(encounter.Team)};{encounter.Team.LeaderId};{encounter.Difficulty};" +
        string.Join("|", encounter.Combatants.Select(hero => $"{hero.Hero.Id}:{hero.MaxHealth}:{hero.Attack}"));

    public static string State(Battle battle) =>
        $"{battle.Round};{battle.Phase};{battle.Result};{battle.CanRestore};" +
        string.Join("|", battle.Players.Concat(battle.Enemies).Select(hero =>
            $"{hero.Id}:{hero.MaxHealth}:{hero.Attack}:{hero.Health}:{hero.Shield}:{hero.HasActed}:" +
            $"{hero.ReadyOnRound}:{hero.IsWeakened}:{hero.IsEmpowered}:{hero.PoisonRounds}:{hero.IsStunned}:{hero.HasStunProtection}")) +
        ";" + string.Join("|", battle.Intents.Select(intent =>
            $"{intent.ActorId}:{intent.IsSpecial}:{intent.TargetId}:{intent.Description}")) +
        ";" + string.Join("|", battle.Log) +
        ";" + string.Join("|", battle.Events);
}
