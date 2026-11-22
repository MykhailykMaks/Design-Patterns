using Arena.Core;

namespace Arena.Wpf.ViewModels;

public sealed record BattleLogEntry(int Round, string Message, string Symbol, string Color, string Category)
{
    public static BattleLogEntry From(BattleEvent entry)
    {
        var (symbol, color, label) = entry.Kind switch
        {
            BattleEventKind.Ability => ("›", "#CCBB9B", "Навичка"),
            BattleEventKind.Damage => ("−", "#EEAB95", "Удар"),
            BattleEventKind.Critical => ("✦", "#FFCB69", "Критичний удар"),
            BattleEventKind.Heal => ("+", "#99DEC6", "Лікування"),
            BattleEventKind.Shield => ("◇", "#A7CEFA", "Щит"),
            BattleEventKind.Buff => ("↑", "#9FDCC1", "Позитивний ефект"),
            BattleEventKind.Debuff when entry.Effect == EffectKind.Stun => ("✦", "#FFCE84", "Оглушення"),
            BattleEventKind.Debuff => ("↓", "#D5A9DE", "Негативний ефект"),
            BattleEventKind.Periodic => ("◈", "#D5A9DE", "Періодична шкода"),
            BattleEventKind.StunSkip => ("✦", "#FFCE84", "Оглушення"),
            BattleEventKind.StunProtectionEnded => ("⊘", "#B4DBEC", "Захист завершено"),
            BattleEventKind.Skip => ("↷", "#BAAFBE", "Пропуск дії"),
            BattleEventKind.Defeat => ("×", "#F4A294", "Герой вибув"),
            BattleEventKind.RoundStart => ("▶", "#DEC28D", "Початок раунду"),
            BattleEventKind.RoundEnd => ("■", "#CAAF86", "Кінець раунду"),
            _ => ("◆", "#DEC28D", "Підсумок")
        };
        return new(entry.Round, entry.Message, symbol, color, label);
    }
}

public sealed record StatusBadge(string Label, string Color, string Description);
