using Arena.Core;

namespace Arena.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private void CyclePlaybackSpeed()
    {
        _playbackMode = (_playbackMode + 1) % 3;
        foreach (var card in Cards) { card.AnimateHealth = _playbackMode != 2; }
        Raise(nameof(PlaybackSpeedText));
    }

    private Task PauseAsync(int milliseconds) => _playbackMode switch
    {
        0 => Task.Delay(milliseconds),
        1 => Task.Delay(milliseconds / 2),
        _ => Task.CompletedTask
    };

    private void ClearCues()
    {
        foreach (var card in Cards)
        {
            card.IsPerformer = false;
            card.ClearImpact();
        }
    }

    private void ShowCue(string title, string detail)
    {
        _phaseCue = title;
        _actionCue = detail;
        Raise(nameof(PhaseText));
        Raise(nameof(ActionHint));
        Raise(nameof(RoundText));
    }

    private async Task PlayEventsAsync(int firstEvent)
    {
        var events = _facade!.Battle.Events;
        for (var index = firstEvent; index < events.Count; index++)
        {
            var entry = events[index];
            var row = BattleLogEntry.From(entry);
            _shownRound = entry.Round;
            var actor = Cards.FirstOrDefault(card => card.Id == entry.ActorId);
            var target = Cards.FirstOrDefault(card => card.Id == entry.TargetId);
            if (entry.Kind == BattleEventKind.Ability)
            {
                ClearCues();
                if (actor is not null) { actor.IsPerformer = true; }
                var side = actor is not null && Enemies.Contains(actor) ? "Супротивник" : "Ваша команда";
                ShowCue(entry.SkillName ?? "Дія", $"{side}  ·  {actor?.Name}");
                BattleLog.Insert(0, row);
                await PauseAsync(340);
            }
            else if (entry.Kind is BattleEventKind.RoundStart or BattleEventKind.RoundEnd or BattleEventKind.Result or BattleEventKind.Skip)
            {
                ClearCues();
                ShowCue(row.Category, entry.Message);
                BattleLog.Insert(0, row);
                await PauseAsync(400);
            }
            else if (target is not null)
            {
                if (entry.Kind == BattleEventKind.StunProtectionEnded)
                {
                    target.ApplyImpact(entry, row);
                    BattleLog.Insert(0, row);
                    continue;
                }
                if (entry.Kind == BattleEventKind.StunSkip)
                {
                    ClearCues();
                    ShowCue(row.Category, entry.Message);
                    target.ApplyImpact(entry, row);
                    BattleLog.Insert(0, row);
                    await PauseAsync(420);
                    target.ClearImpact();
                    continue;
                }
                if (entry.Kind == BattleEventKind.Periodic) { ClearCues(); }
                target.IsReceiving = true;
                ShowCue(entry.Kind == BattleEventKind.Periodic ? row.Category : entry.SkillName ?? _phaseCue,
                    actor is null ? target.Name : $"{actor.Name}   →   {target.Name}");
                await PauseAsync(135);
                target.ApplyImpact(entry, row);
                BattleLog.Insert(0, row);
                await PauseAsync(entry.Kind == BattleEventKind.Critical ? 610 : 450);
                target.ClearImpact();
            }
            else
            {
                BattleLog.Insert(0, row);
            }
        }
        ClearCues();
        SynchronizeCards();
    }
}
