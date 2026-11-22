using Arena.Core;

namespace Arena.Core.Tests;

internal static class CriticalAndRoundTests
{
    public static void CriticalChancesApplyToEveryDirectTarget()
    {
        foreach (var chance in new[] { 0, 50, 100 })
        {
            var archer = HeroCatalog.Get("lira") with { Attack = 20, CritChance = chance };
            var team = BattleFixture.Team(archer, BattleFixture.Hero("лідер", aura: AuraKind.Health),
                BattleFixture.Hero("союзник"));
            var battle = new Battle(team, BattleFixture.Enemies(), seed: 11);
            var actor = battle.Players[0];
            Check.Succeeds(battle.UseSkill(actor.Id, true, null));

            var hits = battle.Events.Where(e => e.Kind is BattleEventKind.Damage or BattleEventKind.Critical).ToArray();
            Check.Equal(3, hits.Length, "Кожна жива ціль отримує одне влучання.");
            var criticals = hits.Count(e => e.Kind == BattleEventKind.Critical);
            Check.True(chance switch { 0 => criticals == 0, 100 => criticals == 3, _ => criticals is 1 or 2 },
                "Шанси 0% і 100% гарантовані, а для 50% це зерно дає різні влучання в одному залпі.");
            foreach (var target in battle.Enemies)
            {
                var hit = hits.Single(e => e.TargetId == target.Id);
                Check.Equal(actor.Id, hit.ActorId, "У влучання є виконавець.");
                Check.Equal(archer.SpecialSkill.Name, hit.SkillName, "Назва влучання відповідає навичці.");
                var damage = hit.Kind == BattleEventKind.Critical ? 30 : 15;
                Check.Equal(damage, hit.Amount, "Критичний удар подвоює 75% атаки.");
                Check.Equal(target.MaxHealth - damage, target.Health, "Подія збігається з втратою здоров’я.");
            }
            var events = battle.Events.ToArray();
            Check.Succeeds(battle.RestoreRound());
            Check.Succeeds(battle.UseSkill(actor.Id, true, null));
            Check.True(battle.Events.SequenceEqual(events), "Відкат раунду відтворює крит кожної стріли та весь журнал.");
        }
    }

