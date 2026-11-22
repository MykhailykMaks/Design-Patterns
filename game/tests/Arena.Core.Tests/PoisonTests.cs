using Arena.Core;

namespace Arena.Core.Tests;

internal static class PoisonTests
{
    public static void PoisonReducesDirectDamageRefreshesAndExpires()
    {
        var poison = BattleFixture.Skill(TargetKind.Enemy, new SkillEffect(EffectKind.Poison));
        var first = BattleFixture.Hero("перша", health: 5000, skill: poison, aura: AuraKind.Health) with { CritChance = 100 };
        var team = BattleFixture.Team(first, first with { Id = "друга", Aura = AuraKind.None }, BattleFixture.Hero("третя", health: 5000));
        var normalHit = BattleFixture.Skill(TargetKind.Enemy, new SkillEffect(EffectKind.Damage, 1));
        var enemy = BattleFixture.Hero("ворог", skill: normalHit, aura: AuraKind.Attack);
        var battle = new Battle(team, BattleFixture.Enemies(5000, 10, enemy, BattleFixture.Hero("другий"), BattleFixture.Hero("третій")));
        var facade = new BattleFacade(battle);
        var target = battle.Enemies[0];
        for (var round = 1; round <= 4; round++)
        {
            if (round <= 2)
            {
                Check.Succeeds(battle.UseSkill(battle.Players[round - 1].Id, true, target.Id));
                Check.Equal(2, target.PoisonRounds, "Повторне накладання поновлює тривалість до двох раундів.");
            }
            BattleFixture.FinishPlayerTurn(facade);
            Check.True(!battle.Events.Any(e => e.Kind == BattleEventKind.Periodic && e.Round == round),
                "Перехід між сторонами не завдає періодичної шкоди.");
            BattleFixture.FinishEnemyTurn(facade);

            var hit = battle.Events.Single(e => e.Kind == BattleEventKind.Damage && e.ActorId == target.Id && e.Round == round);
            Check.Equal(round <= 3 ? 8 : 10, hit.Amount, "Отрута зменшує пряму шкоду на 20% до кінця її строку.");
            var ticks = battle.Events.Where(e => e.TargetId == target.Id && e.Round == round && e.Kind == BattleEventKind.Periodic).ToArray();
            Check.Equal(round <= 3 ? 1 : 0, ticks.Length, "Кожний отруєний раунд має рівно одне спрацювання.");
            if (ticks.Length > 0)
            {
                Check.Equal(6, ticks[0].Amount, "Отрута завдає шість шкоди навіть за шансу криту 100%.");
            }
            Check.Equal(round <= 2 ? 1 : 0, target.PoisonRounds, "Тривалість змінюється тільки на межі раунду.");
        }
    }

    public static void PoisonKillsLastEnemyAtRoundBoundary()
    {
        var poison = BattleFixture.Skill(TargetKind.AllEnemies, new SkillEffect(EffectKind.Poison));
        var guard = BattleFixture.Skill(TargetKind.Self, new SkillEffect(EffectKind.Shield, 1));
        var team = BattleFixture.Team(BattleFixture.Hero("отруйниця", skill: poison, aura: AuraKind.Health),
            BattleFixture.Hero("другий", skill: guard), BattleFixture.Hero("третій", skill: guard));
        var battle = new Battle(team, BattleFixture.Enemies(health: 5, attack: 1));
        Check.Succeeds(battle.UseSkill(battle.Players[0].Id, true, null));
        foreach (var hero in battle.Players.Skip(1))
        {
            Check.Succeeds(battle.UseSkill(hero.Id, true, hero.Id));
        }
        BattleFixture.FinishEnemyTurn(new BattleFacade(battle));
        Check.Equal(BattleResult.Victory, battle.Result, "Отрута вирішує бій наприкінці ворожої черги.");
        Check.Equal(1, battle.Round, "Після летального спрацювання новий раунд не починається.");
        foreach (var enemy in battle.Enemies)
        {
            Check.Equal(0, enemy.Health, "Здоров’я після вибуття дорівнює нулю.");
            Check.Equal(0, enemy.PoisonRounds, "Вибулий ворог втрачає отруту.");
            var lethal = battle.Events.Where(e => e.TargetId == enemy.Id && e.Kind is BattleEventKind.Periodic or BattleEventKind.Defeat).ToArray();
            Check.True(lethal.Select(e => e.Kind).SequenceEqual(new[] { BattleEventKind.Periodic, BattleEventKind.Defeat }),
                "За летальним періодичним ушкодженням іде подія вибуття.");
            Check.Equal(5, lethal[0].Amount, "Журнал обмежує шкоду залишком здоров’я цілі.");
        }
        Check.Equal(1, battle.Events.Count(e => e.Kind == BattleEventKind.Result), "Завершений бій має один результат.");
        Check.Rejects(battle.StepEnemy());
    }

