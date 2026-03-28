using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OverDraw;

public partial class ColorPickerWindow : Window
{
    private bool _suppressEvents;
    public WpfColor SelectedColor { get; private set; }
    public bool Confirmed { get; private set; }

    private static readonly string[] Palette =
    [
        // Row 1: basics
        "#FFFF0000", "#FFFF6600", "#FFFFCC00", "#FFFFFF00",
        "#FF00FF00", "#FF00CC66", "#FF00CCFF", "#FF0066FF",
        // Row 2: more hues
        "#FF0000FF", "#FF6600CC", "#FFCC00FF", "#FFFF00CC",
        "#FFFFFFFF", "#FFCCCCCC", "#FF888888", "#FF444444",
        // Row 3: soft tones
        "#FFFFAAAA", "#FFFFDDAA", "#FFFFFFAA", "#FFAAFFAA",
        "#FFAAFFFF", "#FFAAAAFF", "#FFFFAAFF", "#FF000000",
        // Row 4: vivid
        "#FFCC0000", "#FFCC6600", "#FF00CC00", "#FF009999",
        "#FF0000CC", "#FF6600FF", "#FFFF0066", "#FF663300"
    ];

    private readonly List<string> _recentColors;

    public ColorPickerWindow(WpfColor initial, List<string> recentColors)
    {
        _recentColors = recentColors;
        _suppressEvents = true;

        InitializeComponent();

        BuildPalette();
        BuildRecent();

        SelectedColor = initial;
        SliderR.Value = initial.R;
        SliderG.Value = initial.G;
        SliderB.Value = initial.B;
        HexBox.Text = initial.ToString();
        UpdatePreview();

        _suppressEvents = false;
    }

    private void BuildPalette()
    {
        foreach (var hex in Palette)
        {
            var swatch = CreateSwatchBorder(hex, 22);
            swatch.MouseLeftButtonUp += (_, _) => PickColor(hex);
            PalettePanel.Children.Add(swatch);
        }
    }

    private void BuildRecent()
    {
        RecentPanel.Children.Clear();
        foreach (var hex in _recentColors)
        {
            var swatch = CreateSwatchBorder(hex, 22);
            swatch.MouseLeftButtonUp += (_, _) => PickColor(hex);
            RecentPanel.Children.Add(swatch);
        }
    }

    private Border CreateSwatchBorder(string hex, double size)
    {
        WpfColor color;
        try { color = (WpfColor)WpfColorConverter.ConvertFromString(hex); }
        catch { color = Colors.White; }

        return new Border
        {
            Width = size,
            Height = size,
            Margin = new Thickness(0, 0, 3, 3),
            CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(color),
            BorderBrush = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#55FFFFFF")),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }

    private void PickColor(string hex)
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(hex);
            _suppressEvents = true;
            SliderR.Value = color.R;
            SliderG.Value = color.G;
            SliderB.Value = color.B;
            HexBox.Text = hex;
            SelectedColor = color;
            UpdatePreview();
            _suppressEvents = false;
        }
        catch { }
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressEvents || LabelR == null) return;

        var r = (byte)SliderR.Value;
        var g = (byte)SliderG.Value;
        var b = (byte)SliderB.Value;
        SelectedColor = WpfColor.FromArgb(255, r, g, b);

        LabelR.Text = r.ToString();
        LabelG.Text = g.ToString();
        LabelB.Text = b.ToString();

        _suppressEvents = true;
        HexBox.Text = SelectedColor.ToString();
        _suppressEvents = false;

        UpdatePreview();
    }

    private void OnHexChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressEvents) return;
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(HexBox.Text);
            SelectedColor = color;

            _suppressEvents = true;
            SliderR.Value = color.R;
            SliderG.Value = color.G;
            SliderB.Value = color.B;
            _suppressEvents = false;

            UpdatePreview();
        }
        catch { }
    }

    private void UpdatePreview()
    {
        PreviewSwatch.Background = new SolidColorBrush(SelectedColor);
    }

    private void AddToRecent()
    {
        var hex = SelectedColor.ToString().ToUpperInvariant();
        _recentColors.Remove(hex);
        _recentColors.Insert(0, hex);
        if (_recentColors.Count > 12)
            _recentColors.RemoveRange(12, _recentColors.Count - 12);
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        AddToRecent();
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
