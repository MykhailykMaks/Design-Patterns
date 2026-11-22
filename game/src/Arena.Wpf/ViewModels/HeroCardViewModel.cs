using System.Windows.Media;
using Arena.Core;
using Arena.Wpf.Infrastructure;

namespace Arena.Wpf.ViewModels;

public sealed class HeroCardViewModel(HeroDefinition hero) : ObservableObject
{
    private bool _isSelected;
    private bool _isInspected;
    private bool _isLeader;
    private bool _isTarget;
    private bool _isPerformer;
    private bool _isReceiving;
    private bool _animateHealth = true;
    private string? _combatantId;
    private int _health = hero.MaxHealth;
    private int _maxHealth = hero.MaxHealth;
    private int _attack = hero.Attack;
    private int _shield;
    private int _poisonRounds;
    private bool _weakened;
    private bool _empowered;
    private bool _hasActed;
    private bool _stunned;
    private bool _stunProtected;
    private EnemyIntentPreview? _intent;

    public HeroDefinition Hero { get; } = hero;
    public string Id => _combatantId ?? Hero.Id;
    public string Name => Hero.Name;
    public string Role => Presentation.RoleName(Hero.Role);
    public Brush Portrait { get; } = Presentation.Portrait(hero);
    public string Stats => $"Здоров’я {Hero.MaxHealth}   ·   Атака {Hero.Attack}   ·   Крит {Hero.CritChance}%";
    public string BasicName => Hero.BasicSkill.Name;
    public string BasicDescription => Hero.BasicSkill.Description;
    public string BasicTooltip => $"{BasicName} — {BasicDescription}";
    public string SpecialTooltip => $"{SpecialName} — {SpecialDescription}\n{CooldownDescription}";
    public string SpecialName => Hero.SpecialSkill.Name;
    public string SpecialDescription => Hero.SpecialSkill.Description;
    public string CooldownDescription => Hero.SpecialSkill.CooldownRounds == 1
        ? "Перезарядка — 1 повний раунд. Після ходу в першому раунді знову доступна в третьому."
        : $"Перезарядка в повних раундах — {Hero.SpecialSkill.CooldownRounds}.";
    public string CombatTooltip => $"{BasicTooltip}\n{SpecialTooltip}";
    public string Aura => Presentation.AuraText(Hero.Aura);
    public bool HasAura => Hero.Aura != AuraKind.None;
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
    public bool IsInspected { get => _isInspected; set => Set(ref _isInspected, value); }
    public string LeaderButtonText => IsLeader ? "◆ Лідер команди" : "Призначити лідером";
    public bool IsLeader { get => _isLeader; set { Set(ref _isLeader, value); Raise(nameof(LeaderButtonText)); } }
    public bool IsTarget { get => _isTarget; set => Set(ref _isTarget, value); }
    public bool IsPerformer { get => _isPerformer; set => Set(ref _isPerformer, value); }
    public bool IsReceiving { get => _isReceiving; set => Set(ref _isReceiving, value); }
    public bool AnimateHealth { get => _animateHealth; set => Set(ref _animateHealth, value); }
    public int Health => _health;
    public int MaxHealth => _maxHealth;
    public bool IsAlive => _health > 0;
    public string HealthText => $"{Health} / {MaxHealth}";
    public string AttackText => $"Атака {_attack}    ·    Крит {Hero.CritChance}%";
    public string Status => !IsAlive ? "Герой вибув" : (_stunned, _hasActed, _stunProtected) switch
    {
        (_, true, true) => "Дію пропущено",
        (_, true, _) => "Дію завершено",
        (true, _, _) => "Оглушено",
        _ => "Дія готова"
    };
    public bool IsEnemy { get; private set; }
    public string Footer => IsAlive ? $"Навичка  ·  {SpecialName}" : "";
    public EnemyIntentPreview? Intent => !IsEnemy || !IsAlive ? null
        : (_stunned && !_hasActed) || (_hasActed && _stunProtected) ? EnemyIntentPreview.Stunned
        : _hasActed ? null : _intent;
    public IReadOnlyList<StatusBadge> Effects { get; private set; } = [];
    public string Impact { get; private set; } = "";
    public string ImpactColor { get; private set; } = "#FFCCB8";
    public bool IsCritical { get; private set; }
    public bool HasImpact => Impact.Length > 0;

