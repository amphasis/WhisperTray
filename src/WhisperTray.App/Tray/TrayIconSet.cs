using System.Drawing.Drawing2D;
using WhisperTray.App.Adapters;
using WhisperTray.Core.Orchestration;
using WpfApplication = System.Windows.Application;

namespace WhisperTray.App.Tray;

/// <summary>
/// Owns a set of programmatically generated tray icons — one per orchestrator state.
/// Icons are drawn into 32x32 ARGB bitmaps and converted via <c>Bitmap.GetHicon()</c>.
/// The resulting <see cref="Icon"/> does not own its native handle, so we track
/// each hIcon and call <c>DestroyIcon</c> on <see cref="Dispose"/> to avoid leaks.
/// </summary>
public sealed class TrayIconSet : IDisposable
{
    private const int IconSize = 32;

    private readonly Dictionary<OrchestratorState, Icon> _icons;
    private readonly List<nint> _nativeHandles = new();
    private bool _disposed;

    public TrayIconSet()
    {
        // Idle uses the bundled AppIcon.ico — the same artwork that ships on the .exe and
        // on every WPF window. Recording / Transcribing / Injecting keep their bespoke
        // glyphs so the user can tell at a glance what stage the pipeline is in.
        _icons = new Dictionary<OrchestratorState, Icon>
        {
            [OrchestratorState.Idle] = LoadFromPackUri("pack://application:,,,/Assets/AppIcon.ico"),
            [OrchestratorState.Recording] = Build(DrawRecording),
            [OrchestratorState.Transcribing] = Build(DrawTranscribing),
            [OrchestratorState.Injecting] = Build(DrawInjecting),
        };
    }

    public Icon Get(OrchestratorState state) =>
        _icons.TryGetValue(state, out var icon) ? icon : _icons[OrchestratorState.Idle];

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        foreach (var icon in _icons.Values)
        {
            icon.Dispose();
        }
        foreach (var handle in _nativeHandles)
        {
            NativeMethods.DestroyIcon(handle);
        }
        _icons.Clear();
        _nativeHandles.Clear();
        _disposed = true;
    }

    private Icon Build(Action<Graphics> draw)
    {
        using var bmp = new Bitmap(IconSize, IconSize);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.Transparent);
            g.TranslateTransform(-4f, -4f);
            g.ScaleTransform(1.2f, 1.2f);
            draw(g);
        }
        var hIcon = bmp.GetHicon();
        _nativeHandles.Add(hIcon);
        // Icon.FromHandle returns a wrapper that does NOT own the handle — we clean it up in Dispose.
        return Icon.FromHandle(hIcon);
    }

    private static Icon LoadFromPackUri(string uri)
    {
        var resource = WpfApplication.GetResourceStream(new Uri(uri, UriKind.Absolute))
            ?? throw new InvalidOperationException($"Resource not found: {uri}");
        using var stream = resource.Stream;
        // An Icon constructed from a stream owns its handle, so disposing the Icon (in our
        // Dispose) cleans it up. We deliberately keep it out of _nativeHandles so we don't
        // double-free via DestroyIcon.
        return new Icon(stream);
    }

    // ---- glyphs ----

    /// <summary>Red microphone + solid REC dot in the corner.</summary>
    private static void DrawRecording(Graphics g)
    {
        DrawMicrophone(g, Color.DarkRed, Color.DarkRed);

        // Pulsating "REC" dot — static here, but visually distinct from idle.
        using var dot = new SolidBrush(Color.Red);
        g.FillEllipse(dot, 16, 4, 12, 12);
        using var ring = new Pen(Color.White, 1.5f);
        g.DrawEllipse(ring, 16, 4, 12, 12);
    }

    /// <summary>Blue cloud with three animated-looking dots — "thinking".</summary>
    private static void DrawTranscribing(Graphics g)
    {
        // Rounded square background.
        using var fill = new SolidBrush(Color.FromArgb(70, 130, 220));
        FillRoundedRect(g, fill, new RectangleF(2, 4, 28, 24), 6f);
        using var border = new Pen(Color.FromArgb(40, 80, 160), 1.5f);
        DrawRoundedRect(g, border, new RectangleF(2, 4, 28, 24), 6f);

        // Three ellipsis dots.
        using var dot = new SolidBrush(Color.White);
        g.FillEllipse(dot, 7, 13, 5, 5);
        g.FillEllipse(dot, 14, 13, 5, 5);
        g.FillEllipse(dot, 21, 13, 5, 5);
    }

    /// <summary>Green right-arrow — text is flowing into the target window.</summary>
    private static void DrawInjecting(Graphics g)
    {
        using var fill = new SolidBrush(Color.FromArgb(80, 180, 100));
        using var border = new Pen(Color.FromArgb(40, 120, 60), 1.5f);
        var arrow = new[]
        {
            new PointF(4, 11),
            new PointF(18, 11),
            new PointF(18, 5),
            new PointF(30, 16),
            new PointF(18, 27),
            new PointF(18, 21),
            new PointF(4, 21),
        };
        g.FillPolygon(fill, arrow);
        g.DrawPolygon(border, arrow);
    }

    // ---- shared helpers ----

    private static void DrawMicrophone(Graphics g, Color body, Color outline)
    {
        using var fill = new SolidBrush(body);
        using var pen = new Pen(outline, 1.5f);

        // Capsule (mic head).
        var head = new RectangleF(11, 4, 10, 17);
        FillRoundedRect(g, fill, head, 5f);
        DrawRoundedRect(g, pen, head, 5f);

        // Stand arc.
        var arc = new RectangleF(7, 13, 18, 12);
        g.DrawArc(pen, arc, 0, 180);

        // Base line.
        g.DrawLine(pen, 16, 25, 16, 29);
        g.DrawLine(pen, 11, 29, 21, 29);
    }

    private static void FillRoundedRect(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = BuildRoundedPath(rect, radius);
        g.FillPath(brush, path);
    }

    private static void DrawRoundedRect(Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using var path = BuildRoundedPath(rect, radius);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath BuildRoundedPath(RectangleF rect, float radius)
    {
        var d = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

