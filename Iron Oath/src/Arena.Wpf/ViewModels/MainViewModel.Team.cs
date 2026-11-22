using System.Collections.ObjectModel;
using Arena.Core;

namespace Arena.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private HeroCardViewModel _selectedHero;
    public ObservableCollection<HeroCardViewModel> Team { get; } = [];
    public HeroCardViewModel SelectedHero
    {
        get => _selectedHero;
        private set
        {
            _selectedHero.IsInspected = false;
            value.IsInspected = true;
            Set(ref _selectedHero, value);
            Refresh();
        }
    }
    public bool CanBeginBattle => Team.Count == 3 && Team.Any(hero => hero.IsLeader && hero.HasAura);
    public string ToggleHeroLabel => SelectedHero.IsSelected ? "Прибрати із команди" : "Додати до команди";
    public string TeamCount => $"ВАША КОМАНДА · {Team.Count} / 3";
    public string TeamHint => Team.Count < 3
        ? Team.Count switch { 0 => "Оберіть трьох героїв, серед них хоча б одного з аурою.", 1 => "Оберіть ще двох героїв.", _ => "Оберіть ще одного героя." }
        : Team.Any(hero => hero.IsLeader) ? "Команда готова. Діє лише аура обраного лідера." : "Потрібен лідер з аурою: Даран або Торен.";
    public string TeamAura => Team.FirstOrDefault(hero => hero.IsLeader)?.Aura ?? "Лідера ще не призначено";

    private void OpenTeam() => Navigate("Team");

    private void ToggleHero()
    {
        var hero = SelectedHero;
        if (hero.IsSelected)
        {
            hero.IsSelected = false;
            hero.IsLeader = false;
            Team.Remove(hero);
        }
        else if (Team.Count < 3)
        {
            hero.IsSelected = true;
            Team.Add(hero);
        }
        if (!Team.Any(member => member.IsLeader))
        {
            var leader = Team.FirstOrDefault(member => member.HasAura);
            if (leader is not null) { leader.IsLeader = true; }
        }
        Refresh();
    }

    private void SetLeader(HeroCardViewModel? leader)
    {
        if (leader is null || !leader.HasAura || !Team.Contains(leader)) { return; }
        foreach (var member in Team) { member.IsLeader = member == leader; }
        Refresh();
    }

    private TeamDefinition BuildTeam()
    {
        var builder = new TeamBuilder();
        foreach (var hero in Team) { builder.Add(hero.Hero); }
        builder.SetLeader(Team.Single(hero => hero.IsLeader).Hero.Id);
        return builder.Build();
    }

    private void BeginBattle()
    {
        _session.StartBattle(BuildTeam());
        BindBattle();
    }

    private void ContinueBattle()
    {
        _session.RetryBattle();
        BindBattle();
    }
}
