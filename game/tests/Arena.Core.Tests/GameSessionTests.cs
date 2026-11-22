using Arena.Core;

namespace Arena.Core.Tests;

internal static class GameSessionTests
{
    public static void RetryRequiresCompletedAttempt()
    {
        var session = new GameSession(new Random(91));
        Check.Throws<InvalidOperationException>(() => session.RetryBattle());
        session.StartBattle(BattleFixture.Players());
        Check.Throws<InvalidOperationException>(() => session.RetryBattle());
    }

    public static void LossRetryPreservesEncounterAndResetsBothTeams()
    {
        var session = new GameSession(new Random(31)) { SelectedDifficulty = Difficulty.Hard };
        var first = session.StartBattle(BattleFixture.Players(health: 1, attack: 1));
        var encounter = session.CurrentEncounter!;
        var original = BattleFixture.EncounterState(encounter);
        BattleFixture.Complete(first);
        Check.Equal(BattleResult.Defeat, first.Result, "Слабкий загін програв.");

        session.SelectedDifficulty = Difficulty.Nightmare;
        var retry = session.RetryBattle();

        Check.Equal(original, BattleFixture.EncounterState(session.CurrentEncounter!),
            "Повтор не змінює склад, лідера, складність і параметри ворогів.");
        Check.True(ReferenceEquals(encounter, session.CurrentEncounter), "Повтор використовує збережену зустріч.");
        Check.True(!ReferenceEquals(first, retry), "Повтор створює незалежний бойовий стан.");
        Check.Equal(1, retry.Round, "Повтор починається з першого раунду.");
        Check.Equal(BattlePhase.PlayerTurn, retry.Phase, "Гравець починає повтор.");
        Check.True(retry.Players.Concat(retry.Enemies).All(hero => hero.Health == hero.MaxHealth
            && hero.Shield == 0 && !hero.HasActed && !hero.IsWeakened && !hero.IsEmpowered
            && hero.PoisonRounds == 0 && hero.ReadyOnRound == 1),
            "Новий бій має повне здоров'я і не успадковує тимчасові ефекти.");
        Check.Equal(1, retry.Events.Count, "Повтор починає власний журнал.");
        Check.Equal(BattleEventKind.RoundStart, retry.Events[0].Kind, "Перша подія повтору відкриває раунд.");
        Check.Equal(string.Join(",", encounter.Combatants.Select(hero => $"{hero.MaxHealth}/{hero.Attack}")),
            string.Join(",", retry.Enemies.Select(hero => $"{hero.MaxHealth}/{hero.Attack}")),
            "Повтор не масштабує ворогів удруге.");
    }

    public static void LossChangingTeamPreservesEncounter()
    {
        var session = new GameSession(new Random(71));
        var first = session.StartBattle(BattleFixture.Players(health: 1, attack: 1));
        var original = BattleFixture.EncounterState(session.CurrentEncounter!);
        BattleFixture.Complete(first);
        Check.Equal(BattleResult.Defeat, first.Result, "Перед зміною загону завершено поразку.");
        var replacement = BattleFixture.Team(BattleFixture.Hero("new-a", aura: AuraKind.Health),
            BattleFixture.Hero("new-b"), BattleFixture.Hero("new-c"));

        var changed = session.StartBattle(replacement);

        Check.Equal(original, BattleFixture.EncounterState(session.CurrentEncounter!),
            "Зміна загону після поразки зберігає противника.");
        Check.Equal("new-a,new-b,new-c", string.Join(",", changed.Players.Select(hero => hero.Hero.Id).Order()),
            "У новій спробі діє новий загін гравця.");
    }

    public static void ReturningToSelectionDuringBattleKeepsEncounter()
    {
        var session = new GameSession(new Random(12));
        var first = session.StartBattle(BattleFixture.Players());
        var original = BattleFixture.EncounterState(session.CurrentEncounter!);
        var facade = new BattleFacade(first);
        Check.Succeeds(facade.UseSkill(first.Players[0].Id, false, first.Enemies[0].Id));
        session.SelectedDifficulty = Difficulty.Hard;

        var restarted = session.StartBattle(BattleFixture.Players(600, 15));

        Check.Equal(original, BattleFixture.EncounterState(session.CurrentEncounter!),
            "Повернення через меню не витрачає новий підбір.");
        Check.True(restarted.Enemies.All(hero => hero.Health == hero.MaxHealth), "Нова спроба очищає завдану шкоду.");
    }

    public static void VictoryContinuationGeneratesDifferentTeamAtSelectedDifficulty()
    {
        foreach (var changeTeam in new[] { false, true })
        {
            var session = new GameSession(new Random(82));
            var first = session.StartBattle(BattleFixture.Players(5000, 1000));
            var original = BattleFixture.TeamKey(session.CurrentEncounter!.Team);
            BattleFixture.Complete(first);
            Check.Equal(BattleResult.Victory, first.Result, "Сильний загін переміг.");
            Check.Equal(original, BattleFixture.TeamKey(session.CurrentEncounter.Team),
                "Показ результату ще не запускає підбір наступної зустрічі.");
            session.SelectedDifficulty = Difficulty.Nightmare;

            var next = changeTeam
                ? session.StartBattle(BattleFixture.Players(6000, 1200))
                : session.RetryBattle();

            Check.True(original != BattleFixture.TeamKey(session.CurrentEncounter!.Team),
                "Після перемоги новий склад відрізняється навіть без урахування порядку.");
            Check.Equal(Difficulty.Nightmare, session.CurrentEncounter.Difficulty,
                "Нова зустріч застосовує поточну складність.");
            Check.Equal(BattleResult.InProgress, next.Result, "Наступна партія почалася.");
        }
    }
}
