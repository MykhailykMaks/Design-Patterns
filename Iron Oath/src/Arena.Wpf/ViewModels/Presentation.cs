using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Arena.Core;

namespace Arena.Wpf.ViewModels;

public static class Presentation
{
    private static readonly BitmapImage Atlas = LoadAtlas();

    private static BitmapImage LoadAtlas()
    {
        var image = new BitmapImage(new Uri("pack://application:,,,/Arena.Tactics;component/Assets/Portraits.png"));
        image.Freeze();
        return image;
    }

    public static string DifficultyName(Difficulty value) => value switch
    {
        Difficulty.Hard => "Trial by Fire", Difficulty.Nightmare => "Against All Odds", _ => "First Blood"
    };

    public static string RoleName(HeroRole value) => value switch
    {
        HeroRole.Damage => "Боєць", HeroRole.Support => "Підтримка",
        HeroRole.Control => "Контроль", _ => "Захисник"
    };

    public static string AuraText(AuraKind value) => value switch
    {
        AuraKind.Attack => "Аура лідера: атака всієї команди +15%",
        AuraKind.Health => "Аура лідера: здоров’я всієї команди +20%", _ => "Без аури"
    };

    public static Brush Portrait(HeroDefinition hero)
    {
        var index = hero.Id switch
        {
            "daran" => 0, "lira" => 1, "mira" => 2, "saira" => 3,
            "toren" => 4, "crusher" => 5, "ash-archer" => 6, "reaper" => 7,
            "bone-doctor" => 8, "seal-keeper" => 9, "hexer" => 10, "will-weaver" => 11,
            _ => throw new ArgumentException("Для героя бракує портрета.", nameof(hero))
        };
        var cell = new Rect(index % 4 / 4d, index / 4 / 3d, 1d / 4, 1d / 3);
        var brush = new ImageBrush(Atlas) { Viewbox = cell, ViewboxUnits = BrushMappingMode.RelativeToBoundingBox, Stretch = Stretch.Uniform };
        brush.Freeze();
        return brush;
    }
}
