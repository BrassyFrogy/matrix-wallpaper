using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace MatrixWallpaperEngine
{
    // =========================================================================
    // WIN32 DESKTOP WALLPAPER HOST WINDOW (Attaches behind Windows Desktop Icons)
    // =========================================================================
    public class WallpaperWindow : System.Windows.Forms.Form
    {
        private FastMatrixEngine renderer;
        private EngineConfig config;
        private System.Drawing.Point lastScreenMouse = new System.Drawing.Point(-9999, -9999);

        public WallpaperWindow(FastMatrixEngine sharedRenderer, EngineConfig cfg)
        {
            this.renderer = sharedRenderer;
            this.config = cfg;

            this.Text = "Matrix Live Wallpaper Layer";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.BackColor = System.Drawing.Color.FromArgb(6, 9, 12);

            System.Drawing.Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            this.Bounds = bounds;

            this.SetStyle(
                System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
                System.Windows.Forms.ControlStyles.UserPaint |
                System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer |
                System.Windows.Forms.ControlStyles.Opaque, true);
        }

        public void AttachToDesktopBackground()
        {
            System.Drawing.Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            string statusMsg;
            NativeWin32.AttachWindowToDesktop(this, out statusMsg);
            renderer.Resize(bounds.Width, bounds.Height);
        }

        public void DetachFromDesktop()
        {
            NativeWin32.DetachWindowFromDesktop(this, System.Windows.Forms.Screen.PrimaryScreen.Bounds);
            this.Hide();
        }

        public void PollDesktopMouse()
        {
            if (!this.Visible || config.MouseMode != MouseEffectMode.FollowCursor || !config.MouseFollowEnabled) return;

            System.Drawing.Point pt = System.Windows.Forms.Cursor.Position;
            System.Drawing.Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            int rx = pt.X - bounds.X;
            int ry = pt.Y - bounds.Y;
            if (rx >= 0 && ry >= 0 && rx < bounds.Width && ry < bounds.Height)
            {
                if (Math.Abs(rx - lastScreenMouse.X) > 1 || Math.Abs(ry - lastScreenMouse.Y) > 1)
                {
                    float nx = (float)rx / Math.Max(1, bounds.Width);
                    float ny = (float)ry / Math.Max(1, bounds.Height);
                    renderer.NotifyMouseInput(nx, ny, true);
                    lastScreenMouse = new System.Drawing.Point(rx, ry);
                }
            }
        }

        protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs e) { }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            try
            {
                renderer.DrawToGraphicsUnscaled(e.Graphics);
            }
            catch { }
        }
    }

    // =========================================================================
    // REUSABLE WPF VECTOR CARD BUTTON (Auto-Wrapping Text, Never Truncates!)
    // =========================================================================
    public class WpfCardOption
    {
        public Border CardBorder;
        public Border LeftAccentBar;
        public TextBlock TitleBlock;
        public TextBlock SubBlock;
        public Border BadgeBorder;
        public TextBlock BadgeBlock;
        public Color AccentColor;
        public Color BaseBgColor;
        public Color SelectedBgColor;
        public bool IsSelected;
        public bool IsPrimaryCta;

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            RefreshVisuals(false);
        }

        public void RefreshVisuals(bool hovered)
        {
            if (IsPrimaryCta)
            {
                Color bg = IsSelected
                    ? Color.FromRgb(215, 48, 72)
                    : (hovered ? Color.FromRgb(22, 230, 125) : Color.FromRgb(0, 198, 102));
                Color border = IsSelected
                    ? Color.FromRgb(255, 140, 155)
                    : Color.FromRgb(150, 255, 205);
                CardBorder.Background = new SolidColorBrush(bg);
                CardBorder.BorderBrush = new SolidColorBrush(border);
                CardBorder.BorderThickness = new Thickness(2.0);
                TitleBlock.Foreground = IsSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(6, 22, 12));
                if (SubBlock != null)
                {
                    SubBlock.Foreground = IsSelected
                        ? new SolidColorBrush(Color.FromRgb(255, 225, 230))
                        : new SolidColorBrush(Color.FromRgb(10, 48, 24));
                }
                return;
            }

            Color fill = IsSelected
                ? SelectedBgColor
                : (hovered
                    ? Color.FromRgb((byte)Math.Min(255, BaseBgColor.R + 12), (byte)Math.Min(255, BaseBgColor.G + 16), (byte)Math.Min(255, BaseBgColor.B + 24))
                    : BaseBgColor);

            Color stroke = IsSelected
                ? AccentColor
                : (hovered ? Color.FromArgb(180, AccentColor.R, AccentColor.G, AccentColor.B) : Color.FromRgb(42, 54, 74));

            CardBorder.Background = new SolidColorBrush(fill);
            CardBorder.BorderBrush = new SolidColorBrush(stroke);
            CardBorder.BorderThickness = new Thickness(IsSelected ? 2.0 : 1.2);

            if (LeftAccentBar != null)
            {
                LeftAccentBar.Background = new SolidColorBrush(AccentColor);
                LeftAccentBar.Visibility = IsSelected ? Visibility.Visible : Visibility.Collapsed;
            }

            TitleBlock.Foreground = IsSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(235, 242, 250));
            if (SubBlock != null)
            {
                SubBlock.Foreground = IsSelected
                    ? new SolidColorBrush(AccentColor)
                    : new SolidColorBrush(Color.FromRgb(162, 178, 202));
            }

            if (BadgeBorder != null && BadgeBlock != null)
            {
                BadgeBorder.Background = new SolidColorBrush(IsSelected ? AccentColor : Color.FromRgb(34, 45, 64));
                BadgeBlock.Foreground = new SolidColorBrush(IsSelected ? Color.FromRgb(8, 14, 20) : Color.FromRgb(190, 205, 225));
            }
        }
    }

    // =========================================================================
    // MODERN WPF VECTOR STUDIO WINDOW (Zero Text Clipping, No Broken Menus!)
    // =========================================================================
    public class MainStudioWpfWindow : Window
    {
        private EngineConfig config = new EngineConfig();
        private FastMatrixEngine renderer;
        private WallpaperWindow wallpaperForm;
        private DispatcherTimer renderTimer;
        private WriteableBitmap previewWriteableBmp;
        private Stopwatch fpsWatch = Stopwatch.StartNew();
        private int frameCounter = 0;
        private int currentFps = 60;

        // Top Bar Controls
        private WpfCardOption btnHeaderMouseToggle;
        private WpfCardOption btnHeaderRandomize;
        private WpfCardOption btnHeaderTutorial;
        private WpfCardOption btnHeaderDesktopToggle;

        // Left Navigation Tabs (6 Clear Categories in a 3x2 Grid)
        private WpfCardOption[] tabButtons = new WpfCardOption[6];
        private ScrollViewer[] tabPages = new ScrollViewer[6];
        private Border leftDeckHighlightBorder;
        private int activeTabIndex = 0;

        // Cloud Updater & Performance Controls (Tab 5 for Bradley)
        private TextBlock lblUpdateStatusBanner;
        private TextBlock lblUpdateDetail;
        private TextBlock lblUpdateNotes;
        private Border updateNotesCard;
        private WpfCardOption btnCheckOrInstallUpdate;
        private WpfCardOption btnEcoModeToggle;
        private CheckBox chkAutoCheckUpdates;
        private CheckBox chkPauseWhenMinimized;
        private UpdateInfo latestAvailableUpdate = null;
        private bool isCheckingOrDownloading = false;
        private bool justUpdatedFromUpdater = false;

        // Right Preview & Action Bar
        private Image previewImageControl;
        private Border previewViewportBorder;
        private TextBlock lblLiveStatus;
        private TextBlock lblMouseBadge;
        private Border bottomDesktopBarBorder;
        private TextBlock lblDesktopStatusTitle;
        private TextBlock lblDesktopStatusSub;
        private WpfCardOption btnBigDesktopToggle;

        // Tab 0: 6 Wallpaper Modes + MP4 Video Upload
        private WpfCardOption[] modeCards = new WpfCardOption[6];
        private TextBlock lblLoadedVideoStatus;
        private CheckBox chkLoopVideo;
        private CheckBox chkMuteAudio;

        // Tab 1: Background Image Wallpaper Deck
        private WpfCardOption[] bgImageCards = new WpfCardOption[5];
        private WpfCardOption[] bgStyleCards = new WpfCardOption[3];
        private Slider sldBgOpacity;
        private TextBlock lblBgOpacityVal;
        private TextBlock lblCustomBgStatus;

        // Tab 2: Soyjak Meme Deck
        private WpfCardOption[] soyjakCards = new WpfCardOption[6];
        private WpfCardOption[] soyjakStyleCards = new WpfCardOption[3];
        private Slider sldSoyjakSize;
        private TextBlock lblSoyjakSizeVal;
        private CheckBox chkSoyjakBounce;

        // Tab 3: Colors & Text (All Visible Pill Grids — Zero Disappearing Dropdowns!)
        private WpfCardOption[] themeCards = new WpfCardOption[7];
        private Button btnPickPrimary;
        private Button btnPickHead;
        private Button btnPickBg;
        private WpfCardOption[] vidColorCards = new WpfCardOption[4];
        private WpfCardOption[] charPresetCards = new WpfCardOption[6];
        private TextBox txtCustomChars;

        // Tab 4: Mouse Controls (Turn Off Mouse!) + Cool Extra Features + Sliders
        private WpfCardOption[] mouseModeCards = new WpfCardOption[3];
        private WpfCardOption cardToggleClock;
        private WpfCardOption cardToggleBanner;
        private WpfCardOption cardToggleCrt;
        private WpfCardOption cardToggleReverseRain;
        private TextBox txtBannerInput;
        private Slider sldSpeed;
        private TextBlock lblSpeedVal;
        private Slider sldFontSize;
        private TextBlock lblFontSizeVal;
        private Slider sldMouseRadius;
        private TextBlock lblMouseRadiusVal;
        private Slider sldFluidFade;
        private TextBlock lblFluidFadeVal;

        // Interactive Highlight-Box Tutorial Overlay (Native WPF)
        private Grid tutorialOverlayGrid;
        private Border tutorialCalloutCard;
        private TextBlock tutStepBadgeText;
        private TextBlock tutTitleText;
        private TextBlock tutDescText;
        private TextBlock tutArrowHintText;
        private Button tutBackBtn;
        private Button tutNextBtn;
        private int tutorialStepIndex = -1;

        // CLI Automation for Screenshot Verification
        private string screenshotOutputPath = null;
        private int screenshotFrameTarget = 38;
        private int totalFramesRendered = 0;

        public MainStudioWpfWindow(string[] args)
        {
            SettingsStore.Load(config);
            AppUpdater.CleanupLeftovers();
            ParseCommandLineArgs(args);
            renderer = new FastMatrixEngine(config);

            BuildWpfStudioUI();
            LoadDefaultDemoVideoIfAvailable();

            this.Loaded += (s, e) =>
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                if (source != null && source.CompositionTarget != null)
                {
                    config.DpiScale = (float)source.CompositionTarget.TransformToDevice.M11;
                }
                SwitchLeftTab(activeTabIndex);
                SyncAllControlsWithConfig();
                if (tutorialStepIndex >= 0)
                {
                    SetTutorialStep(tutorialStepIndex);
                }
                if (string.IsNullOrEmpty(screenshotOutputPath) && config.AutoCheckUpdates)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(_ => SilentUpdateCheck());
                }
            };

            renderTimer = new DispatcherTimer(DispatcherPriority.Render);
            renderTimer.Interval = TimeSpan.FromMilliseconds(config.TargetFps <= 30 ? 33 : 16);
            renderTimer.Tick += OnRenderFrameTick;
            renderTimer.Start();
        }

        private void ParseCommandLineArgs(string[] args)
        {
            if (args == null) return;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                if (a == "--updated")
                {
                    justUpdatedFromUpdater = true;
                }
                else if (a == "--screenshot" && i + 1 < args.Length)
                {
                    screenshotOutputPath = args[i + 1];
                    i++;
                }
                else if (a == "--mode" && i + 1 < args.Length)
                {
                    string m = args[i + 1].ToLowerInvariant();
                    if (m == "video" || m == "ascii") config.Mode = RenderMode.VideoToAscii;
                    else if (m == "regvideo" || m == "regularvideo") config.Mode = RenderMode.RegularVideoWallpaper;
                    else if (m == "hybrid") config.Mode = RenderMode.VideoMatrixHybrid;
                    else if (m == "fluid") config.Mode = RenderMode.AsciiFluidSim;
                    else if (m == "image" || m == "bgimage") config.Mode = RenderMode.ImageWallpaper;
                    else config.Mode = RenderMode.MatrixWaterfall;
                    i++;
                }
                else if (a == "--bgimage" && i + 1 < args.Length)
                {
                    int bIdx;
                    if (int.TryParse(args[i + 1], out bIdx) && bIdx >= 0 && bIdx <= 4)
                        config.ActiveBgImage = (BackgroundImagePreset)bIdx;
                    i++;
                }
                else if (a == "--mouse" && i + 1 < args.Length)
                {
                    string mm = args[i + 1].ToLowerInvariant();
                    if (mm == "off" || mm == "0") renderer.SetMouseMode(MouseEffectMode.Off);
                    else if (mm == "auto" || mm == "2") renderer.SetMouseMode(MouseEffectMode.AutoGhostSwirl);
                    else renderer.SetMouseMode(MouseEffectMode.FollowCursor);
                    i++;
                }
                else if (a == "--clock")
                {
                    config.ShowClockOverlay = true;
                }
                else if (a == "--banner" && i + 1 < args.Length)
                {
                    config.ShowCustomBanner = true;
                    config.CustomBannerText = args[i + 1];
                    i++;
                }
                else if (a == "--theme" && i + 1 < args.Length)
                {
                    int tIdx;
                    if (int.TryParse(args[i + 1], out tIdx) && tIdx >= 0 && tIdx <= 6)
                        config.Theme = (ColorTheme)tIdx;
                    i++;
                }
                else if (a == "--soyjak" && i + 1 < args.Length)
                {
                    int sIdx;
                    if (int.TryParse(args[i + 1], out sIdx) && sIdx >= 0 && sIdx <= 5)
                        config.ActiveSoyjak = (SoyjakPreset)sIdx;
                    i++;
                }
                else if (a == "--tab" && i + 1 < args.Length)
                {
                    int tabIdx;
                    if (int.TryParse(args[i + 1], out tabIdx) && tabIdx >= 0 && tabIdx <= 5)
                        activeTabIndex = tabIdx;
                    i++;
                }
                else if (a == "--tutorial")
                {
                    tutorialStepIndex = 0;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        int step;
                        if (int.TryParse(args[i + 1], out step))
                        {
                            tutorialStepIndex = Math.Max(0, Math.Min(3, step));
                            i++;
                        }
                    }
                }
            }
        }

        private void LoadDefaultDemoVideoIfAvailable()
        {
            try
            {
                string samplePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample_demo.mp4");
                if (File.Exists(samplePath))
                {
                    renderer.LoadVideo(samplePath);
                    return;
                }
                string tempSamplePath = Path.Combine(Path.GetTempPath(), "MatrixWallpaper_sample_demo.mp4");
                if (!File.Exists(tempSamplePath))
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    using (Stream s = asm.GetManifestResourceStream("sample_demo.mp4"))
                    {
                        if (s != null)
                        {
                            using (FileStream fs = new FileStream(tempSamplePath, FileMode.Create, FileAccess.Write))
                            {
                                s.CopyTo(fs);
                            }
                        }
                    }
                }
                if (File.Exists(tempSamplePath))
                {
                    renderer.LoadVideo(tempSamplePath);
                }
            }
            catch { }
        }

        // =====================================================================
        // BUILD HARDWARE-ACCELERATED WPF VECTOR UI
        // =====================================================================
        private void BuildWpfStudioUI()
        {
            this.Title = "Matrix & Soyjak Wallpaper Studio — WPF Vector Edition (Zero Text Clipping)";
            this.Width = 1240;
            this.Height = 800;
            this.MinWidth = 1040;
            this.MinHeight = 680;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = new SolidColorBrush(Color.FromRgb(10, 13, 18));
            this.Foreground = Brushes.White;
            this.FontFamily = new FontFamily("Segoe UI");
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);

            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "matrix_logo.ico");
                if (File.Exists(icoPath))
                {
                    this.Icon = new BitmapImage(new Uri(icoPath, UriKind.Absolute));
                }
                else
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    using (Stream s = asm.GetManifestResourceStream("matrix_logo.png"))
                    {
                        if (s != null)
                        {
                            BitmapImage bi = new BitmapImage();
                            bi.BeginInit();
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.StreamSource = s;
                            bi.EndInit();
                            bi.Freeze();
                            this.Icon = bi;
                        }
                    }
                }
            }
            catch { }

            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(74) }); // Top Header Bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Main Body

            // -----------------------------------------------------------------
            // 1. TOP HEADER BAR
            // -----------------------------------------------------------------
            Border topHeaderBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 19, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(32, 46, 66)),
                BorderThickness = new Thickness(0, 0, 0, 1.5),
                Padding = new Thickness(18, 8, 18, 8)
            };
            Grid.SetRow(topHeaderBorder, 0);

            Grid topBarGrid = new Grid();
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel brandStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            brandStack.Children.Add(new TextBlock
            {
                Text = "MATRIX, IMAGE & SOYJAK WALLPAPER ENGINE",
                FontSize = 18.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 135))
            });
            brandStack.Children.Add(new TextBlock
            {
                Text = "WPF Vector Studio • Turn Off Mouse Anytime • Custom Image & MP4 Backgrounds",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(162, 180, 205)),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(brandStack, 0);
            topBarGrid.Children.Add(brandStack);

            // Top Right Action Buttons
            StackPanel topActionsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            btnHeaderMouseToggle = CreateCardButton(
                "◉ MOUSE FX: ON",
                "Click to turn OFF or Auto-Swirl",
                null,
                Color.FromRgb(192, 132, 252),
                Color.FromRgb(24, 28, 42),
                Color.FromRgb(38, 28, 58),
                false,
                () => CycleMouseModeQuick());
            btnHeaderMouseToggle.CardBorder.Width = 175;
            btnHeaderMouseToggle.CardBorder.Margin = new Thickness(0, 0, 8, 0);
            topActionsPanel.Children.Add(btnHeaderMouseToggle.CardBorder);

            btnHeaderRandomize = CreateCardButton(
                "🎲 SURPRISE ME",
                "Random cool preset",
                null,
                Color.FromRgb(255, 185, 40),
                Color.FromRgb(28, 30, 38),
                Color.FromRgb(48, 38, 20),
                false,
                () => TriggerRandomCoolPreset());
            btnHeaderRandomize.CardBorder.Width = 148;
            btnHeaderRandomize.CardBorder.Margin = new Thickness(0, 0, 8, 0);
            topActionsPanel.Children.Add(btnHeaderRandomize.CardBorder);

            btnHeaderTutorial = CreateCardButton(
                "[?] HOW TO USE",
                "Highlight tutorial",
                null,
                Color.FromRgb(56, 189, 248),
                Color.FromRgb(20, 32, 48),
                Color.FromRgb(24, 52, 82),
                false,
                () => StartOrToggleTutorial());
            btnHeaderTutorial.CardBorder.Width = 152;
            btnHeaderTutorial.CardBorder.Margin = new Thickness(0, 0, 10, 0);
            topActionsPanel.Children.Add(btnHeaderTutorial.CardBorder);

            btnHeaderDesktopToggle = CreateCardButton(
                "[+] ADD TO DESKTOP WALLPAPER",
                "1-Click Apply Behind Icons",
                null,
                Color.FromRgb(0, 235, 125),
                Color.FromRgb(0, 195, 98),
                Color.FromRgb(215, 50, 75),
                true,
                () => ToggleDesktopWallpaperMode());
            btnHeaderDesktopToggle.CardBorder.Width = 250;
            topActionsPanel.Children.Add(btnHeaderDesktopToggle.CardBorder);

            Grid.SetColumn(topActionsPanel, 1);
            topBarGrid.Children.Add(topActionsPanel);
            topHeaderBorder.Child = topBarGrid;
            rootGrid.Children.Add(topHeaderBorder);

            // -----------------------------------------------------------------
            // 2. MAIN TWO-ZONE BODY (Left 470px Control Deck + Right Live Preview)
            // -----------------------------------------------------------------
            Grid bodyGrid = new Grid();
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(475) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(bodyGrid, 1);

            // LEFT CONTROL DECK
            leftDeckHighlightBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(13, 17, 24)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(32, 44, 62)),
                BorderThickness = new Thickness(0, 0, 1.5, 0),
                Padding = new Thickness(12, 10, 12, 10)
            };
            Grid.SetColumn(leftDeckHighlightBorder, 0);

            Grid leftDeckGrid = new Grid();
            leftDeckGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5 Category Tab Buttons
            leftDeckGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Tab Content

            // 6 Category Tab Buttons (Organized in a balanced 3x2 UniformGrid so all text fits 100%!)
            Grid navTabsGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            navTabsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            navTabsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            UniformGrid topNavRow = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 6) };
            UniformGrid botNavRow = new UniformGrid { Columns = 3 };
            Grid.SetRow(topNavRow, 0);
            Grid.SetRow(botNavRow, 1);
            navTabsGrid.Children.Add(topNavRow);
            navTabsGrid.Children.Add(botNavRow);

            string[] tabTitles = new string[]
            {
                "1. Modes & Video",
                "2. Image BG",
                "3. Soyjak Memes",
                "4. Colors & Text",
                "5. Mouse & FX",
                "6. Updates & Eco"
            };
            string[] tabSubs = new string[]
            {
                "6 Modes + MP4",
                "Set Photo / Art BG",
                "Pointing Memes",
                "Themes & Chars",
                "Mouse Off + Clock",
                "Check & Laptop Eco"
            };
            Color[] tabAccents = new Color[]
            {
                Color.FromRgb(0, 235, 125),
                Color.FromRgb(56, 189, 248),
                Color.FromRgb(255, 190, 40),
                Color.FromRgb(244, 114, 182),
                Color.FromRgb(192, 132, 252),
                Color.FromRgb(0, 245, 200)
            };

            for (int i = 0; i < 6; i++)
            {
                int capturedIdx = i;
                WpfCardOption tabBtn = CreateCardButton(
                    tabTitles[i],
                    tabSubs[i],
                    null,
                    tabAccents[i],
                    Color.FromRgb(19, 25, 36),
                    Color.FromRgb(22, 38, 52),
                    false,
                    () => SwitchLeftTab(capturedIdx));
                tabBtn.CardBorder.Margin = new Thickness(3, 0, 3, 0);
                tabButtons[i] = tabBtn;

                if (i < 3) topNavRow.Children.Add(tabBtn.CardBorder);
                else botNavRow.Children.Add(tabBtn.CardBorder);

                ScrollViewer sv = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Visibility = (i == 0) ? Visibility.Visible : Visibility.Collapsed,
                    Padding = new Thickness(2, 2, 6, 12)
                };
                Grid.SetRow(sv, 1);
                tabPages[i] = sv;
                leftDeckGrid.Children.Add(sv);
            }

            leftDeckGrid.Children.Add(navTabsGrid);
            leftDeckHighlightBorder.Child = leftDeckGrid;
            bodyGrid.Children.Add(leftDeckHighlightBorder);

            // Populate all 6 Tab Pages with Auto-Wrapping WPF Vector Controls
            tabPages[0].Content = BuildTab0_ModesAndVideo();
            tabPages[1].Content = BuildTab1_ImageBackgrounds();
            tabPages[2].Content = BuildTab2_SoyjakMemes();
            tabPages[3].Content = BuildTab3_ColorsAndText();
            tabPages[4].Content = BuildTab4_MouseAndCoolFx();
            tabPages[5].Content = BuildTab5_RemoteUpdater();

            // -----------------------------------------------------------------
            // 3. RIGHT LIVE PREVIEW & GIANT "ADD TO DESKTOP WALLPAPER" BAR
            // -----------------------------------------------------------------
            Grid rightZoneGrid = new Grid { Margin = new Thickness(16, 12, 16, 14) };
            rightZoneGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status Bar
            rightZoneGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Live Preview
            rightZoneGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Bottom Desktop Action Bar
            Grid.SetColumn(rightZoneGrid, 1);

            // Top Preview Header
            Grid previewHeaderGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            previewHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            previewHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            previewHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lblPreviewTitle = new TextBlock
            {
                Text = "● LIVE INTERACTIVE PREVIEW",
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 235, 130)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblPreviewTitle, 0);
            previewHeaderGrid.Children.Add(lblPreviewTitle);

            lblMouseBadge = new TextBlock
            {
                Text = "◉ Mouse Follow: ON",
                FontSize = 12.0,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(192, 132, 252)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 10, 0)
            };
            Grid.SetColumn(lblMouseBadge, 1);
            previewHeaderGrid.Children.Add(lblMouseBadge);

            lblLiveStatus = new TextBlock
            {
                Text = "60 FPS • Ready",
                FontSize = 12.0,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 182, 210)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblLiveStatus, 2);
            previewHeaderGrid.Children.Add(lblLiveStatus);

            Grid.SetRow(previewHeaderGrid, 0);
            rightZoneGrid.Children.Add(previewHeaderGrid);

            // Center Live Preview Viewport + Tutorial Overlay Grid
            previewViewportBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(6, 9, 12)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(42, 58, 82)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Cursor = Cursors.Cross
            };
            Grid.SetRow(previewViewportBorder, 1);

            Grid previewInnerGrid = new Grid();
            previewImageControl = new Image
            {
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            RenderOptions.SetBitmapScalingMode(previewImageControl, BitmapScalingMode.LowQuality);
            previewInnerGrid.Children.Add(previewImageControl);

            // Interactive Mouse Events on Preview
            previewViewportBorder.MouseMove += (s, e) =>
            {
                if (config.MouseMode != MouseEffectMode.FollowCursor || !config.MouseFollowEnabled) return;
                Point pt = e.GetPosition(previewViewportBorder);
                double w = Math.Max(1.0, previewViewportBorder.ActualWidth);
                double h = Math.Max(1.0, previewViewportBorder.ActualHeight);
                renderer.PushMouseNormalized((float)(pt.X / w), (float)(pt.Y / h));
            };
            previewViewportBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (config.MouseMode != MouseEffectMode.FollowCursor || !config.MouseFollowEnabled) return;
                Point pt = e.GetPosition(previewViewportBorder);
                double w = Math.Max(1.0, previewViewportBorder.ActualWidth);
                double h = Math.Max(1.0, previewViewportBorder.ActualHeight);
                renderer.TriggerBurstAt((float)(pt.X / w), (float)(pt.Y / h));
            };

            // Build Native WPF Highlight-Box Tutorial Overlay inside previewInnerGrid
            BuildTutorialOverlayUI();
            previewInnerGrid.Children.Add(tutorialOverlayGrid);

            previewViewportBorder.Child = previewInnerGrid;
            rightZoneGrid.Children.Add(previewViewportBorder);

            // Bottom Giant "ADD TO DESKTOP WALLPAPER" Action Bar
            bottomDesktopBarBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 26, 36)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 225, 120)),
                BorderThickness = new Thickness(2.0),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 12, 16, 12),
                Margin = new Thickness(0, 12, 0, 0)
            };
            Grid.SetRow(bottomDesktopBarBorder, 2);

            Grid bottomBarGrid = new Grid();
            bottomBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel bottomTextStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
            lblDesktopStatusTitle = new TextBlock
            {
                Text = "READY FOR YOUR WINDOWS DESKTOP?",
                FontSize = 14.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            lblDesktopStatusSub = new TextBlock
            {
                Text = "Click the big green button on the right to attach this live wallpaper behind your desktop icons!",
                FontSize = 12.0,
                Foreground = new SolidColorBrush(Color.FromRgb(165, 220, 190)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            bottomTextStack.Children.Add(lblDesktopStatusTitle);
            bottomTextStack.Children.Add(lblDesktopStatusSub);
            Grid.SetColumn(bottomTextStack, 0);
            bottomBarGrid.Children.Add(bottomTextStack);

            btnBigDesktopToggle = CreateCardButton(
                "[+] ADD TO DESKTOP WALLPAPER",
                "1-Click Apply Behind Desktop Icons",
                null,
                Color.FromRgb(0, 235, 125),
                Color.FromRgb(0, 195, 98),
                Color.FromRgb(215, 50, 75),
                true,
                () => ToggleDesktopWallpaperMode());
            btnBigDesktopToggle.CardBorder.Width = 285;
            Grid.SetColumn(btnBigDesktopToggle.CardBorder, 1);
            bottomBarGrid.Children.Add(btnBigDesktopToggle.CardBorder);

            bottomDesktopBarBorder.Child = bottomBarGrid;
            rightZoneGrid.Children.Add(bottomDesktopBarBorder);

            bodyGrid.Children.Add(rightZoneGrid);
            rootGrid.Children.Add(bodyGrid);

            this.Content = rootGrid;
        }

        // =====================================================================
        // TAB 0: 6 WALLPAPER MODES + MP4 VIDEO UPLOAD
        // =====================================================================
        private UIElement BuildTab0_ModesAndVideo()
        {
            StackPanel sp = new StackPanel();
            AddSectionHeader(sp, "STEP 1: PICK YOUR WALLPAPER MODE", "Every card automatically wraps text so nothing is ever cut off:");

            string[] mTitles = new string[]
            {
                "[01] Matrix Number Waterfall",
                "[~] Interactive / Auto ASCII Fluid Sim",
                "[IMG] Custom Image / Photo Wallpaper",
                "[MP4] Video -> Colored ASCII Text",
                "[HD] Regular Full-Color Video Wallpaper",
                "[+] Hybrid: Video + Falling Matrix Rain"
            };
            string[] mSubs = new string[]
            {
                "Classic falling glowing numbers (supports custom background image & mouse wake)",
                "Glowing ASCII fluid currents — follow your mouse OR swirl automatically!",
                "Set any uploaded PNG/JPG photo or cyber artwork as your background wallpaper",
                "Turns any uploaded MP4 video into sharp colored text characters",
                "Plays your uploaded MP4 in crisp full-color HD video (non-ASCII)",
                "Combines your MP4 video with glowing Matrix rain falling on top"
            };
            string[] mBadges = new string[] { "CLASSIC", "FLUID SIM", "IMAGE BG", "TEXT VIDEO", "HD VIDEO", "HYBRID" };
            RenderMode[] modeEnumValues = new RenderMode[]
            {
                RenderMode.MatrixWaterfall,
                RenderMode.AsciiFluidSim,
                RenderMode.ImageWallpaper,
                RenderMode.VideoToAscii,
                RenderMode.RegularVideoWallpaper,
                RenderMode.VideoMatrixHybrid
            };

            for (int i = 0; i < 6; i++)
            {
                RenderMode targetMode = modeEnumValues[i];
                WpfCardOption card = CreateCardButton(
                    mTitles[i],
                    mSubs[i],
                    mBadges[i],
                    Color.FromRgb(0, 235, 125),
                    Color.FromRgb(20, 26, 36),
                    Color.FromRgb(16, 44, 34),
                    false,
                    () =>
                    {
                        config.Mode = targetMode;
                        if (targetMode == RenderMode.ImageWallpaper && config.ActiveBgImage == BackgroundImagePreset.None)
                        {
                            config.ActiveBgImage = BackgroundImagePreset.CyberpunkGrid;
                            renderer.InvalidateBackgroundCache();
                        }
                        if (targetMode == RenderMode.AsciiFluidSim)
                        {
                            renderer.TriggerBurstAt(0.5f, 0.5f);
                        }
                        SyncAllControlsWithConfig();
                    });
                card.CardBorder.Margin = new Thickness(0, 0, 0, 6);
                modeCards[i] = card;
                sp.Children.Add(card.CardBorder);
            }

            AddSectionHeader(sp, "UPLOAD MP4 VIDEO (FOR ASCII OR HD VIDEO)", "Pick any .MP4, .WMV, or .AVI video file from your computer:");

            WpfCardOption btnUploadMp4 = CreateCardButton(
                "[+] UPLOAD YOUR OWN MP4 VIDEO...",
                "Works in Colored ASCII Text mode, Regular HD Video mode & Hybrid mode",
                "BROWSE PC",
                Color.FromRgb(56, 189, 248),
                Color.FromRgb(18, 34, 52),
                Color.FromRgb(22, 48, 72),
                false,
                () => OnUploadMp4Clicked());
            btnUploadMp4.CardBorder.Margin = new Thickness(0, 0, 0, 6);
            sp.Children.Add(btnUploadMp4.CardBorder);

            WpfCardOption btnLoadDemo = CreateCardButton(
                "[>] Load Built-in Demo Video (sample_demo.mp4)",
                "Test video modes immediately with the included demo clip",
                null,
                Color.FromRgb(0, 235, 125),
                Color.FromRgb(20, 26, 36),
                Color.FromRgb(16, 44, 34),
                false,
                () =>
                {
                    LoadDefaultDemoVideoIfAvailable();
                    if (config.Mode != RenderMode.VideoToAscii &&
                        config.Mode != RenderMode.RegularVideoWallpaper &&
                        config.Mode != RenderMode.VideoMatrixHybrid)
                    {
                        config.Mode = RenderMode.VideoToAscii;
                    }
                    lblLoadedVideoStatus.Text = "✔ Loaded: sample_demo.mp4 (Playing)";
                    SyncAllControlsWithConfig();
                });
            btnLoadDemo.CardBorder.Margin = new Thickness(0, 0, 0, 6);
            sp.Children.Add(btnLoadDemo.CardBorder);

            lblLoadedVideoStatus = new TextBlock
            {
                Text = "✔ Loaded: sample_demo.mp4 (Built-in Demo Ready)",
                FontSize = 12.0,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 235, 130)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 2, 4, 6)
            };
            sp.Children.Add(lblLoadedVideoStatus);

            UniformGrid videoChecks = new UniformGrid { Columns = 2, Margin = new Thickness(4, 0, 4, 4) };
            chkLoopVideo = CreateStyledCheckBox("Loop Video Infinitely", config.LoopVideo, val => config.LoopVideo = val);
            chkMuteAudio = CreateStyledCheckBox("Mute Video Audio", config.MuteAudio, val =>
            {
                config.MuteAudio = val;
                renderer.UpdateAudioMute();
            });
            videoChecks.Children.Add(chkLoopVideo);
            videoChecks.Children.Add(chkMuteAudio);
            sp.Children.Add(videoChecks);

            return sp;
        }

        // =====================================================================
        // TAB 1: SET IMAGES AS BACKGROUNDS (Custom Upload + Built-In + Blend Modes)
        // =====================================================================
        private UIElement BuildTab1_ImageBackgrounds()
        {
            StackPanel sp = new StackPanel();
            AddSectionHeader(sp, "SET ANY IMAGE AS YOUR WALLPAPER BACKGROUND", "Use your own photo/wallpaper behind the Matrix rain, or as a pure HD or ASCII wallpaper!");

            WpfCardOption btnUploadBgImg = CreateCardButton(
                "[+] UPLOAD YOUR OWN BACKGROUND IMAGE (PNG / JPG)...",
                "Pick any photo, anime wallpaper, or artwork from your PC to set as the background!",
                "UPLOAD PHOTO",
                Color.FromRgb(56, 189, 248),
                Color.FromRgb(18, 36, 56),
                Color.FromRgb(24, 54, 82),
                false,
                () => OnUploadCustomBgImageClicked());
            btnUploadBgImg.CardBorder.Margin = new Thickness(0, 0, 0, 6);
            sp.Children.Add(btnUploadBgImg.CardBorder);

            lblCustomBgStatus = new TextBlock
            {
                Text = "Tip: You can also pick any built-in HD background preset below!",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 195, 235)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 0, 4, 8)
            };
            sp.Children.Add(lblCustomBgStatus);

            string[] bgTitles = new string[]
            {
                "[OFF] Solid Dark Background (No Image)",
                "[BG 1] Cyberpunk Neon Grid Horizon",
                "[BG 2] Synthwave Sunset Mountains",
                "[BG 3] Deep Space Emerald Nebula",
                "[+] Custom Uploaded Image / Photo"
            };
            string[] bgSubs = new string[]
            {
                "Uses your solid background color without an image",
                "Built-in glowing neon cyber grid & retro sun horizon",
                "Built-in synthwave mountain skyline at dusk",
                "Built-in deep space starfield & glowing emerald nebula",
                "Uses the custom PNG/JPG photo you uploaded from your PC"
            };

            for (int i = 0; i < 5; i++)
            {
                int captured = i;
                BitmapSource thumbSrc = GetWpfBitmapSource(renderer.GetBackgroundBitmap((BackgroundImagePreset)i));
                WpfCardOption card = CreateCardButtonWithThumb(
                    bgTitles[i],
                    bgSubs[i],
                    thumbSrc,
                    Color.FromRgb(56, 189, 248),
                    Color.FromRgb(20, 26, 36),
                    Color.FromRgb(18, 40, 58),
                    () =>
                    {
                        if (captured == (int)BackgroundImagePreset.CustomUploadedImage && string.IsNullOrEmpty(config.CustomBgImagePath))
                        {
                            OnUploadCustomBgImageClicked();
                            return;
                        }
                        config.ActiveBgImage = (BackgroundImagePreset)captured;
                        renderer.InvalidateBackgroundCache();
                        SyncAllControlsWithConfig();
                    });
                card.CardBorder.Margin = new Thickness(0, 0, 0, 6);
                bgImageCards[i] = card;
                sp.Children.Add(card.CardBorder);
            }

            AddSectionHeader(sp, "HOW SHOULD THE BACKGROUND IMAGE DISPLAY?", "Choose whether Matrix rain falls over your image, or show pure HD / ASCII:");

            string[] styleTitles = new string[]
            {
                "1. Image Behind Matrix Rain & Effects (Recommended)",
                "2. Pure Crisp HD Image Wallpaper Only",
                "3. Convert Background Image -> Colored ASCII Text!"
            };
            string[] styleSubs = new string[]
            {
                "Your image sits cleanly in the background while Matrix numbers / Soyjaks animate on top",
                "Shows your full-resolution background image clearly without Matrix rain covering it",
                "Transforms your background photo into thousands of glowing colored text characters!"
            };

            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                WpfCardOption sc = CreateCardButton(
                    styleTitles[i],
                    styleSubs[i],
                    null,
                    Color.FromRgb(56, 189, 248),
                    Color.FromRgb(20, 26, 36),
                    Color.FromRgb(18, 40, 58),
                    false,
                    () =>
                    {
                        config.BgImageStyle = (BgImageRenderStyle)captured;
                        if (captured == (int)BgImageRenderStyle.PureHdImageOnly)
                        {
                            config.Mode = RenderMode.ImageWallpaper;
                        }
                        SyncAllControlsWithConfig();
                    });
                sc.CardBorder.Margin = new Thickness(0, 0, 0, 6);
                bgStyleCards[i] = sc;
                sp.Children.Add(sc.CardBorder);
            }

            sldBgOpacity = AddLabeledSlider(sp, "Background Image Brightness / Visibility", 15, 100, (int)(config.BgImageOpacity * 100), "%", out lblBgOpacityVal, val =>
            {
                config.BgImageOpacity = (float)(val / 100.0);
                renderer.InvalidateBackgroundCache();
            });

            return sp;
        }

        // =====================================================================
        // TAB 2: SOYJAK MEME DECK + CUSTOM MEME UPLOAD
        // =====================================================================
        private UIElement BuildTab2_SoyjakMemes()
        {
            StackPanel sp = new StackPanel();
            AddSectionHeader(sp, "SOYJAK MEME OVERLAY DECK", "Put pointing & shocked Soyjaks on top of any Matrix, Video, or Image wallpaper!");

            string[] sjTitles = new string[]
            {
                "[OFF] No Soyjak Overlay",
                "★ Two Soyjaks Pointing!",
                "★ Shocked Glasses Soyjak",
                "★ Single Pointer Soyjak",
                "◆ Diamond Gem Soyjak",
                "[+] Upload Your Own Soyjak / Meme Image..."
            };
            string[] sjSubs = new string[]
            {
                "Show only the wallpaper background",
                "Classic viral meme — two Soyjaks pointing at your screen!",
                "Wide-eyed shocked Wojak/Soyjak reaction face",
                "Excited Soyjak pointing directly at your center content",
                "Glowing cyan diamond gem Soyjak character",
                "Pick any PNG, JPG, or transparent meme cutout from your PC!"
            };

            for (int i = 0; i < 6; i++)
            {
                int captured = i;
                BitmapSource thumb = GetWpfBitmapSource(renderer.GetSoyjakBitmap((SoyjakPreset)i));
                WpfCardOption card = CreateCardButtonWithThumb(
                    sjTitles[i],
                    sjSubs[i],
                    thumb,
                    Color.FromRgb(255, 190, 40),
                    Color.FromRgb(20, 26, 36),
                    Color.FromRgb(46, 36, 16),
                    () =>
                    {
                        if (captured == (int)SoyjakPreset.CustomUploadedImage)
                        {
                            OnUploadCustomSoyjakClicked();
                        }
                        else
                        {
                            config.ActiveSoyjak = (SoyjakPreset)captured;
                            renderer.InvalidateSoyjakCache();
                            SyncAllControlsWithConfig();
                        }
                    });
                card.CardBorder.Margin = new Thickness(0, 0, 0, 6);
                soyjakCards[i] = card;
                sp.Children.Add(card.CardBorder);
            }

            AddSectionHeader(sp, "SOYJAK DISPLAY STYLE", "All options visible right here — no hidden menus:");

            UniformGrid styleGrid = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 8) };
            string[] stNames = new string[] { "Real Image Cutout", "ASCII Text Meme", "Cutout + Matrix" };
            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                WpfCardOption sc = CreateCardButton(
                    stNames[i],
                    null,
                    null,
                    Color.FromRgb(255, 190, 40),
                    Color.FromRgb(20, 26, 36),
                    Color.FromRgb(46, 36, 16),
                    false,
                    () =>
                    {
                        config.SoyjakStyle = (SoyjakRenderStyle)captured;
                        renderer.InvalidateSoyjakCache();
                        SyncAllControlsWithConfig();
                    });
                sc.CardBorder.Margin = new Thickness(2);
                soyjakStyleCards[i] = sc;
                styleGrid.Children.Add(sc.CardBorder);
            }
            sp.Children.Add(styleGrid);

            sldSoyjakSize = AddLabeledSlider(sp, "Soyjak Meme Size on Screen", 35, 115, (int)(config.SoyjakScale * 100), "%", out lblSoyjakSizeVal, val =>
            {
                config.SoyjakScale = (float)(val / 100.0);
                renderer.InvalidateSoyjakCache();
            });

            chkSoyjakBounce = CreateStyledCheckBox("Animated Excited Bounce Effect", config.SoyjakBounceReactive, val => config.SoyjakBounceReactive = val);
            chkSoyjakBounce.Margin = new Thickness(4, 4, 4, 4);
            sp.Children.Add(chkSoyjakBounce);

            return sp;
        }

        // =====================================================================
        // TAB 3: COLORS & TEXT (Zero Dropdown Menus — All Options Always Visible!)
        // =====================================================================
        private UIElement BuildTab3_ColorsAndText()
        {
            StackPanel sp = new StackPanel();
            AddSectionHeader(sp, "COLOR THEME PRESETS", "Click any color theme below (or pick custom RGB colors):");

            UniformGrid themeGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 4) };
            string[] themeNames = new string[]
            {
                "● Matrix Green",
                "● Cyberpunk Neon",
                "● Ocean & Fire",
                "● Crimson Hacker",
                "● Golden Amber",
                "● Rainbow Wave",
                "★ Custom User Colors (Use Color Pickers Below)"
            };

            for (int i = 0; i < 6; i++)
            {
                int captured = i;
                WpfCardOption tc = CreateCardButton(
                    themeNames[i],
                    null,
                    null,
                    Color.FromRgb(244, 114, 182),
                    Color.FromRgb(20, 26, 36),
                    Color.FromRgb(44, 24, 42),
                    false,
                    () =>
                    {
                        config.Theme = (ColorTheme)captured;
                        SyncAllControlsWithConfig();
                    });
                tc.CardBorder.Margin = new Thickness(2);
                themeCards[i] = tc;
                themeGrid.Children.Add(tc.CardBorder);
            }
            sp.Children.Add(themeGrid);

            WpfCardOption customTc = CreateCardButton(
                themeNames[6],
                null,
                null,
                Color.FromRgb(244, 114, 182),
                Color.FromRgb(20, 26, 36),
                Color.FromRgb(44, 24, 42),
                false,
                () =>
                {
                    config.Theme = ColorTheme.CustomColors;
                    SyncAllControlsWithConfig();
                });
            customTc.CardBorder.Margin = new Thickness(2, 0, 2, 8);
            themeCards[6] = customTc;
            sp.Children.Add(customTc.CardBorder);

            AddSectionHeader(sp, "CUSTOM COLOR SWATCHES", "Click any color button to open the RGB Color Picker:");

            UniformGrid swatchGrid = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 10) };
            btnPickPrimary = CreateColorPickerWpfButton("Rain / Text Color", config.PrimaryColor, c =>
            {
                config.PrimaryColor = c;
                config.Theme = ColorTheme.CustomColors;
                SyncAllControlsWithConfig();
            });
            btnPickHead = CreateColorPickerWpfButton("Glow Tip Color", config.HeadColor, c =>
            {
                config.HeadColor = c;
                config.Theme = ColorTheme.CustomColors;
                SyncAllControlsWithConfig();
            });
            btnPickBg = CreateColorPickerWpfButton("Background Color", config.BackgroundColor, c =>
            {
                config.BackgroundColor = c;
                renderer.InvalidateBackgroundCache();
                SyncAllControlsWithConfig();
            });
            swatchGrid.Children.Add(btnPickPrimary);
            swatchGrid.Children.Add(btnPickHead);
            swatchGrid.Children.Add(btnPickBg);
            sp.Children.Add(swatchGrid);

            // ALWAYS-VISIBLE CHARACTER SET BUTTON GRID (Replaces old disappearing dropdown menu!)
            AddSectionHeader(sp, "FALLING CHARACTERS STYLE (NO HIDDEN MENUS!)", "Click any character style below — text never disappears:");

            UniformGrid charGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 8) };
            string[] charPresets = new string[]
            {
                "Numbers Only (0-9)",
                "Binary (0-1)",
                "Hexadecimal (0-9, A-F)",
                "Matrix Classic (Numbers + Symbols)",
                "Fluid Density Ramp (.:-=+*#%@0189)",
                "Custom"
            };
            string[] charLabels = new string[]
            {
                "0-9 Numbers Only",
                "0-1 Pure Binary",
                "0-9 A-F Hexadecimal",
                "Matrix Classic Symbols",
                "ASCII Density Ramp",
                "Custom Text Below"
            };

            for (int i = 0; i < 6; i++)
            {
                int captured = i;
                string presetVal = charPresets[i];
                WpfCardOption cc = CreateCardButton(
                    charLabels[i],
                    null,
                    null,
                    Color.FromRgb(0, 235, 125),
                    Color.FromRgb(20, 26, 36),
                    Color.FromRgb(16, 44, 34),
                    false,
                    () =>
                    {
                        config.CharSetPreset = presetVal;
                        SyncAllControlsWithConfig();
                    });
                cc.CardBorder.Margin = new Thickness(2);
                charPresetCards[i] = cc;
                charGrid.Children.Add(cc.CardBorder);
            }
            sp.Children.Add(charGrid);

            TextBlock lblCustomBox = new TextBlock
            {
                Text = "Custom Characters / Custom Word (Type anything here):",
                FontSize = 12.0,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(210, 225, 245)),
                Margin = new Thickness(4, 2, 4, 4)
            };
            sp.Children.Add(lblCustomBox);

            txtCustomChars = new TextBox
            {
                Text = config.CustomCharSet,
                Background = new SolidColorBrush(Color.FromRgb(20, 28, 40)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 140)),
                CaretBrush = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1.5),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14.0,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(2, 0, 2, 10)
            };
            txtCustomChars.TextChanged += (s, e) =>
            {
                config.CustomCharSet = txtCustomChars.Text;
                if (!string.IsNullOrEmpty(txtCustomChars.Text))
                {
                    config.CharSetPreset = "Custom";
                    SyncAllControlsWithConfig();
                }
            };
            sp.Children.Add(txtCustomChars);

            // ALWAYS-VISIBLE VIDEO/IMAGE ASCII COLOR STYLE GRID (Replaces old dropdown!)
            AddSectionHeader(sp, "ASCII VIDEO & IMAGE COLOR MODE", "Choose how ASCII video & ASCII images are colored:");

            UniformGrid vidColGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 6) };
            string[] vcLabels = new string[]
            {
                "True Original RGB Color",
                "Match Active Color Theme",
                "Custom Single Color Tint",
                "Animated Rainbow Wave"
            };
            for (int i = 0; i < 4; i++)
            {
                int captured = i;
                WpfCardOption vc = CreateCardButton(
                    vcLabels[i],
                    null,
                    null,
                    Color.FromRgb(56, 189, 248),
                    Color.FromRgb(20, 26, 36),
                    Color.FromRgb(18, 40, 58),
                    false,
                    () =>
                    {
                        config.VidColorMode = (VideoColorMode)captured;
                        SyncAllControlsWithConfig();
                    });
                vc.CardBorder.Margin = new Thickness(2);
                vidColorCards[i] = vc;
                vidColGrid.Children.Add(vc.CardBorder);
            }
            sp.Children.Add(vidColGrid);

            return sp;
        }

        // =====================================================================
        // TAB 4: TURN OFF MOUSE STUFF + COOL EXTRA FEATURES + SLIDERS
        // =====================================================================
        private UIElement BuildTab4_MouseAndCoolFx()
        {
            StackPanel sp = new StackPanel();
            AddSectionHeader(sp, "MOUSE INTERACTION CONTROL (TURN OFF MOUSE ANYTIME!)", "Choose whether the wallpaper reacts to your mouse, stays completely untouched, or swirls on its own:");

            string[] mmTitles = new string[]
            {
                "🚫 Turn OFF All Mouse Effects (Disabled)",
                "◉ Interactive Mouse Follow (ON)",
                "◎ Auto-Pilot Ghost Swirl (No Mouse Needed!)"
            };
            string[] mmSubs = new string[]
            {
                "Moving your mouse will NOT affect Matrix rain or fluid — pure distraction-free wallpaper!",
                "Matrix streams and glowing ASCII fluid swirl and follow underneath your mouse cursor",
                "Automatically swirls glowing fluid and Matrix wakes across the screen by itself!"
            };
            MouseEffectMode[] mmModes = new MouseEffectMode[]
            {
                MouseEffectMode.Off,
                MouseEffectMode.FollowCursor,
                MouseEffectMode.AutoGhostSwirl
            };

            for (int i = 0; i < 3; i++)
            {
                MouseEffectMode targetM = mmModes[i];
                WpfCardOption mc = CreateCardButton(
                    mmTitles[i],
                    mmSubs[i],
                    null,
                    Color.FromRgb(192, 132, 252),
                    Color.FromRgb(20, 26, 36),
                    Color.FromRgb(38, 26, 58),
                    false,
                    () =>
                    {
                        renderer.SetMouseMode(targetM);
                        SyncAllControlsWithConfig();
                    });
                mc.CardBorder.Margin = new Thickness(0, 0, 0, 6);
                mouseModeCards[i] = mc;
                sp.Children.Add(mc.CardBorder);
            }

            AddSectionHeader(sp, "COOL EXTRA WALLPAPER FEATURES", "1-Click overlays and retro visual effects:");

            UniformGrid coolFxGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 6) };

            cardToggleClock = CreateCardButton(
                "◷ Live Digital Clock",
                "Cyberpunk time & date",
                null,
                Color.FromRgb(0, 235, 125),
                Color.FromRgb(20, 26, 36),
                Color.FromRgb(16, 44, 34),
                false,
                () =>
                {
                    config.ShowClockOverlay = !config.ShowClockOverlay;
                    SyncAllControlsWithConfig();
                });
            cardToggleClock.CardBorder.Margin = new Thickness(2);
            coolFxGrid.Children.Add(cardToggleClock.CardBorder);

            cardToggleBanner = CreateCardButton(
                "★ Custom Text Banner",
                "Show headline on screen",
                null,
                Color.FromRgb(56, 189, 248),
                Color.FromRgb(20, 26, 36),
                Color.FromRgb(18, 40, 58),
                false,
                () =>
                {
                    config.ShowCustomBanner = !config.ShowCustomBanner;
                    SyncAllControlsWithConfig();
                });
            cardToggleBanner.CardBorder.Margin = new Thickness(2);
            coolFxGrid.Children.Add(cardToggleBanner.CardBorder);

            cardToggleCrt = CreateCardButton(
                "▤ Retro CRT Scanlines",
                "Classic monitor lines",
                null,
                Color.FromRgb(255, 190, 40),
                Color.FromRgb(20, 26, 36),
                Color.FromRgb(46, 36, 16),
                false,
                () =>
                {
                    config.CrtScanlines = !config.CrtScanlines;
                    SyncAllControlsWithConfig();
                });
            cardToggleCrt.CardBorder.Margin = new Thickness(2);
            coolFxGrid.Children.Add(cardToggleCrt.CardBorder);

            cardToggleReverseRain = CreateCardButton(
                "▲ Anti-Gravity Upward Rain",
                "Matrix rises upward!",
                null,
                Color.FromRgb(244, 114, 182),
                Color.FromRgb(20, 26, 36),
                Color.FromRgb(44, 24, 42),
                false,
                () =>
                {
                    config.ReverseRainDirection = !config.ReverseRainDirection;
                    SyncAllControlsWithConfig();
                });
            cardToggleReverseRain.CardBorder.Margin = new Thickness(2);
            coolFxGrid.Children.Add(cardToggleReverseRain.CardBorder);

            sp.Children.Add(coolFxGrid);

            TextBlock lblBannerInput = new TextBlock
            {
                Text = "Custom Floating Banner Text (Shown when 'Custom Text Banner' is ON):",
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 218, 240)),
                Margin = new Thickness(4, 2, 4, 4)
            };
            sp.Children.Add(lblBannerInput);

            txtBannerInput = new TextBox
            {
                Text = config.CustomBannerText,
                Background = new SolidColorBrush(Color.FromRgb(20, 28, 40)),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1.5),
                FontSize = 13.0,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(2, 0, 2, 10)
            };
            txtBannerInput.TextChanged += (s, e) =>
            {
                config.CustomBannerText = txtBannerInput.Text;
            };
            sp.Children.Add(txtBannerInput);

            AddSectionHeader(sp, "SPEED, TEXT SIZE & FLUID SLIDERS", "Adjust animation speed, crisp 1:1 text size, and brush size:");

            sldSpeed = AddLabeledSlider(sp, "Animation Speed", 10, 300, (int)(config.Speed * 100), "%", out lblSpeedVal, val => config.Speed = (float)(val / 100.0));
            sldFontSize = AddLabeledSlider(sp, "Text & Number Size (Crisp 1:1)", 10, 32, config.FontSize, " px", out lblFontSizeVal, val => config.FontSize = (int)val);
            sldMouseRadius = AddLabeledSlider(sp, "Mouse Wake / Brush Size", 5, 35, (int)(config.MouseRadius * 100), "%", out lblMouseRadiusVal, val => config.MouseRadius = (float)(val / 100.0));
            sldFluidFade = AddLabeledSlider(sp, "Fluid Trail Persistence", 82, 99, (int)(config.FluidViscosity * 100), "%", out lblFluidFadeVal, val => config.FluidViscosity = (float)(val / 100.0));

            return sp;
        }

        // =====================================================================
        // TAB 5: GITHUB CLOUD UPDATER & LAPTOP ECO PERFORMANCE
        // =====================================================================
        private UIElement BuildTab5_RemoteUpdater()
        {
            StackPanel sp = new StackPanel();
            AddSectionHeader(sp, "🌍 GITHUB CLOUD UPDATES (BRASSYFROGY/MATRIX-WALLPAPER)", "Check and download the latest version in 1 click over HTTPS TLS 1.2 — works on ANY Wi-Fi worldwide!");

            // Live Updater Status Banner
            Border statusCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 34, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 235, 165)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };
            StackPanel statusStack = new StackPanel();

            string selfSha = AppUpdater.SelfSha();
            string shortSha = string.IsNullOrEmpty(selfSha) ? "local" : (selfSha.Length > 12 ? selfSha.Substring(0, 12) : selfSha);

            lblUpdateStatusBanner = new TextBlock
            {
                Text = justUpdatedFromUpdater
                    ? "🎉 Updated Successfully to the Latest Release!"
                    : "✔ Matrix Wallpaper Engine — Connected to GitHub Channel",
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 175)),
                TextWrapping = TextWrapping.Wrap
            };
            lblUpdateDetail = new TextBlock
            {
                Text = string.Format("Repository: {0} ({1}) • Build: {2}", AppUpdater.Repo, AppUpdater.Branch, shortSha),
                FontSize = 11.0,
                Foreground = new SolidColorBrush(Color.FromRgb(165, 215, 205)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            statusStack.Children.Add(lblUpdateStatusBanner);
            statusStack.Children.Add(lblUpdateDetail);
            statusCard.Child = statusStack;
            sp.Children.Add(statusCard);

            // Big 1-Click Update Action Button
            btnCheckOrInstallUpdate = CreateCardButton(
                "⬇ CHECK FOR UPDATES FROM GITHUB",
                "Queries BrassyFrogy/matrix-wallpaper for the latest release (safe SHA-256 verification)",
                "GITHUB HTTPS",
                Color.FromRgb(0, 245, 185),
                Color.FromRgb(12, 46, 38),
                Color.FromRgb(18, 68, 56),
                false,
                () =>
                {
                    if (latestAvailableUpdate != null)
                        DownloadAndInstallUpdateClicked();
                    else
                        CheckForUpdatesClicked();
                });
            btnCheckOrInstallUpdate.CardBorder.Margin = new Thickness(0, 0, 0, 8);
            sp.Children.Add(btnCheckOrInstallUpdate.CardBorder);

            // Auto-check on startup checkbox
            chkAutoCheckUpdates = CreateStyledCheckBox(
                "Check for updates automatically in the background on startup",
                config.AutoCheckUpdates,
                val =>
                {
                    config.AutoCheckUpdates = val;
                    SettingsStore.SaveIfChanged(config);
                });
            chkAutoCheckUpdates.Margin = new Thickness(2, 0, 2, 8);
            sp.Children.Add(chkAutoCheckUpdates);

            // Release Notes Card (hidden until update info loaded)
            updateNotesCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 24, 38)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = Visibility.Collapsed
            };
            StackPanel notesStack = new StackPanel();
            TextBlock lblNotesTitle = new TextBlock
            {
                Text = "📜 WHAT'S NEW IN THIS VERSION:",
                FontSize = 11.2,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                Margin = new Thickness(0, 0, 0, 3)
            };
            lblUpdateNotes = new TextBlock
            {
                Text = "",
                FontSize = 11.5,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            notesStack.Children.Add(lblNotesTitle);
            notesStack.Children.Add(lblUpdateNotes);
            updateNotesCard.Child = notesStack;
            sp.Children.Add(updateNotesCard);

            // 2. LAPTOP PERFORMANCE & ECO MODE (OPTIMIZED FOR BRADLEY'S ACER CELERON)
            AddSectionHeader(sp, "⚡ LAPTOP PERFORMANCE & ECO MODE (ACER / CELERON BOOST)", "Designed specifically to run cool, lightweight, and stutter-free on 10-year-old dual-core laptops:");

            bool isEco = config.TargetFps <= 30;
            btnEcoModeToggle = CreateCardButton(
                isEco ? "🌱 ECO MODE: ACTIVE (30 FPS)" : "⚡ MAXIMUM SMOOTHNESS: ACTIVE (60 FPS)",
                isEco ? "Low CPU & battery saver for older Acer/Celeron laptops (Click to toggle 60 FPS)" : "Full 60 FPS buttery smooth desktop rendering (Click to switch to 30 FPS Eco Mode)",
                isEco ? "30 FPS ECO" : "60 FPS TURBO",
                isEco ? Color.FromRgb(0, 245, 160) : Color.FromRgb(255, 190, 40),
                isEco ? Color.FromRgb(14, 44, 34) : Color.FromRgb(38, 30, 16),
                isEco ? Color.FromRgb(20, 60, 46) : Color.FromRgb(54, 42, 22),
                isEco,
                () =>
                {
                    if (config.TargetFps <= 30)
                        config.TargetFps = 60;
                    else
                        config.TargetFps = 30;

                    renderTimer.Interval = TimeSpan.FromMilliseconds(config.TargetFps <= 30 ? 33 : 16);
                    SettingsStore.SaveIfChanged(config);
                    SyncAllControlsWithConfig();
                });
            btnEcoModeToggle.CardBorder.Margin = new Thickness(0, 0, 0, 8);
            sp.Children.Add(btnEcoModeToggle.CardBorder);

            chkPauseWhenMinimized = CreateStyledCheckBox(
                "Pause rendering calculations when this window is minimized & detached",
                config.PauseWhenMinimized,
                val =>
                {
                    config.PauseWhenMinimized = val;
                    SettingsStore.SaveIfChanged(config);
                });
            chkPauseWhenMinimized.Margin = new Thickness(2, 0, 2, 8);
            sp.Children.Add(chkPauseWhenMinimized);

            Border perfInfoCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 20, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 52, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            StackPanel perfStack = new StackPanel();
            TextBlock p1 = new TextBlock
            {
                Text = "• 100% Native GDI32 Direct Blitting (No slow Bitmap.LockBits or memory leaks)",
                FontSize = 10.8,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 185, 210)),
                Margin = new Thickness(0, 0, 0, 2)
            };
            TextBlock p2 = new TextBlock
            {
                Text = "• Zero per-frame GC allocations — preallocated contiguous 32-bit pixel buffers",
                FontSize = 10.8,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 185, 210)),
                Margin = new Thickness(0, 0, 0, 2)
            };
            TextBlock p3 = new TextBlock
            {
                Text = "• Background updater uses tiny ~200B JSON manifest to prevent Wi-Fi bandwidth usage",
                FontSize = 10.8,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 185, 210))
            };
            perfStack.Children.Add(p1);
            perfStack.Children.Add(p2);
            perfStack.Children.Add(p3);
            perfInfoCard.Child = perfStack;
            sp.Children.Add(perfInfoCard);

            // 3. DIAGNOSTICS & OPEN APPDATA / GITHUB
            AddSectionHeader(sp, "📁 DIAGNOSTICS & LOGS", "View rolling logs or open the GitHub releases page:");

            Grid diagRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            diagRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            diagRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            diagRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Button btnOpenLog = CreateFlatWpfButton("📋 VIEW LOG FILE", Color.FromRgb(42, 56, 76), Color.FromRgb(140, 210, 255), () =>
            {
                try
                {
                    string p = System.IO.Path.Combine(AppUpdater.DataDir, "app.log");
                    if (!System.IO.File.Exists(p))
                    {
                        System.IO.File.WriteAllText(p, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  Log initialized.\r\n");
                    }
                    Process.Start("notepad.exe", p);
                }
                catch { }
            });
            Grid.SetColumn(btnOpenLog, 0);
            diagRow.Children.Add(btnOpenLog);

            Button btnOpenRepo = CreateFlatWpfButton("🌐 GITHUB REPO", Color.FromRgb(36, 46, 68), Color.FromRgb(0, 235, 170), () =>
            {
                try { Process.Start(AppUpdater.RepoUrl); } catch { }
            });
            Grid.SetColumn(btnOpenRepo, 2);
            diagRow.Children.Add(btnOpenRepo);

            sp.Children.Add(diagRow);
            return sp;
        }

        private void CheckForUpdatesClicked()
        {
            if (isCheckingOrDownloading) return;
            isCheckingOrDownloading = true;

            lblUpdateStatusBanner.Text = "⏳ Checking GitHub for updates...";
            lblUpdateStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(120, 215, 255));
            btnCheckOrInstallUpdate.TitleBlock.Text = "⏳ CHECKING GITHUB...";
            btnCheckOrInstallUpdate.SubBlock.Text = "Fetching version manifest from BrassyFrogy/matrix-wallpaper...";

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                string err;
                UpdateInfo info = AppUpdater.FetchLatest(out err);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    isCheckingOrDownloading = false;
                    if (info == null)
                    {
                        lblUpdateStatusBanner.Text = "⚠ " + (string.IsNullOrEmpty(err) ? "Update check failed." : err);
                        lblUpdateStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(255, 160, 90));
                        btnCheckOrInstallUpdate.TitleBlock.Text = "⬇ CHECK FOR UPDATES AGAIN";
                        btnCheckOrInstallUpdate.SubBlock.Text = "Couldn't reach GitHub. Click to retry.";
                        return;
                    }

                    if (AppUpdater.IsNewer(info))
                    {
                        latestAvailableUpdate = info;
                        double mb = info.Size / (1024.0 * 1024.0);
                        lblUpdateStatusBanner.Text = string.Format("🚀 New Update Available: v{0}! ({1:F2} MB)", info.Version, mb);
                        lblUpdateStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 175));

                        btnCheckOrInstallUpdate.TitleBlock.Text = string.Format("🚀 DOWNLOAD & INSTALL UPDATE v{0} NOW", info.Version);
                        btnCheckOrInstallUpdate.SubBlock.Text = string.IsNullOrEmpty(info.Notes) ? "Click to download, verify checksum, and restart" : ("Notes: " + info.Notes);
                        btnCheckOrInstallUpdate.CardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 170));
                        btnCheckOrInstallUpdate.CardBorder.Background = new SolidColorBrush(Color.FromRgb(16, 48, 38));

                        if (!string.IsNullOrEmpty(info.Notes) && updateNotesCard != null)
                        {
                            lblUpdateNotes.Text = info.Notes;
                            updateNotesCard.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        latestAvailableUpdate = null;
                        lblUpdateStatusBanner.Text = string.Format("✔ You're up to date! (v{0} is running)", string.IsNullOrEmpty(info.Version) ? "latest" : info.Version);
                        lblUpdateStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 165));
                        btnCheckOrInstallUpdate.TitleBlock.Text = "✔ UP TO DATE — CHECK AGAIN";
                        btnCheckOrInstallUpdate.SubBlock.Text = "Your files match the latest release on GitHub.";
                        if (!string.IsNullOrEmpty(info.Notes) && updateNotesCard != null)
                        {
                            lblUpdateNotes.Text = "Latest Release Notes (v" + info.Version + "):\n" + info.Notes;
                            updateNotesCard.Visibility = Visibility.Visible;
                        }
                    }
                }));
            });
        }

        private void DownloadAndInstallUpdateClicked()
        {
            if (latestAvailableUpdate == null || isCheckingOrDownloading) return;
            isCheckingOrDownloading = true;

            lblUpdateStatusBanner.Text = "⏳ Downloading update from GitHub... 0%";
            lblUpdateStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(120, 220, 255));
            btnCheckOrInstallUpdate.TitleBlock.Text = "⏳ DOWNLOADING UPDATE...";
            btnCheckOrInstallUpdate.SubBlock.Text = "Downloading and verifying SHA-256 checksum...";

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                string err;
                bool success = AppUpdater.DownloadAndInstall(latestAvailableUpdate, pct =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        lblUpdateStatusBanner.Text = string.Format("⏳ Downloading update from GitHub... {0}%", pct);
                        btnCheckOrInstallUpdate.SubBlock.Text = string.Format("Downloading payload: {0}% complete", pct);
                    }));
                }, out err);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    isCheckingOrDownloading = false;
                    if (success)
                    {
                        lblUpdateStatusBanner.Text = "⚡ Update verified and installed! Restarting...";
                        lblUpdateStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 165));
                        btnCheckOrInstallUpdate.TitleBlock.Text = "⚡ RESTARTING APPLICATION...";
                        btnCheckOrInstallUpdate.SubBlock.Text = "Launching into the new update now!";

                        System.Threading.ThreadPool.QueueUserWorkItem(__ =>
                        {
                            System.Threading.Thread.Sleep(400);
                            AppUpdater.RestartSelf();
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Application.Current.Shutdown();
                            }));
                        });
                    }
                    else
                    {
                        lblUpdateStatusBanner.Text = "⚠ Update installation failed: " + err;
                        lblUpdateStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(255, 140, 120));
                        btnCheckOrInstallUpdate.TitleBlock.Text = "⚠ RETRY UPDATE";
                        btnCheckOrInstallUpdate.SubBlock.Text = "Click to retry downloading the update.";
                    }
                }));
            });
        }

        private void SilentUpdateCheck()
        {
            try
            {
                string err;
                UpdateInfo info = AppUpdater.FetchLatest(out err);
                if (info != null && AppUpdater.IsNewer(info))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        latestAvailableUpdate = info;
                        if (lblUpdateStatusBanner != null)
                        {
                            lblUpdateStatusBanner.Text = string.Format("🚀 New Update Available: v{0}! Click below to install.", info.Version);
                            lblUpdateStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 175));
                        }
                        if (btnCheckOrInstallUpdate != null)
                        {
                            btnCheckOrInstallUpdate.TitleBlock.Text = string.Format("🚀 DOWNLOAD & INSTALL UPDATE v{0} NOW", info.Version);
                            btnCheckOrInstallUpdate.SubBlock.Text = string.IsNullOrEmpty(info.Notes) ? "Click to install update and restart" : ("Update notes: " + info.Notes);
                            btnCheckOrInstallUpdate.CardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 170));
                            btnCheckOrInstallUpdate.CardBorder.Background = new SolidColorBrush(Color.FromRgb(16, 48, 38));
                        }
                        if (!string.IsNullOrEmpty(info.Notes) && updateNotesCard != null)
                        {
                            lblUpdateNotes.Text = info.Notes;
                            updateNotesCard.Visibility = Visibility.Visible;
                        }
                    }));
                }
            }
            catch { }
        }

        // =====================================================================
        // NATIVE WPF HIGHLIGHT-BOX TUTORIAL OVERLAY
        // =====================================================================
        private void BuildTutorialOverlayUI()
        {
            tutorialOverlayGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(145, 6, 10, 18)),
                Visibility = Visibility.Collapsed
            };

            tutorialCalloutCard = new Border
            {
                MaxWidth = 500,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(14, 22, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 230, 0)),
                BorderThickness = new Thickness(3.0),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(22, 18, 22, 18),
                Margin = new Thickness(24),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(0, 245, 160),
                    BlurRadius = 24,
                    ShadowDepth = 0,
                    Opacity = 0.65
                }
            };

            StackPanel cardStack = new StackPanel();

            // Top row: Step Badge + Close button
            Grid topRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border badgePill = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 230, 0)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            tutStepBadgeText = new TextBlock
            {
                Text = "STEP 1 OF 4 • INTERACTIVE GUIDE",
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(10, 14, 20))
            };
            badgePill.Child = tutStepBadgeText;
            Grid.SetColumn(badgePill, 0);
            topRow.Children.Add(badgePill);

            Button closeBtn = CreateFlatWpfButton("✕ Close Guide", Color.FromRgb(42, 54, 72), Colors.White, () => SetTutorialStep(-1));
            closeBtn.Padding = new Thickness(10, 4, 10, 4);
            Grid.SetColumn(closeBtn, 1);
            topRow.Children.Add(closeBtn);
            cardStack.Children.Add(topRow);

            tutTitleText = new TextBlock
            {
                Text = "Welcome to Matrix & Soyjak Wallpaper Studio!",
                FontSize = 18.0,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            cardStack.Children.Add(tutTitleText);

            tutDescText = new TextBlock
            {
                Text = "",
                FontSize = 13.5,
                Foreground = new SolidColorBrush(Color.FromRgb(215, 228, 245)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20.0,
                Margin = new Thickness(0, 0, 0, 12)
            };
            cardStack.Children.Add(tutDescText);

            Border arrowBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 38, 52)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 230, 0)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 14)
            };
            tutArrowHintText = new TextBlock
            {
                Text = "⬅️ Look at the highlighted yellow border on the left panel!",
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 235, 80)),
                TextWrapping = TextWrapping.Wrap
            };
            arrowBox.Child = tutArrowHintText;
            cardStack.Children.Add(arrowBox);

            // Navigation Buttons Row
            Grid navRow = new Grid();
            navRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            navRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            navRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            tutBackBtn = CreateFlatWpfButton("← Previous Step", Color.FromRgb(36, 48, 68), Colors.White, () =>
            {
                if (tutorialStepIndex > 0) SetTutorialStep(tutorialStepIndex - 1);
            });
            tutBackBtn.Padding = new Thickness(14, 8, 14, 8);
            Grid.SetColumn(tutBackBtn, 0);
            navRow.Children.Add(tutBackBtn);

            tutNextBtn = CreateFlatWpfButton("Next Step →", Color.FromRgb(0, 215, 115), Color.FromRgb(8, 22, 12), () =>
            {
                if (tutorialStepIndex + 1 < 4) SetTutorialStep(tutorialStepIndex + 1);
                else SetTutorialStep(-1);
            });
            tutNextBtn.Padding = new Thickness(18, 8, 18, 8);
            Grid.SetColumn(tutNextBtn, 2);
            navRow.Children.Add(tutNextBtn);

            cardStack.Children.Add(navRow);
            tutorialCalloutCard.Child = cardStack;
            tutorialOverlayGrid.Children.Add(tutorialCalloutCard);
        }

        private void StartOrToggleTutorial()
        {
            if (tutorialStepIndex >= 0) SetTutorialStep(-1);
            else SetTutorialStep(0);
        }

        private void SetTutorialStep(int step)
        {
            tutorialStepIndex = step;
            leftDeckHighlightBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(32, 44, 62));
            leftDeckHighlightBorder.BorderThickness = new Thickness(0, 0, 1.5, 0);
            previewViewportBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 58, 82));
            previewViewportBorder.BorderThickness = new Thickness(1.5);
            bottomDesktopBarBorder.BorderBrush = new SolidColorBrush(config.AttachedToDesktop ? Color.FromRgb(245, 90, 110) : Color.FromRgb(0, 225, 120));
            bottomDesktopBarBorder.BorderThickness = new Thickness(2.0);

            if (step < 0 || step >= 4)
            {
                tutorialStepIndex = -1;
                tutorialOverlayGrid.Visibility = Visibility.Collapsed;
                btnHeaderTutorial.SetSelected(false);
                btnHeaderTutorial.TitleBlock.Text = "[?] HOW TO USE";
                return;
            }

            tutorialOverlayGrid.Visibility = Visibility.Visible;
            btnHeaderTutorial.SetSelected(true);
            btnHeaderTutorial.TitleBlock.Text = string.Format("[?] STEP {0} OF 4", step + 1);
            tutBackBtn.Visibility = (step > 0) ? Visibility.Visible : Visibility.Collapsed;
            tutNextBtn.Content = (step < 3) ? "Next Step →" : "✔ Got It! Finish";

            if (step == 0)
            {
                SwitchLeftTab(0);
                leftDeckHighlightBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 230, 0));
                leftDeckHighlightBorder.BorderThickness = new Thickness(3.5);
                tutStepBadgeText.Text = "STEP 1 OF 4 • PICK A MODE OR VIDEO";
                tutTitleText.Text = "1. Choose Your Live Wallpaper Mode!";
                tutDescText.Text = "On the left panel (outlined in bright yellow), click any of the 6 modes:\n" +
                                   "• Matrix Number Waterfall or Interactive ASCII Fluid\n" +
                                   "• Custom Image Wallpaper or MP4 Video (Colored ASCII or Full HD)!";
                tutArrowHintText.Text = "← Highlighted in Yellow: Left Control Deck (Tab 1: Modes & Video)";
            }
            else if (step == 1)
            {
                SwitchLeftTab(1);
                leftDeckHighlightBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 230, 0));
                leftDeckHighlightBorder.BorderThickness = new Thickness(3.5);
                tutStepBadgeText.Text = "STEP 2 OF 4 • SET IMAGE BACKGROUNDS & SOYJAKS";
                tutTitleText.Text = "2. Set Any Image as Background or Add Soyjaks!";
                tutDescText.Text = "In Tab 2 (Image BG), upload any PNG/JPG photo to sit behind the Matrix rain or convert into ASCII! In Tab 3 (Soyjak Memes), add funny pointing Soyjaks on top of your screen.";
                tutArrowHintText.Text = "← Highlighted in Yellow: Tab 2 (Image BG) & Tab 3 (Soyjak Memes)";
            }
            else if (step == 2)
            {
                SwitchLeftTab(4);
                leftDeckHighlightBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 230, 0));
                leftDeckHighlightBorder.BorderThickness = new Thickness(3.5);
                previewViewportBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 230, 0));
                previewViewportBorder.BorderThickness = new Thickness(3.5);
                tutStepBadgeText.Text = "STEP 3 OF 4 • TURN MOUSE EFFECTS ON / OFF + COOL FX";
                tutTitleText.Text = "3. Control Mouse Effects & Cool Overlays!";
                tutDescText.Text = "Want a distraction-free wallpaper? Click '◉ MOUSE FX' in the top bar (or in Tab 5) to turn Mouse Effects OFF anytime, or switch to Auto-Pilot Swirl! You can also toggle the Live Digital Clock and Custom Text Banner.";
                tutArrowHintText.Text = "↑ ← Highlighted: Tab 5 (Mouse & Cool FX) + Top 'MOUSE FX' Button";
            }
            else if (step == 3)
            {
                SwitchLeftTab(0);
                bottomDesktopBarBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 230, 0));
                bottomDesktopBarBorder.BorderThickness = new Thickness(4.0);
                tutStepBadgeText.Text = "STEP 4 OF 4 • APPLY TO YOUR WINDOWS DESKTOP";
                tutTitleText.Text = "4. Click '[+] ADD TO DESKTOP WALLPAPER'!";
                tutDescText.Text = "When you're happy with your wallpaper, click the big green '[+] ADD TO DESKTOP WALLPAPER' button at the bottom-right or top-right! It attaches cleanly behind your desktop icons in 1 click.";
                tutArrowHintText.Text = "↓ Highlighted in Yellow Below: The Big Green 'ADD TO DESKTOP WALLPAPER' Bar!";
            }
        }

        // =====================================================================
        // HELPER WPF UI BUILDERS (Auto-Wrapping, Never Clipping!)
        // =====================================================================
        private void AddSectionHeader(StackPanel parent, string title, string subtitle)
        {
            TextBlock t = new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 240, 135)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 8, 2, 1)
            };
            parent.Children.Add(t);

            if (!string.IsNullOrEmpty(subtitle))
            {
                TextBlock s = new TextBlock
                {
                    Text = subtitle,
                    FontSize = 11.2,
                    Foreground = new SolidColorBrush(Color.FromRgb(154, 172, 198)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(2, 0, 2, 6)
                };
                parent.Children.Add(s);
            }
        }

        private WpfCardOption CreateCardButton(
            string title,
            string subtitle,
            string badge,
            Color accentColor,
            Color baseBgColor,
            Color selectedBgColor,
            bool isPrimaryCta,
            Action onClick)
        {
            WpfCardOption opt = new WpfCardOption
            {
                AccentColor = accentColor,
                BaseBgColor = baseBgColor,
                SelectedBgColor = selectedBgColor,
                IsPrimaryCta = isPrimaryCta
            };

            Border border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 7, 10, 7),
                Cursor = Cursors.Hand
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Left accent bar
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Text stack (Wraps!)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Optional badge

            Border leftBar = new Border
            {
                Width = 4,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 2, 8, 2),
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(leftBar, 0);
            grid.Children.Add(leftBar);
            opt.LeftAccentBar = leftBar;

            StackPanel textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            if (isPrimaryCta) textStack.HorizontalAlignment = HorizontalAlignment.Center;

            TextBlock tbTitle = new TextBlock
            {
                Text = title,
                FontSize = isPrimaryCta ? 13.0 : 12.2,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = isPrimaryCta ? TextAlignment.Center : TextAlignment.Left
            };
            textStack.Children.Add(tbTitle);
            opt.TitleBlock = tbTitle;

            if (!string.IsNullOrEmpty(subtitle))
            {
                TextBlock tbSub = new TextBlock
                {
                    Text = subtitle,
                    FontSize = 11.0,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = isPrimaryCta ? TextAlignment.Center : TextAlignment.Left,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                textStack.Children.Add(tbSub);
                opt.SubBlock = tbSub;
            }

            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            if (!string.IsNullOrEmpty(badge))
            {
                Border badgeBorder = new Border
                {
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(7, 3, 7, 3),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock badgeTxt = new TextBlock
                {
                    Text = badge,
                    FontSize = 10.0,
                    FontWeight = FontWeights.Bold
                };
                badgeBorder.Child = badgeTxt;
                Grid.SetColumn(badgeBorder, 2);
                grid.Children.Add(badgeBorder);
                opt.BadgeBorder = badgeBorder;
                opt.BadgeBlock = badgeTxt;
            }

            border.Child = grid;
            opt.CardBorder = border;
            opt.RefreshVisuals(false);

            border.MouseEnter += (s, e) => opt.RefreshVisuals(true);
            border.MouseLeave += (s, e) => opt.RefreshVisuals(false);
            border.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };

            return opt;
        }

        private WpfCardOption CreateCardButtonWithThumb(
            string title,
            string subtitle,
            BitmapSource thumbSource,
            Color accentColor,
            Color baseBgColor,
            Color selectedBgColor,
            Action onClick)
        {
            WpfCardOption opt = new WpfCardOption
            {
                AccentColor = accentColor,
                BaseBgColor = baseBgColor,
                SelectedBgColor = selectedBgColor,
                IsPrimaryCta = false
            };

            Border border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                Cursor = Cursors.Hand
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Left bar
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Thumbnail
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Wrapping text

            Border leftBar = new Border
            {
                Width = 4,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 2, 8, 2),
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(leftBar, 0);
            grid.Children.Add(leftBar);
            opt.LeftAccentBar = leftBar;

            if (thumbSource != null)
            {
                Border imgBox = new Border
                {
                    Width = 40,
                    Height = 40,
                    CornerRadius = new CornerRadius(5),
                    Background = new SolidColorBrush(Color.FromRgb(10, 14, 20)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(55, 70, 92)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 10, 0),
                    ClipToBounds = true
                };
                Image img = new Image
                {
                    Source = thumbSource,
                    Stretch = Stretch.UniformToFill
                };
                imgBox.Child = img;
                Grid.SetColumn(imgBox, 1);
                grid.Children.Add(imgBox);
            }

            StackPanel textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock tbTitle = new TextBlock
            {
                Text = title,
                FontSize = 12.2,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };
            TextBlock tbSub = new TextBlock
            {
                Text = subtitle,
                FontSize = 11.0,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            };
            textStack.Children.Add(tbTitle);
            textStack.Children.Add(tbSub);
            Grid.SetColumn(textStack, 2);
            grid.Children.Add(textStack);

            opt.TitleBlock = tbTitle;
            opt.SubBlock = tbSub;
            border.Child = grid;
            opt.CardBorder = border;
            opt.RefreshVisuals(false);

            border.MouseEnter += (s, e) => opt.RefreshVisuals(true);
            border.MouseLeave += (s, e) => opt.RefreshVisuals(false);
            border.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };

            return opt;
        }

        private Slider AddLabeledSlider(StackPanel parent, string label, double min, double max, double initial, string suffix, out TextBlock valueBlock, Action<double> onChanged)
        {
            Grid header = new Grid { Margin = new Thickness(2, 4, 2, 2) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lbl = new TextBlock
            {
                Text = label,
                FontSize = 12.0,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 232, 246)),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(lbl, 0);
            header.Children.Add(lbl);

            TextBlock valTxt = new TextBlock
            {
                Text = ((int)Math.Round(initial)).ToString() + suffix,
                FontSize = 12.0,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 240, 135)),
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(valTxt, 1);
            header.Children.Add(valTxt);
            valueBlock = valTxt;
            parent.Children.Add(header);

            Slider sld = new Slider
            {
                Minimum = min,
                Maximum = max,
                Value = initial,
                Margin = new Thickness(2, 0, 2, 8),
                Cursor = Cursors.Hand
            };
            TextBlock capturedVal = valTxt;
            sld.ValueChanged += (s, e) =>
            {
                capturedVal.Text = ((int)Math.Round(sld.Value)).ToString() + suffix;
                if (onChanged != null) onChanged(sld.Value);
            };
            parent.Children.Add(sld);
            return sld;
        }

        private CheckBox CreateStyledCheckBox(string text, bool initial, Action<bool> onChanged)
        {
            CheckBox chk = new CheckBox
            {
                IsChecked = initial,
                Margin = new Thickness(2, 4, 6, 4),
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = new TextBlock
                {
                    Text = text,
                    FontSize = 12.0,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 232, 245)),
                    TextWrapping = TextWrapping.Wrap
                }
            };
            chk.Checked += (s, e) => { if (onChanged != null) onChanged(true); };
            chk.Unchecked += (s, e) => { if (onChanged != null) onChanged(false); };
            return chk;
        }

        private Button CreateColorPickerWpfButton(string title, System.Drawing.Color initialCol, Action<System.Drawing.Color> onPicked)
        {
            Button btn = new Button
            {
                Content = title,
                Margin = new Thickness(3),
                Padding = new Thickness(6, 8, 6, 8),
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(initialCol.R, initialCol.G, initialCol.B)),
                Foreground = GetReadableWpfBrush(initialCol),
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 105, 135)),
                BorderThickness = new Thickness(1.5)
            };
            btn.Click += (s, e) =>
            {
                using (System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog())
                {
                    cd.FullOpen = true;
                    cd.Color = initialCol;
                    if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        initialCol = cd.Color;
                        btn.Background = new SolidColorBrush(Color.FromRgb(cd.Color.R, cd.Color.G, cd.Color.B));
                        btn.Foreground = GetReadableWpfBrush(cd.Color);
                        if (onPicked != null) onPicked(cd.Color);
                    }
                }
            };
            return btn;
        }

        private Button CreateFlatWpfButton(string text, Color bg, Color fg, Action onClick)
        {
            Button b = new Button
            {
                Content = text,
                Background = new SolidColorBrush(bg),
                Foreground = new SolidColorBrush(fg),
                BorderBrush = new SolidColorBrush(Color.FromRgb(90, 120, 155)),
                BorderThickness = new Thickness(1),
                FontWeight = FontWeights.Bold,
                FontSize = 12.0,
                Cursor = Cursors.Hand
            };
            b.Click += (s, e) => { if (onClick != null) onClick(); };
            return b;
        }

        private Brush GetReadableWpfBrush(System.Drawing.Color c)
        {
            double lum = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
            return lum > 135 ? new SolidColorBrush(Color.FromRgb(10, 14, 20)) : Brushes.White;
        }

        private BitmapSource GetWpfBitmapSource(System.Drawing.Bitmap bmp)
        {
            if (bmp == null) return null;
            try
            {
                int w = bmp.Width;
                int h = bmp.Height;
                using (System.Drawing.Bitmap copy = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(copy))
                    {
                        g.DrawImage(bmp, 0, 0, w, h);
                    }
                    System.Drawing.Imaging.BitmapData bd = copy.LockBits(
                        new System.Drawing.Rectangle(0, 0, w, h),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    try
                    {
                        BitmapSource bs = BitmapSource.Create(
                            w, h, 96, 96,
                            PixelFormats.Pbgra32,
                            null,
                            bd.Scan0,
                            bd.Stride * h,
                            bd.Stride);
                        bs.Freeze();
                        return bs;
                    }
                    finally
                    {
                        copy.UnlockBits(bd);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // STATE SYNCHRONIZATION & ACTIONS
        // =====================================================================
        private void SwitchLeftTab(int idx)
        {
            activeTabIndex = idx;
            for (int i = 0; i < 6; i++)
            {
                if (tabButtons[i] != null) tabButtons[i].SetSelected(i == idx);
                if (tabPages[i] != null) tabPages[i].Visibility = (i == idx) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void CycleMouseModeQuick()
        {
            if (config.MouseMode == MouseEffectMode.FollowCursor)
                renderer.SetMouseMode(MouseEffectMode.Off);
            else if (config.MouseMode == MouseEffectMode.Off)
                renderer.SetMouseMode(MouseEffectMode.AutoGhostSwirl);
            else
                renderer.SetMouseMode(MouseEffectMode.FollowCursor);

            SyncAllControlsWithConfig();
        }

        private void TriggerRandomCoolPreset()
        {
            Random rng = new Random();
            RenderMode[] modes = new RenderMode[]
            {
                RenderMode.MatrixWaterfall,
                RenderMode.AsciiFluidSim,
                RenderMode.ImageWallpaper,
                RenderMode.VideoToAscii,
                RenderMode.VideoMatrixHybrid
            };
            config.Mode = modes[rng.Next(modes.Length)];
            config.Theme = (ColorTheme)rng.Next(0, 6);
            config.ActiveBgImage = (BackgroundImagePreset)rng.Next(0, 4);
            config.ShowClockOverlay = (rng.Next(2) == 0);
            renderer.InvalidateBackgroundCache();
            renderer.TriggerBurstAt(0.5f, 0.5f);
            SyncAllControlsWithConfig();
        }

        private void SyncAllControlsWithConfig()
        {
            RenderMode[] modeEnumValues = new RenderMode[]
            {
                RenderMode.MatrixWaterfall,
                RenderMode.AsciiFluidSim,
                RenderMode.ImageWallpaper,
                RenderMode.VideoToAscii,
                RenderMode.RegularVideoWallpaper,
                RenderMode.VideoMatrixHybrid
            };
            for (int i = 0; i < modeCards.Length; i++)
            {
                if (modeCards[i] != null) modeCards[i].SetSelected(config.Mode == modeEnumValues[i]);
            }

            for (int i = 0; i < bgImageCards.Length; i++)
            {
                if (bgImageCards[i] != null) bgImageCards[i].SetSelected((int)config.ActiveBgImage == i);
            }
            for (int i = 0; i < bgStyleCards.Length; i++)
            {
                if (bgStyleCards[i] != null) bgStyleCards[i].SetSelected((int)config.BgImageStyle == i);
            }

            for (int i = 0; i < soyjakCards.Length; i++)
            {
                if (soyjakCards[i] != null) soyjakCards[i].SetSelected((int)config.ActiveSoyjak == i);
            }
            for (int i = 0; i < soyjakStyleCards.Length; i++)
            {
                if (soyjakStyleCards[i] != null) soyjakStyleCards[i].SetSelected((int)config.SoyjakStyle == i);
            }

            for (int i = 0; i < themeCards.Length; i++)
            {
                if (themeCards[i] != null) themeCards[i].SetSelected((int)config.Theme == i);
            }
            for (int i = 0; i < vidColorCards.Length; i++)
            {
                if (vidColorCards[i] != null) vidColorCards[i].SetSelected((int)config.VidColorMode == i);
            }

            string[] charPresets = new string[]
            {
                "Numbers Only (0-9)",
                "Binary (0-1)",
                "Hexadecimal (0-9, A-F)",
                "Matrix Classic (Numbers + Symbols)",
                "Fluid Density Ramp (.:-=+*#%@0189)",
                "Custom"
            };
            for (int i = 0; i < charPresetCards.Length; i++)
            {
                if (charPresetCards[i] != null) charPresetCards[i].SetSelected(config.CharSetPreset == charPresets[i]);
            }

            MouseEffectMode[] mmModes = new MouseEffectMode[]
            {
                MouseEffectMode.Off,
                MouseEffectMode.FollowCursor,
                MouseEffectMode.AutoGhostSwirl
            };
            for (int i = 0; i < mouseModeCards.Length; i++)
            {
                if (mouseModeCards[i] != null) mouseModeCards[i].SetSelected(config.MouseMode == mmModes[i]);
            }

            if (btnHeaderMouseToggle != null)
            {
                if (config.MouseMode == MouseEffectMode.Off)
                {
                    btnHeaderMouseToggle.TitleBlock.Text = "🚫 MOUSE FX: OFF";
                    btnHeaderMouseToggle.SubBlock.Text = "Cursor effects disabled";
                    btnHeaderMouseToggle.SetSelected(false);
                    if (lblMouseBadge != null)
                    {
                        lblMouseBadge.Text = "🚫 Mouse Effects: OFF (Disabled)";
                        lblMouseBadge.Foreground = new SolidColorBrush(Color.FromRgb(255, 120, 130));
                    }
                }
                else if (config.MouseMode == MouseEffectMode.AutoGhostSwirl)
                {
                    btnHeaderMouseToggle.TitleBlock.Text = "◎ MOUSE FX: AUTO";
                    btnHeaderMouseToggle.SubBlock.Text = "Swirling automatically!";
                    btnHeaderMouseToggle.SetSelected(true);
                    if (lblMouseBadge != null)
                    {
                        lblMouseBadge.Text = "◎ Mouse Mode: Auto-Pilot Swirl";
                        lblMouseBadge.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                    }
                }
                else
                {
                    btnHeaderMouseToggle.TitleBlock.Text = "◉ MOUSE FX: ON";
                    btnHeaderMouseToggle.SubBlock.Text = "Click to turn OFF or Auto";
                    btnHeaderMouseToggle.SetSelected(true);
                    if (lblMouseBadge != null)
                    {
                        lblMouseBadge.Text = "◉ Mouse Follow: ON (Move cursor below)";
                        lblMouseBadge.Foreground = new SolidColorBrush(Color.FromRgb(0, 235, 135));
                    }
                }
            }

            if (cardToggleClock != null) cardToggleClock.SetSelected(config.ShowClockOverlay);
            if (cardToggleBanner != null) cardToggleBanner.SetSelected(config.ShowCustomBanner);
            if (cardToggleCrt != null) cardToggleCrt.SetSelected(config.CrtScanlines);
            if (cardToggleReverseRain != null) cardToggleReverseRain.SetSelected(config.ReverseRainDirection);

            if (btnHeaderDesktopToggle != null && btnBigDesktopToggle != null)
            {
                bool active = config.AttachedToDesktop;
                btnHeaderDesktopToggle.SetSelected(active);
                btnHeaderDesktopToggle.TitleBlock.Text = active ? "[X] REMOVE FROM DESKTOP" : "[+] ADD TO DESKTOP WALLPAPER";
                btnHeaderDesktopToggle.SubBlock.Text = active ? "Live behind your desktop icons" : "1-Click Apply Behind Icons";

                btnBigDesktopToggle.SetSelected(active);
                btnBigDesktopToggle.TitleBlock.Text = active ? "[X] REMOVE FROM DESKTOP WALLPAPER" : "[+] ADD TO DESKTOP WALLPAPER";
                btnBigDesktopToggle.SubBlock.Text = active ? "Currently running on your desktop" : "1-Click Apply Behind Desktop Icons";

                if (lblDesktopStatusTitle != null)
                {
                    lblDesktopStatusTitle.Text = active
                        ? "✔ LIVE WALLPAPER IS ACTIVE ON YOUR WINDOWS DESKTOP!"
                        : "READY FOR YOUR WINDOWS DESKTOP?";
                    lblDesktopStatusTitle.Foreground = active
                        ? new SolidColorBrush(Color.FromRgb(0, 245, 135))
                        : Brushes.White;
                }
                if (lblDesktopStatusSub != null)
                {
                    lblDesktopStatusSub.Text = active
                        ? "Minimize this window to enjoy your live wallpaper! Click the red button to detach."
                        : "Click the big green button on the right to attach this live wallpaper behind your desktop icons!";
                }
                if (bottomDesktopBarBorder != null && tutorialStepIndex != 3)
                {
                    bottomDesktopBarBorder.BorderBrush = new SolidColorBrush(active ? Color.FromRgb(245, 90, 110) : Color.FromRgb(0, 225, 120));
                }
            }

            if (btnEcoModeToggle != null)
            {
                bool isEco = config.TargetFps <= 30;
                btnEcoModeToggle.SetSelected(isEco);
                btnEcoModeToggle.TitleBlock.Text = isEco
                    ? "🌱 ECO MODE: ACTIVE (30 FPS)"
                    : "⚡ MAXIMUM SMOOTHNESS: ACTIVE (60 FPS)";
                btnEcoModeToggle.SubBlock.Text = isEco
                    ? "Low CPU & battery saver for older Acer/Celeron laptops (Click to toggle 60 FPS)"
                    : "Full 60 FPS buttery smooth desktop rendering (Click to switch to 30 FPS Eco Mode)";
            }
            if (chkAutoCheckUpdates != null) chkAutoCheckUpdates.IsChecked = config.AutoCheckUpdates;
            if (chkPauseWhenMinimized != null) chkPauseWhenMinimized.IsChecked = config.PauseWhenMinimized;
            SettingsStore.SaveIfChanged(config);
        }

        private void OnUploadMp4Clicked()
        {
            using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
            {
                ofd.Title = "Select MP4 or Video File for Live Wallpaper";
                ofd.Filter = "Video Files (*.mp4;*.wmv;*.avi;*.mov;*.mkv)|*.mp4;*.wmv;*.avi;*.mov;*.mkv|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    renderer.LoadVideo(ofd.FileName);
                    if (config.Mode != RenderMode.VideoToAscii &&
                        config.Mode != RenderMode.RegularVideoWallpaper &&
                        config.Mode != RenderMode.VideoMatrixHybrid)
                    {
                        config.Mode = RenderMode.VideoToAscii;
                    }
                    lblLoadedVideoStatus.Text = "✔ Loaded Video: " + Path.GetFileName(ofd.FileName);
                    SyncAllControlsWithConfig();
                }
            }
        }

        private void OnUploadCustomBgImageClicked()
        {
            using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
            {
                ofd.Title = "Select Background Image / Photo (PNG, JPG, BMP)";
                ofd.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (renderer.LoadCustomBackgroundImage(ofd.FileName))
                    {
                        lblCustomBgStatus.Text = "✔ Active Custom Background: " + Path.GetFileName(ofd.FileName);
                        lblCustomBgStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 240, 135));
                        SyncAllControlsWithConfig();
                    }
                }
            }
        }

        private void OnUploadCustomSoyjakClicked()
        {
            using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
            {
                ofd.Title = "Select Custom Soyjak or Meme Image (PNG / JPG)";
                ofd.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (renderer.LoadCustomSoyjakImage(ofd.FileName))
                    {
                        soyjakCards[(int)SoyjakPreset.CustomUploadedImage].SubBlock.Text = "✔ Loaded: " + Path.GetFileName(ofd.FileName);
                        SyncAllControlsWithConfig();
                    }
                }
            }
        }

        private void ToggleDesktopWallpaperMode()
        {
            if (!config.AttachedToDesktop)
            {
                if (wallpaperForm == null || wallpaperForm.IsDisposed)
                {
                    wallpaperForm = new WallpaperWindow(renderer, config);
                }
                wallpaperForm.Show();
                wallpaperForm.AttachToDesktopBackground();
                config.AttachedToDesktop = true;
            }
            else
            {
                if (wallpaperForm != null && !wallpaperForm.IsDisposed)
                {
                    wallpaperForm.DetachFromDesktop();
                }
                config.AttachedToDesktop = false;
            }
            SyncAllControlsWithConfig();
        }

        // =====================================================================
        // 60 FPS RENDER LOOP -> WPF WRITEABLEBITMAP
        // =====================================================================
        private bool isRenderingFrame = false;

        private void OnRenderFrameTick(object sender, EventArgs e)
        {
            if (isRenderingFrame) return;
            isRenderingFrame = true;
            try
            {
                if (config.PauseWhenMinimized && this.WindowState == WindowState.Minimized && !config.AttachedToDesktop)
                {
                    return;
                }

                float dt = config.TargetFps <= 30 ? 0.033f : 0.016f;

                if (config.AttachedToDesktop && wallpaperForm != null && !wallpaperForm.IsDisposed)
                {
                    wallpaperForm.PollDesktopMouse();
                    System.Drawing.Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                    renderer.RenderStep(bounds.Width, bounds.Height, dt);
                    wallpaperForm.Invalidate();
                }
                else
                {
                    int pw = Math.Max(320, (int)previewViewportBorder.ActualWidth);
                    int ph = Math.Max(220, (int)previewViewportBorder.ActualHeight);
                    renderer.RenderStep(pw, ph, dt);
                }

                // If minimized or hidden, bypass copying into WriteableBitmap preview to save massive CPU on Celeron laptops
                if (this.WindowState != WindowState.Minimized && this.IsVisible)
                {
                    int cw = renderer.CanvasWidth;
                    int ch = renderer.CanvasHeight;
                    if (cw > 0 && ch > 0)
                    {
                        if (previewWriteableBmp == null || previewWriteableBmp.PixelWidth != cw || previewWriteableBmp.PixelHeight != ch)
                        {
                            previewWriteableBmp = new WriteableBitmap(cw, ch, 96, 96, PixelFormats.Pbgra32, null);
                            previewImageControl.Source = previewWriteableBmp;
                        }
                        renderer.CopyToWriteableBitmap(previewWriteableBmp);
                    }
                }

                frameCounter++;
                totalFramesRendered++;
                if (fpsWatch.ElapsedMilliseconds >= 500)
                {
                    currentFps = (int)Math.Round(frameCounter * 1000.0 / fpsWatch.ElapsedMilliseconds);
                    frameCounter = 0;
                    fpsWatch.Restart();
                    lblLiveStatus.Text = string.Format("{0} FPS • Mode: {1} • BG: {2}", currentFps, config.Mode, config.ActiveBgImage);
                }

                // Automated Screenshot Verification Support
                if (!string.IsNullOrEmpty(screenshotOutputPath) && totalFramesRendered == 8)
                {
                    if (config.MouseMode == MouseEffectMode.FollowCursor)
                    {
                        renderer.TriggerBurstAt(0.48f, 0.48f);
                    }
                }
                if (!string.IsNullOrEmpty(screenshotOutputPath) && totalFramesRendered >= screenshotFrameTarget)
                {
                    CaptureWpfWindowScreenshot(screenshotOutputPath);
                    screenshotOutputPath = null;
                    Application.Current.Shutdown();
                }
            }
            catch { }
            finally
            {
                isRenderingFrame = false;
            }
        }

        private void CaptureWpfWindowScreenshot(string path)
        {
            try
            {
                this.UpdateLayout();
                PresentationSource src = PresentationSource.FromVisual(this);
                double dpiX = 96.0;
                double dpiY = 96.0;
                if (src != null && src.CompositionTarget != null)
                {
                    dpiX = 96.0 * src.CompositionTarget.TransformToDevice.M11;
                    dpiY = 96.0 * src.CompositionTarget.TransformToDevice.M22;
                }
                Visual rootVisual = (Visual)this.Content;
                FrameworkElement fe = this.Content as FrameworkElement;
                int w = (int)Math.Round((fe != null ? fe.ActualWidth : this.ActualWidth) * (dpiX / 96.0));
                int h = (int)Math.Round((fe != null ? fe.ActualHeight : this.ActualHeight) * (dpiY / 96.0));
                if (w <= 0) w = 1240;
                if (h <= 0) h = 800;

                RenderTargetBitmap rtb = new RenderTargetBitmap(w, h, dpiX, dpiY, PixelFormats.Pbgra32);
                rtb.Render(rootVisual);

                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    enc.Save(fs);
                }
            }
            catch { }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (renderTimer != null) renderTimer.Stop();
            try { SettingsStore.SaveIfChanged(config); } catch { }
            if (wallpaperForm != null && !wallpaperForm.IsDisposed)
            {
                wallpaperForm.DetachFromDesktop();
                wallpaperForm.Close();
            }
            if (renderer != null) renderer.Dispose();
            base.OnClosed(e);
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try { NativeWin32.SetProcessDPIAware(); } catch { }
            try
            {
                System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);
                System.Windows.Forms.Application.ThreadException += (s, e) => { };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => { };
            }
            catch { }

            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            Application wpfApp = new Application();
            wpfApp.DispatcherUnhandledException += (s, e) => { e.Handled = true; };
            MainStudioWpfWindow mainWin = new MainStudioWpfWindow(args);
            wpfApp.Run(mainWin);
        }
    }
}
