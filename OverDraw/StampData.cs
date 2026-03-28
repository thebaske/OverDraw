namespace OverDraw;

public class StampStroke
{
    public List<double[]> Points { get; set; } = new(); // each is [x, y]
    public string ColorHex { get; set; } = "#FFFFFFFF";
    public double Thickness { get; set; } = 3.0;
}

public class StampData
{
    public string Name { get; set; } = "";
    public List<StampStroke> Strokes { get; set; } = new();
    public double CanvasWidth { get; set; } = 200;
    public double CanvasHeight { get; set; } = 200;

    public bool IsEmpty => Strokes.Count == 0;
}
