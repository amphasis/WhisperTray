using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// IconForge: generates candidate app-icon designs as 256x256 PNGs and (when asked)
// converts a chosen design into a multi-resolution Windows .ico file.
//
// Usage:
//   dotnet run --project tools/IconForge -- candidates
//       → writes docs/icon-candidates/{variant-a..d}.png at 256x256
//
//   dotnet run --project tools/IconForge -- ico <variant>
//       → writes src/WhisperTray.App/Assets/AppIcon.ico containing
//         16, 24, 32, 48, 64, 128 and 256 px frames of the chosen design.
//         <variant> is one of: classic | studio | bubble | rec
//
// All drawing is GDI+ (System.Drawing.Common); no external assets required.

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var args0 = args.Length > 0 ? args[0] : "candidates";

switch (args0)
{
    case "candidates":
        GenerateCandidates(repoRoot);
        break;
    case "ico":
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Pass a variant name: classic | studio | bubble | rec");
            return 1;
        }
        BuildIco(repoRoot, args[1]);
        break;
    default:
        Console.Error.WriteLine($"Unknown command: {args0}. Expected 'candidates' or 'ico'.");
        return 1;
}
return 0;

static void GenerateCandidates(string repoRoot)
{
    var outDir = Path.Combine(repoRoot, "docs", "icon-candidates");
    Directory.CreateDirectory(outDir);

    foreach (var (name, draw) in EnumerateVariants())
    {
        var path = Path.Combine(outDir, $"variant-{name}.png");
        SavePng(path, 256, draw);
        Console.WriteLine($"Wrote {path}");
    }
}

static void BuildIco(string repoRoot, string variant)
{
    var draw = ResolveDraw(variant)
        ?? throw new ArgumentException($"Unknown variant '{variant}'. Use classic | studio | bubble | rec.");

    var assetsDir = Path.Combine(repoRoot, "src", "WhisperTray.App", "Assets");
    Directory.CreateDirectory(assetsDir);
    var icoPath = Path.Combine(assetsDir, "AppIcon.ico");

    int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
    var pngs = new List<(int Size, byte[] Bytes)>();
    foreach (var size in sizes)
    {
        using var ms = new MemoryStream();
        DrawToStream(ms, size, draw);
        pngs.Add((size, ms.ToArray()));
    }

    using var fs = File.Create(icoPath);
    WriteIco(fs, pngs);
    Console.WriteLine($"Wrote {icoPath} ({pngs.Count} resolutions)");
}

static IEnumerable<(string Name, Action<Graphics, int> Draw)> EnumerateVariants() => new (string, Action<Graphics, int>)[]
{
    ("a-classic",      DrawClassic),
    ("b-studio",       DrawStudio),
    ("c-speechbubble", DrawSpeechBubble),
    ("d-recbutton",    DrawRecButton),
};

static Action<Graphics, int>? ResolveDraw(string name) => name.ToLowerInvariant() switch
{
    "classic" or "a" or "a-classic"           => DrawClassic,
    "studio"  or "b" or "b-studio"            => DrawStudio,
    "bubble"  or "c" or "c-speechbubble"      => DrawSpeechBubble,
    "rec"     or "d" or "d-recbutton"         => DrawRecButton,
    _ => null,
};

static void SavePng(string path, int size, Action<Graphics, int> draw)
{
    using var bmp = RenderBitmap(size, draw);
    bmp.Save(path, ImageFormat.Png);
}

static void DrawToStream(Stream stream, int size, Action<Graphics, int> draw)
{
    using var bmp = RenderBitmap(size, draw);
    bmp.Save(stream, ImageFormat.Png);
}

static Bitmap RenderBitmap(int size, Action<Graphics, int> draw)
{
    var bmp = new Bitmap(size, size);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);
    draw(g, size);
    return bmp;
}

// ---- Multi-resolution ICO writer (PNG-encoded frames; supported by Vista+) ----
static void WriteIco(Stream stream, IReadOnlyList<(int Size, byte[] Bytes)> images)
{
    using var w = new BinaryWriter(stream);

    // ICONDIR
    w.Write((ushort)0);                  // Reserved
    w.Write((ushort)1);                  // Type = ICO
    w.Write((ushort)images.Count);       // Number of images

    // ICONDIRENTRY[count]
    var directorySize = 6 + 16 * images.Count;
    var offset = directorySize;
    foreach (var (size, bytes) in images)
    {
        w.Write((byte)(size >= 256 ? 0 : size));   // Width  (0 = 256)
        w.Write((byte)(size >= 256 ? 0 : size));   // Height (0 = 256)
        w.Write((byte)0);                          // Color count (0 = >= 256 colors)
        w.Write((byte)0);                          // Reserved
        w.Write((ushort)1);                        // Color planes
        w.Write((ushort)32);                       // Bits per pixel
        w.Write((uint)bytes.Length);               // Image data size
        w.Write((uint)offset);                     // Offset to image data
        offset += bytes.Length;
    }

    foreach (var (_, bytes) in images)
    {
        w.Write(bytes);
    }
}

