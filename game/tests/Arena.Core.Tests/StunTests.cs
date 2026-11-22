using Arena.Core;

namespace Arena.Core.Tests;

internal static class StunTests
{
    public static void RuneUsesIndividualBasicAndReplaysStun()
    {
        var rune = HeroCatalog.Get("lira");
        var team = BattleFixture.Team(rune, BattleFixture.Hero("leader", aura: AuraKind.Attack), BattleFixture.Hero("third"));
        var outcomes = new HashSet<bool>();
        for (var seed = 0; seed < 64; seed++)
        {
            var battle = new Battle(team, BattleFixture.Enemies(), seed);
            var target = battle.Enemies[0];
            Check.Succeeds(battle.UseSkill(battle.Players[0].Id, false, target.Id));
            outcomes.Add(target.IsStunned);
            Check.True(battle.Enemies.Skip(1).All(unit => !unit.IsStunned), "Базовий постріл діє на одну ціль.");
            var events = battle.Events.ToArray();
            Check.Succeeds(battle.RestoreRound());
            Check.True(!target.IsStunned, "Повернення відновлює стан до оглушення.");
            Check.Succeeds(battle.UseSkill(battle.Players[0].Id, false, target.Id));
            Check.True(events.SequenceEqual(battle.Events), "Повтор відтворює кидки та журнал.");
            Check.Succeeds(battle.RestoreRound());
            Check.Succeeds(battle.UseSkill(battle.Players[0].Id, true, null));
            Check.True(battle.Enemies.All(unit => unit.Health < unit.MaxHealth && !unit.IsStunned),
                "Злива стріл завдає масової шкоди.");
        }
        Check.Equal(2, outcomes.Count, "Постріл із шансом 10% може оглушити або промахнутися щодо ефекту.");
        Check.Equal(10, rune.BasicSkill.Effects.Single(effect => effect.Kind == EffectKind.Stun).ChancePercent,
            "Шанс Руної атаки задано в каталозі.");
    }

    public static void EnemyStunsAnActedHeroAndCooldownKeepsAdvancing()
    {
        var caster = BattleFixture.Hero("caster", aura: AuraKind.Attack,
            skill: Stun(TargetKind.Enemy) with { CooldownRounds = 0 });
        var battle = new Battle(BattleFixture.Players(), BattleFixture.Enemies(500, 1,
            caster, BattleFixture.Hero("second"), BattleFixture.Hero("third")));
        var facade = new BattleFacade(battle);
        var target = battle.Players[0];
        Check.Succeeds(battle.UseSkill(target.Id, true, battle.Enemies[0].Id));
        BattleFixture.FinishPlayerTurn(facade);
        Check.Succeeds(battle.StepEnemy());
        Check.True(target.HasActed && target.IsStunned, "Оглушення після дії очікує наступного раунду.");
        BattleFixture.FinishEnemyTurn(facade);
        Check.True(target.HasActed && target.HasStunProtection && !target.IsStunned, "Пропуск застосовано автоматично.");
        Check.True(!battle.CanUseSpecial(target.Id), "КД ще триває у другому раунді.");
        Check.Rejects(battle.UseSkill(target.Id, false, battle.Enemies[0].Id));
        var state = BattleFixture.State(battle);
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        Check.Equal(1, battle.Events.Count(entry => entry.Kind == BattleEventKind.StunSkip), "Захист блокує повторний контроль.");
        Check.True(!target.HasActed && !target.IsStunned && target.HasStunProtection, "Після пропуску герой має доступну дію.");
        Check.True(battle.CanUseSpecial(target.Id), "КД відновився за спільними раундами.");
        Check.Succeeds(battle.UseSkill(target.Id, true, battle.Enemies[0].Id));
        Check.True(!target.HasStunProtection, "Успішна дія знімає захист.");
        Check.True(state.Contains("оглушення", StringComparison.Ordinal), "Пропуск зафіксовано в журналі.");
    }

