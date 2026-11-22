using System.Collections.ObjectModel;
using Arena.Core;

namespace Arena.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private BattleFacade? _facade;
    private HeroCardViewModel? _actor;
    private bool? _special;
    private int _shownRound;
    private int _playbackMode;
    private string _phaseCue = "";
    private string _actionCue = "";

    public ObservableCollection<HeroCardViewModel> Players { get; } = [];
    public ObservableCollection<HeroCardViewModel> Enemies { get; } = [];
    public ObservableCollection<BattleLogEntry> BattleLog { get; } = [];
    public bool CanPlay => !IsBusy && Screen == "Battle" && _facade?.Battle.Phase == BattlePhase.PlayerTurn;
    public bool CanChooseBasic => CanPlay && _actor is not null && Targets(false).Count > 0;
    public bool CanChooseSpecial => CanPlay && _actor is not null && _facade!.CanUseSpecial(_actor.Id) && Targets(true).Count > 0;
    public bool CanConfirmAll => CanPlay && _special == true && _actor?.Hero.SpecialSkill.Target == TargetKind.AllEnemies;
    public bool IsBasicSelected => _special == false;
    public bool IsSpecialSelected => _special == true;
    public string ActorName => IsBusy ? "Виконання дії" : _actor?.Name ?? "Оберіть союзника";
    public string BasicName => _actor?.BasicName ?? HeroCatalog.BasicAttack.Name;
    public string SpecialName => RemainingCooldown > 0 ? $"{_actor!.SpecialName}  ·  КД {RemainingCooldown}" : _actor?.SpecialName ?? "Особлива навичка";
    public string BasicDescription => _actor?.BasicDescription ?? HeroCatalog.BasicAttack.Description;
    public string BasicTooltip => _actor?.BasicTooltip ?? "Оберіть героя своєї команди.";
    public string SkillDescription => _actor is null ? "Кожен живий герой має одну дію за раунд." :
        IsBasicSelected ? BasicDescription : $"{_actor.SpecialDescription} {_actor.CooldownDescription}";
    public string SpecialTooltip => _actor?.SpecialTooltip ?? "Оберіть героя своєї команди.";
    private int RemainingCooldown => _actor is null || _facade is null ? 0 :
        Math.Max(0, _facade.Battle.Players.First(unit => unit.Id == _actor.Id).ReadyOnRound - _shownRound);
    public string SpecialAvailability => _actor is null ? "" : RemainingCooldown > 0 ? $"Готова в раунді {_shownRound + RemainingCooldown}" :
        CanChooseSpecial ? "Особлива готова" : "Немає доступної цілі";
    public string RoundText => _shownRound.ToString("00");
    public string BattleDifficulty => _session.CurrentEncounter is { } encounter ? Presentation.DifficultyName(encounter.Difficulty) : "";
    public string PlaybackSpeedText => _playbackMode switch { 0 => "Темп  ·  1×", 1 => "Темп  ·  2×", _ => "Миттєво" };
    public string PhaseText => IsBusy ? _phaseCue : _facade?.Battle.Phase == BattlePhase.Finished ? "Бій завершено" : "Ваш хід";
    public string ActionHint => IsBusy ? _actionCue : _actor is null ? "Оберіть героя, навичку та ціль" :
        _special is null ? "Оберіть навичку внизу" : CanConfirmAll ? "Усі вороги · натисніть «Застосувати»" : "Натисніть підсвічену ціль";
    public bool IsVictory => _facade?.Battle.Result == BattleResult.Victory;
    public string ResultTitle => IsVictory ? "Перемога" : "Поразка";
    public string ResultDescription => IsVictory
        ? "Арена ваша. Попереду нові супротивники."
        : "Цього разу вороги втримали арену. Спробуйте ще раз проти тієї самої команди.";
    public string ContinueLabel => IsVictory ? "Наступний бій" : "Повторити бій";

    private void BindBattle()
    {
        _actor = null;
        _special = null;
        _facade = new BattleFacade(_session.Battle ?? throw new InvalidOperationException("Бій не створено."));
        Players.Clear();
        Enemies.Clear();
        foreach (var unit in _facade.Battle.Players) { Players.Add(new HeroCardViewModel(unit.Hero)); }
        foreach (var unit in _facade.Battle.Enemies) { Enemies.Add(new HeroCardViewModel(unit.Hero)); }
        Navigate("Battle");
        ResetPresentation();
    }

    private IEnumerable<HeroCardViewModel> Cards => Players.Concat(Enemies);
    private IReadOnlyList<Combatant> Targets(bool special) => _actor is null || _facade is null ? [] : _facade.GetTargets(_actor.Id, special);

    private async Task HandleCardAsync(object? value)
    {
        if (value is not HeroCardViewModel card || !CanPlay || _facade is null) { return; }
        if (_actor is not null && _special is not null && card.IsTarget && !CanConfirmAll)
        {
            await UseSkillAsync(card.Id);
            return;
        }
        if (!_facade.Battle.Players.Any(unit => unit.Id == card.Id && unit.IsAlive && !unit.HasActed && !unit.IsStunned)) { return; }
        _actor = card;
        _special = null;
        Message = "";
        RefreshSelection();
    }

    private void SelectSkill(bool special)
    {
        _special = special;
        Message = "";
        RefreshSelection();
    }

    private async Task UseSkillAsync(string? targetId)
    {
        if (!CanPlay || _actor is null || _special is null || _facade is null) { return; }
        var actor = _actor;
        var special = _special.Value;
        var battle = _facade.Battle;
        var firstEvent = battle.Events.Count;
        _phaseCue = actor.Name;
        _actionCue = special ? actor.SpecialName : BasicName;
        IsBusy = true;
        try
        {
            var action = _facade.UseSkill(actor.Id, special, targetId);
            Message = action.Success ? "" : action.Message;
            if (!action.Success) { return; }
            _actor = null;
            _special = null;
            RefreshSelection();
            await PlayEventsAsync(firstEvent);
            while (battle.Phase == BattlePhase.EnemyTurn)
            {
                firstEvent = battle.Events.Count;
                var step = _facade.StepEnemy();
                if (!step.Success) { Message = step.Message; break; }
                await PlayEventsAsync(firstEvent);
            }
        }
        finally
        {
            ClearCues();
            SynchronizeCards();
            if (battle.Phase == BattlePhase.Finished) { Screen = "Result"; }
            IsBusy = false;
            RefreshSelection();
        }
    }

    private void RestoreRound()
    {
        if (_facade is null || IsBusy) { return; }
        var result = _facade.RestoreRound();
        Message = result.Success ? "Відновлено початок раунду." : result.Message;
        if (result.Success)
        {
            Screen = "Battle";
            ResetPresentation();
        }
    }

    private void ResetPresentation()
    {
        _actor = null;
        _special = null;
        ClearCues();
        foreach (var card in Cards) { card.AnimateHealth = false; }
        BattleLog.Clear();
        foreach (var entry in _facade!.Battle.Events.Reverse()) { BattleLog.Add(BattleLogEntry.From(entry)); }
        SynchronizeCards();
        foreach (var card in Cards) { card.AnimateHealth = _playbackMode != 2; }
        RefreshSelection();
    }

    private void SynchronizeCards()
    {
        var battle = _facade!.Battle;
        _shownRound = battle.Round;
        for (var index = 0; index < Players.Count; index++)
        {
            var unit = battle.Players[index];
            Players[index].Update(unit, _session.PlayerTeam?.LeaderId == unit.Hero.Id);
        }
        for (var index = 0; index < Enemies.Count; index++)
        {
            var unit = battle.Enemies[index];
            Enemies[index].Update(unit, _session.CurrentEncounter?.Team.LeaderId == unit.Hero.Id);
        }
        foreach (var enemy in Enemies)
        {
            var intent = battle.Intents.FirstOrDefault(item => item.ActorId == enemy.Id);
            enemy.UpdateIntent(intent is null ? null : PreviewIntent(battle, enemy, intent));
        }
    }

    private EnemyIntentPreview PreviewIntent(Battle battle, HeroCardViewModel enemy, EnemyIntent intent)
    {
        var skill = intent.IsSpecial ? enemy.Hero.SpecialSkill : enemy.Hero.BasicSkill;
        var validTargets = battle.GetTargets(enemy.Id, intent.IsSpecial);
        if (skill.Target == TargetKind.AllEnemies && validTargets.Count > 0)
        {
            return EnemyIntentPreview.ForSkill(skill, null);
        }
        if (intent.TargetId is not null && validTargets.Any(target => target.Id == intent.TargetId))
        {
            var target = Cards.First(card => card.Id == intent.TargetId);
            return EnemyIntentPreview.ForSkill(skill, target);
        }
        return EnemyIntentPreview.Unavailable;
    }

    private void RefreshSelection()
    {
        var targets = _special.HasValue && CanPlay ? Targets(_special.Value).Select(unit => unit.Id).ToHashSet() : [];
        foreach (var card in Cards)
        {
            card.IsSelected = card == _actor;
            card.IsTarget = targets.Contains(card.Id);
        }
        Refresh();
    }
}
