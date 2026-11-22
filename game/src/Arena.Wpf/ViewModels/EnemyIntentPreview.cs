using System.Windows.Media;
using Arena.Core;

namespace Arena.Wpf.ViewModels;

public sealed record EnemyIntentPreview(string Icon, string Category, string Color, string SkillName, string Detail,
    string TargetName = "", Brush? TargetPortrait = null)
{
    public bool HasTarget => TargetName.Length > 0;
    public bool HasPortrait => TargetPortrait is not null;

    internal static EnemyIntentPreview Stunned { get; } =
        new("✦", "Пропуск", "#FFCE84", "Оглушення", "Ворог пропускає дію через оглушення.");

    internal static EnemyIntentPreview Unavailable { get; } =
        new("↷", "Пропуск", "#B0BABD", "Ціль недоступна", "Ворог не може застосувати оголошену навичку до своєї цілі.");

    internal static EnemyIntentPreview ForSkill(SkillDefinition skill, HeroCardViewModel? target)
    {
        var effects = skill.Effects;
        var (icon, label, color) = skill.Target switch
        {
            TargetKind.AllEnemies => ("✹", "Масова атака", "#FFB28E"),
            _ when effects.Any(effect => effect.Kind == EffectKind.Heal) => ("✚", "Лікування", "#99E1BF"),
            _ when effects.Any(effect => effect.Kind == EffectKind.Shield) => ("◇", "Щит", "#A7CEFA"),
            _ when effects.Any(effect => effect.Kind == EffectKind.Empower) => ("↑", "Підсилення", "#9FDCC1"),
            _ when effects.Any(effect => effect.Kind == EffectKind.Stun) => ("✦", "Контроль", "#FFCE84"),
            _ => ("⚔", "Атака", "#F3AD95")
        };
        var targetName = skill.Target == TargetKind.AllEnemies ? "Уся команда" : target?.Name ?? "";
        return new(icon, label, color, skill.Name,
            $"{skill.Name} → {targetName}\n{skill.Description}", targetName, target?.Portrait);
    }
}
