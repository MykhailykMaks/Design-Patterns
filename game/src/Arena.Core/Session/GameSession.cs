namespace Arena.Core;

public sealed class GameSession(Random? random = null)
{
    private readonly EncounterGenerator _generator = new(random);

    public Difficulty SelectedDifficulty { get; set; }
    public EncounterDefinition? CurrentEncounter { get; private set; }
    public TeamDefinition? PlayerTeam { get; private set; }
    public Battle? Battle { get; private set; }

    public Battle StartBattle(TeamDefinition team)
    {
        ArgumentNullException.ThrowIfNull(team);
        if (CurrentEncounter is null || Battle?.Result == BattleResult.Victory)
        {
            CurrentEncounter = _generator.Generate(SelectedDifficulty, CurrentEncounter);
        }
        PlayerTeam = team;
        Battle = new Battle(team, CurrentEncounter, random?.Next());
        return Battle;
    }

    public Battle RetryBattle()
    {
        if (PlayerTeam is null || Battle is null || Battle.Result == BattleResult.InProgress)
        {
            throw new InvalidOperationException("Спочатку завершіть поточний бій.");
        }
        return StartBattle(PlayerTeam);
    }
}