    public static void EnemyCriticalHitsShieldBeforeHealth()
    {
        var shield = BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Shield, 1));
        var team = BattleFixture.Team(BattleFixture.Hero("вартовий", attack: 15, skill: shield, aura: AuraKind.Health),
            BattleFixture.Hero("другий"), BattleFixture.Hero("третій"));
        var enemy = BattleFixture.Hero("боєць", aura: AuraKind.Attack,
            skill: BattleFixture.Skill(TargetKind.Enemy, new SkillEffect(EffectKind.Damage, 1))) with { CritChance = 100 };
        var battle = new Battle(team, BattleFixture.Enemies(500, 10, enemy,
            BattleFixture.Hero("другий"), BattleFixture.Hero("третій")));
        var facade = new BattleFacade(battle);
        var target = battle.Players.Single(p => p.Id == battle.Intents[0].TargetId);
        Check.Succeeds(battle.UseSkill(battle.Players[0].Id, true, target.Id));
        Check.Equal(15, target.Shield, "До удару ціль має щит на 15.");
        BattleFixture.FinishPlayerTurn(facade);
        Check.Succeeds(battle.StepEnemy());
        var hit = battle.Events.Single(e => e.Kind == BattleEventKind.Critical);
        Check.Equal(target.Id, hit.TargetId, "Критичний удар влучив в оголошену ціль.");
        Check.Equal(5, hit.Amount, "Із 20 шкоди щит поглинає 15.");
        Check.Equal(15, hit.Absorbed, "Подія окремо зберігає поглинену щитом частину.");
        Check.Equal(target.MaxHealth - 5, target.Health, "Після щита знято п’ять здоров’я.");
        Check.Equal(0, target.Shield, "Увесь щит вичерпано.");
    }

    public static void RoundBoundariesCoverBothSidesAndTheirCooldowns()
    {
        var battle = new Battle(BattleFixture.Players(5000), BattleFixture.Enemies(5000, 1));
        var facade = new BattleFacade(battle);
        var player = battle.Players[0];
        var enemy = battle.Enemies[0];
        Check.Succeeds(battle.UseSkill(player.Id, true, enemy.Id));
        BattleFixture.FinishPlayerTurn(facade);
        Check.Equal(1, battle.Round, "Зміна активної сторони не завершує раунд.");
        Check.True(battle.Events.All(e => e.Round == 1 && e.Kind != BattleEventKind.RoundEnd),
            "Раунд залишається відкритим до завершення ворожої черги.");
        BattleFixture.FinishEnemyTurn(facade);
        Check.Equal(3, player.ReadyOnRound, "Союзник пропускає другий раунд.");
        Check.Equal(3, enemy.ReadyOnRound, "Ворог пропускає той самий другий раунд.");
        Check.True(!battle.Intents.Single(i => i.ActorId == enemy.Id).IsSpecial, "Ворог оголошує базову атаку.");
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        Check.True(battle.CanUseSpecial(player.Id) && battle.Intents.Single(i => i.ActorId == enemy.Id).IsSpecial,
            "У третьому раунді знову доступні навички обох сторін.");

        var boundaries = battle.Events.Where(e => e.Kind is BattleEventKind.RoundStart or BattleEventKind.RoundEnd)
            .Select(e => (e.Kind, e.Round)).ToArray();
        Check.True(boundaries.SequenceEqual(new[] { (BattleEventKind.RoundStart, 1), (BattleEventKind.RoundEnd, 1),
            (BattleEventKind.RoundStart, 2), (BattleEventKind.RoundEnd, 2), (BattleEventKind.RoundStart, 3) }),
            "Кожен повний раунд має одну пару меж у правильному порядку.");
    }

    public static void MidRoundReplayRestoresPoisonRandomRollsAndEvents()
    {
        var attacker = BattleFixture.Hero("стрілець", health: 5000, aura: AuraKind.Health) with { CritChance = 50 };
        var controller = HeroCatalog.Get("saira") with { MaxHealth = 5000, CritChance = 50 };
        var team = BattleFixture.Team(attacker, controller, attacker with { Id = "другий", Aura = AuraKind.None });
        var battle = new Battle(team, BattleFixture.Enemies(5000, 1), seed: 27);
        Check.Succeeds(battle.UseSkill(battle.Players[1].Id, true, battle.Enemies[1].Id));
        var snapshot = battle.CreateMemento();
        var start = BattleFixture.State(battle);
        var eventCount = battle.Events.Count;
        var facade = new BattleFacade(battle);

        void PlayFromSnapshot()
        {
            for (var i = 0; i < 12; i++)
            {
                BattleFixture.FinishPlayerTurn(facade);
                BattleFixture.FinishEnemyTurn(facade);
            }
        }

        PlayFromSnapshot();
        var end = BattleFixture.State(battle);
        var events = battle.Events.Skip(eventCount).ToArray();
        var playerHits = events.Where(e => battle.Players.Any(p => p.Id == e.ActorId)).ToArray();
        Check.True(playerHits.Any(e => e.Kind == BattleEventKind.Damage) && playerHits.Any(e => e.Kind == BattleEventKind.Critical),
            "Зерно дало і звичайні, і критичні влучання.");
        Check.Equal(2, events.Count(e => e.Kind == BattleEventKind.Periodic), "У первинній гілці отрута спрацювала двічі.");
        battle.Restore(snapshot);
        Check.Equal(start, BattleFixture.State(battle), "Знімок прибрав майбутні події і відновив стан отрути.");
        Check.Equal(2, battle.Enemies[1].PoisonRounds, "Відновлено обидва невикористані спрацювання.");
        PlayFromSnapshot();
        Check.True(battle.Events.Skip(eventCount).SequenceEqual(events), "Повтор зберігає кожен кидок, ефект і запис.");
        Check.Equal(end, BattleFixture.State(battle), "Повтор відтворює здоров’я, щити, наміри та повний журнал.");
    }
}
