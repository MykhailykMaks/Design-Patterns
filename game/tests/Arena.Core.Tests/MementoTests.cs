using Arena.Core;

namespace Arena.Core.Tests;

internal static class MementoTests
{
    public static void RoundRestoreRestoresEffectsCooldownActionsIntentsAndLog()
    {
        var battle = CreateEffectBattle();
        var facade = new BattleFacade(battle);
        var initial = BattleFixture.State(battle);
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, battle.Enemies[0].Id));
        Check.Succeeds(facade.UseSkill(battle.Players[1].Id, true, battle.Players[2].Id));
        Check.True(battle.Enemies[0].IsWeakened, "Послаблення накладено.");
        Check.True(battle.Players[2].Shield > 0, "Щит накладено.");
        Check.True(battle.Players[2].IsEmpowered, "Підсилення також належить до відновлюваного стану.");

        Check.Succeeds(facade.RestoreRound());

        Check.Equal(initial, BattleFixture.State(battle), "Відновлено здоров'я, щит, ефекти, готовність, дії, наміри та журнал.");
    }

    public static void SnapshotIsIndependentFromRepeatedChanges()
    {
        var battle = CreateEffectBattle();
        var facade = new BattleFacade(battle);
        var snapshot = battle.CreateMemento();
        var initial = BattleFixture.State(battle);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, battle.Enemies[attempt].Id));
            Check.Succeeds(facade.UseSkill(battle.Players[1].Id, true, battle.Players[attempt].Id));
            battle.Restore(snapshot);
            Check.Equal(initial, BattleFixture.State(battle), "Подальші зміни не змінюють збережений знімок.");
        }
    }

    public static void EnemyPhaseAndForeignBattleRejectRestore()
    {
        var battle = CreateEffectBattle();
        var facade = new BattleFacade(battle);
        var own = battle.CreateMemento();
        var foreign = CreateEffectBattle().CreateMemento();
        var initial = BattleFixture.State(battle);
        Check.Throws<ArgumentException>(() => battle.Restore(foreign));
        Check.Equal(initial, BattleFixture.State(battle), "Чужий знімок не змінює бій.");
        BattleFixture.FinishPlayerTurn(facade);
        var state = BattleFixture.State(battle);
        Check.True(!battle.CanRestore, "Під час ворожої черги відновлення недоступне.");
        Check.Rejects(facade.RestoreRound());
        Check.Throws<InvalidOperationException>(() => battle.Restore(own));
        Check.Equal(state, BattleFixture.State(battle), "Заборонене відновлення не змінює ворожу чергу.");
    }

    public static void OlderRestoreDoesNotLeakFutureCheckpoint()
    {
        var battle = CreateEffectBattle();
        var facade = new BattleFacade(battle);
        var oldest = battle.CreateMemento();
        var firstRound = BattleFixture.State(battle);
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, battle.Enemies[0].Id));
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        var secondRound = BattleFixture.State(battle);
        Check.Equal(2, battle.Round, "Досягнуто другого раунду.");
        Check.Succeeds(facade.UseSkill(battle.Players[1].Id, true, battle.Players[1].Id));
        Check.Succeeds(facade.RestoreRound());
        Check.Equal(secondRound, BattleFixture.State(battle), "Відновлюється початок поточного, а не першого раунду.");

        battle.Restore(oldest);
        Check.Equal(firstRound, BattleFixture.State(battle), "Давніший власний знімок теж відновлюється.");
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, false, battle.Enemies[1].Id));
        Check.Succeeds(facade.RestoreRound());
        Check.Equal(firstRound, BattleFixture.State(battle), "RestoreRound після давнього знімка не переносить у майбутній раунд.");
    }

    public static void RestoringVictoryCancelsResultWithoutNewEncounter()
    {
        var session = new GameSession(new Random(62));
        var mass = BattleFixture.Skill(TargetKind.AllEnemies, new SkillEffect(EffectKind.Damage, 100));
        var team = BattleFixture.Team(BattleFixture.Hero("winner", attack: 1000, skill: mass, aura: AuraKind.Attack),
            BattleFixture.Hero("two"), BattleFixture.Hero("three"));
        var battle = session.StartBattle(team);
        var encounter = BattleFixture.EncounterState(session.CurrentEncounter!);
        var initial = BattleFixture.State(battle);
        var facade = new BattleFacade(battle);
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, null));
        Check.Equal(BattleResult.Victory, battle.Result, "Отримано результат перемоги.");
        session.SelectedDifficulty = Difficulty.Nightmare;

        Check.Succeeds(facade.RestoreRound());

        Check.Equal(initial, BattleFixture.State(battle), "Відновлення результату повертає початок раунду.");
        Check.Equal(Difficulty.Nightmare, session.SelectedDifficulty, "Майбутня складність не входить до знімка.");
        session.StartBattle(team);
        Check.Equal(encounter, BattleFixture.EncounterState(session.CurrentEncounter!),
            "Скасована перемога не запускає новий підбір при перезапуску.");

        var loss = new Battle(BattleFixture.Players(1, 1), BattleFixture.Enemies(500, 100,
            BattleFixture.Hero("executioner", skill: mass, aura: AuraKind.Attack),
            BattleFixture.Hero("second"), BattleFixture.Hero("third")));
        var lossFacade = new BattleFacade(loss);
        var beforeLoss = BattleFixture.State(loss);
        BattleFixture.FinishPlayerTurn(lossFacade);
        Check.Succeeds(lossFacade.StepEnemy());
        Check.Equal(BattleResult.Defeat, loss.Result, "Отримано результат поразки.");
        Check.Succeeds(lossFacade.RestoreRound());
        Check.Equal(beforeLoss, BattleFixture.State(loss), "Відновлення поразки також повертає живий загін і початок раунду.");
    }

    private static Battle CreateEffectBattle()
    {
        var weaken = BattleFixture.Skill(TargetKind.Enemy,
            new SkillEffect(EffectKind.Damage, .8), new SkillEffect(EffectKind.Weaken));
        var shield = BattleFixture.Skill(TargetKind.Ally,
            new SkillEffect(EffectKind.Shield, 2), new SkillEffect(EffectKind.Empower));
        return new Battle(BattleFixture.Team(
            BattleFixture.Hero("controller", health: 5000, skill: weaken, aura: AuraKind.Attack),
            BattleFixture.Hero("guard", health: 5000, skill: shield),
            BattleFixture.Hero("third", health: 5000)), BattleFixture.Enemies(5000, 10));
    }
}