    public static void MiraCleansesWoundedOrShieldedAllies()
    {
        foreach (var protect in new[] { false, true })
        {
            var shield = BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Shield, 10));
            var leader = BattleFixture.Hero("вартовий", health: 200, attack: 10, skill: shield, aura: AuraKind.Health);
            var mira = HeroCatalog.Get("mira") with { Attack = 10, MaxHealth = 500, CritChance = 100 };
            var team = BattleFixture.Team(leader, mira, BattleFixture.Hero("третій", health: 500));
            var poison = BattleFixture.Skill(TargetKind.Enemy, new SkillEffect(EffectKind.Damage, 4), new SkillEffect(EffectKind.Poison));
            var idle = BattleFixture.Skill(TargetKind.Self, new SkillEffect(EffectKind.Empower));
            var enemies = BattleFixture.Enemies(5000, 10, BattleFixture.Hero("ворог", skill: poison, aura: AuraKind.Attack),
                BattleFixture.Hero("другий", skill: idle), BattleFixture.Hero("третій", skill: idle));
            var battle = new Battle(team, enemies);
            var facade = new BattleFacade(battle);
            var target = battle.Players[0];
            if (protect)
            {
                Check.Succeeds(battle.UseSkill(target.Id, true, target.Id));
            }
            BattleFixture.FinishPlayerTurn(facade);
            BattleFixture.FinishEnemyTurn(facade);
            Check.Equal(target.MaxHealth - (protect ? 0 : 46), target.Health, "Щит поглинає пряму і періодичну шкоду.");
            Check.Equal(protect ? 54 : 0, target.Shield, "Отрута витрачає залишок щита.");
            Check.Equal(1, target.PoisonRounds, "До очищення лишалося одне спрацювання.");
            var healer = battle.Players[1];
            Check.True(battle.GetTargets(healer.Id, true).Contains(target), "Отруєного союзника можна обрати навіть за повного здоров’я.");
            var count = battle.Events.Count;
            Check.Succeeds(battle.UseSkill(healer.Id, true, target.Id));
            Check.Equal(0, target.PoisonRounds, "Живе світло очищує отруту.");
            var healed = battle.Events.Skip(count).Single(e => e.Kind == BattleEventKind.Heal);
            Check.Equal(protect ? 0 : 20, healed.Amount, "Лікування Міри не подвоюється за 100% шансу криту.");
            Check.True(battle.Events.Skip(count).All(e => e.Kind != BattleEventKind.Critical), "Очищення та лікування не позначаються як крит.");
            BattleFixture.FinishPlayerTurn(facade);
            BattleFixture.FinishEnemyTurn(facade);
            Check.True(!battle.Events.Any(e => e.Kind == BattleEventKind.Periodic && e.Round == 2),
                "Очищений союзник не отримує другого спрацювання.");
        }
    }

    public static void DaransSpecialGainsDamageOnlyAgainstPoisonedTarget()
    {
        var daran = HeroCatalog.Get("daran") with { CritChance = 0, Attack = 20 };
        var saira = HeroCatalog.Get("saira") with { CritChance = 0 };
        var team = BattleFixture.Team(daran, saira, BattleFixture.Hero("третій"));
        var battle = new Battle(team, BattleFixture.Enemies());
        var start = battle.CreateMemento();
        var target = battle.Enemies[0];
        foreach (var poisoned in new[] { false, true })
        {
            battle.Restore(start);
            if (poisoned)
            {
                Check.Succeeds(battle.UseSkill(battle.Players[1].Id, true, target.Id));
                Check.Equal(2, target.PoisonRounds, "Навичка Сайри відкриває комбінацію.");
            }
            var health = target.Health;
            Check.Succeeds(battle.UseSkill(battle.Players[0].Id, true, target.Id));
            Check.Equal(poisoned ? 52 : 41, health - target.Health, "Даран завдає 180% або 225% від атаки 23 з аурою.");
        }
    }
}
