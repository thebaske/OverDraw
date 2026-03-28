using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OverDraw;

public partial class StampEditorWindow : Window
{
    private readonly int _slotIndex;
    private readonly Action<int, StampData> _onSave;
    private readonly List<StampStroke> _strokes = new();
    private StampStroke? _currentStroke;
    private Polyline? _currentPolyline;
    private string _currentColorHex = "#FFFFFFFF";

    private static readonly string[] ColorPresets =
        ["#FFFFFFFF", "#FFFF0000", "#FF00FF00", "#FF0000FF",
         "#FFFFFF00", "#FFFF00FF", "#FF00FFFF", "#FFFF6600"];
    private int _colorIndex;

    public StampEditorWindow(int slotIndex, StampData? existing, Action<int, StampData> onSave)
    {
        _slotIndex = slotIndex;
        _onSave = onSave;

        InitializeComponent();

        TitleLabel.Text = $"Stamp {slotIndex + 1} (Ctrl+{(slotIndex + 1) % 10})";
        UpdateSwatchColor();

        // Load existing stamp if any
        if (existing != null && !existing.IsEmpty)
            LoadExistingStamp(existing);
    }

    private void LoadExistingStamp(StampData stamp)
    {
        foreach (var strokeData in stamp.Strokes)
        {
            _strokes.Add(strokeData);
            var polyline = CreatePolyline(strokeData.ColorHex, strokeData.Thickness);
            foreach (var pt in strokeData.Points)
            {
                var canvasW = DrawCanvas.ActualWidth > 0 ? DrawCanvas.ActualWidth : 200;
                var canvasH = DrawCanvas.ActualHeight > 0 ? DrawCanvas.ActualHeight : 200;
                polyline.Points.Add(new WpfPoint(pt[0] * canvasW, pt[1] * canvasH));
            }
            DrawCanvas.Children.Add(polyline);
        }
    }

    private Polyline CreatePolyline(string colorHex, double thickness)
    {
        WpfColor color;
        try { color = (WpfColor)WpfColorConverter.ConvertFromString(colorHex); }
        catch { color = Colors.White; }

        return new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        };
    }

    private void UpdateSwatchColor()
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(_currentColorHex);
            StampColorSwatch.Background = new SolidColorBrush(color);
        }
        catch { }
    }

    private void OnStampColorClick(object sender, MouseButtonEventArgs e)
    {
        _colorIndex = (_colorIndex + 1) % ColorPresets.Length;
        _currentColorHex = ColorPresets[_colorIndex];
        UpdateSwatchColor();
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        DrawCanvas.CaptureMouse();
        var pos = e.GetPosition(DrawCanvas);

        _currentStroke = new StampStroke
        {
            ColorHex = _currentColorHex,
            Thickness = StampThicknessSlider.Value
        };

        var canvasW = DrawCanvas.ActualWidth;
        var canvasH = DrawCanvas.ActualHeight;
        _currentStroke.Points.Add([pos.X / canvasW, pos.Y / canvasH]);

        _currentPolyline = CreatePolyline(_currentColorHex, StampThicknessSlider.Value);
        _currentPolyline.Points.Add(pos);
        DrawCanvas.Children.Add(_currentPolyline);
    }

    private void OnCanvasMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_currentStroke == null || _currentPolyline == null) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(DrawCanvas);
        var canvasW = DrawCanvas.ActualWidth;
        var canvasH = DrawCanvas.ActualHeight;

        _currentStroke.Points.Add([pos.X / canvasW, pos.Y / canvasH]);
        _currentPolyline.Points.Add(pos);
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        DrawCanvas.ReleaseMouseCapture();
        if (_currentStroke != null && _currentStroke.Points.Count >= 2)
            _strokes.Add(_currentStroke);
        _currentStroke = null;
        _currentPolyline = null;
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        _strokes.Clear();
        DrawCanvas.Children.Clear();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var stamp = new StampData
        {
            Name = $"Stamp {_slotIndex + 1}",
            Strokes = new List<StampStroke>(_strokes),
            CanvasWidth = DrawCanvas.ActualWidth > 0 ? DrawCanvas.ActualWidth : 200,
            CanvasHeight = DrawCanvas.ActualHeight > 0 ? DrawCanvas.ActualHeight : 200
        };
        _onSave(_slotIndex, stamp);
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
