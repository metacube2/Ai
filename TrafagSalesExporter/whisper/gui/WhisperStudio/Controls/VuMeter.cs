using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace WhisperStudio.Controls;

/// <summary>
/// Analog VU meter in the style of a classic tape deck: backlit face, voltage-linear scale
/// from -20 to +3 VU with a red zone above 0, a damped needle and a PEAK lamp.
/// 0 VU corresponds to -18 dBFS RMS (usual digital alignment); the lamp lights from -1 dBFS peak.
/// </summary>
public sealed class VuMeter : FrameworkElement
{
    public static readonly DependencyProperty RmsProperty = DependencyProperty.Register(
        nameof(Rms), typeof(double), typeof(VuMeter), new PropertyMetadata(0.0, (d, _) => ((VuMeter)d).Wake()));

    public static readonly DependencyProperty PeakProperty = DependencyProperty.Register(
        nameof(Peak), typeof(double), typeof(VuMeter), new PropertyMetadata(0.0, (d, e) => ((VuMeter)d).OnPeak((double)e.NewValue)));

    /// <summary>RMS of the signal, 0..1 of full scale.</summary>
    public double Rms { get => (double)GetValue(RmsProperty); set => SetValue(RmsProperty, value); }

    /// <summary>Sample peak of the signal, 0..1 of full scale.</summary>
    public double Peak { get => (double)GetValue(PeakProperty); set => SetValue(PeakProperty, value); }

    private const double ReferenceDbfs = -18;          // 0 VU
    private const double MaxVu = 3;
    private const double MinVu = -20;
    private const double HalfSweepDeg = 47;
    private const double PeakLampThreshold = 0.891;      // -1 dBFS
    private static readonly TimeSpan PeakHold = TimeSpan.FromMilliseconds(1200);

    // Needle mechanics: slightly underdamped spring, reaches the reading in roughly 300 ms like a real VU.
    private const double Omega = 2 * Math.PI * 2.2;
    private const double Zeta = 0.78;

    private double _pos, _vel;
    private TimeSpan _lastFrame = TimeSpan.Zero;
    private DateTime _peakUntil = DateTime.MinValue;
    private bool _peakLit;
    private bool _hooked;
    private DrawingGroup? _face;
    private Size _faceSize;

    public VuMeter()
    {
        SnapsToDevicePixels = true;
        IsVisibleChanged += (_, _) => Wake();
        Unloaded += (_, _) => Unhook();
        ToolTip = "Lautstärke wie am Tonbandgerät. Die Nadel sollte beim Sprechen um 0 pendeln; "
                + "bleibt sie im roten Bereich oder leuchtet PEAK, ist die Aufnahme zu laut.";
    }

    /// <summary>Needle position 0..1 along the scale for a VU reading (scale is linear in voltage).</summary>
    internal static double PositionForVu(double vu) => Math.Pow(10, (vu - MaxVu) / 20);

    /// <summary>Target needle position for an RMS value 0..1 of full scale.</summary>
    internal static double PositionForRms(double rms)
    {
        if (rms <= 0) return 0;
        var vu = 20 * Math.Log10(rms) - ReferenceDbfs;
        return Math.Min(1.06, PositionForVu(vu));
    }

    private void OnPeak(double peak)
    {
        if (peak >= PeakLampThreshold) _peakUntil = DateTime.UtcNow + PeakHold;
        Wake();
    }

    private void Wake()
    {
        if (IsVisible && !_hooked)
        {
            _lastFrame = TimeSpan.Zero;
            CompositionTarget.Rendering += OnFrame;
            _hooked = true;
        }
        else if (!IsVisible)
        {
            Unhook();
        }
    }

    private void Unhook()
    {
        if (!_hooked) return;
        CompositionTarget.Rendering -= OnFrame;
        _hooked = false;
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        var now = ((RenderingEventArgs)e).RenderingTime;
        if (now == _lastFrame) return;
        var dt = _lastFrame == TimeSpan.Zero ? 1.0 / 60 : Math.Min(0.05, (now - _lastFrame).TotalSeconds);
        _lastFrame = now;

        var target = PositionForRms(Rms);
        var acc = Omega * Omega * (target - _pos) - 2 * Zeta * Omega * _vel;
        _vel += acc * dt;
        _pos += _vel * dt;
        // Mechanical end stops with a soft bounce.
        if (_pos < -0.015) { _pos = -0.015; _vel = -_vel * 0.25; }
        if (_pos > 1.08) { _pos = 1.08; _vel = -_vel * 0.25; }

        var lit = DateTime.UtcNow < _peakUntil;
        var moving = Math.Abs(target - _pos) > 0.0005 || Math.Abs(_vel) > 0.0005;
        if (moving || lit != _peakLit)
        {
            _peakLit = lit;
            InvalidateVisual();
        }
        else if (!lit && Rms <= 0)
        {
            Unhook(); // at rest, nothing to animate until the next level arrives
        }
    }