    public void Update(Combatant unit, bool isLeader)
    {
        _combatantId = unit.Id;
        _health = unit.Health;
        _maxHealth = unit.MaxHealth;
        _attack = unit.Attack;
        _shield = unit.Shield;
        _poisonRounds = unit.PoisonRounds;
        _weakened = unit.IsWeakened;
        _empowered = unit.IsEmpowered;
        _hasActed = unit.HasActed;
        _stunned = unit.IsStunned;
        _stunProtected = unit.HasStunProtection;
        IsEnemy = unit.Side == Side.Enemy;
        _isLeader = isLeader;
        UpdateEffects();
        RaiseAll();
    }

    public void UpdateIntent(EnemyIntentPreview? intent)
    {
        _intent = intent;
        Raise(nameof(Intent));
    }

    // Each event carries the target's state at impact. The engine may already be in the next round.
    public void ApplyImpact(BattleEvent entry, BattleLogEntry row)
    {
        _health = entry.TargetHealth ?? _health;
        _shield = entry.TargetShield ?? _shield;
        _poisonRounds = entry.TargetPoisonRounds ?? _poisonRounds;
        _weakened = entry.TargetIsWeakened ?? _weakened;
        _empowered = entry.TargetIsEmpowered ?? _empowered;
        _stunned = entry.TargetIsStunned ?? _stunned;
        _stunProtected = entry.TargetHasStunProtection ?? _stunProtected;
        if (entry.Kind == BattleEventKind.StunSkip) { _hasActed = true; }
        UpdateEffects();
        Impact = entry.Kind switch
        {
            BattleEventKind.Critical when entry.Amount == 0 && entry.Absorbed > 0 => $"КРИТ!  ЩИТ −{entry.Absorbed}",
            BattleEventKind.Critical => $"КРИТ!  −{entry.Amount}",
            BattleEventKind.Damage or BattleEventKind.Periodic when entry.Amount == 0 && entry.Absorbed > 0 => $"ЩИТ  −{entry.Absorbed}",
            BattleEventKind.Damage or BattleEventKind.Periodic => $"−{entry.Amount}",
            BattleEventKind.Heal => $"+{entry.Amount}",
            BattleEventKind.Shield => $"Щит {entry.TargetShield}",
            BattleEventKind.Buff when entry.Effect == EffectKind.CleansePoison => "Очищення",
            BattleEventKind.Buff => "Підсилення",
            BattleEventKind.Debuff when entry.Effect == EffectKind.Poison => "Отруєння",
            BattleEventKind.Debuff when entry.Effect == EffectKind.Stun => "Оглушення",
            BattleEventKind.StunSkip => "Пропуск дії",
            BattleEventKind.Debuff => "Послаблення",
            BattleEventKind.Defeat => "Вибуває",
            _ => ""
        };
        IsCritical = entry.Kind == BattleEventKind.Critical;
        ImpactColor = row.Color;
        RaiseAll();
    }

    public void ClearImpact()
    {
        IsReceiving = false;
        if (!HasImpact) { return; }
        Impact = "";
        IsCritical = false;
        Raise(nameof(HasImpact));
        Raise(nameof(Impact));
        Raise(nameof(IsCritical));
    }

    private void UpdateEffects()
    {
        var badges = new List<StatusBadge>();
        if (IsAlive)
        {
            if (_shield > 0) { badges.Add(new($"◇ {_shield}", "#A7CEFA", $"Щит поглине ще {_shield} шкоди. Діє до вичерпання.")); }
            if (_poisonRounds > 0) { badges.Add(new($"◈ {_poisonRounds}", "#D8ADE4", $"Отрута · 6 шкоди наприкінці раунду та −20% шкоди ударів носія. Залишилося спрацьовувань: {_poisonRounds}.")); }
            if (_stunned) { badges.Add(new("✦ 1", "#FFCE84", "Оглушення · герой пропустить одну дію.")); }
            if (_stunProtected) { badges.Add(new("⊘", "#B4DBEC", "Захист від оглушення до завершення наступної успішної дії.")); }
            if (_weakened) { badges.Add(new("↓ 1", "#F1AC9D", "Послаблення · шкода наступної дії знижена на 25%.")); }
            if (_empowered) { badges.Add(new("↑ 1", "#99DEC6", "Підсилення · шкода наступної дії збільшена на 25%.")); }
        }
        Effects = badges;
    }
}
