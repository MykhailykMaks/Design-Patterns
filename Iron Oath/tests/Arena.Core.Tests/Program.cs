using System.Text;
using Arena.Core.Tests;

Console.OutputEncoding = Encoding.UTF8;
var tests = new (string Name, Action Run)[]
{
    ("Каталог п'ятьох союзників і сімох ворогів", TeamAndEncounterTests.CatalogHasRequiredRolesAndAuras),
    ("Builder відхиляє неповний, повторюваний і позбавлений аури загін", TeamAndEncounterTests.BuilderRejectsIncompleteDuplicateAndAuralessTeams),
    ("Два носії аури потребують явного лідера", TeamAndEncounterTests.BuilderRequiresExplicitChoiceForTwoAuras),
    ("Автовибір лідера та незалежність готового загону", TeamAndEncounterTests.BuilderAutoSelectsOnlyAuraAndKeepsBuiltTeamIndependent),
    ("Діє одна аура обраного лідера", TeamAndEncounterTests.OnlyLeaderAuraAppliesOnce),
    ("Підбір валідних трійок без негайного повторення", TeamAndEncounterTests.GeneratedTeamsAlwaysSatisfyRoleConstraints),
    ("Фіксоване зерно відтворює зустрічі", TeamAndEncounterTests.SeededGenerationIsReproducible),
    ("Складність, округлення та аура рівно один раз", TeamAndEncounterTests.DifficultyThenSingleAuraUsesSpecifiedRounding),
    ("Повтор заборонено до завершення спроби", GameSessionTests.RetryRequiresCompletedAttempt),
    ("Поразка й повтор зберігають ворогів та очищають стан", GameSessionTests.LossRetryPreservesEncounterAndResetsBothTeams),
    ("Зміна загону після поразки зберігає противника", GameSessionTests.LossChangingTeamPreservesEncounter),
    ("Повернення через вибір зберігає противника", GameSessionTests.ReturningToSelectionDuringBattleKeepsEncounter),
    ("Обидва продовження перемоги створюють нову трійку", GameSessionTests.VictoryContinuationGeneratesDifferentTeamAtSelectedDifficulty),
    ("Дії перемикають фази та не виконуються двічі", CombatTests.PlayerActionsDrivePhasesAndCannotActTwice),
    ("Хибні виконавці й цілі не витрачають дії", CombatTests.InvalidActorsAndTargetsDoNotSpendActions),
    ("Масова атака влучає лише в живих", CombatTests.MassAttackRequiresNullAndHitsOnlyLivingEnemies),
    ("Навичка пропускає повний наступний раунд", CombatTests.SpecialSkipsOneWholeRoundWhileBasicRemainsAvailable),
    ("Лікування себе обмежене максимумом", CombatTests.HealingAcceptsWoundedSelfAndClampsAtMaximum),
    ("Щит поглинає шкоду до здоров'я", CombatTests.ShieldKeepsLargerValueAndAbsorbsDamageBeforeHealth),
    ("Послаблення зменшує шкоду та зникає", CombatTests.WeakenReducesNextDamageThenExpires),
    ("Підсилення шкоди та його витрата підтримкою", CombatTests.EmpowerBoostsDamageAndSupportConsumesIt),
    ("Ворожі масова атака й самолікування", CombatTests.EnemyMassAttackAndSelfHealingUseSharedEffects),
    ("Цілитель лікує пораненого або атакує", CombatTests.EnemyHealerPlansOnlyWoundedAllyOrFallsBackToBasic),
    ("Контроль підсилює бійця та враховує відновлення", CombatTests.ControlPlansEmpowerForFighterAndCooldownFallsBack),
    ("Вибулі виконавці й оголошені цілі пропускаються", CombatTests.DeadActorsAndAnnouncedDeadTargetsAreSkipped),
    ("Результат негайно зупиняє чергу", CombatTests.FinishingEitherSideStopsQueuedActions),
    ("Memento повертає всі дані раунду", MementoTests.RoundRestoreRestoresEffectsCooldownActionsIntentsAndLog),
    ("Memento незалежний від повторних змін", MementoTests.SnapshotIsIndependentFromRepeatedChanges),
    ("Чужий бій і ворожа фаза забороняють відновлення", MementoTests.EnemyPhaseAndForeignBattleRejectRestore),
    ("Давній Memento не залишає майбутньої контрольної точки", MementoTests.OlderRestoreDoesNotLeakFutureCheckpoint),
    ("Відновлена перемога не генерує нової зустрічі", MementoTests.RestoringVictoryCancelsResultWithoutNewEncounter),
    ("Ткач волі атакує після вибуття останнього бійця", RegressionTests.WeaverUsesBasicAfterLastFighterDies),
    ("Масовий намір підтримки не має одиночної цілі", RegressionTests.SupportMassIntentHasNoSingleTarget),
    ("Масовий намір контролю не має одиночної цілі", RegressionTests.ControlMassIntentHasNoSingleTarget),
    ("Самопідсилення чекає наступної власної дії", RegressionTests.SelfEmpowerSurvivesItsCastingAction),
    ("Граничні шанси криту для кожної цілі масової атаки", CriticalAndRoundTests.CriticalChancesApplyToEveryDirectTarget),
    ("Критичний удар ворога спершу витрачає щит", CriticalAndRoundTests.EnemyCriticalHitsShieldBeforeHealth),
    ("Спільні межі раунду та відновлення навичок обох загонів", CriticalAndRoundTests.RoundBoundariesCoverBothSidesAndTheirCooldowns),
    ("Memento із середини раунду відтворює отруту, кидки і журнал", CriticalAndRoundTests.MidRoundReplayRestoresPoisonRandomRollsAndEvents),
    ("Отрута зменшує атаку, оновлює тривалість і спливає", PoisonTests.PoisonReducesDirectDamageRefreshesAndExpires),
    ("Летальне спрацювання отрути завершує бій", PoisonTests.PoisonKillsLastEnemyAtRoundBoundary),
    ("Естель лікує й очищує пораненого або повністю захищеного союзника", PoisonTests.MiraCleansesWoundedOrShieldedAllies),
    ("Даран використовує накладену Веспер отруту", PoisonTests.DaransSpecialGainsDamageOnlyAgainstPoisonedTarget),
    ("Вороги враховують складність й дотримуються оголошеної цілі", BattleSimulationTests.DifficultyChangesTargetChoiceAndKeepsAnnouncedIntent),
    ("Хранитель печаті атакує в одиночній дуелі", BattleSimulationTests.LastShieldSupportAttacksInDuel),
    ("Руна оглушує пострілом і відтворює кидки", StunTests.RuneUsesIndividualBasicAndReplaysStun),
    ("Оглушення після дії та відновлення КД", StunTests.EnemyStunsAnActedHeroAndCooldownKeepsAdvancing),
    ("Пропуск усієї команди та контрольна точка", StunTests.WholeTeamSkipsAndCheckpointRestores),
    ("Граничні шанси та пропуск ворожого наміру", StunTests.EnemyIntentsSkipOnceAndChanceBoundsAreRespected),
    ("Оглушення, щит, крит і смертельний удар", StunTests.StunIsIndependentOfShieldCriticalAndDeath),
    ("Фіксовані симуляції трьох загонів на трьох складностях", BattleSimulationTests.SeededCatalogBattlesFinishAndReplayOnEveryDifficulty)
};

var failures = 0;
foreach (var (name, run) in tests)
{
    try
    {
        run();
        Console.WriteLine($"УСПІХ: {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"ПОМИЛКА: {name}\n{exception}");
    }
}

Console.WriteLine($"Перевірок: {tests.Length}; успішних: {tests.Length - failures}; помилок: {failures}.");
return failures == 0 ? 0 : 1;