    /// <summary>Puts the needle at rest on the reading for an RMS value (used by the render test).</summary>
    internal void SetNeedleForTest(double rms, bool peakLit)
    {
        _pos = PositionForRms(rms);
        _vel = 0;
        _peakLit = peakLit;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsInfinity(availableSize.Width) ? 340 : Math.Min(availableSize.Width, 420);
        return new Size(w, w * 0.56);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var size = RenderSize;
        if (size.Width < 40 || size.Height < 30) return;
        if (_face == null || _faceSize != size)
        {
            _face = BuildFace(size);
            _faceSize = size;
        }
        dc.DrawDrawing(_face);

        var g = Geometry(size);
        dc.PushClip(new RectangleGeometry(g.Face, 7, 7));

        // Needle with a soft shadow on the face.
        var angle = AngleFor(Math.Clamp(_pos, -0.03, 1.1));
        var tip = Polar(g, angle, g.R + g.FaceH * 0.10);
        var shadow = new Pen(new SolidColorBrush(Color.FromArgb(0x38, 0x40, 0x28, 0x00)), g.FaceH * 0.016) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var offset = new Vector(g.FaceH * 0.022, g.FaceH * 0.03);
        dc.DrawLine(shadow, g.Pivot + offset, tip + offset);
        var needle = new Pen(new SolidColorBrush(Color.FromRgb(0x1A, 0x16, 0x12)), g.FaceH * 0.012) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Triangle };
        dc.DrawLine(needle, g.Pivot, tip);

