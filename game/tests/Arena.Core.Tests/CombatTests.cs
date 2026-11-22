using Arena.Core;

namespace Arena.Core.Tests;

internal static class CombatTests
{
    public static void PlayerActionsDrivePhasesAndCannotActTwice()
    {
        var battle = new Battle(BattleFixture.Players(), BattleFixture.Enemies());
        var facade = new BattleFacade(battle);
        Check.Equal(BattlePhase.PlayerTurn, battle.Phase, "Гравець починає бій.");
        Check.Rejects(facade.StepEnemy());
        var actor = battle.Players[2];
        Check.Succeeds(facade.UseSkill(actor.Id, false, battle.Enemies[0].Id));
        var health = battle.Enemies[0].Health;
        Check.Rejects(facade.UseSkill(actor.Id, false, battle.Enemies[0].Id));
        Check.Equal(health, battle.Enemies[0].Health, "Повторне натискання не завдає шкоди.");
        Check.Equal(BattlePhase.PlayerTurn, battle.Phase, "Інші союзники ще можуть діяти.");
        BattleFixture.FinishPlayerTurn(facade);
        Check.Equal(BattlePhase.EnemyTurn, battle.Phase, "Після останнього союзника діють вороги.");
        Check.Rejects(facade.UseSkill(battle.Players[0].Id, false, battle.Enemies[0].Id));
        BattleFixture.FinishEnemyTurn(facade);
        Check.Equal(2, battle.Round, "Після ворожої черги почався наступний раунд.");
        Check.True(battle.Players.All(hero => !hero.HasActed), "Дії союзників відновлено.");
    }

    public static void InvalidActorsAndTargetsDoNotSpendActions()
    {
        var healer = BattleFixture.Hero("healer", skill: BattleFixture.Skill(TargetKind.Ally,
            new SkillEffect(EffectKind.Heal, 2)));
        var battle = new Battle(BattleFixture.Team(BattleFixture.Hero("leader", aura: AuraKind.Attack),
            healer, BattleFixture.Hero("third")), BattleFixture.Enemies());
        var facade = new BattleFacade(battle);
        var actor = battle.Players[0];
        var health = battle.Enemies.Sum(hero => hero.Health);
        Check.Rejects(facade.UseSkill("missing", false, battle.Enemies[0].Id));
        Check.Rejects(facade.UseSkill(battle.Enemies[0].Id, false, actor.Id));
        Check.Rejects(facade.UseSkill(actor.Id, false, battle.Players[1].Id));
        Check.Rejects(facade.UseSkill(actor.Id, false, "missing"));
        Check.Rejects(facade.UseSkill(actor.Id, false, null));
        var healerUnit = BattleFixture.Player(battle, "healer");
        Check.Equal(0, facade.GetTargets(healerUnit.Id, true).Count, "Повністю здорові герої не є цілями лікування.");
        Check.Rejects(facade.UseSkill(healerUnit.Id, true, actor.Id));
        Check.True(battle.Players.All(hero => !hero.HasActed), "Недопустима дія не витрачає хід.");
        Check.Equal(health, battle.Enemies.Sum(hero => hero.Health), "Недопустимі дії не завдають шкоди.");
        Check.True(facade.CanUseSpecial(healerUnit.Id), "Невдала навичка не запускає відновлення.");
    }

