using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace OverDraw;

public partial class OverlayWindow : Window
{
    private readonly List<FadingStroke> _strokes = new();
    private readonly List<FadingStamp> _stamps = new();
    private FadingStroke? _activeStroke;
    private readonly DispatcherTimer _renderTimer;
    private AppSettings _settings;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _mouseProc;
    private NativeMethods.LowLevelKeyboardProc? _keyboardProc;
    private bool _isDrawing;

    public OverlayWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
        };
        _renderTimer.Tick += OnRenderTick;

        Loaded += OnLoaded;
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionOverlay();
        MakeClickThrough();
        InstallMouseHook();
        InstallKeyboardHook();
        _renderTimer.Start();
    }

    private void PositionOverlay()
    {
        var x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        var y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        var cx = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        var cy = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

        Left = x;
        Top = y;
        Width = cx;
        Height = cy;
    }

    private void MakeClickThrough()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            exStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW);
    }

    private void InstallMouseHook()
    {
        _mouseProc = MouseHookCallback;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _mouseHookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _mouseProc,
            NativeMethods.GetModuleHandle(curModule.ModuleName),
            0);
    }

    private void InstallKeyboardHook()
    {
        _keyboardProc = KeyboardHookCallback;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _keyboardHookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _keyboardProc,
            NativeMethods.GetModuleHandle(curModule.ModuleName),
            0);
    }

    private bool IsModifierHeld()
    {
        var vk = _settings.GetModifierVirtualKey();
        return (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (int)wParam == NativeMethods.WM_KEYDOWN)
        {
            var hookStruct = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            int vk = (int)hookStruct.vkCode;

            // Check for modifier+1..0 (uses same modifier as drawing)
            if (vk >= NativeMethods.VK_0 && vk <= NativeMethods.VK_0 + 9)
            {
                if (IsModifierHeld())
                {
                    // VK_1=0x31 -> index 0, VK_2 -> index 1, ..., VK_0=0x30 -> index 9
                    int slotIndex = vk == NativeMethods.VK_0 ? 9 : vk - NativeMethods.VK_1;
                    Dispatcher.BeginInvoke(() => SpawnStamp(slotIndex));
                }
            }
        }
        return NativeMethods.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private void SpawnStamp(int slotIndex)
    {
        _settings.EnsureStampSlots();
        if (slotIndex < 0 || slotIndex >= _settings.Stamps.Count) return;
        var stamp = _settings.Stamps[slotIndex];
        if (stamp == null || stamp.IsEmpty) return;

        NativeMethods.GetCursorPos(out var cursorPt);
        var localPt = ScreenToLocal(cursorPt);

        _stamps.Add(new FadingStamp
        {
            Stamp = stamp,
            Center = localPt,
            FadeDuration = _settings.FadeDurationSeconds,
            SpawnTime = DateTime.UtcNow
        });
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            int msg = (int)wParam;

            if (msg == NativeMethods.WM_LBUTTONDOWN && IsModifierHeld())
            {
                StartDrawing(hookStruct.pt);
                return (IntPtr)1;
            }
            else if (msg == NativeMethods.WM_MOUSEMOVE && _isDrawing)
            {
                if (IsModifierHeld())
                {
                    AddPoint(hookStruct.pt);
                }
                else
                {
                    StopDrawing();
                }
            }
            else if (msg == NativeMethods.WM_LBUTTONUP && _isDrawing)
            {
                StopDrawing();
                return (IntPtr)1;
            }
        }
        return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void StartDrawing(NativeMethods.POINT pt)
    {
        _isDrawing = true;
        _activeStroke = new FadingStroke
        {
            Color = _settings.GetPenColor(),
            Thickness = _settings.PenThickness,
            FadeDuration = _settings.FadeDurationSeconds
        };
        _activeStroke.Points.Add(ScreenToLocal(pt));
        _strokes.Add(_activeStroke);
    }

    private void AddPoint(NativeMethods.POINT pt)
    {
        _activeStroke?.Points.Add(ScreenToLocal(pt));
    }

    private void StopDrawing()
    {
        _isDrawing = false;
        _activeStroke?.StartFading();
        _activeStroke = null;
    }

    private WpfPoint ScreenToLocal(NativeMethods.POINT pt)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            var transform = source.CompositionTarget.TransformFromDevice;
            var wpfPt = transform.Transform(new WpfPoint(pt.x, pt.y));
            return new WpfPoint(wpfPt.X - Left, wpfPt.Y - Top);
        }
        return new WpfPoint(pt.x - Left, pt.y - Top);
    }

    private void OnRenderTick(object? sender, EventArgs e)
    {
        bool needsRender = false;

        for (int i = _strokes.Count - 1; i >= 0; i--)
        {
            var stroke = _strokes[i];
            if (stroke.IsFading)
            {
                stroke.UpdateOpacity();
                if (stroke.IsExpired)
                    _strokes.RemoveAt(i);
                needsRender = true;
            }
            else if (stroke == _activeStroke)
            {
                needsRender = true;
            }
        }

        for (int i = _stamps.Count - 1; i >= 0; i--)
        {
            _stamps[i].Update();
            if (_stamps[i].IsExpired)
                _stamps.RemoveAt(i);
            needsRender = true;
        }

        if (needsRender || _strokes.Count > 0 || _stamps.Count > 0)
            InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        // Draw strokes
        foreach (var stroke in _strokes)
        {
            if (stroke.Points.Count < 2) continue;

            var color = stroke.Color;
            color.A = (byte)(255 * stroke.Opacity);
            var pen = new WpfPen(new SolidColorBrush(color), stroke.Thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(stroke.Points[0], false, false);
                for (int i = 1; i < stroke.Points.Count; i++)
                    ctx.LineTo(stroke.Points[i], true, true);
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        }

        // Draw stamps
        foreach (var stamp in _stamps)
            stamp.Render(dc);
    }

    public void Cleanup()
    {
        _renderTimer.Stop();
        if (_mouseHookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
        if (_keyboardHookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }
    }
}
