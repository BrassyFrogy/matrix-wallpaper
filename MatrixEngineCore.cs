using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MatrixWallpaperEngine
{
    public enum RenderMode
    {
        MatrixWaterfall = 0,
        AsciiFluidSim = 1,
        VideoToAscii = 2,
        RegularVideoWallpaper = 3,
        VideoMatrixHybrid = 4,
        ImageWallpaper = 5
    }

    public enum MouseEffectMode
    {
        Off = 0,
        FollowCursor = 1,
        AutoGhostSwirl = 2
    }

    public enum BackgroundImagePreset
    {
        None = 0,
        CyberpunkGrid = 1,
        SynthwaveSunset = 2,
        SpaceNebula = 3,
        CustomUploadedImage = 4
    }

    public enum BgImageRenderStyle
    {
        BehindMatrixAndEffects = 0,
        PureHdImageOnly = 1,
        ConvertImageToAscii = 2
    }

    public enum ColorTheme
    {
        ClassicMatrixGreen = 0,
        CyberpunkNeon = 1,
        FluidOceanFire = 2,
        CrimsonHacker = 3,
        GoldenAmber = 4,
        RainbowWave = 5,
        CustomColors = 6
    }

    public enum VideoColorMode
    {
        TrueVideoColor = 0,
        ThemeTinted = 1,
        CustomTint = 2,
        RainbowOverlay = 3
    }

    public enum SoyjakPreset
    {
        None = 0,
        TwoSoyjaksPointing = 1,
        ShockedGlassesSoyjak = 2,
        SinglePointerSoyjak = 3,
        DiamondGemSoyjak = 4,
        CustomUploadedImage = 5
    }

    public enum SoyjakRenderStyle
    {
        FullCrispOverlay = 0,
        AsciiTextMeme = 1,
        OverlayPlusMatrix = 2
    }

    public class MouseTrailNode
    {
        public float X;
        public float Y;
        public float VX;
        public float VY;
        public float Intensity;
        public float Hue;
    }

    public class EngineConfig
    {
        public RenderMode Mode = RenderMode.MatrixWaterfall;
        public ColorTheme Theme = ColorTheme.ClassicMatrixGreen;
        public VideoColorMode VidColorMode = VideoColorMode.TrueVideoColor;

        // Custom Colors
        public System.Drawing.Color PrimaryColor = System.Drawing.Color.FromArgb(0, 255, 115);
        public System.Drawing.Color HeadColor = System.Drawing.Color.FromArgb(230, 255, 240);
        public System.Drawing.Color BackgroundColor = System.Drawing.Color.FromArgb(6, 9, 12);

        // Matrix & Fluid Parameters
        public float Speed = 1.0f;
        public int FontSize = 15;
        public int TrailLength = 22;
        public float Density = 0.85f;
        public string CharSetPreset = "Numbers & Binary";
        public string CustomCharSet = "0123456789";
        public bool GlitchMutation = true;

        // Interactive Mouse Physics
        public bool MouseFollowEnabled = true;
        public MouseEffectMode MouseMode = MouseEffectMode.FollowCursor;
        public float MouseRadius = 0.14f;
        public float FluidViscosity = 0.94f; // Fades smoothly back to pure blank black screen when mouse stops!

        // Background Image Wallpaper Parameters
        public BackgroundImagePreset ActiveBgImage = BackgroundImagePreset.None;
        public BgImageRenderStyle BgImageStyle = BgImageRenderStyle.BehindMatrixAndEffects;
        public float BgImageOpacity = 0.72f; // 0.1 to 1.0
        public string CustomBgImagePath = "";

        // Cool Extra Features
        public bool ShowClockOverlay = false;
        public bool ShowCustomBanner = false;
        public string CustomBannerText = "MATRIX WALLPAPER ENGINE";
        public bool CrtScanlines = false;
        public bool ReverseRainDirection = false;
        public int TargetFps = 60; // 60 for smooth, 30 for low-spec Celeron laptops
        public bool AutoCheckUpdates = true;
        public bool PauseWhenMinimized = true;

        // Video Parameters
        public string VideoPath = "";
        public float Brightness = 1.15f;
        public float Contrast = 1.20f;
        public float Saturation = 1.25f;
        public bool InvertLuminance = false;
        public bool LoopVideo = true;
        public bool MuteAudio = true;
        public string AsciiRamp = " .:-=+*#%@0189";

        // Soyjak Meme Overlay Parameters
        public SoyjakPreset ActiveSoyjak = SoyjakPreset.None;
        public SoyjakRenderStyle SoyjakStyle = SoyjakRenderStyle.FullCrispOverlay;
        public float SoyjakScale = 0.82f; // 0.3 to 1.2
        public bool SoyjakBounceReactive = true;
        public string CustomSoyjakPath = "";

        public bool AttachedToDesktop = false;
        public float DpiScale = 1.0f; // Screen DPI multiplier so text size looks the same on any monitor

        public string GetActiveCharacters()
        {
            switch (CharSetPreset)
            {
                case "Numbers Only (0-9)":
                    return "0123456789";
                case "Binary (0-1)":
                    return "01";
                case "Hexadecimal (0-9, A-F)":
                    return "0123456789ABCDEF";
                case "Matrix Classic (Numbers + Symbols)":
                    return "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ@#$%&*+-=<>?!";
                case "Fluid Density Ramp (.:-=+*#%@0189)":
                    return " .:-=+*#%@0189";
                case "Custom":
                    return string.IsNullOrEmpty(CustomCharSet) ? "0123456789" : CustomCharSet;
                default:
                    return "01010123456789";
            }
        }
    }

    // Native Win32 Desktop Attachment & DPI Awareness
    public static class NativeWin32
    {
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
            uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors;
        }

        [DllImport("gdi32.dll", SetLastError = false)]
        public static extern int SetDIBitsToDevice(
            IntPtr hdc,
            int xDest,
            int yDest,
            uint dwWidth,
            uint dwHeight,
            int xSrc,
            int ySrc,
            uint uStartScan,
            uint cScanLines,
            byte[] lpvBits,
            ref BITMAPINFO lpbmi,
            uint fuColorUse);

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public static IntPtr GetDesktopWorkerW()
        {
            IntPtr progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero) return IntPtr.Zero;

            IntPtr result = IntPtr.Zero;
            SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, 0x0000, 1000, out result);

            IntPtr workerw = IntPtr.Zero;
            EnumWindows(new EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (p != IntPtr.Zero)
                {
                    workerw = FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
                }
                return true;
            }), IntPtr.Zero);

            if (workerw == IntPtr.Zero)
                workerw = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
            if (workerw == IntPtr.Zero)
                workerw = progman;

            return workerw;
        }

        public static bool AttachWindowToDesktop(Form form, out string statusMessage)
        {
            IntPtr workerw = GetDesktopWorkerW();
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;

            form.FormBorderStyle = FormBorderStyle.None;
            form.StartPosition = FormStartPosition.Manual;

            int ex = GetWindowLong(form.Handle, GWL_EXSTYLE);
            SetWindowLong(form.Handle, GWL_EXSTYLE, ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

            if (workerw == IntPtr.Zero)
            {
                form.Bounds = screenBounds;
                SetWindowPos(form.Handle, HWND_BOTTOM, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
                statusMessage = "Live on Desktop (Overlay Mode)";
                return true;
            }

            SetParent(form.Handle, workerw);
            form.Location = new Point(0, 0);
            form.Size = new Size(screenBounds.Width, screenBounds.Height);
            SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, screenBounds.Width, screenBounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);

            statusMessage = "Live on Desktop Wallpaper (Behind Icons)";
            return true;
        }

        public static void DetachWindowFromDesktop(Form form, Rectangle restoreBounds)
        {
            SetParent(form.Handle, IntPtr.Zero);
            int ex = GetWindowLong(form.Handle, GWL_EXSTYLE);
            SetWindowLong(form.Handle, GWL_EXSTYLE, ex & ~(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE));
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.Bounds = restoreBounds;
            form.TopMost = false;
            SystemParametersInfo(0x0014, 0, null, 0);
        }
    }

    // Matrix Column Stream State
    public class MatrixStream
    {
        public float Y;
        public float Speed;
        public int Length;
        public byte[] GlyphIndices;
        public bool Active;
        public float MouseGlowBoost;

        public MatrixStream(int maxRows, Random rng, int glyphCount, EngineConfig cfg)
        {
            GlyphIndices = new byte[Math.Max(maxRows + 80, 160)];
            Reset(maxRows, rng, glyphCount, cfg, true);
        }

        public void Reset(int maxRows, Random rng, int glyphCount, EngineConfig cfg, bool randomStart)
        {
            Length = Math.Max(6, cfg.TrailLength + rng.Next(-cfg.TrailLength / 3, cfg.TrailLength / 2 + 1));
            Speed = 0.55f + (float)rng.NextDouble() * 0.90f;
            Y = randomStart
                ? (float)(rng.NextDouble() * (maxRows + Length)) - Length
                : -rng.Next(2, Math.Max(4, maxRows / 2));
            Active = rng.NextDouble() <= cfg.Density;
            MouseGlowBoost = 0f;
            PopulateGlyphs(rng, glyphCount);
        }

        public void PopulateGlyphs(Random rng, int glyphCount)
        {
            if (glyphCount <= 0) glyphCount = 1;
            for (int i = 0; i < GlyphIndices.Length; i++)
            {
                GlyphIndices[i] = (byte)rng.Next(glyphCount);
            }
        }
    }

    // RAZOR-SHARP 1:1 Anti-Aliased Vector Glyph Atlas
    public class CrispGlyphAtlas
    {
        public int CellW;
        public int CellH;
        public char[] Characters;
        public byte[] AlphaMasks;
        public int GlyphCount;

        public CrispGlyphAtlas(int fontSize, string charSet)
        {
            if (string.IsNullOrEmpty(charSet)) charSet = "0123456789";

            List<char> unique = new List<char>();
            for (int i = 0; i < charSet.Length; i++)
            {
                if (!unique.Contains(charSet[i]))
                    unique.Add(charSet[i]);
            }
            if (unique.Count == 0) unique.Add('0');

            Characters = unique.ToArray();
            GlyphCount = Characters.Length;

            CellW = Math.Max(8, (int)Math.Round(fontSize * 0.72f));
            CellH = Math.Max(11, (int)Math.Round(fontSize * 1.16f));
            AlphaMasks = new byte[GlyphCount * CellW * CellH];

            int superW = CellW * 2;
            int superH = CellH * 2;

            using (Font font = new Font("Consolas", fontSize * 2f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Bitmap superBmp = new Bitmap(superW, superH, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(superBmp))
            using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                RectangleF rect = new RectangleF(0, 0, superW, superH);

                for (int idx = 0; idx < GlyphCount; idx++)
                {
                    g.Clear(System.Drawing.Color.Black);
                    if (Characters[idx] != ' ')
                    {
                        g.DrawString(Characters[idx].ToString(), font, System.Drawing.Brushes.White, rect, sf);
                    }

                    BitmapData bd = superBmp.LockBits(new Rectangle(0, 0, superW, superH), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    int stride = bd.Stride;
                    byte[] raw = new byte[stride * superH];
                    try
                    {
                        Marshal.Copy(bd.Scan0, raw, 0, raw.Length);
                    }
                    finally
                    {
                        superBmp.UnlockBits(bd);
                    }

                    int baseMaskOffset = idx * CellW * CellH;
                    for (int y = 0; y < CellH; y++)
                    {
                        int sy0 = (y * 2) * stride;
                        int sy1 = (y * 2 + 1) * stride;
                        int dstRow = baseMaskOffset + y * CellW;
                        for (int x = 0; x < CellW; x++)
                        {
                            int sx0 = (x * 2) * 4 + 1;
                            int sx1 = (x * 2 + 1) * 4 + 1;
                            int sum = raw[sy0 + sx0] + raw[sy0 + sx1] + raw[sy1 + sx0] + raw[sy1 + sx1];
                            int avg = sum >> 2;
                            avg = Math.Min(255, (avg * 290) >> 8);
                            AlphaMasks[dstRow + x] = (byte)avg;
                        }
                    }
                }
            }
        }
    }

    // Pure Mouse-Driven ASCII Fluid Simulator (Blank screen when mouse is idle, fluid follows cursor!)
    public class AsciiFluidSimulator
    {
        private int w;
        private int h;
        public float[] Vx;
        public float[] Vy;
        public float[] VxPrev;
        public float[] VyPrev;
        public float[] Density;
        public float[] DensityPrev;
        public float[] HueField;

        public void EnsureSize(int cols, int rows)
        {
            if (cols == w && rows == h && Density != null) return;
            w = Math.Max(16, cols);
            h = Math.Max(16, rows);
            int len = w * h;
            Vx = new float[len];
            Vy = new float[len];
            VxPrev = new float[len];
            VyPrev = new float[len];
            Density = new float[len];
            DensityPrev = new float[len];
            HueField = new float[len];
        }

        public void ClearFluid()
        {
            if (Density == null) return;
            Array.Clear(Density, 0, Density.Length);
            Array.Clear(DensityPrev, 0, DensityPrev.Length);
            Array.Clear(Vx, 0, Vx.Length);
            Array.Clear(Vy, 0, Vy.Length);
        }

        public void SplatNormalized(float normX, float normY, float dvx, float dvy, float radiusRatio, float amount, float hue)
        {
            if (Density == null) return;
            int cx = (int)(normX * w);
            int cy = (int)(normY * h);
            int rad = Math.Max(3, (int)(Math.Max(w, h) * radiusRatio));
            int radSq = rad * rad;

            int x0 = Math.Max(1, cx - rad);
            int x1 = Math.Min(w - 2, cx + rad);
            int y0 = Math.Max(1, cy - rad);
            int y1 = Math.Min(h - 2, cy + rad);

            for (int y = y0; y <= y1; y++)
            {
                int dy = y - cy;
                int rowOff = y * w;
                for (int x = x0; x <= x1; x++)
                {
                    int dx = x - cx;
                    int d2 = dx * dx + dy * dy;
                    if (d2 <= radSq)
                    {
                        float falloff = 1.0f - (float)d2 / (radSq + 1f);
                        falloff = falloff * falloff;
                        int idx = rowOff + x;
                        Density[idx] = Math.Min(2.4f, Density[idx] + amount * falloff);
                        Vx[idx] = Math.Max(-8f, Math.Min(8f, Vx[idx] + dvx * falloff * 14f));
                        Vy[idx] = Math.Max(-8f, Math.Min(8f, Vy[idx] + dvy * falloff * 14f));
                        HueField[idx] = hue;
                    }
                }
            }
        }

        public void StepFluid(float dt, float speed, float dissipation)
        {
            if (Density == null) return;

            int len = w * h;
            Buffer.BlockCopy(Vx, 0, VxPrev, 0, len * 4);
            Buffer.BlockCopy(Vy, 0, VyPrev, 0, len * 4);
            Buffer.BlockCopy(Density, 0, DensityPrev, 0, len * 4);

            float advScale = dt * 24f * speed;
            for (int y = 1; y < h - 1; y++)
            {
                int row = y * w;
                for (int x = 1; x < w - 1; x++)
                {
                    int idx = row + x;

                    float nvx = (VxPrev[idx - 1] + VxPrev[idx + 1] + VxPrev[idx - w] + VxPrev[idx + w]) * 0.25f;
                    float nvy = (VyPrev[idx - 1] + VyPrev[idx + 1] + VyPrev[idx - w] + VyPrev[idx + w]) * 0.25f;

                    Vx[idx] = (VxPrev[idx] * 0.65f + nvx * 0.35f) * 0.95f;
                    Vy[idx] = (VyPrev[idx] * 0.65f + nvy * 0.35f) * 0.95f;

                    float srcX = x - Vx[idx] * advScale;
                    float srcY = y - Vy[idx] * advScale;

                    if (float.IsNaN(srcX) || srcX < 1f) srcX = 1f; else if (srcX > w - 2.001f) srcX = w - 2.001f;
                    if (float.IsNaN(srcY) || srcY < 1f) srcY = 1f; else if (srcY > h - 2.001f) srcY = h - 2.001f;

                    int ix = Math.Max(1, Math.Min(w - 2, (int)srcX));
                    int iy = Math.Max(1, Math.Min(h - 2, (int)srcY));
                    float fx = srcX - ix;
                    float fy = srcY - iy;

                    int b00 = iy * w + ix;
                    float d0 = DensityPrev[b00] * (1f - fx) + DensityPrev[b00 + 1] * fx;
                    float d1 = DensityPrev[b00 + w] * (1f - fx) + DensityPrev[b00 + w + 1] * fx;
                    float advected = d0 * (1f - fy) + d1 * fy;

                    float neighborD = (DensityPrev[idx - 1] + DensityPrev[idx + 1] + DensityPrev[idx - w] + DensityPrev[idx + w]) * 0.25f;
                    float newD = (advected * 0.84f + neighborD * 0.16f) * dissipation;
                    // Subtract a tiny floor so idle screen returns to 100% pure blank black!
                    newD = Math.Max(0f, newD - 0.0025f);
                    Density[idx] = newD;

                    HueField[idx] = (HueField[b00] + dt * 25f) % 360f;
                }
            }
        }
    }

    // Native 1:1 Resolution Software Renderer + Regular Video + Soyjak Meme Engine
    public class FastMatrixEngine : IDisposable
    {
        private EngineConfig config;
        private Random rng = new Random();
        private MatrixStream[] streams = new MatrixStream[0];
        private AsciiFluidSimulator fluidSim = new AsciiFluidSimulator();
        private List<MouseTrailNode> mouseNodes = new List<MouseTrailNode>(32);

        private CrispGlyphAtlas mainAtlas;
        private CrispGlyphAtlas asciiRampAtlas;
        private int lastFontSize = -1;
        private string lastCharSet = "";
        private string lastAsciiRamp = "";

        private int cols = 0;
        private int rows = 0;

        // Native 1:1 Canvas Buffer (100% Managed byte[] Buffer — Zero Per-Frame LockBits!)
        private readonly object renderSyncLock = new object();
        private Bitmap canvasBitmap;
        private byte[] pixelBuffer;
        private int canvasW = 0;
        private int canvasH = 0;
        private int canvasStride = 0;

        // Cached Scaled Soyjak Cutout Overlay Buffer
        private byte[] cachedSoyjakOverlayPixels;
        private int soyjakOverlayW = 0;
        private int soyjakOverlayH = 0;
        private SoyjakPreset lastOverlayPreset = SoyjakPreset.None;
        private float lastOverlayScale = -1f;
        private int lastOverlayCanvasW = 0;
        private int lastOverlayCanvasH = 0;

        // Cached HUD (Clock + Custom Banner) Overlay Buffer
        private byte[] cachedHudPixels;
        private int cachedHudW = 0;
        private int cachedHudH = 0;
        private int lastHudSecond = -1;
        private string lastHudBanner = null;
        private bool lastHudClockOn = false;
        private bool lastHudBannerOn = false;
        private ColorTheme lastHudTheme = ColorTheme.ClassicMatrixGreen;

        // Single Shared WPF MediaPlayer for MP4 Frame Extraction & Regular Video Playback
        private MediaPlayer mediaPlayer;
        private DrawingVisual videoDrawingVisual;
        private RenderTargetBitmap wpfRenderTarget;
        private RenderTargetBitmap wpfFullVideoTarget;
        private byte[] videoPixelBuffer;
        private bool videoLoaded = false;
        private string loadedVideoPath = "";
        private long lastVideoSampleTick = 0;

        // Loaded Soyjak Meme Images
        private Dictionary<SoyjakPreset, Bitmap> soyjakBitmaps = new Dictionary<SoyjakPreset, Bitmap>();
        private Bitmap cachedSoyjakGridBmp;
        private byte[] soyjakGridPixels;
        private SoyjakPreset lastGridPreset = SoyjakPreset.None;
        private int lastGridW = 0;
        private int lastGridH = 0;
        private float lastGridScale = -1f;

        // Loaded Background Images & Fast Scaled Buffers
        private Dictionary<BackgroundImagePreset, Bitmap> bgBitmaps = new Dictionary<BackgroundImagePreset, Bitmap>();
        private byte[] cachedBgCanvasPixels;
        private BackgroundImagePreset lastBgCanvasPreset = BackgroundImagePreset.None;
        private int lastBgCanvasW = 0;
        private int lastBgCanvasH = 0;
        private float lastBgOpacity = -1f;

        private byte[] cachedBgGridPixels;
        private BackgroundImagePreset lastBgGridPreset = BackgroundImagePreset.None;
        private int lastBgGridW = 0;
        private int lastBgGridH = 0;

        // Smooth delta time & mouse tracking
        private float smoothDt = 0.028f;
        private float demoTime = 0f;
        private float lastMouseNormX = 0.5f;
        private float lastMouseNormY = 0.5f;
        private float mouseHue = 120f;
        private float mouseActivityEnergy = 0f;
        private float[] sinTable = new float[1024];

        public Bitmap CurrentCanvas { get { return canvasBitmap; } }
        public bool IsVideoLoaded { get { return videoLoaded; } }
        public string VideoStatusText { get; private set; }
        public int GridCols { get { return cols; } }
        public int GridRows { get { return rows; } }
        public float LastMouseNormX { get { return lastMouseNormX; } }
        public float LastMouseNormY { get { return lastMouseNormY; } }
        public int CanvasWidth { get { return canvasW; } }
        public int CanvasHeight { get { return canvasH; } }

        public FastMatrixEngine(EngineConfig cfg)
        {
            config = cfg;
            VideoStatusText = "Ready — Upload any MP4 for Colored ASCII or Regular Video Wallpaper";
            for (int i = 0; i < 1024; i++)
            {
                sinTable[i] = (float)Math.Sin((i / 1024.0) * Math.PI * 2.0);
            }
            LoadBuiltInSoyjakMemes();
            GenerateBuiltInBackgroundImages();
            InitializeMediaPlayer();
        }

        private float FastSin(float rad)
        {
            int idx = (int)(rad * 162.97466f) & 1023;
            return sinTable[idx];
        }

        private Bitmap CloneToStandaloneArgbBitmap(Bitmap source)
        {
            if (source == null) return null;
            Bitmap clean = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(clean))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.DrawImage(source, 0, 0, source.Width, source.Height);
            }
            return clean;
        }

        private Bitmap LoadImageFromFileOrResource(string fileName)
        {
            try
            {
                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                if (File.Exists(localPath))
                {
                    using (Bitmap raw = new Bitmap(localPath))
                    {
                        return CloneToStandaloneArgbBitmap(raw);
                    }
                }
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream(fileName))
                {
                    if (s != null)
                    {
                        using (Bitmap raw = new Bitmap(s))
                        {
                            return CloneToStandaloneArgbBitmap(raw);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private void LoadBuiltInSoyjakMemes()
        {
            Bitmap p1 = LoadImageFromFileOrResource("meme_pointing.png");
            if (p1 != null) soyjakBitmaps[SoyjakPreset.TwoSoyjaksPointing] = p1;

            Bitmap p2 = LoadImageFromFileOrResource("meme_shocked.png");
            if (p2 != null) soyjakBitmaps[SoyjakPreset.ShockedGlassesSoyjak] = p2;

            Bitmap p3 = LoadImageFromFileOrResource("meme_pointer_single.png");
            if (p3 != null) soyjakBitmaps[SoyjakPreset.SinglePointerSoyjak] = p3;

            Bitmap p4 = LoadImageFromFileOrResource("meme_gem_soyjak.png");
            if (p4 != null) soyjakBitmaps[SoyjakPreset.DiamondGemSoyjak] = p4;
        }

        public bool LoadCustomSoyjakImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            try
            {
                using (Bitmap raw = new Bitmap(path))
                {
                    // Convert to clean 32bpp ARGB and optionally key out pure white background if non-transparent
                    Bitmap clean = new Bitmap(raw.Width, raw.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(clean))
                    {
                        g.DrawImage(raw, 0, 0, raw.Width, raw.Height);
                    }
                    if (soyjakBitmaps.ContainsKey(SoyjakPreset.CustomUploadedImage) && soyjakBitmaps[SoyjakPreset.CustomUploadedImage] != null)
                    {
                        soyjakBitmaps[SoyjakPreset.CustomUploadedImage].Dispose();
                    }
                    soyjakBitmaps[SoyjakPreset.CustomUploadedImage] = clean;
                    config.CustomSoyjakPath = path;
                    config.ActiveSoyjak = SoyjakPreset.CustomUploadedImage;
                    lastGridPreset = SoyjakPreset.None; // force ASCII grid cache refresh
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public Bitmap GetSoyjakBitmap(SoyjakPreset preset)
        {
            Bitmap bmp;
            if (soyjakBitmaps.TryGetValue(preset, out bmp)) return bmp;
            return null;
        }

        public Bitmap GetBackgroundBitmap(BackgroundImagePreset preset)
        {
            Bitmap bmp;
            if (bgBitmaps.TryGetValue(preset, out bmp)) return bmp;
            return null;
        }

        private void GenerateBuiltInBackgroundImages()
        {
            // 1. Cyberpunk Neon Grid Horizon (640x360 crisp procedural artwork)
            Bitmap gridBmp = new Bitmap(640, 360, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(gridBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (System.Drawing.Drawing2D.LinearGradientBrush sky = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, 640, 200),
                    System.Drawing.Color.FromArgb(8, 6, 24),
                    System.Drawing.Color.FromArgb(48, 12, 68),
                    90f))
                {
                    g.FillRectangle(sky, 0, 0, 640, 200);
                }
                // Glowing Neon Sun / Core
                using (SolidBrush sunGlow = new SolidBrush(System.Drawing.Color.FromArgb(65, 255, 40, 140)))
                {
                    g.FillEllipse(sunGlow, 220, 45, 200, 200);
                }
                using (System.Drawing.Drawing2D.LinearGradientBrush sun = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(250, 75, 140, 140),
                    System.Drawing.Color.FromArgb(255, 240, 90),
                    System.Drawing.Color.FromArgb(255, 25, 130),
                    90f))
                {
                    g.FillEllipse(sun, 250, 75, 140, 140);
                }
                // Floor horizon
                using (System.Drawing.Drawing2D.LinearGradientBrush floor = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 195, 640, 165),
                    System.Drawing.Color.FromArgb(26, 8, 44),
                    System.Drawing.Color.FromArgb(6, 10, 20),
                    90f))
                {
                    g.FillRectangle(floor, 0, 195, 640, 165);
                }
                using (System.Drawing.Pen cyanPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(160, 0, 245, 255), 1.5f))
                {
                    for (int i = -12; i <= 12; i++)
                    {
                        g.DrawLine(cyanPen, 320 + i * 12, 195, 320 + i * 75, 360);
                    }
                    int[] hy = new int[] { 196, 204, 215, 230, 250, 276, 310, 352 };
                    for (int i = 0; i < hy.Length; i++)
                    {
                        g.DrawLine(cyanPen, 0, hy[i], 640, hy[i]);
                    }
                }
            }
            bgBitmaps[BackgroundImagePreset.CyberpunkGrid] = gridBmp;

            // 2. Synthwave Sunset Mountains
            Bitmap sunsetBmp = new Bitmap(640, 360, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(sunsetBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (System.Drawing.Drawing2D.LinearGradientBrush bg = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, 640, 360),
                    System.Drawing.Color.FromArgb(15, 10, 38),
                    System.Drawing.Color.FromArgb(185, 45, 92),
                    90f))
                {
                    g.FillRectangle(bg, 0, 0, 640, 360);
                }
                using (SolidBrush sunB = new SolidBrush(System.Drawing.Color.FromArgb(220, 255, 175, 45)))
                {
                    g.FillEllipse(sunB, 230, 60, 180, 180);
                }
                Point[] mtn1 = new Point[]
                {
                    new Point(0, 360), new Point(0, 220), new Point(110, 155), new Point(220, 235),
                    new Point(330, 145), new Point(460, 230), new Point(555, 165), new Point(640, 225), new Point(640, 360)
                };
                using (SolidBrush mb = new SolidBrush(System.Drawing.Color.FromArgb(24, 14, 42)))
                using (System.Drawing.Pen mp = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 0, 235, 195), 2f))
                {
                    g.FillPolygon(mb, mtn1);
                    g.DrawLines(mp, mtn1);
                }
            }
            bgBitmaps[BackgroundImagePreset.SynthwaveSunset] = sunsetBmp;

            // 3. Deep Space Emerald & Cyan Nebula
            Bitmap spaceBmp = new Bitmap(640, 360, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(spaceBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.FromArgb(5, 9, 16));
                using (SolidBrush n1 = new SolidBrush(System.Drawing.Color.FromArgb(45, 0, 220, 140)))
                using (SolidBrush n2 = new SolidBrush(System.Drawing.Color.FromArgb(45, 30, 140, 255)))
                using (SolidBrush n3 = new SolidBrush(System.Drawing.Color.FromArgb(35, 160, 40, 240)))
                {
                    g.FillEllipse(n1, 40, 30, 360, 220);
                    g.FillEllipse(n2, 220, 80, 360, 230);
                    g.FillEllipse(n3, 140, 40, 280, 190);
                }
                Random starRng = new Random(1337);
                for (int i = 0; i < 180; i++)
                {
                    int sx = starRng.Next(640);
                    int sy = starRng.Next(360);
                    int sz = starRng.Next(1, 4);
                    using (SolidBrush sb = new SolidBrush(System.Drawing.Color.FromArgb(starRng.Next(120, 255), 220, 245, 255)))
                    {
                        g.FillEllipse(sb, sx, sy, sz, sz);
                    }
                }
            }
            bgBitmaps[BackgroundImagePreset.SpaceNebula] = spaceBmp;
        }

        public bool LoadCustomBackgroundImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            try
            {
                using (Bitmap raw = new Bitmap(path))
                {
                    Bitmap clean = new Bitmap(raw.Width, raw.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(clean))
                    {
                        g.DrawImage(raw, 0, 0, raw.Width, raw.Height);
                    }
                    if (bgBitmaps.ContainsKey(BackgroundImagePreset.CustomUploadedImage) && bgBitmaps[BackgroundImagePreset.CustomUploadedImage] != null)
                    {
                        bgBitmaps[BackgroundImagePreset.CustomUploadedImage].Dispose();
                    }
                    bgBitmaps[BackgroundImagePreset.CustomUploadedImage] = clean;
                    config.CustomBgImagePath = path;
                    config.ActiveBgImage = BackgroundImagePreset.CustomUploadedImage;
                    InvalidateBackgroundCache();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public void InvalidateBackgroundCache()
        {
            lastBgCanvasPreset = BackgroundImagePreset.None;
            lastBgGridPreset = BackgroundImagePreset.None;
            lastBgOpacity = -1f;
        }

        public void SetMouseMode(MouseEffectMode mode)
        {
            config.MouseMode = mode;
            config.MouseFollowEnabled = (mode == MouseEffectMode.FollowCursor);
            if (mode == MouseEffectMode.Off)
            {
                mouseNodes.Clear();
                mouseActivityEnergy = 0f;
                fluidSim.ClearFluid();
                if (streams != null)
                {
                    for (int i = 0; i < streams.Length; i++)
                    {
                        if (streams[i] != null) streams[i].MouseGlowBoost = 0f;
                    }
                }
            }
        }

        public void ClearFluidScreen()
        {
            fluidSim.ClearFluid();
        }

        public void NotifyMouseInput(float normX, float normY, bool isDraggingOrMoving)
        {
            normX = Math.Max(0.01f, Math.Min(0.99f, normX));
            normY = Math.Max(0.01f, Math.Min(0.99f, normY));

            float dx = normX - lastMouseNormX;
            float dy = normY - lastMouseNormY;
            lastMouseNormX = normX;
            lastMouseNormY = normY;

            if (!config.MouseFollowEnabled) return;

            float speedSq = dx * dx + dy * dy;
            if (!isDraggingOrMoving && speedSq < 0.000001f) return;

            mouseActivityEnergy = Math.Min(1.0f, mouseActivityEnergy + 0.35f);
            mouseHue = (mouseHue + 5.5f) % 360f;

            if (mouseNodes.Count > 28) mouseNodes.RemoveAt(0);
            mouseNodes.Add(new MouseTrailNode
            {
                X = normX,
                Y = normY,
                VX = dx * 35f,
                VY = dy * 35f,
                Intensity = 1.0f,
                Hue = mouseHue
            });

            // Splat into ASCII Fluid Simulator ONLY when mouse moves!
            fluidSim.SplatNormalized(normX, normY, dx * 32f, dy * 32f, config.MouseRadius * 1.15f, 2.20f, mouseHue);

            // Spawn / wake Matrix waterfall streams directly under the cursor
            if (cols > 0 && rows > 0 && streams.Length == cols && mainAtlas != null)
            {
                int centerCol = (int)(normX * cols);
                int targetRow = (int)(normY * rows);
                int colSpread = Math.Max(2, (int)(cols * config.MouseRadius * 0.45f));

                int c0 = Math.Max(0, centerCol - colSpread);
                int c1 = Math.Min(cols - 1, centerCol + colSpread);
                for (int c = c0; c <= c1; c++)
                {
                    float distRatio = 1.0f - Math.Abs(c - centerCol) / (float)(colSpread + 1);
                    MatrixStream s = streams[c];
                    s.Active = true;
                    s.MouseGlowBoost = Math.Min(1.5f, s.MouseGlowBoost + distRatio * 0.9f);

                    if (Math.Abs(s.Y - targetRow) > s.Length * 0.65f && rng.NextDouble() < 0.45 * distRatio)
                    {
                        s.Y = targetRow + rng.Next(-2, 3);
                        s.Speed = 0.85f + (float)rng.NextDouble() * 0.8f;
                    }
                }
            }
        }

        private void InitializeMediaPlayer()
        {
            try
            {
                videoDrawingVisual = new DrawingVisual();
                mediaPlayer = new MediaPlayer();
                mediaPlayer.Volume = 0;
                mediaPlayer.ScrubbingEnabled = false;
                mediaPlayer.MediaEnded += (s, e) =>
                {
                    if (config.LoopVideo && mediaPlayer != null)
                    {
                        mediaPlayer.Position = TimeSpan.Zero;
                        mediaPlayer.Play();
                    }
                };
                mediaPlayer.MediaOpened += (s, e) =>
                {
                    videoLoaded = true;
                    VideoStatusText = string.Format("Playing: {0} ({1}x{2})",
                        Path.GetFileName(loadedVideoPath),
                        mediaPlayer.NaturalVideoWidth,
                        mediaPlayer.NaturalVideoHeight);
                };
                mediaPlayer.MediaFailed += (s, e) =>
                {
                    videoLoaded = false;
                    VideoStatusText = "Codec Error: " + (e.ErrorException != null ? e.ErrorException.Message : "Unsupported format");
                };
            }
            catch (Exception ex)
            {
                VideoStatusText = "Video Engine Note: " + ex.Message;
            }
        }

        public void LoadVideo(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                videoLoaded = false;
                VideoStatusText = "Video file not found.";
                return;
            }

            try
            {
                loadedVideoPath = path;
                config.VideoPath = path;
                VideoStatusText = "Opening " + Path.GetFileName(path) + "...";
                mediaPlayer.Open(new Uri(Path.GetFullPath(path)));
                mediaPlayer.Volume = config.MuteAudio ? 0.0 : 0.8;
                mediaPlayer.Play();
                videoLoaded = true;
            }
            catch (Exception ex)
            {
                videoLoaded = false;
                VideoStatusText = "Failed to load MP4: " + ex.Message;
            }
        }

        public void SetAudioMute(bool mute)
        {
            config.MuteAudio = mute;
            if (mediaPlayer != null)
            {
                mediaPlayer.Volume = mute ? 0.0 : 0.8;
            }
        }

        public void ResetStreams()
        {
            if (mainAtlas == null) return;
            for (int i = 0; i < streams.Length; i++)
            {
                if (streams[i] != null)
                    streams[i].Reset(rows, rng, mainAtlas.GlyphCount, config, true);
            }
        }

        private void EnsureResources(int exactViewportW, int exactViewportH)
        {
            if (exactViewportW < 64) exactViewportW = 64;
            if (exactViewportH < 64) exactViewportH = 64;

            int fontSize = Math.Max(10, Math.Min(64, (int)Math.Round(config.FontSize * Math.Max(1.0f, config.DpiScale))));
            string activeChars = config.GetActiveCharacters();
            string rampChars = string.IsNullOrEmpty(config.AsciiRamp) ? " .:-=+*#%@0189" : config.AsciiRamp;

            if (mainAtlas == null || fontSize != lastFontSize || activeChars != lastCharSet || rampChars != lastAsciiRamp)
            {
                mainAtlas = new CrispGlyphAtlas(fontSize, activeChars);
                asciiRampAtlas = new CrispGlyphAtlas(fontSize, rampChars);
                lastFontSize = fontSize;
                lastCharSet = activeChars;
                lastAsciiRamp = rampChars;
                cols = 0;
            }

            int newCols = Math.Max(8, exactViewportW / mainAtlas.CellW);
            int newRows = Math.Max(8, exactViewportH / mainAtlas.CellH);

            if (canvasBitmap == null || canvasW != exactViewportW || canvasH != exactViewportH || newCols != cols || newRows != rows)
            {
                cols = newCols;
                rows = newRows;
                canvasW = exactViewportW;
                canvasH = exactViewportH;

                if (canvasBitmap != null) canvasBitmap.Dispose();
                canvasBitmap = new Bitmap(canvasW, canvasH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                canvasStride = canvasW * 4;
                pixelBuffer = new byte[canvasStride * canvasH];

                streams = new MatrixStream[cols];
                for (int c = 0; c < cols; c++)
                {
                    streams[c] = new MatrixStream(rows, rng, mainAtlas.GlyphCount, config);
                }

                fluidSim.EnsureSize(cols, rows);
                wpfRenderTarget = null;
                wpfFullVideoTarget = null;
                videoPixelBuffer = new byte[cols * rows * 4];
            }
        }

        private bool SampleVideoGridThrottled()
        {
            if (!videoLoaded || mediaPlayer == null || mediaPlayer.NaturalVideoWidth <= 0 || mediaPlayer.NaturalVideoHeight <= 0)
                return false;

            long nowMs = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
            if (nowMs - lastVideoSampleTick < 33 && videoPixelBuffer != null)
            {
                return true;
            }
            lastVideoSampleTick = nowMs;

            try
            {
                if (wpfRenderTarget == null || wpfRenderTarget.PixelWidth != cols || wpfRenderTarget.PixelHeight != rows)
                {
                    wpfRenderTarget = new RenderTargetBitmap(cols, rows, 96, 96, PixelFormats.Pbgra32);
                    videoPixelBuffer = new byte[cols * rows * 4];
                }

                using (DrawingContext dc = videoDrawingVisual.RenderOpen())
                {
                    dc.DrawVideo(mediaPlayer, new System.Windows.Rect(0, 0, cols, rows));
                }
                wpfRenderTarget.Render(videoDrawingVisual);
                wpfRenderTarget.CopyPixels(videoPixelBuffer, cols * 4, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Render Regular HD Video Frame into pixelBuffer
        private bool RenderRegularVideoFrame()
        {
            if (!videoLoaded || mediaPlayer == null || mediaPlayer.NaturalVideoWidth <= 0 || mediaPlayer.NaturalVideoHeight <= 0)
                return false;

            try
            {
                // Cap internal video surface to 960x540 max for Celeron speed, then blit into canvasBitmap
                int vw = Math.Min(960, canvasW);
                int vh = Math.Min(540, canvasH);
                if (wpfFullVideoTarget == null || wpfFullVideoTarget.PixelWidth != vw || wpfFullVideoTarget.PixelHeight != vh)
                {
                    wpfFullVideoTarget = new RenderTargetBitmap(vw, vh, 96, 96, PixelFormats.Pbgra32);
                }

                using (DrawingContext dc = videoDrawingVisual.RenderOpen())
                {
                    dc.DrawVideo(mediaPlayer, new System.Windows.Rect(0, 0, vw, vh));
                }
                wpfFullVideoTarget.Render(videoDrawingVisual);

                if (vw == canvasW && vh == canvasH)
                {
                    wpfFullVideoTarget.CopyPixels(pixelBuffer, canvasStride, 0);
                }
                else
                {
                    byte[] temp = new byte[vw * vh * 4];
                    wpfFullVideoTarget.CopyPixels(temp, vw * 4, 0);
                    // Fast nearest-neighbor scale into pixelBuffer
                    for (int y = 0; y < canvasH; y++)
                    {
                        int sy = (y * vh) / canvasH;
                        int srcRow = sy * vw * 4;
                        int dstRow = y * canvasStride;
                        for (int x = 0; x < canvasW; x++)
                        {
                            int sx = (x * vw) / canvasW;
                            int sIdx = srcRow + (sx << 2);
                            int dIdx = dstRow + (x << 2);
                            pixelBuffer[dIdx + 0] = temp[sIdx + 0];
                            pixelBuffer[dIdx + 1] = temp[sIdx + 1];
                            pixelBuffer[dIdx + 2] = temp[sIdx + 2];
                            pixelBuffer[dIdx + 3] = 255;
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void GenerateProceduralDemoPixels()
        {
            if (videoPixelBuffer == null || videoPixelBuffer.Length < cols * rows * 4)
                videoPixelBuffer = new byte[cols * rows * 4];

            float cx = cols * 0.5f;
            float cy = rows * 0.5f;
            float invCols = 1.0f / cols;
            float invRows = 1.0f / rows;
            float t = demoTime;

            for (int r = 0; r < rows; r++)
            {
                float dy = (r - cy) * invRows;
                int rowOff = r * cols * 4;
                for (int c = 0; c < cols; c++)
                {
                    float dx = (c - cx) * invCols;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                    float w1 = FastSin(dist * 16.0f - t * 3.2f);
                    float w2 = FastSin(dx * 10.0f + t * 1.8f) * FastSin(dy * 10.0f - t * 1.4f);
                    float v = (w1 * 0.55f + w2 * 0.45f + 1.0f) * 0.5f;

                    int red = (int)(128 + 127 * FastSin(v * 6.28f + t));
                    int grn = (int)(128 + 127 * FastSin(v * 6.28f + t + 2.09f));
                    int blu = (int)(128 + 127 * FastSin(v * 6.28f + t + 4.18f));

                    int mask = (int)(v * v * 255);
                    int idx = rowOff + (c << 2);
                    videoPixelBuffer[idx + 0] = (byte)((blu * mask) >> 8);
                    videoPixelBuffer[idx + 1] = (byte)((grn * mask) >> 8);
                    videoPixelBuffer[idx + 2] = (byte)((red * mask) >> 8);
                    videoPixelBuffer[idx + 3] = 255;
                }
            }
        }

        private void GetThemeBaseColor(int col, int row, out int tr, out int tg, out int tb)
        {
            switch (config.Theme)
            {
                case ColorTheme.ClassicMatrixGreen:
                    tr = 0; tg = 255; tb = 95;
                    break;
                case ColorTheme.CyberpunkNeon:
                    float blend = FastSin(col * 0.14f + demoTime * 2.2f) * 0.5f + 0.5f;
                    tr = (int)(255 * blend);
                    tg = (int)(25 * blend + 245 * (1f - blend));
                    tb = 255;
                    break;
                case ColorTheme.FluidOceanFire:
                    float fBlend = FastSin(col * 0.10f - row * 0.08f + demoTime * 2.6f) * 0.5f + 0.5f;
                    tr = (int)(255 * fBlend + 15 * (1f - fBlend));
                    tg = (int)(110 * fBlend + 210 * (1f - fBlend));
                    tb = (int)(30 * fBlend + 255 * (1f - fBlend));
                    break;
                case ColorTheme.CrimsonHacker:
                    tr = 255; tg = 38; tb = 55;
                    break;
                case ColorTheme.GoldenAmber:
                    tr = 255; tg = 195; tb = 25;
                    break;
                case ColorTheme.RainbowWave:
                    float hue = (col * 4f + row * 2f + demoTime * 55f) % 360f;
                    HsvToRgb(hue, 0.88f, 1.0f, out tr, out tg, out tb);
                    break;
                case ColorTheme.CustomColors:
                default:
                    tr = config.PrimaryColor.R;
                    tg = config.PrimaryColor.G;
                    tb = config.PrimaryColor.B;
                    break;
            }
        }

        public static void HsvToRgb(float h, float s, float v, out int r, out int g, out int b)
        {
            if (h < 0) h += 360f;
            h = h % 360f;
            float c = v * s;
            float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
            float m = v - c;
            float rf = 0, gf = 0, bf = 0;
            if (h < 60) { rf = c; gf = x; }
            else if (h < 120) { rf = x; gf = c; }
            else if (h < 180) { gf = c; bf = x; }
            else if (h < 240) { gf = x; bf = c; }
            else if (h < 300) { rf = x; bf = c; }
            else { rf = c; bf = x; }
            r = (int)((rf + m) * 255);
            g = (int)((gf + m) * 255);
            b = (int)((bf + m) * 255);
        }

        private void DrawGlyphFast(CrispGlyphAtlas atlas, int glyphIndex, int gridCol, int gridRow, int r, int g, int b)
        {
            if (config.ReverseRainDirection)
            {
                gridRow = (rows - 1) - gridRow;
            }
            if (gridRow < 0 || gridRow >= rows || gridCol < 0 || gridCol >= cols) return;

            int cw = atlas.CellW;
            int ch = atlas.CellH;
            int startX = gridCol * cw;
            int startY = gridRow * ch;
            if (startX + cw > canvasW || startY + ch > canvasH) return;

            int maskOffset = glyphIndex * cw * ch;

            byte[] masks = atlas.AlphaMasks;
            byte[] buf = pixelBuffer;
            int stride = canvasStride;

            for (int y = 0; y < ch; y++)
            {
                int dstIdx = (startY + y) * stride + (startX << 2);
                int mIdx = maskOffset + y * cw;
                for (int x = 0; x < cw; x++)
                {
                    int alpha = masks[mIdx + x];
                    if (alpha > 6)
                    {
                        int invA = 255 - alpha;
                        buf[dstIdx + 0] = (byte)((b * alpha + buf[dstIdx + 0] * invA) >> 8);
                        buf[dstIdx + 1] = (byte)((g * alpha + buf[dstIdx + 1] * invA) >> 8);
                        buf[dstIdx + 2] = (byte)((r * alpha + buf[dstIdx + 2] * invA) >> 8);
                        buf[dstIdx + 3] = 255;
                    }
                    dstIdx += 4;
                }
            }
        }

        private void EnsureBgImageCanvasBuffer()
        {
            BackgroundImagePreset activePreset = config.ActiveBgImage;
            if (activePreset == BackgroundImagePreset.None && config.Mode == RenderMode.ImageWallpaper)
            {
                activePreset = BackgroundImagePreset.CyberpunkGrid;
            }
            Bitmap bgBmp = GetBackgroundBitmap(activePreset);
            if (bgBmp == null)
            {
                cachedBgCanvasPixels = null;
                return;
            }

            if (cachedBgCanvasPixels != null &&
                lastBgCanvasPreset == activePreset &&
                lastBgCanvasW == canvasW &&
                lastBgCanvasH == canvasH &&
                Math.Abs(lastBgOpacity - config.BgImageOpacity) < 0.005f)
            {
                return;
            }

            using (Bitmap scaled = new Bitmap(canvasW, canvasH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            {
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    // Cover-fit aspect ratio
                    float scale = Math.Max((float)canvasW / bgBmp.Width, (float)canvasH / bgBmp.Height);
                    int dw = (int)(bgBmp.Width * scale);
                    int dh = (int)(bgBmp.Height * scale);
                    int dx = (canvasW - dw) / 2;
                    int dy = (canvasH - dh) / 2;
                    g.DrawImage(bgBmp, dx, dy, dw, dh);
                }
                BitmapData bd = scaled.LockBits(new Rectangle(0, 0, canvasW, canvasH), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                int byteLen = canvasStride * canvasH;
                try
                {
                    if (cachedBgCanvasPixels == null || cachedBgCanvasPixels.Length != byteLen)
                    {
                        cachedBgCanvasPixels = new byte[byteLen];
                    }
                    Marshal.Copy(bd.Scan0, cachedBgCanvasPixels, 0, byteLen);
                }
                finally
                {
                    scaled.UnlockBits(bd);
                }

                // Pre-multiply by BgImageOpacity blended over BackgroundColor so frame copy is a single fast Buffer.BlockCopy!
                int op = Math.Max(15, Math.Min(255, (int)(config.BgImageOpacity * 255f)));
                int invOp = 255 - op;
                int bgB = config.BackgroundColor.B;
                int bgG = config.BackgroundColor.G;
                int bgR = config.BackgroundColor.R;
                for (int i = 0; i < byteLen; i += 4)
                {
                    cachedBgCanvasPixels[i + 0] = (byte)((cachedBgCanvasPixels[i + 0] * op + bgB * invOp) >> 8);
                    cachedBgCanvasPixels[i + 1] = (byte)((cachedBgCanvasPixels[i + 1] * op + bgG * invOp) >> 8);
                    cachedBgCanvasPixels[i + 2] = (byte)((cachedBgCanvasPixels[i + 2] * op + bgR * invOp) >> 8);
                    cachedBgCanvasPixels[i + 3] = 255;
                }
            }

            lastBgCanvasPreset = activePreset;
            lastBgCanvasW = canvasW;
            lastBgCanvasH = canvasH;
            lastBgOpacity = config.BgImageOpacity;
        }

        private bool SampleBackgroundImageToAsciiBuffer()
        {
            BackgroundImagePreset activePreset = config.ActiveBgImage;
            if (activePreset == BackgroundImagePreset.None) activePreset = BackgroundImagePreset.CyberpunkGrid;
            Bitmap bgBmp = GetBackgroundBitmap(activePreset);
            if (bgBmp == null) return false;

            if (cachedBgGridPixels == null || lastBgGridPreset != activePreset || lastBgGridW != cols || lastBgGridH != rows)
            {
                using (Bitmap gridBmp = new Bitmap(cols, rows, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                {
                    using (Graphics g = Graphics.FromImage(gridBmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(bgBmp, 0, 0, cols, rows);
                    }
                    BitmapData bd = gridBmp.LockBits(new Rectangle(0, 0, cols, rows), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    try
                    {
                        cachedBgGridPixels = new byte[cols * rows * 4];
                        Marshal.Copy(bd.Scan0, cachedBgGridPixels, 0, cachedBgGridPixels.Length);
                    }
                    finally
                    {
                        gridBmp.UnlockBits(bd);
                    }
                }
                lastBgGridPreset = activePreset;
                lastBgGridW = cols;
                lastBgGridH = rows;
            }

            if (videoPixelBuffer == null || videoPixelBuffer.Length < cachedBgGridPixels.Length)
            {
                videoPixelBuffer = new byte[cachedBgGridPixels.Length];
            }
            Buffer.BlockCopy(cachedBgGridPixels, 0, videoPixelBuffer, 0, cachedBgGridPixels.Length);
            return true;
        }

        public void RenderStep(int exactViewportW, int exactViewportH, float rawDt)
        {
            lock (renderSyncLock)
            {
                RenderStepInternal(exactViewportW, exactViewportH, rawDt);
            }
        }

        private void RenderStepInternal(int exactViewportW, int exactViewportH, float rawDt)
        {
            EnsureResources(exactViewportW, exactViewportH);

            rawDt = Math.Min(0.055f, Math.Max(0.008f, rawDt));
            smoothDt = smoothDt * 0.82f + rawDt * 0.18f;
            demoTime += smoothDt * config.Speed;
            mouseActivityEnergy = Math.Max(0f, mouseActivityEnergy - smoothDt * 1.8f);

            // Handle Auto-Ghost Swirl mode (or Fluid mode when Mouse Follow is disabled)
            if (config.MouseMode == MouseEffectMode.AutoGhostSwirl)
            {
                float gx = 0.5f + 0.36f * FastSin(demoTime * 1.35f);
                float gy = 0.5f + 0.32f * FastSin(demoTime * 2.10f + 0.8f);
                bool prevFollow = config.MouseFollowEnabled;
                config.MouseFollowEnabled = true;
                NotifyMouseInput(gx, gy, true);
                config.MouseFollowEnabled = prevFollow;
            }
            else if (config.MouseMode == MouseEffectMode.Off || !config.MouseFollowEnabled)
            {
                if (mouseNodes.Count > 0) mouseNodes.Clear();
            }

            for (int i = mouseNodes.Count - 1; i >= 0; i--)
            {
                MouseTrailNode n = mouseNodes[i];
                n.X += n.VX * smoothDt * 0.25f;
                n.Y += n.VY * smoothDt * 0.25f;
                n.Intensity -= smoothDt * 1.45f;
                if (n.Intensity <= 0f) mouseNodes.RemoveAt(i);
            }

            // Clear background buffer OR copy Background Image Wallpaper!
            bool hasBgImg = (config.ActiveBgImage != BackgroundImagePreset.None || config.Mode == RenderMode.ImageWallpaper);
            if (hasBgImg && config.BgImageStyle != BgImageRenderStyle.ConvertImageToAscii)
            {
                EnsureBgImageCanvasBuffer();
                if (cachedBgCanvasPixels != null && cachedBgCanvasPixels.Length == pixelBuffer.Length)
                {
                    Buffer.BlockCopy(cachedBgCanvasPixels, 0, pixelBuffer, 0, pixelBuffer.Length);
                }
            }
            else
            {
                byte bgB = config.BackgroundColor.B;
                byte bgG = config.BackgroundColor.G;
                byte bgR = config.BackgroundColor.R;
                int firstRowBytes = canvasStride;
                for (int i = 0; i < firstRowBytes; i += 4)
                {
                    pixelBuffer[i + 0] = bgB;
                    pixelBuffer[i + 1] = bgG;
                    pixelBuffer[i + 2] = bgR;
                    pixelBuffer[i + 3] = 255;
                }
                for (int y = 1; y < canvasH; y++)
                {
                    Buffer.BlockCopy(pixelBuffer, 0, pixelBuffer, y * firstRowBytes, firstRowBytes);
                }
            }

            // Advance Matrix Streams
            int glyphCount = mainAtlas.GlyphCount;
            float stepScale = smoothDt * 25.0f * config.Speed;
            for (int c = 0; c < cols; c++)
            {
                MatrixStream s = streams[c];
                float boostSpeed = 1.0f + s.MouseGlowBoost * 0.65f;
                s.Y += s.Speed * stepScale * boostSpeed;
                if (s.MouseGlowBoost > 0f)
                {
                    s.MouseGlowBoost = Math.Max(0f, s.MouseGlowBoost - smoothDt * 1.5f);
                }

                if (config.GlitchMutation && rng.Next(100) < 15)
                {
                    s.GlyphIndices[rng.Next(s.GlyphIndices.Length)] = (byte)rng.Next(glyphCount);
                }

                if (s.Y - s.Length > rows)
                {
                    s.Reset(rows, rng, glyphCount, config, false);
                }
            }

            if (config.Mode == RenderMode.ImageWallpaper)
            {
                if (config.BgImageStyle == BgImageRenderStyle.ConvertImageToAscii)
                {
                    if (SampleBackgroundImageToAsciiBuffer())
                    {
                        RasterizeVideoAscii(false);
                    }
                }
                else if (config.BgImageStyle == BgImageRenderStyle.BehindMatrixAndEffects)
                {
                    RasterizeMatrixWaterfall();
                }
                else if (config.MouseFollowEnabled && mouseNodes.Count > 0)
                {
                    RasterizeMouseWakeOnly();
                }
            }
            else if (hasBgImg && config.BgImageStyle == BgImageRenderStyle.ConvertImageToAscii)
            {
                if (SampleBackgroundImageToAsciiBuffer())
                {
                    RasterizeVideoAscii(config.Mode == RenderMode.MatrixWaterfall || config.Mode == RenderMode.VideoMatrixHybrid);
                }
            }
            else if (config.Mode == RenderMode.MatrixWaterfall)
            {
                RasterizeMatrixWaterfall();
            }
            else if (config.Mode == RenderMode.AsciiFluidSim)
            {
                fluidSim.StepFluid(smoothDt, config.Speed, config.FluidViscosity);
                RasterizeAsciiFluidSim();
            }
            else if (config.Mode == RenderMode.RegularVideoWallpaper)
            {
                if (!RenderRegularVideoFrame())
                {
                    SampleVideoGridThrottled();
                    GenerateProceduralDemoPixels();
                    RasterizeVideoAscii(false);
                }
                else if (config.MouseFollowEnabled && mouseNodes.Count > 0)
                {
                    // Draw interactive Matrix numbers under the mouse over the regular video!
                    RasterizeMouseWakeOnly();
                }
            }
            else
            {
                bool hasVideo = SampleVideoGridThrottled();
                if (!hasVideo)
                {
                    GenerateProceduralDemoPixels();
                }
                RasterizeVideoAscii(config.Mode == RenderMode.VideoMatrixHybrid);
            }

            // If Soyjak Meme is active in ASCII Text style, rasterize it into the glyph grid!
            if (config.ActiveSoyjak != SoyjakPreset.None && config.SoyjakStyle == SoyjakRenderStyle.AsciiTextMeme)
            {
                RasterizeSoyjakAsAscii();
            }

            // Optional Retro CRT Scanline Effect
            if (config.CrtScanlines)
            {
                ApplyCrtScanlinesFast();
            }

            // If Soyjak Meme is active in Full Crisp Overlay style, blend the cached Soyjak cutout directly into pixelBuffer!
            if (config.ActiveSoyjak != SoyjakPreset.None &&
                (config.SoyjakStyle == SoyjakRenderStyle.FullCrispOverlay || config.SoyjakStyle == SoyjakRenderStyle.OverlayPlusMatrix))
            {
                DrawSoyjakCrispOverlay();
            }

            // Optional Cool Overlays: Live Digital Clock & Custom Floating Banner Text blended directly into pixelBuffer!
            if (config.ShowClockOverlay || config.ShowCustomBanner)
            {
                DrawClockAndBannerOverlays();
            }
        }

        private void ApplyCrtScanlinesFast()
        {
            for (int y = 0; y < canvasH; y += 3)
            {
                int rowOff = y * canvasStride;
                for (int x = 0; x < canvasStride; x += 4)
                {
                    int idx = rowOff + x;
                    pixelBuffer[idx + 0] = (byte)((pixelBuffer[idx + 0] * 175) >> 8);
                    pixelBuffer[idx + 1] = (byte)((pixelBuffer[idx + 1] * 175) >> 8);
                    pixelBuffer[idx + 2] = (byte)((pixelBuffer[idx + 2] * 175) >> 8);
                }
            }
        }

        private void DrawClockAndBannerOverlays()
        {
            try
            {
                int nowSec = DateTime.Now.Second;
                int hudH = Math.Min(canvasH, 128);
                if (hudH < 32 || canvasW < 64) return;

                if (cachedHudPixels == null ||
                    cachedHudW != canvasW ||
                    cachedHudH != hudH ||
                    lastHudSecond != nowSec ||
                    lastHudClockOn != config.ShowClockOverlay ||
                    lastHudBannerOn != config.ShowCustomBanner ||
                    lastHudBanner != config.CustomBannerText ||
                    lastHudTheme != config.Theme)
                {
                    using (Bitmap hudBmp = new Bitmap(canvasW, hudH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                    {
                        using (Graphics g = Graphics.FromImage(hudBmp))
                        {
                            g.Clear(System.Drawing.Color.Transparent);
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                            int tr, tg, tb;
                            GetThemeBaseColor(cols / 2, rows / 4, out tr, out tg, out tb);
                            System.Drawing.Color themeCol = System.Drawing.Color.FromArgb(255, Math.Max(60, tr), Math.Max(60, tg), Math.Max(60, tb));

                            float clockLeftEdge = canvasW - 14f;
                            if (config.ShowClockOverlay)
                            {
                                string timeStr = DateTime.Now.ToString("HH:mm:ss");
                                string dateStr = DateTime.Now.ToString("ddd, MMM dd yyyy").ToUpperInvariant();
                                float clockSize = Math.Max(15f, Math.Min(38f, canvasW * 0.032f));
                                float subSize = Math.Max(8.5f, clockSize * 0.34f);

                                using (Font fClock = new Font("Consolas", clockSize, FontStyle.Bold))
                                using (Font fDate = new Font("Segoe UI", subSize, FontStyle.Bold))
                                {
                                    SizeF ts = g.MeasureString(timeStr, fClock);
                                    SizeF ds = g.MeasureString(dateStr, fDate);
                                    float boxW = Math.Max(ts.Width, ds.Width) + 22f;
                                    float boxH = Math.Min(hudH - 8f, ts.Height + ds.Height + 10f);
                                    float bx = Math.Max(4f, canvasW - boxW - 16f);
                                    float by = 10f;
                                    clockLeftEdge = bx - 12f;

                                    using (SolidBrush pillBg = new SolidBrush(System.Drawing.Color.FromArgb(195, 8, 12, 18)))
                                    using (System.Drawing.Pen pillPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(215, themeCol.R, themeCol.G, themeCol.B), 1.8f))
                                    {
                                        g.FillRectangle(pillBg, bx, by, boxW, boxH);
                                        g.DrawRectangle(pillPen, bx, by, boxW, boxH);
                                    }
                                    using (SolidBrush clockBrush = new SolidBrush(System.Drawing.Color.White))
                                    using (SolidBrush dateBrush = new SolidBrush(themeCol))
                                    {
                                        g.DrawString(timeStr, fClock, clockBrush, bx + (boxW - ts.Width) * 0.5f, by + 4f);
                                        g.DrawString(dateStr, fDate, dateBrush, bx + (boxW - ds.Width) * 0.5f, by + ts.Height);
                                    }
                                }
                            }

                            if (config.ShowCustomBanner && !string.IsNullOrEmpty(config.CustomBannerText))
                            {
                                float bannerFontSize = Math.Max(12f, Math.Min(28f, canvasW * 0.022f));
                                using (Font fBanner = new Font("Segoe UI", bannerFontSize, FontStyle.Bold))
                                {
                                    SizeF bs = g.MeasureString(config.CustomBannerText, fBanner);
                                    float bw = bs.Width + 28f;
                                    float bh = Math.Min(hudH - 8f, bs.Height + 12f);
                                    float bx = (canvasW - bw) * 0.5f;
                                    if (bx + bw > clockLeftEdge)
                                    {
                                        bx = Math.Max(14f, clockLeftEdge - bw);
                                    }
                                    float by = 12f;

                                    using (SolidBrush bBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 8, 12, 20)))
                                    using (System.Drawing.Pen bPen = new System.Drawing.Pen(themeCol, 2.0f))
                                    using (SolidBrush bText = new SolidBrush(System.Drawing.Color.White))
                                    {
                                        g.FillRectangle(bBg, bx, by, bw, bh);
                                        g.DrawRectangle(bPen, bx, by, bw, bh);
                                        g.DrawString(config.CustomBannerText, fBanner, bText, bx + 14f, by + 5f);
                                    }
                                }
                            }
                        }

                        BitmapData bd = hudBmp.LockBits(new Rectangle(0, 0, canvasW, hudH), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                        try
                        {
                            int len = canvasW * hudH * 4;
                            if (cachedHudPixels == null || cachedHudPixels.Length != len)
                            {
                                cachedHudPixels = new byte[len];
                            }
                            Marshal.Copy(bd.Scan0, cachedHudPixels, 0, len);
                        }
                        finally
                        {
                            hudBmp.UnlockBits(bd);
                        }
                    }

                    cachedHudW = canvasW;
                    cachedHudH = hudH;
                    lastHudSecond = nowSec;
                    lastHudClockOn = config.ShowClockOverlay;
                    lastHudBannerOn = config.ShowCustomBanner;
                    lastHudBanner = config.CustomBannerText;
                    lastHudTheme = config.Theme;
                }

                if (cachedHudPixels != null)
                {
                    int len = Math.Min(cachedHudPixels.Length, pixelBuffer.Length);
                    for (int i = 0; i < len; i += 4)
                    {
                        int a = cachedHudPixels[i + 3];
                        if (a > 4)
                        {
                            int invA = 255 - a;
                            pixelBuffer[i + 0] = (byte)(cachedHudPixels[i + 0] + ((pixelBuffer[i + 0] * invA) >> 8));
                            pixelBuffer[i + 1] = (byte)(cachedHudPixels[i + 1] + ((pixelBuffer[i + 1] * invA) >> 8));
                            pixelBuffer[i + 2] = (byte)(cachedHudPixels[i + 2] + ((pixelBuffer[i + 2] * invA) >> 8));
                            pixelBuffer[i + 3] = 255;
                        }
                    }
                }
            }
            catch { }
        }

        private void DrawSoyjakCrispOverlay()
        {
            try
            {
                Bitmap memeBmp = GetSoyjakBitmap(config.ActiveSoyjak);
                if (memeBmp == null || canvasW < 32 || canvasH < 32) return;

                float scale = config.SoyjakScale;
                if (cachedSoyjakOverlayPixels == null ||
                    lastOverlayPreset != config.ActiveSoyjak ||
                    Math.Abs(lastOverlayScale - scale) > 0.005f ||
                    lastOverlayCanvasW != canvasW ||
                    lastOverlayCanvasH != canvasH)
                {
                    int drawW, drawH;
                    if (config.ActiveSoyjak == SoyjakPreset.TwoSoyjaksPointing)
                    {
                        drawW = Math.Max(16, (int)(canvasW * Math.Min(1.0f, scale * 1.12f)));
                        drawH = Math.Max(16, (int)(drawW * ((float)memeBmp.Height / memeBmp.Width)));
                        if (drawH > canvasH * 0.92f)
                        {
                            drawH = Math.Max(16, (int)(canvasH * 0.92f));
                            drawW = Math.Max(16, (int)(drawH * ((float)memeBmp.Width / memeBmp.Height)));
                        }
                    }
                    else
                    {
                        drawH = Math.Max(16, (int)(canvasH * Math.Min(0.95f, scale * 0.85f)));
                        drawW = Math.Max(16, (int)(drawH * ((float)memeBmp.Width / memeBmp.Height)));
                        if (drawW > canvasW)
                        {
                            drawW = canvasW;
                            drawH = Math.Max(16, (int)(drawW * ((float)memeBmp.Height / memeBmp.Width)));
                        }
                    }

                    using (Bitmap scaledBmp = new Bitmap(drawW, drawH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                    {
                        using (Graphics g = Graphics.FromImage(scaledBmp))
                        {
                            g.Clear(System.Drawing.Color.Transparent);
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(memeBmp, 0, 0, drawW, drawH);
                        }
                        BitmapData bd = scaledBmp.LockBits(new Rectangle(0, 0, drawW, drawH), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                        try
                        {
                            int byteLen = drawW * drawH * 4;
                            cachedSoyjakOverlayPixels = new byte[byteLen];
                            Marshal.Copy(bd.Scan0, cachedSoyjakOverlayPixels, 0, byteLen);
                        }
                        finally
                        {
                            scaledBmp.UnlockBits(bd);
                        }
                    }

                    soyjakOverlayW = drawW;
                    soyjakOverlayH = drawH;
                    lastOverlayPreset = config.ActiveSoyjak;
                    lastOverlayScale = scale;
                    lastOverlayCanvasW = canvasW;
                    lastOverlayCanvasH = canvasH;
                }

                if (cachedSoyjakOverlayPixels == null || soyjakOverlayW <= 0 || soyjakOverlayH <= 0) return;

                float bounceY = 0f;
                float bounceX = 0f;
                if (config.SoyjakBounceReactive)
                {
                    float amp = 3.5f + mouseActivityEnergy * 14.0f;
                    bounceY = FastSin(demoTime * 12.0f) * amp;
                    bounceX = FastSin(demoTime * 8.5f + 1.5f) * (amp * 0.45f);
                }

                int dx = (canvasW - soyjakOverlayW) / 2 + (int)bounceX;
                int dy = canvasH - soyjakOverlayH + (int)Math.Max(0, bounceY);

                int srcStride = soyjakOverlayW * 4;
                for (int sy = 0; sy < soyjakOverlayH; sy++)
                {
                    int py = dy + sy;
                    if (py < 0 || py >= canvasH) continue;
                    int dstRow = py * canvasStride;
                    int srcRow = sy * srcStride;

                    for (int sx = 0; sx < soyjakOverlayW; sx++)
                    {
                        int px = dx + sx;
                        if (px < 0 || px >= canvasW) continue;
                        int sIdx = srcRow + (sx << 2);
                        int a = cachedSoyjakOverlayPixels[sIdx + 3];
                        if (a > 4)
                        {
                            int dIdx = dstRow + (px << 2);
                            if (a >= 250)
                            {
                                pixelBuffer[dIdx + 0] = cachedSoyjakOverlayPixels[sIdx + 0];
                                pixelBuffer[dIdx + 1] = cachedSoyjakOverlayPixels[sIdx + 1];
                                pixelBuffer[dIdx + 2] = cachedSoyjakOverlayPixels[sIdx + 2];
                                pixelBuffer[dIdx + 3] = 255;
                            }
                            else
                            {
                                int invA = 255 - a;
                                // cachedSoyjakOverlayPixels is Format32bppPArgb (premultiplied alpha)
                                pixelBuffer[dIdx + 0] = (byte)Math.Min(255, cachedSoyjakOverlayPixels[sIdx + 0] + ((pixelBuffer[dIdx + 0] * invA) >> 8));
                                pixelBuffer[dIdx + 1] = (byte)Math.Min(255, cachedSoyjakOverlayPixels[sIdx + 1] + ((pixelBuffer[dIdx + 1] * invA) >> 8));
                                pixelBuffer[dIdx + 2] = (byte)Math.Min(255, cachedSoyjakOverlayPixels[sIdx + 2] + ((pixelBuffer[dIdx + 2] * invA) >> 8));
                                pixelBuffer[dIdx + 3] = 255;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void RasterizeSoyjakAsAscii()
        {
            Bitmap memeBmp = GetSoyjakBitmap(config.ActiveSoyjak);
            if (memeBmp == null) return;

            if (cachedSoyjakGridBmp == null || lastGridPreset != config.ActiveSoyjak || lastGridW != cols || lastGridH != rows || Math.Abs(lastGridScale - config.SoyjakScale) > 0.001f)
            {
                if (cachedSoyjakGridBmp != null) cachedSoyjakGridBmp.Dispose();
                cachedSoyjakGridBmp = new Bitmap(cols, rows, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(cachedSoyjakGridBmp))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    float aspect = (float)memeBmp.Width / memeBmp.Height;
                    int drawH = (int)(rows * Math.Min(0.95f, config.SoyjakScale));
                    int drawW = Math.Min(cols, (int)(drawH * aspect * (mainAtlas.CellH / (float)mainAtlas.CellW)));
                    int dx = (cols - drawW) / 2;
                    int dy = rows - drawH;
                    g.DrawImage(memeBmp, dx, dy, drawW, drawH);
                }
                BitmapData bd = cachedSoyjakGridBmp.LockBits(new Rectangle(0, 0, cols, rows), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    soyjakGridPixels = new byte[cols * rows * 4];
                    Marshal.Copy(bd.Scan0, soyjakGridPixels, 0, soyjakGridPixels.Length);
                }
                finally
                {
                    cachedSoyjakGridBmp.UnlockBits(bd);
                }

                lastGridPreset = config.ActiveSoyjak;
                lastGridScale = config.SoyjakScale;
                lastGridW = cols;
                lastGridH = rows;
            }

            int glyphCount = asciiRampAtlas.GlyphCount;
            for (int r = 0; r < rows; r++)
            {
                int rowOff = r * cols * 4;
                for (int c = 0; c < cols; c++)
                {
                    int idx = rowOff + (c << 2);
                    int alpha = soyjakGridPixels[idx + 3];
                    if (alpha < 40) continue;

                    int b = soyjakGridPixels[idx + 0];
                    int grn = soyjakGridPixels[idx + 1];
                    int red = soyjakGridPixels[idx + 2];

                    // Soyjak line art has black lines on white fill — invert dark lines so edges glow brightly!
                    float lum = (0.299f * red + 0.587f * grn + 0.114f * b) / 255f;
                    float edgeIntensity = 1.0f - lum * 0.65f;
                    int gIdx = (int)(edgeIntensity * (glyphCount - 1));
                    gIdx = Math.Max(1, Math.Min(glyphCount - 1, gIdx));

                    int tr, tg, tb;
                    GetThemeBaseColor(c, r, out tr, out tg, out tb);
                    // Highlight black line-art strokes in bright HeadColor and fill in Primary theme color
                    int outR = lum < 0.35f ? config.HeadColor.R : (int)(tr * 0.85f);
                    int outG = lum < 0.35f ? config.HeadColor.G : (int)(tg * 0.85f);
                    int outB = lum < 0.35f ? config.HeadColor.B : (int)(tb * 0.85f);

                    DrawGlyphFast(asciiRampAtlas, gIdx, c, r, outR, outG, outB);
                }
            }
        }

        private void RasterizeMouseWakeOnly()
        {
            int headR = config.HeadColor.R;
            int headG = config.HeadColor.G;
            int headB = config.HeadColor.B;
            int glyphCount = mainAtlas.GlyphCount;

            for (int m = 0; m < mouseNodes.Count; m++)
            {
                MouseTrailNode node = mouseNodes[m];
                int mc = (int)(node.X * cols);
                int mr = (int)(node.Y * rows);
                int radC = Math.Max(2, (int)(cols * config.MouseRadius * 0.45f));
                int radR = Math.Max(2, (int)(rows * config.MouseRadius * 0.45f));

                for (int r = Math.Max(0, mr - radR); r <= Math.Min(rows - 1, mr + radR); r++)
                {
                    for (int c = Math.Max(0, mc - radC); c <= Math.Min(cols - 1, mc + radC); c++)
                    {
                        float dx = (c - mc) / (float)radC;
                        float dy = (r - mr) / (float)radR;
                        float d2 = dx * dx + dy * dy;
                        if (d2 <= 1.0f)
                        {
                            float glow = (1.0f - d2) * node.Intensity;
                            if (glow > 0.12f)
                            {
                                int tr, tg, tb;
                                GetThemeBaseColor(c, r, out tr, out tg, out tb);
                                int cr = Math.Min(255, (int)(tr * glow + headR * glow * 0.5f));
                                int cg = Math.Min(255, (int)(tg * glow + headG * glow * 0.5f));
                                int cb = Math.Min(255, (int)(tb * glow + headB * glow * 0.5f));
                                int gIdx = ((c * 19 + r * 31 + (int)(demoTime * 15f)) & 0x7FFFFFFF) % glyphCount;
                                DrawGlyphFast(mainAtlas, gIdx, c, r, cr, cg, cb);
                            }
                        }
                    }
                }
            }
        }

        private void RasterizeMatrixWaterfall()
        {
            int headR = config.HeadColor.R;
            int headG = config.HeadColor.G;
            int headB = config.HeadColor.B;
            int glyphCount = mainAtlas.GlyphCount;
            float invCols = 1.0f / cols;
            float invRows = 1.0f / rows;
            float mouseRadSq = config.MouseRadius * config.MouseRadius;

            for (int c = 0; c < cols; c++)
            {
                MatrixStream s = streams[c];
                if (!s.Active) continue;

                float exactY = s.Y;
                int headRow = (int)Math.Floor(exactY);
                int effectiveLen = s.Length + (int)(s.MouseGlowBoost * 10f);

                int startRow = Math.Max(0, headRow - effectiveLen);
                int endRow = Math.Min(rows - 1, headRow + 1);
                float normX = c * invCols;

                for (int r = startRow; r <= endRow; r++)
                {
                    float distFromHead = exactY - r;
                    if (distFromHead < -0.95f || distFromHead > effectiveLen) continue;

                    int gIdx = s.GlyphIndices[(r + c * 17) % s.GlyphIndices.Length] % glyphCount;

                    int cr, cg, cb;
                    if (distFromHead < 0.0f)
                    {
                        float emerge = 1.0f + distFromHead;
                        cr = (int)(headR * emerge);
                        cg = (int)(headG * emerge);
                        cb = (int)(headB * emerge);
                    }
                    else if (distFromHead < 1.15f)
                    {
                        GetThemeBaseColor(c, r, out cr, out cg, out cb);
                        float headMix = 1.0f - (distFromHead / 1.15f);
                        cr = (int)(cr * (1f - headMix) + headR * headMix);
                        cg = (int)(cg * (1f - headMix) + headG * headMix);
                        cb = (int)(cb * (1f - headMix) + headB * headMix);
                    }
                    else
                    {
                        float normDist = distFromHead / (float)(effectiveLen + 1);
                        float fade = 1.0f - normDist;
                        fade = fade * fade;
                        GetThemeBaseColor(c, r, out cr, out cg, out cb);
                        cr = (int)(cr * fade);
                        cg = (int)(cg * fade);
                        cb = (int)(cb * fade);
                    }

                    if (config.MouseFollowEnabled && mouseNodes.Count > 0)
                    {
                        float normY = r * invRows;
                        for (int m = 0; m < mouseNodes.Count; m++)
                        {
                            MouseTrailNode node = mouseNodes[m];
                            float dx = normX - node.X;
                            float dy = normY - node.Y;
                            float d2 = dx * dx + dy * dy;
                            if (d2 < mouseRadSq)
                            {
                                float prox = (1.0f - d2 / mouseRadSq) * node.Intensity;
                                cr = Math.Min(255, cr + (int)(headR * prox * 0.85f));
                                cg = Math.Min(255, cg + (int)(headG * prox * 0.95f));
                                cb = Math.Min(255, cb + (int)(headB * prox * 0.85f));
                            }
                        }
                    }

                    if (cr < 6 && cg < 6 && cb < 6) continue;
                    DrawGlyphFast(mainAtlas, gIdx, c, r, cr, cg, cb);
                }
            }

            if (config.MouseFollowEnabled && mouseNodes.Count > 0)
            {
                RasterizeMouseWakeOnly();
            }
        }

        // Pure Mouse-Following ASCII Fluid Simulator (Blank black screen except where mouse moves!)
        private void RasterizeAsciiFluidSim()
        {
            CrispGlyphAtlas activeAtlas = (config.CharSetPreset == "Numbers Only (0-9)" || config.CharSetPreset == "Binary (0-1)" || config.CharSetPreset == "Custom")
                ? mainAtlas
                : asciiRampAtlas;

            int glyphCount = activeAtlas.GlyphCount;
            float[] dens = fluidSim.Density;
            float[] vx = fluidSim.Vx;
            float[] vy = fluidSim.Vy;
            float[] hues = fluidSim.HueField;

            for (int r = 0; r < rows; r++)
            {
                int rowOff = r * cols;
                for (int c = 0; c < cols; c++)
                {
                    int idx = rowOff + c;
                    float d = dens[idx];
                    // Strictly blank screen unless fluid was splatted by the mouse!
                    if (d < 0.025f) continue;

                    float normD = Math.Min(1.0f, d * 0.90f);
                    int gIdx = (int)(normD * (glyphCount - 1));
                    if (gIdx < 0) gIdx = 0;
                    if (gIdx >= glyphCount) gIdx = glyphCount - 1;
                    if (activeAtlas.Characters[gIdx] == ' ') continue;

                    float velMag = (float)Math.Sqrt(vx[idx] * vx[idx] + vy[idx] * vy[idx]);
                    int cr, cg, cb;
                    if (config.Theme == ColorTheme.CustomColors || config.Theme == ColorTheme.CrimsonHacker || config.Theme == ColorTheme.GoldenAmber)
                    {
                        GetThemeBaseColor(c, r, out cr, out cg, out cb);
                        float coreWhite = Math.Max(0f, (normD - 0.60f) * 2.5f);
                        cr = Math.Min(255, (int)(cr * normD + config.HeadColor.R * coreWhite));
                        cg = Math.Min(255, (int)(cg * normD + config.HeadColor.G * coreWhite));
                        cb = Math.Min(255, (int)(cb * normD + config.HeadColor.B * coreWhite));
                    }
                    else
                    {
                        float hue = (hues[idx] + velMag * 35f + c * 1.8f + r * 1.2f) % 360f;
                        float sat = Math.Max(0.30f, 0.95f - normD * 0.30f);
                        float val = Math.Min(1.0f, normD * 1.15f + velMag * 0.25f);
                        HsvToRgb(hue, sat, val, out cr, out cg, out cb);
                    }

                    if (cr < 6 && cg < 6 && cb < 6) continue;
                    DrawGlyphFast(activeAtlas, gIdx, c, r, cr, cg, cb);
                }
            }
        }

        private void RasterizeVideoAscii(bool hybridRain)
        {
            float contrast = config.Contrast;
            float brightness = config.Brightness;
            float saturation = config.Saturation;

            bool useMainCharset = hybridRain ||
                                  config.CharSetPreset == "Numbers Only (0-9)" ||
                                  config.CharSetPreset == "Binary (0-1)" ||
                                  config.CharSetPreset == "Custom";

            CrispGlyphAtlas activeAtlas = useMainCharset ? mainAtlas : asciiRampAtlas;
            int glyphCount = activeAtlas.GlyphCount;

            for (int r = 0; r < rows; r++)
            {
                int rowOffset = r * cols * 4;
                for (int c = 0; c < cols; c++)
                {
                    int idx = rowOffset + (c << 2);
                    int b = videoPixelBuffer[idx + 0];
                    int grn = videoPixelBuffer[idx + 1];
                    int red = videoPixelBuffer[idx + 2];

                    float rf = (((red * 0.00392156f) - 0.5f) * contrast + 0.5f) * brightness;
                    float gf = (((grn * 0.00392156f) - 0.5f) * contrast + 0.5f) * brightness;
                    float bf = (((b * 0.00392156f) - 0.5f) * contrast + 0.5f) * brightness;

                    rf = rf < 0f ? 0f : (rf > 1f ? 1f : rf);
                    gf = gf < 0f ? 0f : (gf > 1f ? 1f : gf);
                    bf = bf < 0f ? 0f : (bf > 1f ? 1f : bf);

                    float lum = 0.299f * rf + 0.587f * gf + 0.114f * bf;
                    if (config.InvertLuminance) lum = 1.0f - lum;

                    rf = lum + (rf - lum) * saturation;
                    gf = lum + (gf - lum) * saturation;
                    bf = lum + (bf - lum) * saturation;

                    rf = rf < 0f ? 0f : (rf > 1f ? 1f : rf);
                    gf = gf < 0f ? 0f : (gf > 1f ? 1f : gf);
                    bf = bf < 0f ? 0f : (bf > 1f ? 1f : bf);

                    if (lum < 0.045f && !hybridRain) continue;

                    int gIdx;
                    if (hybridRain)
                    {
                        MatrixStream s = streams[c];
                        gIdx = s.GlyphIndices[(r + c * 13) % s.GlyphIndices.Length] % glyphCount;
                        float dist = s.Y - r;
                        float rainBoost = (dist >= 0f && dist < s.Length) ? (1.0f - dist / s.Length) : 0.18f;
                        lum = Math.Min(1.0f, lum * (0.42f + 0.90f * rainBoost + s.MouseGlowBoost * 0.5f));
                        if (dist >= 0f && dist < 1.0f)
                        {
                            rf = Math.Min(1f, rf + 0.40f);
                            gf = Math.Min(1f, gf + 0.50f);
                            bf = Math.Min(1f, bf + 0.40f);
                        }
                    }
                    else
                    {
                        gIdx = (int)(lum * (glyphCount - 1));
                        if (gIdx < 0) gIdx = 0;
                        if (gIdx >= glyphCount) gIdx = glyphCount - 1;
                    }

                    if (activeAtlas.Characters[gIdx] == ' ') continue;

                    int outR, outG, outB;
                    switch (config.VidColorMode)
                    {
                        case VideoColorMode.ThemeTinted:
                            int tr, tg, tb;
                            GetThemeBaseColor(c, r, out tr, out tg, out tb);
                            outR = (int)(tr * lum);
                            outG = (int)(tg * lum);
                            outB = (int)(tb * lum);
                            break;
                        case VideoColorMode.CustomTint:
                            outR = (int)(config.PrimaryColor.R * lum);
                            outG = (int)(config.PrimaryColor.G * lum);
                            outB = (int)(config.PrimaryColor.B * lum);
                            break;
                        case VideoColorMode.RainbowOverlay:
                            float hue = (c * 3.5f + r * 2.5f + demoTime * 50f) % 360f;
                            HsvToRgb(hue, 0.85f, lum, out outR, out outG, out outB);
                            break;
                        case VideoColorMode.TrueVideoColor:
                        default:
                            outR = (int)(rf * 255f);
                            outG = (int)(gf * 255f);
                            outB = (int)(bf * 255f);
                            break;
                    }

                    if (outR < 6 && outG < 6 && outB < 6) continue;
                    DrawGlyphFast(activeAtlas, gIdx, c, r, outR, outG, outB);
                }
            }
        }

        public void Resize(int w, int h)
        {
            EnsureResources(Math.Max(64, w), Math.Max(64, h));
        }

        public void PushMouseNormalized(float nx, float ny)
        {
            NotifyMouseInput(nx, ny, true);
        }

        public void TriggerBurstAt(float nx, float ny)
        {
            for (int i = 0; i < 6; i++)
            {
                float ang = i * 1.047f;
                float ox = nx + (float)Math.Cos(ang) * 0.04f;
                float oy = ny + (float)Math.Sin(ang) * 0.04f;
                NotifyMouseInput(ox, oy, true);
            }
            NotifyMouseInput(nx, ny, true);
        }

        public void InvalidateSoyjakCache()
        {
            lastGridPreset = SoyjakPreset.None;
            lastGridScale = -1f;
            lastOverlayPreset = SoyjakPreset.None;
            lastOverlayScale = -1f;
        }

        public void UpdateAudioMute()
        {
            SetAudioMute(config.MuteAudio);
        }

        public void StepFrame(float dt)
        {
            int w = canvasW > 0 ? canvasW : 720;
            int h = canvasH > 0 ? canvasH : 480;
            RenderStep(w, h, dt);
        }

        // Direct GDI32 byte[]-to-HDC blit — ZERO System.Drawing.Bitmap or LockBits!
        public void DrawToGraphicsUnscaled(Graphics g)
        {
            if (g == null) return;
            lock (renderSyncLock)
            {
                if (pixelBuffer == null || canvasW <= 0 || canvasH <= 0) return;
                IntPtr hdc = IntPtr.Zero;
                try
                {
                    hdc = g.GetHdc();
                    NativeWin32.BITMAPINFO bmi = new NativeWin32.BITMAPINFO();
                    bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(NativeWin32.BITMAPINFOHEADER));
                    bmi.bmiHeader.biWidth = canvasW;
                    bmi.bmiHeader.biHeight = -canvasH; // top-down DIB
                    bmi.bmiHeader.biPlanes = 1;
                    bmi.bmiHeader.biBitCount = 32;
                    bmi.bmiHeader.biCompression = 0; // BI_RGB
                    bmi.bmiHeader.biSizeImage = (uint)(canvasStride * canvasH);

                    NativeWin32.SetDIBitsToDevice(
                        hdc,
                        0, 0,
                        (uint)canvasW, (uint)canvasH,
                        0, 0,
                        0, (uint)canvasH,
                        pixelBuffer,
                        ref bmi,
                        0);
                }
                catch { }
                finally
                {
                    if (hdc != IntPtr.Zero)
                    {
                        try { g.ReleaseHdc(hdc); } catch { }
                    }
                }
            }
        }

        // Direct managed byte[]-to-WriteableBitmap copy — ZERO System.Drawing.Bitmap or LockBits!
        public void CopyToWriteableBitmap(WriteableBitmap wbmp)
        {
            if (wbmp == null) return;
            lock (renderSyncLock)
            {
                if (pixelBuffer == null || canvasW <= 0 || canvasH <= 0) return;
                if (wbmp.PixelWidth != canvasW || wbmp.PixelHeight != canvasH) return;
                try
                {
                    wbmp.WritePixels(new System.Windows.Int32Rect(0, 0, canvasW, canvasH), pixelBuffer, canvasStride, 0);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            if (canvasBitmap != null) canvasBitmap.Dispose();
            if (cachedSoyjakGridBmp != null) cachedSoyjakGridBmp.Dispose();
            foreach (var kv in soyjakBitmaps)
            {
                if (kv.Value != null) kv.Value.Dispose();
            }
            foreach (var kv in bgBitmaps)
            {
                if (kv.Value != null) kv.Value.Dispose();
            }
            if (mediaPlayer != null) mediaPlayer.Close();
        }
    }


    // Saves/loads every setting to %LocalAppData%\MatrixWallpaperEngine\settings.cfg so nothing resets
    // after a restart or an update.
    public static class SettingsStore
    {
        private static string lastSaved = "";

        private static string FilePath
        {
            get { return Path.Combine(AppUpdater.DataDir, "settings.cfg"); }
        }

        public static string Serialize(EngineConfig c)
        {
            StringBuilder sb = new StringBuilder();
            FieldInfo[] fields = typeof(EngineConfig).GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (f.Name == "DpiScale") continue;
                object v = f.GetValue(c);
                if (v == null) continue;
                string s;
                if (f.FieldType == typeof(System.Drawing.Color)) s = ((System.Drawing.Color)v).ToArgb().ToString();
                else if (f.FieldType == typeof(float)) s = ((float)v).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                else if (f.FieldType.IsEnum) s = v.ToString();
                else s = Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
                s = s.Replace("\r", " ").Replace("\n", " ");
                sb.Append(f.Name).Append('=').Append(s).Append("\r\n");
            }
            return sb.ToString();
        }

        public static void Load(EngineConfig c)
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                Dictionary<string, string> map = new Dictionary<string, string>();
                string[] lines = File.ReadAllLines(FilePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    int eq = lines[i].IndexOf('=');
                    if (eq <= 0) continue;
                    map[lines[i].Substring(0, eq).Trim()] = lines[i].Substring(eq + 1);
                }

                FieldInfo[] fields = typeof(EngineConfig).GetFields(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo f = fields[i];
                    string s;
                    if (f.Name == "DpiScale" || !map.TryGetValue(f.Name, out s)) continue;
                    try
                    {
                        if (f.FieldType == typeof(System.Drawing.Color)) f.SetValue(c, System.Drawing.Color.FromArgb(int.Parse(s)));
                        else if (f.FieldType == typeof(float)) f.SetValue(c, float.Parse(s, System.Globalization.CultureInfo.InvariantCulture));
                        else if (f.FieldType == typeof(int)) f.SetValue(c, int.Parse(s));
                        else if (f.FieldType == typeof(bool)) f.SetValue(c, bool.Parse(s));
                        else if (f.FieldType.IsEnum) f.SetValue(c, Enum.Parse(f.FieldType, s));
                        else if (f.FieldType == typeof(string)) f.SetValue(c, s);
                    }
                    catch { }
                }
                lastSaved = Serialize(c);
            }
            catch (Exception ex) { AppLog.Write(ex); }
        }

        public static void SaveIfChanged(EngineConfig c)
        {
            try
            {
                string s = Serialize(c);
                if (s == lastSaved) return;
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, s, Encoding.UTF8);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
                lastSaved = s;
            }
            catch (Exception ex) { AppLog.Write(ex); }
        }
    }

}
