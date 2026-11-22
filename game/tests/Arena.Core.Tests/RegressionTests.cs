using Arena.Core;

namespace Arena.Core.Tests;

internal static class RegressionTests
{
    public static void WeaverUsesBasicAfterLastFighterDies()
    {
        var guard = BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Shield, 1));
        var players = BattleFixture.Team(
            BattleFixture.Hero("killer", health: 5000, attack: 1000, skill: guard, aura: AuraKind.Attack),
            BattleFixture.Hero("second", health: 5000, attack: 1),
            BattleFixture.Hero("third", health: 5000, attack: 1));
        var fighter = BattleFixture.Hero("fighter");
        var enemies = BattleFixture.Enemies(500, 1, HeroCatalog.Get("will-weaver"),
            fighter, HeroCatalog.Get("bone-doctor"));
        var battle = new Battle(players, enemies);
        var facade = new BattleFacade(battle);
        var weaver = battle.Enemies[0];
        var crusher = battle.Enemies[1];
        Check.Equal(crusher.Id, battle.Intents.Single(intent => intent.ActorId == weaver.Id).TargetId,
            "Спочатку Ткач волі підсилює живого бійця.");
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, battle.Players[0].Id));
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, false, crusher.Id));
        Check.True(!crusher.IsAlive, "Останній ворожий боєць вибув.");
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);

        Check.Equal(3, battle.Round, "Навичка Ткача волі знову готова.");
        Check.True(facade.CanUseSpecial(weaver.Id), "Готовність навички не є причиною базового наміру.");
        var intent = battle.Intents.Single(item => item.ActorId == weaver.Id);
        Check.True(!intent.IsSpecial, "Без бійця Ткач волі атакує замість підсилення себе чи цілителя.");
        Check.True(battle.Players.Any(hero => hero.Id == intent.TargetId), "Базова атака спрямована на гравця.");
    }

    public static void SupportMassIntentHasNoSingleTarget() => CheckMassIntent(HeroRole.Support);

    public static void ControlMassIntentHasNoSingleTarget() => CheckMassIntent(HeroRole.Control);

    public static void SelfEmpowerSurvivesItsCastingAction()
    {
        var empower = BattleFixture.Skill(TargetKind.Self, new SkillEffect(EffectKind.Empower));
        var players = BattleFixture.Team(
            BattleFixture.Hero("self", health: 5000, attack: 20, skill: empower, aura: AuraKind.Attack),
            BattleFixture.Hero("second", health: 5000), BattleFixture.Hero("third", health: 5000));
        var battle = new Battle(players, BattleFixture.Enemies(5000, 1));
        var facade = new BattleFacade(battle);
        var actor = battle.Players[0];
        Check.Succeeds(facade.UseSkill(actor.Id, true, actor.Id));
        Check.True(actor.IsEmpowered, "Нове підсилення себе не витрачається дією його накладання.");
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        Check.True(actor.IsEmpowered, "Підсилення чекає наступної власної дії героя.");
        var health = battle.Enemies[0].Health;
        Check.Succeeds(facade.UseSkill(actor.Id, false, battle.Enemies[0].Id));
        Check.Equal(29, health - battle.Enemies[0].Health, "Наступна атака використовує самопідсилення.");
        Check.True(!actor.IsEmpowered, "Підсилення зникло після наступної дії.");
    }

    private static void CheckMassIntent(HeroRole role)
    {
        var mass = BattleFixture.Skill(TargetKind.AllEnemies, new SkillEffect(EffectKind.Damage, 1));
        var enemies = BattleFixture.Enemies(5000, 10,
            BattleFixture.Hero("mass", skill: mass, aura: AuraKind.Attack, role: role),
            BattleFixture.Hero("second"), BattleFixture.Hero("third"));
        var battle = new Battle(BattleFixture.Players(5000), enemies);
        var facade = new BattleFacade(battle);
        var intent = battle.Intents.Single(item => item.ActorId == battle.Enemies[0].Id);
        Check.True(intent.IsSpecial, "Підтримка й контроль можуть планувати власну масову навичку.");
        Check.Equal<string?>(null, intent.TargetId, "Масовий намір не містить одиночної цілі.");
        BattleFixture.FinishPlayerTurn(facade);
        Check.Succeeds(facade.StepEnemy());
        Check.True(battle.Players.All(hero => hero.Health == hero.MaxHealth - 10),
            "Масовий намір підтримки або контролю виконується для всіх живих цілей.");
    }
}
