using Arena.Core;

namespace Arena.Core.Tests;

internal static class BattleSimulationTests
{
    public static void LastShieldSupportAttacksInDuel()
    {
        var sweep = BattleFixture.Skill(TargetKind.AllEnemies, new SkillEffect(EffectKind.Damage, 2));
        var team = BattleFixture.Team(BattleFixture.Hero("лідер", health: 5000, attack: 30, skill: sweep, aura: AuraKind.Health),
            BattleFixture.Hero("другий", health: 1, attack: 1), BattleFixture.Hero("третій", health: 1, attack: 1));
        var keeper = HeroCatalog.Get("seal-keeper");
        var attacker = BattleFixture.Hero("стрілець", skill: sweep, aura: AuraKind.Attack);
        var third = BattleFixture.Hero("третій");
        var enemies = new EncounterDefinition(BattleFixture.Team(keeper, attacker, third), Difficulty.Normal,
            [new CombatantSeed(keeper, 500, 10), new CombatantSeed(attacker, 5, 10), new CombatantSeed(third, 5, 1)]);
        var battle = new Battle(team, enemies, seed: 33);
        var facade = new BattleFacade(battle);
        var guard = battle.Enemies[0];
        Check.True(battle.Intents.Single(i => i.ActorId == guard.Id).IsSpecial, "За наявності загону Хранитель ставить щит.");
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        var survivor = battle.Players.Single(p => p.IsAlive);
        Check.Succeeds(battle.UseSkill(survivor.Id, true, null));
        Check.Equal(guard.Id, battle.Enemies.Single(p => p.IsAlive).Id, "Після масової атаки лишився сам Хранитель.");
        BattleFixture.FinishEnemyTurn(facade);
        Check.True(battle.CanUseSpecial(guard.Id), "На третій раунд щит уже готовий.");
        var intent = battle.Intents.Single();
        Check.True(!intent.IsSpecial && intent.TargetId == survivor.Id, "В одиночній дуелі Хранитель оголошує атаку по супернику.");
    }

    public static void DifficultyChangesTargetChoiceAndKeepsAnnouncedIntent()
    {
        foreach (var difficulty in Enum.GetValues<Difficulty>())
        {
            var shield = BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Shield, 4));
            var team = BattleFixture.Team(BattleFixture.Hero("перший", health: 100, attack: 20, skill: shield, aura: AuraKind.Attack),
                BattleFixture.Hero("другий", health: 110, attack: 20, skill: shield),
                BattleFixture.Hero("цілитель", health: 115, role: HeroRole.Support));
            var empower = BattleFixture.Skill(TargetKind.Self, new SkillEffect(EffectKind.Empower));
            var enemy = BattleFixture.Hero("ворог", skill: empower, aura: AuraKind.Attack);
            var encounter = BattleFixture.Enemies(5000, 20, enemy, enemy with { Id = "другий" }, enemy with { Id = "третій" });
            var battle = new Battle(team, new EncounterDefinition(encounter.Team, difficulty, encounter.Combatants), seed: 42);
            var facade = new BattleFacade(battle);
            Check.Equal(difficulty, battle.Difficulty, "Бій успадковує поточну складність зустрічі.");
            Check.Succeeds(battle.UseSkill(battle.Players[0].Id, true, battle.Players[0].Id));
            BattleFixture.FinishPlayerTurn(facade);
            BattleFixture.FinishEnemyTurn(facade);
            var target = battle.Players[difficulty switch { Difficulty.Normal => 0, Difficulty.Hard => 1, _ => 2 }];
            var intent = battle.Intents[0];
            Check.True(!intent.IsSpecial, "Після особливої навички ворог планує базову атаку.");
            Check.Equal(target.Id, intent.TargetId, "Вибір серед найслабшого, незахищеного й цілителя залежить від складності.");
            Check.Succeeds(battle.UseSkill(battle.Players[1].Id, true, target.Id));
            BattleFixture.FinishPlayerTurn(facade);
            var count = battle.Events.Count;
            Check.Succeeds(battle.StepEnemy());
            Check.True(battle.Events.Skip(count).Any(e => e.Kind == BattleEventKind.Damage && e.TargetId == target.Id),
                "Зміна захисту після оголошення наміру не перемикає ціль ворога.");
        }
    }

    public static void SeededCatalogBattlesFinishAndReplayOnEveryDifficulty()
    {
        string[][] squads = [["daran", "saira", "mira"], ["toren", "lira", "saira"], ["daran", "lira", "mira"]];
        foreach (var difficulty in Enum.GetValues<Difficulty>())
        {
            var victories = 0;
            var rounds = new List<int>();
            var seeds = new Random(1700);
            for (var seed = 0; seed < 24; seed++)
            {
                var encounter = new EncounterGenerator(new Random(seed)).Generate(difficulty);
                var team = BattleFixture.Team(squads[seed % squads.Length].Select(HeroCatalog.Get).ToArray());
                var combatSeed = seeds.Next();
                var first = new Battle(team, encounter, seed: combatSeed);
                var replay = new Battle(team, encounter, seed: combatSeed);
                Play(first);
                Play(replay);
                Check.Equal(BattleFixture.State(first), BattleFixture.State(replay), $"Той самий склад і зерно {seed} відтворюють бій.");
                Check.Equal(1, first.Events.Count(e => e.Kind == BattleEventKind.Result), "Один фінальний результат на бій.");
                Check.True(first.Players.Concat(first.Enemies).All(p => p.Health >= 0 && p.Health <= p.MaxHealth && p.Shield >= 0),
                    "Фінальні характеристики в дозволених межах.");
                var losers = first.Result == BattleResult.Victory ? first.Enemies : first.Players;
                Check.True(losers.All(p => !p.IsAlive), "Результат відповідає фактичному вибуттю всього загону.");
                victories += first.Result == BattleResult.Victory ? 1 : 0;
                rounds.Add(first.Round);
            }
            Console.WriteLine($"  {difficulty} — боїв {rounds.Count}, перемог {victories}, поразок {rounds.Count - victories}, раундів {rounds.Min()}–{rounds.Max()}.");
        }
    }

    private static void Play(Battle battle)
    {
        var facade = new BattleFacade(battle);
        while (battle.Result == BattleResult.InProgress)
        {
            Check.True(battle.Round <= 100, $"Бій на {battle.Difficulty} триває понад сто раундів. " +
                string.Join(", ", battle.Players.Concat(battle.Enemies).Select(p => $"{p.Id} ({p.Health} ОЗ, щит {p.Shield})")));
            foreach (var actor in battle.Players.Where(p => p.IsAlive && !p.HasActed).OrderByDescending(p => p.Hero.Id == "saira"))
            {
                if (battle.Phase != BattlePhase.PlayerTurn)
                {
                    break;
                }
                var skill = actor.Hero.SpecialSkill;
                var special = facade.CanUseSpecial(actor.Id) && facade.GetTargets(actor.Id, true).Count > 0;
                var targets = facade.GetTargets(actor.Id, special);
                var target = special && skill.Target == TargetKind.Ally
                    ? targets.OrderByDescending(p => p.PoisonRounds).ThenByDescending(p => p.MaxHealth - p.Health)
                        .ThenByDescending(p => battle.Intents.Count(i => i.TargetId == p.Id)).First()
                    : targets.OrderBy(p => p.Health + p.Shield).First();
                var targetId = special && skill.Target == TargetKind.AllEnemies ? null : target.Id;
                Check.Succeeds(facade.UseSkill(actor.Id, special, targetId));
            }
            BattleFixture.FinishEnemyTurn(facade);
        }
    }
}
