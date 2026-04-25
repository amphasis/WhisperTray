using System.Drawing.Drawing2D;
using WhisperTray.App.Adapters;
using WhisperTray.Core.Orchestration;

namespace WhisperTray.App.Tray;

/// <summary>
/// Owns a set of programmatically generated tray icons — one per orchestrator state.
/// Glyphs render in a centred coordinate system: origin <c>(0, 0)</c> is the icon's
/// visual centre, X right, Y down, usable canvas <c>[-16, +16]</c>. <see cref="Build"/>
/// installs the centring translate so any rotate / scale / translate inside a glyph
/// composes around the centre. <see cref="Icon"/> values returned by <c>Bitmap.GetHicon</c>
/// do not own their native handle, so each hIcon is tracked and freed in <see cref="Dispose"/>.
/// </summary>
public sealed class TrayIconSet : IDisposable
{
    private const int IconSize = 40;
    private const float Half = IconSize / 2f;
    private const float Scale = 1.45f;

    private readonly Dictionary<OrchestratorState, Icon> _icons;
    private readonly List<nint> _nativeHandles = new();
    private bool _disposed;

    public TrayIconSet()
    {
        _icons = new Dictionary<OrchestratorState, Icon>
        {
            [OrchestratorState.Idle] = Build(DrawIdle),
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
            g.TranslateTransform(Half, Half);
            g.ScaleTransform(Scale, Scale);

            var origin = g.Save();
            try
            {
                draw(g);
            }
            finally
            {
                g.Restore(origin);
            }
        }
        var hIcon = bmp.GetHicon();
        _nativeHandles.Add(hIcon);
        // Icon.FromHandle returns a wrapper that does NOT own the handle — we clean it up in Dispose.
        return Icon.FromHandle(hIcon);
    }

    /// <summary>White microphone silhouette — neutral idle state.</summary>
    private static void DrawIdle(Graphics g)
    {
        DrawMicrophone(g, Color.White, Color.White);
    }

    /// <summary>Dark-red microphone with a bright "REC" bullet in the upper-right corner.</summary>
    private static void DrawRecording(Graphics g)
    {
        // REC bullet — translate to upper-right corner, then draw centred there.
        using var dot = new SolidBrush(Color.Red);
        using var ring = new Pen(Color.White, 2f);
        var bullet = CenteredRect(16f, 16f);
        g.FillEllipse(dot, bullet);
        g.DrawEllipse(ring, bullet);
    }

    /// <summary>Blue rounded card with three ellipsis dots — "thinking".</summary>
    private static void DrawTranscribing(Graphics g)
    {
        using var fill = new SolidBrush(Color.FromArgb(70, 130, 220));
        using var border = new Pen(Color.FromArgb(40, 60, 160), 2f);
        var card = CenteredRect(27f, 23f);
        FillRoundedRect(g, fill, card, 8f);
        DrawRoundedRect(g, border, card, 8f);

        // Three ellipsis dots, evenly spaced along the X axis.
        using var dot = new SolidBrush(Color.White);
        const float dotSize = 6f;
        const float spacing = 8f;

        for (var i = -1; i <= 1; i++)
        {
            g.FillEllipse(dot, CenteredRectAt(i * spacing, 0f, dotSize, dotSize));
        }
    }

    /// <summary>Green right-pointing arrow — text is flowing into the target window.</summary>
    private static void DrawInjecting(Graphics g)
    {
        using var fill = new SolidBrush(Color.FromArgb(80, 180, 100));
        using var border = new Pen(Color.FromArgb(40, 120, 60), 2f);
        // Arrow points laid out around the origin: shaft from x=-13 to x=+1,
        // arrowhead spans x=+1..+13 with apex on the X axis.
        var arrow = new[]
        {
            new PointF(-13f, -5f),
            new PointF(  1f, -5f),
            new PointF(  1f, -11f),
            new PointF( 13f,   0f),
            new PointF(  1f,  11f),
            new PointF(  1f,   5f),
            new PointF(-13f,   5f),
        };
        g.FillPolygon(fill, arrow);
        g.DrawPolygon(border, arrow);
    }

    private static void DrawMicrophone(Graphics g, Color body, Color outline)
    {
        using var fill = new SolidBrush(body);
        using var pen = new Pen(outline, 2f);

        // Capsule (mic head) — centred on X, sits above the centre line.
        var head = CenteredRectAt(0f, -4f, 10f, 17f);
        FillRoundedRect(g, fill, head, 5f);
        DrawRoundedRect(g, pen, head, 5f);

        // Stand arc — opens downward, just below the head.
        var arc = CenteredRectAt(0f, 3f, 18f, 12f);
        g.DrawArc(pen, arc, 0, 180);

        // Vertical post + base line at the bottom of the icon.
        g.DrawLine(pen, 0f, 9f, 0f, 13f);
        g.DrawLine(pen, -5f, 13f, 5f, 13f);
    }

    /// <summary>Rectangle of <paramref name="width"/>×<paramref name="height"/> centred at the current origin.</summary>
    private static RectangleF CenteredRect(float width, float height) =>
        new(-width / 2f, -height / 2f, width, height);

    /// <summary>Rectangle of <paramref name="width"/>×<paramref name="height"/> centred at <c>(cx, cy)</c>.</summary>
    private static RectangleF CenteredRectAt(float cx, float cy, float width, float height) =>
        new(cx - width / 2f, cy - height / 2f, width, height);

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
