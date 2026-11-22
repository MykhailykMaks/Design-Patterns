using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Arena.Wpf.ViewModels;

namespace Arena.Wpf.Views;

public partial class BattleResultView : UserControl
{
    private static readonly Brush Ember = CreateBrush(Color.FromRgb(255, 201, 114));
    private static readonly Brush Ash = CreateBrush(Color.FromRgb(154, 137, 149));

    public BattleResultView() => InitializeComponent();

    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        Atmosphere.Children.Clear();
        if (!IsVisible || DataContext is not MainViewModel model || !SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        var victory = model.IsVictory;
        var duration = victory ? 420 : 780;
        ResultSlide.BeginAnimation(TranslateTransform.YProperty, Animate(victory ? 22 : -14, 0, duration));
        ResultContent.BeginAnimation(OpacityProperty, Animate(.45, 1, duration));
        if (victory)
        {
            VictoryHalo.BeginAnimation(OpacityProperty, Animate(.9, 0, 650));
            HaloScale.BeginAnimation(ScaleTransform.ScaleXProperty, Animate(.85, 1.5, 650));
            HaloScale.BeginAnimation(ScaleTransform.ScaleYProperty, Animate(.85, 1.5, 650));
        }

        AddParticles(victory);
    }

    private void AddParticles(bool victory)
    {
        var scatter = new Random(42);
        for (var index = 0; index < 28; index++)
        {
            var x = scatter.Next(40, 1000);
            var y = scatter.Next(75, 510);
            var size = scatter.Next(2, victory ? 7 : 5);
            var duration = victory ? scatter.Next(900, 2200) : scatter.Next(1700, 3100);
            var particle = new Ellipse { Width = size, Height = size, Fill = victory ? Ember : Ash, Opacity = 0 };
            var motion = new TranslateTransform();
            particle.RenderTransform = motion;
            Canvas.SetLeft(particle, x);
            Canvas.SetTop(particle, y);
            Atmosphere.Children.Add(particle);

            var delay = TimeSpan.FromMilliseconds(scatter.Next(0, 480));
            var drift = Animate(0, scatter.Next(-35, 36), duration);
            drift.BeginTime = delay;
            motion.BeginAnimation(TranslateTransform.XProperty, drift);
            var fall = Animate(0, victory ? -scatter.Next(50, 175) : scatter.Next(45, 125), duration);
            fall.BeginTime = delay;
            motion.BeginAnimation(TranslateTransform.YProperty, fall);
            var fade = new DoubleAnimationUsingKeyFrames { BeginTime = delay, FillBehavior = FillBehavior.Stop };
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(victory ? .8 : .48, TimeSpan.FromMilliseconds(180)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(duration)));
            particle.BeginAnimation(OpacityProperty, fade);
        }
    }

    private static DoubleAnimation Animate(double from, double to, int milliseconds) =>
        new(from, to, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

    private static Brush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