        // Hood over the pivot.
        var hood = new LinearGradientBrush(Color.FromRgb(0x3A, 0x36, 0x32), Color.FromRgb(0x12, 0x11, 0x10), 90);
        dc.DrawEllipse(hood, new Pen(new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF)), 0.8), g.Pivot, g.FaceH * 0.36, g.FaceH * 0.30);

        // Glass reflection over everything.
        var glass = new LinearGradientBrush(
            new GradientStopCollection
            {
                new(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF), 0),
                new(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF), 0.45),
                new(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.46),
            }, 90);
        dc.DrawRectangle(glass, null, new Rect(g.Face.X, g.Face.Y, g.Face.Width, g.Face.Height * 0.55));
        dc.Pop();

        // PEAK lamp.
        var lamp = new Point(g.Face.Right - g.FaceH * 0.12, g.Face.Top + g.FaceH * 0.13);
        var lampR = g.FaceH * 0.045;
        if (_peakLit)
        {
            var glow = new RadialGradientBrush(Color.FromArgb(0xA0, 0xFF, 0x30, 0x20), Color.FromArgb(0x00, 0xFF, 0x30, 0x20));
            dc.DrawEllipse(glow, null, lamp, lampR * 3.2, lampR * 3.2);
            dc.DrawEllipse(new RadialGradientBrush(Color.FromRgb(0xFF, 0xD0, 0xC0), Color.FromRgb(0xF0, 0x20, 0x10)) { GradientOrigin = new Point(0.35, 0.35) },
                new Pen(Brushes.Black, 0.6), lamp, lampR, lampR);
        }
        else
        {
            dc.DrawEllipse(new RadialGradientBrush(Color.FromRgb(0x8A, 0x3A, 0x30), Color.FromRgb(0x4A, 0x14, 0x10)) { GradientOrigin = new Point(0.35, 0.35) },
                new Pen(Brushes.Black, 0.6), lamp, lampR, lampR);
        }
    }

    private readonly record struct Geo(Rect Face, double FaceH, Point Pivot, double R);

    private static Geo Geometry(Size size)
    {
        const double bezel = 9;
        var face = new Rect(bezel, bezel, size.Width - 2 * bezel, size.Height - 2 * bezel);
        var faceH = face.Height;
        var pivot = new Point(face.X + face.Width / 2, face.Bottom + faceH * 0.16);
        var r = Math.Min(faceH * 0.86, face.Width / 2 / Math.Sin(HalfSweepDeg * Math.PI / 180) * 0.82);
        return new Geo(face, faceH, pivot, r);
    }

    private static double AngleFor(double position) => (-HalfSweepDeg + 2 * HalfSweepDeg * position) * Math.PI / 180;

    private static Point Polar(Geo g, double angle, double radius) =>
        new(g.Pivot.X + radius * Math.Sin(angle), g.Pivot.Y - radius * Math.Cos(angle));

    private DrawingGroup BuildFace(Size size)
    {
        var g = Geometry(size);
        var dg = new DrawingGroup();
        var dc = dg.Open();
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // Bezel.
        var bezelBrush = new LinearGradientBrush(Color.FromRgb(0x4A, 0x46, 0x42), Color.FromRgb(0x14, 0x13, 0x12), 90);
        dc.DrawRoundedRectangle(bezelBrush, new Pen(new SolidColorBrush(Color.FromRgb(0x08, 0x08, 0x08)), 1), new Rect(0.5, 0.5, size.Width - 1, size.Height - 1), 13, 13);

        // Backlit face.
        var faceBrush = new RadialGradientBrush(Color.FromRgb(0xFF, 0xF4, 0xD6), Color.FromRgb(0xE8, 0xC4, 0x74))
        {
            Center = new Point(0.5, 0.75), GradientOrigin = new Point(0.5, 0.8), RadiusX = 0.75, RadiusY = 1.0,
        };
        dc.DrawRoundedRectangle(faceBrush, new Pen(new SolidColorBrush(Color.FromArgb(0x90, 0x00, 0x00, 0x00)), 1.2), g.Face, 7, 7);
        dc.PushClip(new RectangleGeometry(g.Face, 7, 7));

        var ink = new SolidColorBrush(Color.FromRgb(0x24, 0x1E, 0x16));
        var red = new SolidColorBrush(Color.FromRgb(0xC4, 0x16, 0x10));
        var inkPen = new Pen(ink, Math.Max(1, g.FaceH * 0.008));
        var redPen = new Pen(red, Math.Max(1, g.FaceH * 0.008));

        // Main arc: black up to 0 VU, red band from 0 to +3.
        var zero = AngleFor(PositionForVu(0));
        dc.DrawGeometry(null, inkPen, Arc(g, AngleFor(0), zero, g.R));
        dc.DrawGeometry(null, new Pen(red, g.FaceH * 0.045), Arc(g, zero, AngleFor(1), g.R + g.FaceH * 0.022));

        // Ticks and numbers.
        var major = new (double Vu, string Label)[]
        {
            (-20, "20"), (-10, "10"), (-7, "7"), (-5, "5"), (-3, "3"), (-2, "2"), (-1, "1"), (0, "0"), (1, "1"), (2, "2"), (3, "3"),
        };
        foreach (var (vu, label) in major)
        {
            var a = AngleFor(PositionForVu(vu));
            var pen = vu > 0 ? redPen : inkPen;
            dc.DrawLine(new Pen(pen.Brush, pen.Thickness * 1.4), Polar(g, a, g.R), Polar(g, a, g.R + g.FaceH * 0.075));
            DrawText(dc, label, Polar(g, a, g.R + g.FaceH * 0.15), g.FaceH * 0.088, vu > 0 ? red : ink, dpi, FontWeights.SemiBold);
        }
        foreach (var vu in new[] { -15.0, -6, -4, -0.5, 0.5, 1.5, 2.5 })
        {
            var a = AngleFor(PositionForVu(vu));
            dc.DrawLine(vu > 0 ? redPen : inkPen, Polar(g, a, g.R), Polar(g, a, g.R + g.FaceH * 0.045));
        }
        DrawText(dc, "−", Polar(g, AngleFor(-0.04), g.R + g.FaceH * 0.26), g.FaceH * 0.12, ink, dpi, FontWeights.Bold);
        DrawText(dc, "+", Polar(g, AngleFor(1.04), g.R + g.FaceH * 0.26), g.FaceH * 0.12, red, dpi, FontWeights.Bold);

        // Inner percentage scale, as on studio meters.
        var thin = new Pen(new SolidColorBrush(Color.FromArgb(0xB0, 0x24, 0x1E, 0x16)), Math.Max(0.8, g.FaceH * 0.005));
        var inner = g.R - g.FaceH * 0.06;
        dc.DrawGeometry(null, thin, Arc(g, AngleFor(0), AngleFor(PositionForVu(0)), inner));
        foreach (var pct in new[] { 0, 20, 40, 60, 80, 100 })
        {
            var a = AngleFor(pct / 100.0 * PositionForVu(0));
            dc.DrawLine(thin, Polar(g, a, inner), Polar(g, a, inner - g.FaceH * 0.035));
            DrawText(dc, pct.ToString(CultureInfo.InvariantCulture), Polar(g, a, inner - g.FaceH * 0.085), g.FaceH * 0.055, thin.Brush, dpi, FontWeights.Normal);
        }

        // Legend.
        DrawText(dc, "VU", new Point(g.Pivot.X, g.Face.Bottom - g.FaceH * 0.33), g.FaceH * 0.16, ink, dpi, FontWeights.SemiBold, "Georgia");
        DrawText(dc, "PEAK", new Point(g.Face.Right - g.FaceH * 0.12, g.Face.Top + g.FaceH * 0.25), g.FaceH * 0.05, red, dpi, FontWeights.SemiBold);
        dc.Pop();
        dc.Close();
        dg.Freeze();
        return dg;
    }

    private static Geometry Arc(Geo g, double from, double to, double radius)
    {
        var fig = new PathFigure { StartPoint = Polar(g, from, radius), IsClosed = false };
        fig.Segments.Add(new ArcSegment(Polar(g, to, radius), new Size(radius, radius), 0, Math.Abs(to - from) > Math.PI, SweepDirection.Clockwise, true));
        var geo = new PathGeometry(new[] { fig });
        geo.Freeze();
        return geo;
    }

    private static void DrawText(DrawingContext dc, string text, Point center, double size, Brush brush, double dpi, FontWeight weight, string family = "Segoe UI Variable Text, Segoe UI")
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily(family), FontStyles.Normal, weight, FontStretches.Normal), Math.Max(6, size), brush, dpi);
        dc.DrawText(ft, new Point(center.X - ft.Width / 2, center.Y - ft.Height / 2));
    }
}
