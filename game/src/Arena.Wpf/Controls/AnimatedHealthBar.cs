using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Arena.Wpf.Controls;

public sealed class AnimatedHealthBar : ProgressBar
{
    private bool _initialized;
    public static readonly DependencyProperty AnimateProperty = DependencyProperty.Register(
        nameof(Animate), typeof(bool), typeof(AnimatedHealthBar), new PropertyMetadata(true, OnAnimateChanged));
    public static readonly DependencyProperty HealthProperty = DependencyProperty.Register(
        nameof(Health), typeof(double), typeof(AnimatedHealthBar), new PropertyMetadata(0d, OnHealthChanged));

    public bool Animate
    {
        get => (bool)GetValue(AnimateProperty);
        set => SetValue(AnimateProperty, value);
    }

    private static void OnAnimateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (!(bool)args.NewValue)
        {
            var bar = (AnimatedHealthBar)sender;
            bar.BeginAnimation(ValueProperty, null);
            bar.ClearValue(ForegroundProperty);
        }
    }

    public double Health
    {
        get => (double)GetValue(HealthProperty);
        set => SetValue(HealthProperty, value);
    }

    private static void OnHealthChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var bar = (AnimatedHealthBar)sender;
        var health = (double)args.NewValue;
        var previous = bar.Value;
        bar.BeginAnimation(ValueProperty, null);
        bar.Value = health;
        if (bar._initialized && bar.Animate)
        {
            bar.BeginAnimation(ValueProperty, new DoubleAnimation(previous, health, TimeSpan.FromMilliseconds(260)));
            if (health < previous)
            {
                var brush = new SolidColorBrush(Color.FromRgb(157, 212, 198));
                bar.Foreground = brush;
                brush.BeginAnimation(SolidColorBrush.ColorProperty,
                    new ColorAnimation(Color.FromRgb(240, 135, 110), Color.FromRgb(157, 212, 198), TimeSpan.FromMilliseconds(450)));
            }
        }
        bar._initialized = true;
    }
}
