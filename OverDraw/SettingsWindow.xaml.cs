using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OverDraw;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action _onSettingsChanged;
    private bool _suppressEvents;

    public SettingsWindow(AppSettings settings, Action onSettingsChanged)
    {
        _settings = settings;
        _onSettingsChanged = onSettingsChanged;
        _suppressEvents = true;

        InitializeComponent();

        ColorHexBox.Text = _settings.PenColorHex;
        UpdateColorSwatch();
        ThicknessSlider.Value = _settings.PenThickness;
        FadeSlider.Value = _settings.FadeDurationSeconds;

        var idx = _settings.ModifierKey switch
        {
            "Shift" => 1,
            "Alt" => 2,
            _ => 0
        };
        ModifierCombo.SelectedIndex = idx;
        _suppressEvents = false;

        BuildStampSlots();
    }

    private void BuildStampSlots()
    {
        StampSlotsPanel.Children.Clear();
        _settings.EnsureStampSlots();

        for (int i = 0; i < 10; i++)
        {
            var slotIndex = i;
            var stamp = _settings.Stamps[i];
            var label = $"{(i + 1) % 10}";

            var border = new Border
            {
                Width = 60,
                Height = 60,
                Margin = new Thickness(0, 0, 6, 6),
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#55FFFFFF")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#FF1E1E1E")),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = $"Ctrl+{label} — Click to edit"
            };

            var grid = new Grid();

            // Draw stamp preview if it exists
            if (stamp != null && !stamp.IsEmpty)
            {
                var canvas = new Canvas { Width = 60, Height = 60, ClipToBounds = true };
                RenderStampPreview(canvas, stamp);
                grid.Children.Add(canvas);
            }
            else
            {
                grid.Children.Add(new TextBlock
                {
                    Text = label,
                    FontSize = 18,
                    Foreground = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#44FFFFFF")),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                });
            }

            border.Child = grid;
            border.MouseLeftButtonUp += (_, _) => OpenStampEditor(slotIndex);
            StampSlotsPanel.Children.Add(border);
        }
    }

    private void RenderStampPreview(Canvas canvas, StampData stamp)
    {
        foreach (var strokeData in stamp.Strokes)
        {
            if (strokeData.Points.Count < 2) continue;

            WpfColor color;
            try { color = (WpfColor)WpfColorConverter.ConvertFromString(strokeData.ColorHex); }
            catch { color = Colors.White; }

            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = Math.Max(1, strokeData.Thickness * (60.0 / stamp.CanvasWidth)),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };

            foreach (var pt in strokeData.Points)
            {
                polyline.Points.Add(new WpfPoint(
                    pt[0] * 60,
                    pt[1] * 60));
            }
            canvas.Children.Add(polyline);
        }
    }

    private void OpenStampEditor(int slotIndex)
    {
        var editor = new StampEditorWindow(slotIndex, _settings.Stamps[slotIndex], OnStampSaved);
        editor.ShowDialog();
    }

    private void OnStampSaved(int slotIndex, StampData stamp)
    {
        _settings.EnsureStampSlots();
        _settings.Stamps[slotIndex] = stamp;
        _settings.Save();
        _onSettingsChanged();
        BuildStampSlots();
    }

    private void UpdateColorSwatch()
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(ColorHexBox.Text);
            ColorSwatch.Background = new SolidColorBrush(color);
        }
        catch { }
    }

    private void OnColorHexChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressEvents) return;
        UpdateColorSwatch();
        _settings.PenColorHex = ColorHexBox.Text;
        Apply();
    }

    private void OnColorSwatchClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        string[] presets = ["#FFFF0000", "#FF00FF00", "#FF0000FF", "#FFFFFF00",
                            "#FFFF00FF", "#FF00FFFF", "#FFFFFFFF", "#FFFF6600"];
        var current = _settings.PenColorHex.ToUpperInvariant();
        var idx = Array.IndexOf(presets, current);
        var next = presets[(idx + 1) % presets.Length];

        _suppressEvents = true;
        ColorHexBox.Text = next;
        _settings.PenColorHex = next;
        UpdateColorSwatch();
        _suppressEvents = false;
        Apply();
    }

    private void OnThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressEvents || ThicknessLabel == null) return;
        _settings.PenThickness = ThicknessSlider.Value;
        ThicknessLabel.Text = $"Thickness: {_settings.PenThickness:0}";
        Apply();
    }

    private void OnFadeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressEvents || FadeLabel == null) return;
        _settings.FadeDurationSeconds = FadeSlider.Value;
        FadeLabel.Text = $"Fade: {_settings.FadeDurationSeconds:0.0}s";
        Apply();
    }

    private void OnModifierChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        _settings.ModifierKey = ModifierCombo.SelectedIndex switch
        {
            1 => "Shift",
            2 => "Alt",
            _ => "Ctrl"
        };
        Apply();
    }

    private void Apply()
    {
        _settings.Save();
        _onSettingsChanged();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
