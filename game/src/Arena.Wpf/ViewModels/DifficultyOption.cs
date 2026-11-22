using Arena.Core;

namespace Arena.Wpf.ViewModels;

public sealed record DifficultyOption(Difficulty Value, string Name, string Multipliers, string Description)
{
    public string Hint => $"{Multipliers}\n{Description}";
}