// ---- Shared helpers ----

static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
{
    var d = r * 2f;
    var path = new GraphicsPath();
    path.AddArc(x,         y,         d, d, 180, 90);
    path.AddArc(x + w - d, y,         d, d, 270, 90);
    path.AddArc(x + w - d, y + h - d, d, d,   0, 90);
    path.AddArc(x,         y + h - d, d, d,  90, 90);
    path.CloseFigure();
    return path;
}

static void DrawMicrophone(Graphics g, int size, Color body, Color outline, float scaleY = 1f, bool withStand = true)
{
    var cx = size / 2f;
    var headW = size * 0.30f;
    var headH = size * 0.42f * scaleY;
    var headX = cx - headW / 2f;
    var headY = size * 0.18f;

    using (var head = RoundedRect(headX, headY, headW, headH, headW / 2f))
    using (var bodyBrush = new SolidBrush(body))
    using (var outlinePen = new Pen(outline, size * 0.018f))
    {
        g.FillPath(bodyBrush, head);
        g.DrawPath(outlinePen, head);
    }

    using (var grillePen = new Pen(Color.FromArgb(120, outline), size * 0.012f))
    {
        for (var i = 1; i <= 3; i++)
        {
            var y = headY + headH * (0.22f * i + 0.05f);
            g.DrawLine(grillePen, headX + headW * 0.18f, y, headX + headW * 0.82f, y);
        }
    }

    if (!withStand)
    {
        return;
    }

    using (var pen = new Pen(outline, size * 0.018f))
    {
        // Yoke (U-shape) that cradles the head. We draw the BOTTOM half of an ellipse
        // (sweep 0..180), so its two visible endpoints — the "arm tops" — sit at the
        // ellipse's vertical midline, and the lowest point of the U is at the rect's
        // bottom edge.
        //
        // Geometry goal:
        //   - arms reach up alongside the head to roughly mid-head height,
        //   - bottom of the U sits just below the head,
        //   - stem starts EXACTLY at the U's lowest point (no visual overlap).
        var yokeWidth      = headW * 1.5f;
        var armsTopY       = headY + headH * 0.55f;          // arms reach mid-head
        var yokeBottomY    = headY + headH + headW * 0.45f;  // U bottom, just under head
        var yokeHalfHeight = yokeBottomY - armsTopY;
        var yokeRect = new RectangleF(
            cx - yokeWidth / 2f,
            armsTopY - yokeHalfHeight,                       // ellipse rect extends above
            yokeWidth,
            yokeHalfHeight * 2f);                            // only the bottom half is drawn
        g.DrawArc(pen, yokeRect, 0, 180);

        // Stem starts at the U's bottom and runs straight down to the base bar.
        var stemBottomY = size * 0.90f;
        g.DrawLine(pen, cx, yokeBottomY, cx, stemBottomY);
        g.DrawLine(pen, cx - headW * 0.55f, stemBottomY, cx + headW * 0.55f, stemBottomY);
    }
}

// ---- Variant A: classic flat blue circle, white mic, sound waves ----
static void DrawClassic(Graphics g, int size)
{
    var bgRect = new RectangleF(0, 0, size, size);
    using (var bg = new LinearGradientBrush(bgRect, Color.FromArgb(80, 145, 255), Color.FromArgb(45, 95, 220), 90f))
    {
        g.FillEllipse(bg, 0, 0, size - 1, size - 1);
    }

    using (var ring = new Pen(Color.FromArgb(80, 255, 255, 255), size * 0.012f))
    {
        g.DrawEllipse(ring, size * 0.04f, size * 0.04f, size * 0.92f, size * 0.92f);
    }

    using (var wavePen = new Pen(Color.FromArgb(220, 255, 255, 255), size * 0.024f)
    {
        StartCap = LineCap.Round,
        EndCap = LineCap.Round,
    })
    {
        var cx = size / 2f;
        var cy = size * 0.42f;
        for (var i = 1; i <= 3; i++)
        {
            var r = size * (0.18f + 0.08f * i);
            var rect = new RectangleF(cx - r, cy - r, 2 * r, 2 * r);
            g.DrawArc(wavePen, rect, 200, 50);
            g.DrawArc(wavePen, rect, -70, 50);
        }
    }

    DrawMicrophone(g, size, Color.White, Color.FromArgb(30, 60, 130));
}

