namespace Arena.Core;

public sealed class Battle
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly List<BattleEvent> _events = [];
    private readonly List<EnemyIntent> _intents = [];
    private readonly BattleHistory _history = new();
    private readonly EnemyPlanner _planner = new();
    private IBattleState _state = new PlayerTurnState();
    private int _enemyIndex;
    private uint _randomState;

    public Battle(TeamDefinition players, EncounterDefinition encounter, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(encounter);
        _randomState = unchecked((uint)(seed ?? Random.Shared.Next()));
        Difficulty = encounter.Difficulty;
        Players = Array.AsReadOnly(CombatantFactory.CreateSeeds(players)
            .Select(seed => new Combatant(seed, Side.Player)).ToArray());
        Enemies = Array.AsReadOnly(encounter.Combatants
            .Select(seed => new Combatant(seed, Side.Enemy)).ToArray());
        Events = _events.AsReadOnly();
        Log = new BattleLog(Events);
        Intents = _intents.AsReadOnly();
        StartRound();
    }

    public IReadOnlyList<Combatant> Players { get; }
    public IReadOnlyList<Combatant> Enemies { get; }
    public IReadOnlyList<string> Log { get; }
    public IReadOnlyList<BattleEvent> Events { get; }
    public Difficulty Difficulty { get; }
    public IReadOnlyList<EnemyIntent> Intents { get; }
    public int Round { get; private set; }
    public BattlePhase Phase => _state.Phase;
    public BattleResult Result { get; private set; }
    public bool CanRestore => _history.Latest is not null && Phase != BattlePhase.EnemyTurn;

    public ActionResult UseSkill(string actorId, bool isSpecial, string? targetId) =>
        _state.UseSkill(this, actorId, isSpecial, targetId);

    public ActionResult StepEnemy() => _state.StepEnemy(this);

    public bool CanUseSpecial(string actorId)
    {
        var actor = Find(actorId);
        return actor is { IsAlive: true } && actor.ReadyOnRound <= Round;
    }

    public IReadOnlyList<Combatant> GetTargets(string actorId, bool isSpecial)
    {
        var actor = Find(actorId);
        if (actor is null || !actor.IsAlive)
        {
            return Array.Empty<Combatant>();
        }
        var skill = isSpecial ? actor.Hero.SpecialSkill : actor.Hero.BasicSkill;
        var allies = actor.Side == Side.Player ? Players : Enemies;
        var enemies = actor.Side == Side.Player ? Enemies : Players;
        IEnumerable<Combatant> targets = skill.Target switch
        {
            TargetKind.Ally => allies,
            TargetKind.Self => [actor],
            _ => enemies
        };
        targets = targets.Where(unit => unit.IsAlive);
        if (skill.Effects.Any(effect => effect.Kind == EffectKind.Heal))
        {
            var cleansesPoison = skill.Effects.Any(effect => effect.Kind == EffectKind.CleansePoison);
            targets = targets.Where(unit => unit.Health < unit.MaxHealth || (cleansesPoison && unit.PoisonRounds > 0));
        }
        return Array.AsReadOnly(targets.ToArray());
    }

    internal ActionResult ApplyPlayerAction(string actorId, bool isSpecial, string? targetId)
    {
        var actor = Find(actorId);
        if (actor is null || actor.Side != Side.Player)
        {
            return ActionResult.Fail("Оберіть героя своєї команди.");
        }
        var result = Execute(actor, isSpecial, targetId);
        if (result.Success && Result == BattleResult.InProgress)
        {
            AdvancePlayerPhase();
        }
        return result;
    }

    internal ActionResult ApplyEnemyStep()
    {
        if (_enemyIndex >= _intents.Count)
        {
            EndRound();
            return ActionResult.Ok();
        }
        var intent = _intents[_enemyIndex++];
        var actor = Find(intent.ActorId)!;
        var result = SkipStunnedAction(actor)
            ? ActionResult.Ok()
            : Execute(actor, intent.IsSpecial, intent.TargetId);
        if (!result.Success)
        {
            var skill = intent.IsSpecial ? actor.Hero.SpecialSkill : actor.Hero.BasicSkill;
            Record(BattleEventKind.Skip, $"{actor.Hero.Name} пропускає дію. {result.Message}", actor,
                intent.TargetId is null ? null : Find(intent.TargetId), skill.Name);
        }
        if (Result == BattleResult.InProgress && _enemyIndex == _intents.Count)
        {
            EndRound();
        }
        return ActionResult.Ok(result.Message);
    }

    public BattleMemento CreateMemento() => new(_id, Round, Phase, Result, _enemyIndex,
        Players.Concat(Enemies).Select(unit => unit.Capture()), _intents, _events, _randomState,
        _history.Latest?.Round == Round ? _history.Latest : null);

    public void Restore(BattleMemento snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.OwnerId != _id)
        {
            throw new ArgumentException("Знімок належить іншому бою.", nameof(snapshot));
        }
        if (Phase == BattlePhase.EnemyTurn)
        {
            throw new InvalidOperationException("Дочекайтеся завершення ходу ворогів.");
        }
        foreach (var state in snapshot.Combatants)
        {
            Find(state.Id)!.Restore(state);
        }
        Round = snapshot.Round;
        Result = snapshot.Result;
        _enemyIndex = snapshot.EnemyIndex;
        _intents.Clear();
        _intents.AddRange(snapshot.Intents);
        _events.Clear();
        _events.AddRange(snapshot.Events);
        _randomState = snapshot.RandomState;
        _history.Save(snapshot.RoundStart ?? snapshot);
        _state = snapshot.Phase switch
        {
            BattlePhase.PlayerTurn => new PlayerTurnState(),
            BattlePhase.EnemyTurn => new EnemyTurnState(),
            _ => new FinishedState()
        };
    }

    public ActionResult RestoreRound()
    {
        if (!CanRestore)
        {
            return ActionResult.Fail("Контрольна точка зараз недоступна.");
        }
        Restore(_history.Latest!);
        return ActionResult.Ok("Раунд відновлено.");
    }

    private Combatant? Find(string id) => Players.Concat(Enemies).FirstOrDefault(unit => unit.Id == id);

    private void StartRound()
    {
        Round++;
        _enemyIndex = 0;
        _state = new PlayerTurnState();
        foreach (var unit in Players.Concat(Enemies))
        {
            unit.HasActed = false;
        }
        Record(BattleEventKind.RoundStart, $"Раунд {Round}. Ваш хід.");
        _intents.Clear();
        foreach (var enemy in Enemies.Where(unit => unit.IsAlive))
        {
            _intents.Add(_planner.Plan(this, enemy));
        }
        AdvancePlayerPhase();
        _history.Save(CreateMemento());
    }

    private void AdvancePlayerPhase()
    {
        foreach (var player in Players)
        {
            SkipStunnedAction(player);
        }
        if (Players.All(player => !player.IsAlive || player.HasActed))
        {
            _state = new EnemyTurnState();
        }
    }

    private bool SkipStunnedAction(Combatant actor)
    {
        if (!actor.IsAlive || actor.HasActed || !actor.IsStunned)
        {
            return false;
        }

        actor.IsStunned = false;
        actor.HasActed = true;
        actor.HasStunProtection = true;
        Record(BattleEventKind.StunSkip, $"{actor.Hero.Name} пропускає дію через оглушення.",
            actor, actor, effect: EffectKind.Stun);
        return true;
    }

    private void EndRound()
    {
        Record(BattleEventKind.RoundEnd, $"Кінець раунду {Round}.");
        foreach (var unit in Players.Concat(Enemies))
        {
            if (unit.Poison is not { } poison || !unit.IsAlive)
            {
                continue;
            }
            unit.Poison = poison.Rounds > 1 ? poison with { Rounds = poison.Rounds - 1 } : null;
            DealDamage(Find(poison.ActorId)!, unit, PoisonStatus.Damage, BattleEventKind.Periodic, poison.SkillName);
            if (!unit.IsAlive && CheckResult())
            {
                return;
            }
        }
        StartRound();
    }

    private ActionResult Execute(Combatant actor, bool isSpecial, string? targetId)
    {
        if (!actor.IsAlive || actor.HasActed || actor.IsStunned)
        {
            return ActionResult.Fail("Герой вибув або вже виконав дію.");
        }
        if (isSpecial && !CanUseSpecial(actor.Id))
        {
            return ActionResult.Fail("Особлива навичка ще відновлюється.");
        }
        var skill = isSpecial ? actor.Hero.SpecialSkill : actor.Hero.BasicSkill;
        var targets = GetTargets(actor.Id, isSpecial);
        if (skill.Target == TargetKind.AllEnemies)
        {
            if (targetId is not null || targets.Count == 0)
            {
                return ActionResult.Fail("Підтвердьте дію на всю ворожу команду.");
            }
        }
        else
        {
            targets = targets.Where(target => target.Id == targetId).ToArray();
            if (targets.Count == 0)
            {
                return ActionResult.Fail("Ціль вибула або не підходить для цієї навички.");
            }
        }
        var damageModifier = 1 + (actor.IsEmpowered ? .25 : 0) - (actor.IsWeakened ? .25 : 0)
            - (actor.PoisonRounds > 0 ? PoisonStatus.AttackPenalty : 0);
        actor.IsWeakened = false;
        actor.IsEmpowered = false;
        ApplyEffects(actor, skill, targets, damageModifier);
        actor.HasActed = true;
        if (actor.HasStunProtection)
        {
            actor.HasStunProtection = false;
            Record(BattleEventKind.StunProtectionEnded, $"{actor.Hero.Name}: захист від оглушення завершився.",
                actor, actor, effect: EffectKind.Stun);
        }
        if (isSpecial)
        {
            actor.ReadyOnRound = Round + skill.CooldownRounds + 1;
        }
        CheckResult();
        return ActionResult.Ok();
    }

    private void ApplyEffects(Combatant actor, SkillDefinition skill, IReadOnlyList<Combatant> targets, double modifier)
    {
        Record(BattleEventKind.Ability, $"{actor.Hero.Name} — {skill.Name}.", actor,
            skill.Target == TargetKind.AllEnemies ? null : targets[0], skill.Name);
        foreach (var effect in skill.Effects)
        {
            if (!actor.IsAlive)
            {
                break;
            }
            if (effect.Kind == EffectKind.HealSelf)
            {
                var restored = actor.Heal(CombatantFactory.Scale(actor.Attack, effect.Multiplier));
                Record(BattleEventKind.Heal, $"{actor.Hero.Name} відновлює {restored} здоров’я.",
                    actor, actor, skill.Name, effect.Kind, restored);
                continue;
            }
            foreach (var target in targets)
            {
                if (target.IsAlive)
                {
                    ApplyEffect(actor, target, effect, modifier, skill.Name);
                }
            }
        }
    }

    private void ApplyEffect(Combatant actor, Combatant target, SkillEffect effect, double damageModifier, string skillName)
    {
        var amount = CombatantFactory.Scale(actor.Attack, effect.Multiplier);
        switch (effect.Kind)
        {
            case EffectKind.Damage:
                var critical = RollChance(actor.Hero.CritChance);
                var targetModifier = target.PoisonRounds > 0 ? 1 + effect.PoisonedTargetBonus : 1;
                var damage = CombatantFactory.Scale(actor.Attack, effect.Multiplier * damageModifier * targetModifier
                    * (critical ? 2 : 1));
                DealDamage(actor, target, damage, critical ? BattleEventKind.Critical : BattleEventKind.Damage, skillName);
                break;
            case EffectKind.Heal:
                var healed = target.Heal(amount);
                Record(BattleEventKind.Heal, $"{target.Hero.Name}: +{healed} здоров’я.", actor, target, skillName, effect.Kind, healed);
                break;
            case EffectKind.Shield:
                target.ApplyShield(amount);
                Record(BattleEventKind.Shield, $"{target.Hero.Name}: щит {target.Shield}.", actor, target, skillName, effect.Kind, target.Shield);
                break;
            case EffectKind.Weaken:
                target.IsWeakened = true;
                Record(BattleEventKind.Debuff, $"{target.Hero.Name}: послаблення наступної дії на 25%.",
                    actor, target, skillName, effect.Kind);
                break;
            case EffectKind.Empower:
                target.IsEmpowered = true;
                Record(BattleEventKind.Buff, $"{target.Hero.Name}: підсилення наступної дії на 25%.",
                    actor, target, skillName, effect.Kind);
                break;
            case EffectKind.Poison:
                var renewed = target.PoisonRounds > 0;
                target.Poison = new PoisonStatus(actor.Id, skillName, PoisonStatus.Duration);
                var action = renewed ? "оновлено" : "накладено";
                Record(BattleEventKind.Debuff, $"{target.Hero.Name}: {action} отруту на 2 раунди (−20% шкоди ударів).",
                    actor, target, skillName, effect.Kind);
                break;
            case EffectKind.Stun when !target.IsStunned && !target.HasStunProtection && RollChance(effect.ChancePercent):
                target.IsStunned = true;
                Record(BattleEventKind.Debuff, $"{target.Hero.Name}: оглушення. Наступну дію буде пропущено.",
                    actor, target, skillName, effect.Kind);
                break;
            case EffectKind.CleansePoison when target.PoisonRounds > 0:
                target.Poison = null;
                Record(BattleEventKind.Buff, $"{target.Hero.Name}: отруту знято.", actor, target, skillName, effect.Kind);
                break;
        }
    }

    private bool RollChance(int chance)
    {
        // SplitMix32: знімок одного лічильника повністю відтворює подальші кидки.
        unchecked
        {
            _randomState += 0x9E3779B9u;
            var bits = _randomState;
            bits = (bits ^ (bits >> 16)) * 0x21F0AAADu;
            bits = (bits ^ (bits >> 15)) * 0x735A2D97u;
            bits ^= bits >> 15;
            return (int)(((ulong)bits * 100) >> 32) < chance;
        }
    }

    private void DealDamage(Combatant actor, Combatant target, int damage, BattleEventKind kind, string skillName)
    {
        var shieldBefore = target.Shield;
        var lost = target.TakeDamage(damage);
        var absorbed = shieldBefore - target.Shield;
        var prefix = kind switch
        {
            BattleEventKind.Periodic => "Отрута. ",
            BattleEventKind.Critical => "Критичний удар! ",
            _ => ""
        };
        var effect = kind == BattleEventKind.Periodic ? EffectKind.Poison : EffectKind.Damage;
        var outcome = lost == 0 && absorbed > 0
            ? $"щит поглинув {absorbed}"
            : $"−{lost} здоров’я" + (absorbed > 0 ? $", щит поглинув {absorbed}" : "");
        Record(kind, $"{prefix}{target.Hero.Name}: {outcome}.",
            actor, target, skillName, effect, lost, absorbed);
        if (!target.IsAlive)
        {
            Record(BattleEventKind.Defeat, $"{target.Hero.Name} вибуває з бою.", actor, target, skillName);
        }
    }

    private void Record(BattleEventKind kind, string message, Combatant? actor = null, Combatant? target = null,
        string? skillName = null, EffectKind? effect = null, int amount = 0, int absorbed = 0)
    {
        _events.Add(new BattleEvent(kind, Round, message, actor?.Id, target?.Id, amount, skillName)
        {
            Effect = effect,
            Absorbed = absorbed,
            TargetHealth = target?.Health,
            TargetShield = target?.Shield,
            TargetPoisonRounds = target?.PoisonRounds,
            TargetIsWeakened = target?.IsWeakened,
            TargetIsEmpowered = target?.IsEmpowered,
            TargetIsStunned = target?.IsStunned,
            TargetHasStunProtection = target?.HasStunProtection
        });
    }

    private bool CheckResult()
    {
        if (Enemies.All(unit => !unit.IsAlive))
        {
            Result = BattleResult.Victory;
        }
        else if (Players.All(unit => !unit.IsAlive))
        {
            Result = BattleResult.Defeat;
        }
        if (Result != BattleResult.InProgress)
        {
            _state = new FinishedState();
            Record(BattleEventKind.Result, Result == BattleResult.Victory ? "Перемога!" : "Поразка.");
        }
        return Result != BattleResult.InProgress;
    }
}