    public static void StunIsIndependentOfShieldCriticalAndDeath()
    {
        var guard = BattleFixture.Hero("guard", aura: AuraKind.Health,
            skill: BattleFixture.Skill(TargetKind.Self, new SkillEffect(EffectKind.Shield, 10)));
        var attackAndStun = BattleFixture.Skill(TargetKind.Enemy,
            new SkillEffect(EffectKind.Damage, 1), new SkillEffect(EffectKind.Stun));
        var enemy = BattleFixture.Hero("caster", aura: AuraKind.Attack, skill: attackAndStun) with { CritChance = 100 };
        var battle = new Battle(BattleFixture.Team(guard, BattleFixture.Hero("second", health: 600),
            BattleFixture.Hero("third", health: 600)), BattleFixture.Enemies(500, 5,
            enemy, BattleFixture.Hero("extra-a"), BattleFixture.Hero("extra-b")));
        var target = battle.Players[0];
        Check.Succeeds(battle.UseSkill(target.Id, true, target.Id));
        BattleFixture.FinishPlayerTurn(new BattleFacade(battle));
        Check.Succeeds(battle.StepEnemy());
        Check.Equal(target.MaxHealth, target.Health, "Щит поглинає крит повністю.");
        Check.True(target.IsStunned && battle.Events.Any(entry => entry.Kind == BattleEventKind.Critical && entry.Absorbed == 10),
            "Крит і щит не змінюють шанс оглушення.");

        var fatal = new Battle(BattleFixture.Players(health: 1), BattleFixture.Enemies(500, 5,
            enemy, BattleFixture.Hero("extra-a"), BattleFixture.Hero("extra-b")));
        BattleFixture.FinishPlayerTurn(new BattleFacade(fatal));
        Check.Succeeds(fatal.StepEnemy());
        Check.True(!fatal.Players[0].IsAlive && !fatal.Players[0].IsStunned
            && !fatal.Events.Any(entry => entry.Effect == EffectKind.Stun), "Смертельний удар не додає оглушення.");
        Check.Throws<ArgumentOutOfRangeException>(() => new SkillEffect(EffectKind.Stun, ChancePercent: 101));
    }

    public static void WholeTeamSkipsAndCheckpointRestores()
    {
        var caster = BattleFixture.Hero("caster", aura: AuraKind.Attack, skill: Stun(TargetKind.AllEnemies));
        var battle = new Battle(BattleFixture.Players(), BattleFixture.Enemies(500, 1,
            caster, BattleFixture.Hero("second"), BattleFixture.Hero("third")));
        var facade = new BattleFacade(battle);
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        Check.Equal(3, battle.Round, "Після пропуску всієї команди автоматично завершився другий раунд.");
        Check.Equal(BattlePhase.PlayerTurn, battle.Phase, "Гравець знову може діяти.");
        Check.True(battle.Players.All(hero => hero.HasStunProtection && !hero.HasActed), "Усі герої встигнуть відповісти.");
        Check.Equal(3, battle.Events.Count(entry => entry.Kind == BattleEventKind.StunSkip), "Один пропуск для кожного героя.");
        var checkpoint = BattleFixture.State(battle);
        Check.Succeeds(battle.UseSkill(battle.Players[0].Id, false, battle.Enemies[0].Id));
        Check.Succeeds(battle.RestoreRound());
        Check.Equal(checkpoint, BattleFixture.State(battle), "Повернення відновлює захист і журнал.");
    }

    public static void EnemyIntentsSkipOnceAndChanceBoundsAreRespected()
    {
        var leader = BattleFixture.Hero("leader", aura: AuraKind.Attack, skill: Stun(TargetKind.Enemy));
        var second = BattleFixture.Hero("second", skill: Stun(TargetKind.Enemy));
        var third = BattleFixture.Hero("third", skill: Stun(TargetKind.Enemy, 0));
        var battle = new Battle(BattleFixture.Team(leader, second, third), BattleFixture.Enemies());
        var facade = new BattleFacade(battle);
        Check.Succeeds(battle.UseSkill(battle.Players[0].Id, true, battle.Enemies[0].Id));
        Check.Succeeds(battle.UseSkill(battle.Players[1].Id, true, battle.Enemies[0].Id));
        Check.Succeeds(battle.UseSkill(battle.Players[2].Id, true, battle.Enemies[1].Id));
        Check.True(!battle.Enemies[1].IsStunned, "Шанс 0% не накладає статус.");
        Check.Succeeds(battle.StepEnemy());
        Check.True(battle.Enemies[0].HasStunProtection && !battle.Enemies[0].IsStunned, "Ворог пропускає оголошений намір.");
        Check.Equal(1, battle.Enemies[0].ReadyOnRound, "Пропущена навичка не запускає КД.");
        BattleFixture.FinishEnemyTurn(facade);
        BattleFixture.FinishPlayerTurn(facade);
        Check.Succeeds(battle.StepEnemy());
        Check.Equal(1, battle.Events.Count(entry => entry.Kind == BattleEventKind.StunSkip), "Повторне накладання не додає пропусків.");
        Check.True(!battle.Enemies[0].HasStunProtection, "Ворог втрачає захист після успішної дії.");
    }

    private static SkillDefinition Stun(TargetKind target, int chance = 100) =>
        BattleFixture.Skill(target, new SkillEffect(EffectKind.Stun, ChancePercent: chance));
}
