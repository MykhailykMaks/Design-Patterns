using System.Collections.ObjectModel;
using System.Windows;
using Arena.Core;
using Arena.Wpf.Infrastructure;

namespace Arena.Wpf.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly GameSession _session;
    private Difficulty _draftDifficulty;
    private bool _settingsSaved;
    private string _screen = "Menu";
    private string _message = "";
    private bool _isBusy;
    private readonly List<RelayCommand> _commands = [];
    private readonly List<AsyncRelayCommand> _asyncCommands = [];

    public MainViewModel() : this(new GameSession()) { }

    public MainViewModel(GameSession session)
    {
        _session = session;
        Heroes = new ObservableCollection<HeroCardViewModel>(HeroCatalog.PlayerHeroes.Select(hero => new HeroCardViewModel(hero)));
        _selectedHero = Heroes[0];
        _selectedHero.IsInspected = true;
        StartCommand = Command(_ => OpenTeam());
        SettingsCommand = Command(_ =>
        {
            _draftDifficulty = _session.SelectedDifficulty;
            _settingsSaved = false;
            Navigate("Settings");
        });
        SaveSettingsCommand = Command(_ =>
        {
            _session.SelectedDifficulty = _draftDifficulty;
            _settingsSaved = true;
            Refresh();
        });
        MenuCommand = Command(_ => Navigate("Menu"), _ => !IsBusy);
        ExitCommand = Command(_ => Application.Current.Shutdown());
        ChooseDifficultyCommand = Command(value =>
        {
            if (value is DifficultyOption option)
            {
                _draftDifficulty = option.Value;
                _settingsSaved = false;
                Message = "";
                Refresh();
            }
        });
        InspectHeroCommand = Command(value =>
        {
            if (value is HeroCardViewModel hero) { SelectedHero = hero; }
        });
        ToggleHeroCommand = Command(_ => ToggleHero(), _ => !IsBusy && (SelectedHero.IsSelected || Team.Count < 3));
        SetLeaderCommand = Command(value => SetLeader(value as HeroCardViewModel), value => value is HeroCardViewModel { IsLeader: false, HasAura: true });
        BeginBattleCommand = Command(_ => BeginBattle(), _ => !IsBusy && CanBeginBattle);
        CardCommand = AsyncCommand(HandleCardAsync, _ => CanPlay);
        BasicCommand = Command(_ => SelectSkill(false), _ => CanChooseBasic);
        SpecialCommand = Command(_ => SelectSkill(true), _ => CanChooseSpecial);
        ConfirmAllCommand = AsyncCommand(_ => UseSkillAsync(null), _ => CanConfirmAll);
        RestoreCommand = Command(_ => RestoreRound(), _ => !IsBusy && _facade?.Battle.CanRestore == true);
        CyclePlaybackSpeedCommand = Command(_ => CyclePlaybackSpeed());
        ContinueCommand = Command(_ => ContinueBattle(), _ => !IsBusy && Screen == "Result" && _facade?.Battle.Phase == BattlePhase.Finished);
        ChangeTeamCommand = Command(_ => OpenTeam(), _ => !IsBusy);
    }

    public string Screen { get => _screen; private set => Set(ref _screen, value); }
    public string Message { get => _message; private set => Set(ref _message, value); }
    public bool IsBusy { get => _isBusy; private set { Set(ref _isBusy, value); Refresh(); } }
    public bool CanNavigate => !IsBusy;
    public ObservableCollection<HeroCardViewModel> Heroes { get; }
    public IReadOnlyList<DifficultyOption> Difficulties { get; } =
    [
        new(Difficulty.Normal, "First Blood", "Здоров’я ×1,00  ·  Атака ×1,00", "Вороги цілять у героя з найменшим запасом здоров’я."),
        new(Difficulty.Hard, "Trial by Fire", "Здоров’я ×1,25  ·  Атака ×1,15", "Вибираючи ціль, вороги враховують її здоров’я та щит."),
        new(Difficulty.Nightmare, "Against All Odds", "Здоров’я ×1,50  ·  Атака ×1,30", "Вороги враховують щити та частіше атакують героїв підтримки.")
    ];
    public string SelectedDifficultyText => Presentation.DifficultyName(_session.SelectedDifficulty);
    public string EncounterSummary => _session.CurrentEncounter is { } encounter
        ? $"Поточний суперник · {Presentation.DifficultyName(encounter.Difficulty)}. Його параметри збережено."
        : $"Наступний суперник · {SelectedDifficultyText}";
    public bool SettingsSaved => _settingsSaved;
    public string SettingsNote => $"Обрано: {Presentation.DifficultyName(_draftDifficulty)}. Для наступного нового суперника.\nПоточна зустріч зберігає свою складність, склад і характеристики.";

    public RelayCommand StartCommand { get; }
    public RelayCommand SaveSettingsCommand { get; }
    public RelayCommand SettingsCommand { get; }
    public RelayCommand MenuCommand { get; }
    public RelayCommand ExitCommand { get; }
    public RelayCommand ChooseDifficultyCommand { get; }
    public RelayCommand InspectHeroCommand { get; }
    public RelayCommand ToggleHeroCommand { get; }
    public RelayCommand SetLeaderCommand { get; }
    public RelayCommand BeginBattleCommand { get; }
    public AsyncRelayCommand CardCommand { get; }
    public RelayCommand BasicCommand { get; }
    public RelayCommand SpecialCommand { get; }
    public AsyncRelayCommand ConfirmAllCommand { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand CyclePlaybackSpeedCommand { get; }
    public RelayCommand ContinueCommand { get; }
    public RelayCommand ChangeTeamCommand { get; }

    private RelayCommand Command(Action<object?> action, Predicate<object?>? canExecute = null)
    {
        var command = new RelayCommand(value =>
        {
            try { action(value); }
            catch (Exception exception) { Message = $"Не вдалося виконати дію: {exception.Message}"; }
        }, canExecute);
        _commands.Add(command);
        return command;
    }

    private AsyncRelayCommand AsyncCommand(Func<object?, Task> action, Predicate<object?>? canExecute = null)
    {
        var command = new AsyncRelayCommand(action, exception =>
        {
            Message = $"Не вдалося виконати дію: {exception.Message}";
            IsBusy = false;
        }, canExecute);
        _asyncCommands.Add(command);
        return command;
    }

    private void Navigate(string screen)
    {
        if (IsBusy) { return; }
        Message = "";
        Screen = screen;
        Refresh();
    }

    private void Refresh()
    {
        RaiseAll();
        foreach (var command in _commands) { command.Refresh(); }
        foreach (var command in _asyncCommands) { command.Refresh(); }
    }
}
