using System.Windows;
using System.Windows.Media;

namespace OverDraw;

public class FadingStroke
{
    public PointCollection Points { get; } = new();
    public WpfColor Color { get; init; }
    public double Thickness { get; init; }
    public double FadeDuration { get; init; }
    public double Opacity { get; set; } = 1.0;
    public bool IsFading { get; set; }
    public DateTime FadeStartTime { get; set; }

    public bool IsExpired => IsFading && Opacity <= 0;

    public void StartFading()
    {
        IsFading = true;
        FadeStartTime = DateTime.UtcNow;
    }

    public void UpdateOpacity()
    {
        if (!IsFading) return;
        var elapsed = (DateTime.UtcNow - FadeStartTime).TotalSeconds;
        Opacity = Math.Max(0, 1.0 - elapsed / FadeDuration);
    }
}
