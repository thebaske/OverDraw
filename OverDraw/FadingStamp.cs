using System.Windows;
using System.Windows.Media;

namespace OverDraw;

public class FadingStamp
{
    public StampData Stamp { get; init; } = null!;
    public WpfPoint Center { get; init; }
    public double FadeDuration { get; init; }
    public DateTime SpawnTime { get; init; }
    public double Scale { get; private set; } = 0;
    public double Opacity { get; private set; } = 1.0;
    public bool IsExpired => _phase == Phase.Done;

    private enum Phase { PopIn, Hold, FadeOut, Done }
    private Phase _phase = Phase.PopIn;

    // Pop-in takes 0.25s, hold 0.15s, then fade for FadeDuration
    private const double PopDuration = 0.25;
    private const double HoldDuration = 0.15;

    public void Update()
    {
        var elapsed = (DateTime.UtcNow - SpawnTime).TotalSeconds;

        if (elapsed < PopDuration)
        {
            _phase = Phase.PopIn;
            // Elastic pop: 0 -> 1.15 -> 1.0
            var t = elapsed / PopDuration;
            Scale = t < 0.7
                ? (t / 0.7) * 1.15
                : 1.15 - 0.15 * ((t - 0.7) / 0.3);
            Opacity = 1.0;
        }
        else if (elapsed < PopDuration + HoldDuration)
        {
            _phase = Phase.Hold;
            Scale = 1.0;
            Opacity = 1.0;
        }
        else
        {
            _phase = Phase.FadeOut;
            var fadeElapsed = elapsed - PopDuration - HoldDuration;
            Scale = 1.0;
            Opacity = Math.Max(0, 1.0 - fadeElapsed / FadeDuration);
            if (Opacity <= 0) _phase = Phase.Done;
        }
    }

    public void Render(DrawingContext dc)
    {
        if (IsExpired || Stamp.IsEmpty) return;

        var scaleX = Scale / Stamp.CanvasWidth;
        var scaleY = Scale / Stamp.CanvasHeight;
        // Render size = canvas size * Scale, centered on Center
        var renderW = Stamp.CanvasWidth * Scale;
        var renderH = Stamp.CanvasHeight * Scale;
        var originX = Center.X - renderW / 2;
        var originY = Center.Y - renderH / 2;

        foreach (var strokeData in Stamp.Strokes)
        {
            if (strokeData.Points.Count < 2) continue;

            WpfColor color;
            try { color = (WpfColor)WpfColorConverter.ConvertFromString(strokeData.ColorHex); }
            catch { color = Colors.White; }
            color.A = (byte)(255 * Opacity);

            var pen = new WpfPen(new SolidColorBrush(color), strokeData.Thickness * Scale)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                var first = strokeData.Points[0];
                ctx.BeginFigure(new WpfPoint(originX + first[0] * scaleX * Stamp.CanvasWidth,
                                              originY + first[1] * scaleY * Stamp.CanvasHeight), false, false);
                for (int i = 1; i < strokeData.Points.Count; i++)
                {
                    var p = strokeData.Points[i];
                    ctx.LineTo(new WpfPoint(originX + p[0] * scaleX * Stamp.CanvasWidth,
                                             originY + p[1] * scaleY * Stamp.CanvasHeight), true, true);
                }
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        }
    }
}