    public static void MassAttackRequiresNullAndHitsOnlyLivingEnemies()
    {
        var mass = BattleFixture.Skill(TargetKind.AllEnemies, new SkillEffect(EffectKind.Damage, .75));
        var battle = new Battle(BattleFixture.Team(BattleFixture.Hero("killer", attack: 1000, aura: AuraKind.Attack),
            BattleFixture.Hero("archer", attack: 20, skill: mass), BattleFixture.Hero("third")),
            BattleFixture.Enemies(health: 500));
        var facade = new BattleFacade(battle);
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, false, battle.Enemies[0].Id));
        var archer = battle.Players[1];
        Check.Equal(2, facade.GetTargets(archer.Id, true).Count, "Масова навичка пропускає вибулих.");
        Check.Rejects(facade.UseSkill(archer.Id, true, battle.Enemies[1].Id));
        Check.Succeeds(facade.UseSkill(archer.Id, true, null));
        Check.Equal(0, battle.Enemies[0].Health, "Вибулий ворог не відновлюється.");
        Check.True(battle.Enemies.Skip(1).All(hero => hero.Health == 483),
            "23 × 0,75 округлюється до 17 та застосовується до кожного живого ворога.");
    }

    public static void SpecialSkipsOneWholeRoundWhileBasicRemainsAvailable()
    {
        var battle = new Battle(BattleFixture.Players(5000), BattleFixture.Enemies(5000, 1));
        var facade = new BattleFacade(battle);
        var actor = battle.Players[0];
        Check.Succeeds(facade.UseSkill(actor.Id, true, battle.Enemies[0].Id));
        Check.Equal(3, actor.ReadyOnRound, "Навичка першого раунду готова в третьому.");
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        Check.True(!facade.CanUseSpecial(actor.Id), "Другий раунд повністю пропускає особливу навичку.");
        var health = battle.Enemies[0].Health;
        Check.Rejects(facade.UseSkill(actor.Id, true, battle.Enemies[0].Id));
        Check.True(!actor.HasActed, "Навичка на відновленні не витрачає дію.");
        Check.Equal(health, battle.Enemies[0].Health, "Навичка на відновленні не завдає шкоди.");
        Check.Succeeds(facade.UseSkill(actor.Id, false, battle.Enemies[0].Id));
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        Check.True(facade.CanUseSpecial(actor.Id), "У третьому раунді навичка знову доступна.");
        Check.Succeeds(facade.UseSkill(actor.Id, true, battle.Enemies[0].Id));
    }

    public static void HealingAcceptsWoundedSelfAndClampsAtMaximum()
    {
        var heal = BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Heal, 100));
        var battle = new Battle(BattleFixture.Team(BattleFixture.Hero("healer", skill: heal, aura: AuraKind.Attack),
            BattleFixture.Hero("second"), BattleFixture.Hero("third")), BattleFixture.Enemies(5000, 10));
        var facade = new BattleFacade(battle);
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        var healer = battle.Players[0];
        Check.True(healer.Health < healer.MaxHealth, "Ворог поранив цілителя.");
        Check.True(facade.GetTargets(healer.Id, true).Any(target => target.Id == healer.Id),
            "Поранений цілитель може лікувати себе.");
        Check.Succeeds(facade.UseSkill(healer.Id, true, healer.Id));
        Check.Equal(healer.MaxHealth, healer.Health, "Лікування не перевищує максимум.");
        Check.True(!facade.GetTargets(healer.Id, true).Any(target => target.Id == healer.Id),
            "Здоровий цілитель більше не є ціллю лікування.");
    }

    public static void ShieldKeepsLargerValueAndAbsorbsDamageBeforeHealth()
    {
        var battle = new Battle(BattleFixture.Team(
            BattleFixture.Hero("strong", attack: 20, aura: AuraKind.Attack,
                skill: BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Shield, 2))),
            BattleFixture.Hero("weak", attack: 10,
                skill: BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Shield, 1))),
            BattleFixture.Hero("third")), BattleFixture.Enemies(5000, 10));
        var facade = new BattleFacade(battle);
        var target = battle.Players.Single(hero => hero.Id == battle.Intents[0].TargetId);
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, target.Id));
        Check.Equal(46, target.Shield, "Сильний щит відповідає атаці носія.");
        Check.Succeeds(facade.UseSkill(battle.Players[1].Id, true, target.Id));
        Check.Equal(46, target.Shield, "Слабший повторний щит не додається й не заміняє сильніший.");
        BattleFixture.FinishPlayerTurn(facade);
        Check.Succeeds(facade.StepEnemy());
        Check.Equal(target.MaxHealth, target.Health, "Щит поглинає перший удар до здоров'я.");
        Check.Equal(28, target.Shield, "Щит витрачено на 18 одиниць шкоди.");
        BattleFixture.FinishEnemyTurn(facade);
        Check.Equal(0, target.Shield, "Подальша шкода вичерпала щит.");
        Check.Equal(target.MaxHealth - 8, target.Health, "Надлишок шкоди проходить у здоров'я.");
    }

    public static void WeakenReducesNextDamageThenExpires()
    {
        var weaken = BattleFixture.Skill(TargetKind.Enemy, new SkillEffect(EffectKind.Weaken));
        var battle = new Battle(BattleFixture.Team(BattleFixture.Hero("controller", skill: weaken, aura: AuraKind.Attack),
            BattleFixture.Hero("second"), BattleFixture.Hero("third")), BattleFixture.Enemies(5000, 20));
        var facade = new BattleFacade(battle);
        var enemy = battle.Enemies[0];
        var target = battle.Players.Single(hero => hero.Id == battle.Intents[0].TargetId);
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, enemy.Id));
        Check.True(enemy.IsWeakened, "Послаблення накладено.");
        BattleFixture.FinishPlayerTurn(facade);
        var health = target.Health;
        Check.Succeeds(facade.StepEnemy());
        Check.Equal(27, health - target.Health, "20 × 1,8 × 0,75 = 27 шкоди.");
        Check.True(!enemy.IsWeakened, "Послаблення зникає після дії ворога.");
    }

    public static void EmpowerBoostsDamageAndSupportConsumesIt()
    {
        var empower = BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Empower));
        var shield = BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Shield, 2));
        var battle = new Battle(BattleFixture.Team(BattleFixture.Hero("buffer", skill: empower, aura: AuraKind.Attack),
            BattleFixture.Hero("striker", attack: 20), BattleFixture.Hero("guard", skill: shield)),
            BattleFixture.Enemies(5000, 1));
        var facade = new BattleFacade(battle);
        var striker = battle.Players[1];
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, striker.Id));
        var health = battle.Enemies[0].Health;
        Check.Succeeds(facade.UseSkill(striker.Id, false, battle.Enemies[0].Id));
        Check.Equal(29, health - battle.Enemies[0].Health, "Підсилена базова атака 23 × 1,25 округлюється вгору.");
        Check.True(!striker.IsEmpowered, "Підсилення витрачено на атаку.");
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        var guard = battle.Players[2];
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, guard.Id));
        Check.Succeeds(facade.UseSkill(guard.Id, true, guard.Id));
        Check.True(!guard.IsEmpowered, "Підтримувальна дія також витрачає підсилення.");
        Check.Equal(24, guard.Shield, "Підсилення шкоди не збільшує щит.");
    }

    public static void EnemyMassAttackAndSelfHealingUseSharedEffects()
    {
        var mass = BattleFixture.Skill(TargetKind.AllEnemies, new SkillEffect(EffectKind.Damage, .7));
        var reap = BattleFixture.Skill(TargetKind.Enemy, new SkillEffect(EffectKind.Damage, 1.2),
            new SkillEffect(EffectKind.HealSelf, .6));
        var enemies = BattleFixture.Enemies(500, 20,
            BattleFixture.Hero("mass", skill: mass, aura: AuraKind.Attack),
            BattleFixture.Hero("reaper", skill: reap), BattleFixture.Hero("third"));
        var battle = new Battle(BattleFixture.Players(5000), enemies);
        var facade = new BattleFacade(battle);
        Check.Succeeds(facade.UseSkill(battle.Players[0].Id, false, battle.Enemies[1].Id));
        BattleFixture.FinishPlayerTurn(facade);
        var before = battle.Players.Select(hero => hero.Health).ToArray();
        Check.Succeeds(facade.StepEnemy());
        Check.True(battle.Players.Select((hero, index) => before[index] - hero.Health).All(damage => damage == 14),
            "Ворожа масова навичка завдає 14 кожному союзнику.");
        var reaper = battle.Enemies[1];
        var woundedHealth = reaper.Health;
        Check.Succeeds(facade.StepEnemy());
        Check.Equal(Math.Min(reaper.MaxHealth, woundedHealth + 12), reaper.Health,
            "Самолікування залежить від атаки, а не від фактичної шкоди.");
    }

    public static void EnemyHealerPlansOnlyWoundedAllyOrFallsBackToBasic()
    {
        var heal = BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Heal, 2));
        var enemies = BattleFixture.Enemies(500, 1,
            BattleFixture.Hero("fighter", aura: AuraKind.Attack),
            BattleFixture.Hero("healer", skill: heal, role: HeroRole.Support), BattleFixture.Hero("third"));
        var battle = new Battle(BattleFixture.Players(5000), enemies);
        var facade = new BattleFacade(battle);
        var healer = battle.Enemies[1];
        Check.True(!battle.Intents.Single(intent => intent.ActorId == healer.Id).IsSpecial,
            "Цілитель без поранених союзників оголошує базову атаку.");
        BattleFixture.FinishPlayerTurn(facade);
        BattleFixture.FinishEnemyTurn(facade);
        var intent = battle.Intents.Single(item => item.ActorId == healer.Id);
        Check.True(intent.IsSpecial, "Цілитель оголошує лікування пораненого союзника.");
        Check.Equal(battle.Enemies[0].Id, intent.TargetId, "Цілитель обирає пораненого, а не здорового.");
        Check.True(!string.IsNullOrWhiteSpace(intent.Description), "Намір має опис для екрана.");
    }

    public static void ControlPlansEmpowerForFighterAndCooldownFallsBack()
    {
        var empower = BattleFixture.Skill(TargetKind.Ally, new SkillEffect(EffectKind.Empower));
        var enemies = BattleFixture.Enemies(5000, 10,
            BattleFixture.Hero("weaver", skill: empower, aura: AuraKind.Attack, role: HeroRole.Control),
            BattleFixture.Hero("fighter"), BattleFixture.Hero("support", role: HeroRole.Support));
        var battle = new Battle(BattleFixture.Players(5000), enemies);
        var facade = new BattleFacade(battle);
        var intent = battle.Intents.Single(item => item.ActorId == battle.Enemies[0].Id);
        Check.True(intent.IsSpecial, "Контроль планує готове підсилення.");
        Check.Equal(battle.Enemies[1].Id, intent.TargetId, "Підсилення спрямоване на бійця.");
        BattleFixture.FinishPlayerTurn(facade);
        Check.Succeeds(facade.StepEnemy());
        Check.True(battle.Enemies[1].IsEmpowered, "Ворожий намір справді наклав підсилення.");
        BattleFixture.FinishEnemyTurn(facade);
        Check.True(!battle.Intents.Single(item => item.ActorId == battle.Enemies[0].Id).IsSpecial,
            "На відновленні контроль повертається до базової атаки.");
    }

    public static void DeadActorsAndAnnouncedDeadTargetsAreSkipped()
    {
        var battle = new Battle(BattleFixture.Players(health: 1, attack: 1), BattleFixture.Enemies(5000, 10));
        var facade = new BattleFacade(battle);
        BattleFixture.FinishPlayerTurn(facade);
        Check.Succeeds(facade.StepEnemy());
        Check.True(!battle.Players[0].IsAlive, "Перший ворог вибив оголошену ціль.");
        BattleFixture.FinishEnemyTurn(facade);
        Check.True(battle.Players.Skip(1).All(hero => hero.Health == hero.MaxHealth),
            "Наступні вороги пропускають вибулу ціль та не переобирають її.");
        Check.Rejects(facade.UseSkill(battle.Players[0].Id, false, battle.Enemies[0].Id));
        Check.True(battle.Log.Any(line => line.Contains("пропускає", StringComparison.OrdinalIgnoreCase)),
            "Пропущений намір пояснюється в журналі.");

        var second = new Battle(BattleFixture.Team(BattleFixture.Hero("killer", attack: 1000, aura: AuraKind.Attack),
            BattleFixture.Hero("two"), BattleFixture.Hero("three")), BattleFixture.Enemies(500, 10));
        var secondFacade = new BattleFacade(second);
        Check.Succeeds(secondFacade.UseSkill(second.Players[0].Id, false, second.Enemies[0].Id));
        Check.Rejects(secondFacade.UseSkill(second.Players[1].Id, false, second.Enemies[0].Id));
        Check.True(!second.Players[1].HasActed, "Вибула ціль не витрачає дію героя.");
        BattleFixture.FinishPlayerTurn(secondFacade);
        var totalHealth = second.Players.Sum(hero => hero.Health);
        Check.Succeeds(secondFacade.StepEnemy());
        Check.Equal(totalHealth, second.Players.Sum(hero => hero.Health), "Вибулий ворог пропустив свій намір.");
    }

    public static void FinishingEitherSideStopsQueuedActions()
    {
        foreach (var victory in new[] { true, false })
        {
            var mass = BattleFixture.Skill(TargetKind.AllEnemies, new SkillEffect(EffectKind.Damage, 100));
            var players = victory
                ? BattleFixture.Team(BattleFixture.Hero("winner", attack: 100, skill: mass, aura: AuraKind.Attack),
                    BattleFixture.Hero("two"), BattleFixture.Hero("three"))
                : BattleFixture.Players(1, 1);
            var enemies = victory
                ? BattleFixture.Enemies(10, 1)
                : BattleFixture.Enemies(500, 100, BattleFixture.Hero("executioner", skill: mass, aura: AuraKind.Attack),
                    BattleFixture.Hero("two"), BattleFixture.Hero("three"));
            var battle = new Battle(players, enemies);
            var facade = new BattleFacade(battle);
            if (victory)
            {
                Check.Succeeds(facade.UseSkill(battle.Players[0].Id, true, null));
            }
            else
            {
                BattleFixture.FinishPlayerTurn(facade);
                Check.Succeeds(facade.StepEnemy());
            }

            Check.Equal(victory ? BattleResult.Victory : BattleResult.Defeat, battle.Result, "Правильний результат партії.");
            Check.Equal(BattlePhase.Finished, battle.Phase, "Бій одразу переходить у Finished.");
            var state = BattleFixture.State(battle);
            Check.Rejects(facade.StepEnemy());
            Check.Rejects(facade.UseSkill(battle.Players[0].Id, false, battle.Enemies[0].Id));
            Check.Equal(state, BattleFixture.State(battle), "Після результату жодна дія не змінює бойовий стан.");
        }
    }
}
