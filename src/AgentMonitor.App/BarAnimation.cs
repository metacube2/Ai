using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace AgentMonitor.App;

/// <summary>
/// Laesst den Wert eines Fortschrittsbalkens weich zum neuen Ziel laufen, statt zu springen.
/// Verwendung: <c>local:BarAnimation.Value="{Binding Progress}"</c> statt <c>Value="..."</c>.
/// Die Prozentzahl im Balken haengt am echten <see cref="RangeBase.Value"/> und zaehlt deshalb mit.
/// </summary>
public static class BarAnimation
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
        "Value", typeof(double), typeof(BarAnimation), new PropertyMetadata(0.0, OnValueChanged));

    public static double GetValue(DependencyObject o) => (double)o.GetValue(ValueProperty);
    public static void SetValue(DependencyObject o, double v) => o.SetValue(ValueProperty, v);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RangeBase bar) return;
        var to = Math.Clamp((double)e.NewValue, bar.Minimum, bar.Maximum);
        var anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(600))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.HoldEnd,
        };
        bar.BeginAnimation(RangeBase.ValueProperty, anim, HandoffBehavior.SnapshotAndReplace);
    }
}