// ---- Variant B: dark squircle, neon cyan mic + equalizer ----
static void DrawStudio(Graphics g, int size)
{
    using (var bgPath = RoundedRect(0, 0, size, size, size * 0.22f))
    using (var bg = new LinearGradientBrush(new RectangleF(0, 0, size, size),
                Color.FromArgb(30, 40, 70), Color.FromArgb(15, 20, 40), 90f))
    {
        g.FillPath(bg, bgPath);
    }

    using (var border = RoundedRect(1, 1, size - 2, size - 2, size * 0.22f))
    using (var pen = new Pen(Color.FromArgb(160, 0, 200, 240), size * 0.014f))
    {
        g.DrawPath(pen, border);
    }

    var bars = new[] { 0.40f, 0.65f, 0.95f, 0.65f, 0.40f };
    var barWidth = size * 0.04f;
    var gap = size * 0.04f;
    var totalW = (barWidth + gap) * bars.Length - gap;
    var startX = (size - totalW) / 2f;
    var baseY = size * 0.84f;
    using (var brush = new SolidBrush(Color.FromArgb(220, 0, 220, 255)))
    {
        for (var i = 0; i < bars.Length; i++)
        {
            var h = size * 0.34f * bars[i];
            var x = startX + i * (barWidth + gap);
            var y = baseY - h;
            using var bar = RoundedRect(x, y, barWidth, h, barWidth / 2f);
            g.FillPath(brush, bar);
        }
    }

    // No stand on Studio — the equalizer bars below act as the visual "ground".
    DrawMicrophone(g, size, Color.FromArgb(0, 230, 255), Color.FromArgb(0, 90, 130), scaleY: 0.85f, withStand: false);
}

// ---- Variant C: warm gradient + speech bubble + waveform ----
static void DrawSpeechBubble(Graphics g, int size)
{
    using (var bgPath = RoundedRect(0, 0, size, size, size * 0.22f))
    using (var bg = new LinearGradientBrush(new RectangleF(0, 0, size, size),
                Color.FromArgb(255, 145, 100), Color.FromArgb(220, 60, 130), 135f))
    {
        g.FillPath(bg, bgPath);
    }

    var bx = size * 0.14f;
    var by = size * 0.20f;
    var bw = size * 0.72f;
    var bh = size * 0.48f;

    using (var bubble = RoundedRect(bx, by, bw, bh, size * 0.12f))
    using (var white = new SolidBrush(Color.White))
    {
        g.FillPath(white, bubble);

        using var tail = new GraphicsPath();
        tail.AddPolygon(new[]
        {
            new PointF(bx + bw * 0.22f, by + bh - 1),
            new PointF(bx + bw * 0.34f, by + bh + size * 0.12f),
            new PointF(bx + bw * 0.42f, by + bh - 1),
        });
        g.FillPath(white, tail);
    }

    var heights = new[] { 0.35f, 0.70f, 1.00f, 0.55f, 0.30f };
    var barWidth = size * 0.05f;
    var gap = size * 0.045f;
    var totalW = (barWidth + gap) * heights.Length - gap;
    var startX = bx + (bw - totalW) / 2f;
    var centerY = by + bh / 2f;
    var maxH = bh * 0.62f;
    using (var brush = new SolidBrush(Color.FromArgb(220, 60, 130)))
    {
        for (var i = 0; i < heights.Length; i++)
        {
            var h = maxH * heights[i];
            var x = startX + i * (barWidth + gap);
            var y = centerY - h / 2f;
            using var bar = RoundedRect(x, y, barWidth, h, barWidth / 2f);
            g.FillPath(brush, bar);
        }
    }
}

// ---- Variant D: bold red REC button with white mic ----
static void DrawRecButton(Graphics g, int size)
{
    using (var outerBrush = new SolidBrush(Color.FromArgb(240, 40, 40)))
    {
        g.FillEllipse(outerBrush, 0, 0, size - 1, size - 1);
    }

    var innerRect = new RectangleF(size * 0.07f, size * 0.07f, size * 0.86f, size * 0.86f);
    using (var inner = new LinearGradientBrush(innerRect,
                Color.FromArgb(255, 90, 90), Color.FromArgb(190, 25, 35), 90f))
    {
        g.FillEllipse(inner, innerRect);
    }

    using (var gloss = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
    {
        g.FillEllipse(gloss, size * 0.18f, size * 0.10f, size * 0.64f, size * 0.30f);
    }

    DrawMicrophone(g, size, Color.White, Color.FromArgb(110, 0, 0));
}
