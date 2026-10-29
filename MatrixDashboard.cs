using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MatrixWallpaperEngine
{
    public class DashboardWindow : Window
    {
        private const string RepoOwnerRepo = "BrassyFrogy/matrix-wallpaper";
        private const string RepoBranch = "main";
        private const string RepoUrl = "https://github.com/" + RepoOwnerRepo;

        private string projectDir;
        private string cscPath;

        // UI Inputs & Controls
        private TextBox txtVersion;
        private TextBox txtNotes;
        private TextBlock lblLocalEngineStatus;
        private TextBlock lblLocalSetupStatus;
        private TextBlock lblRemoteStatus;
        private TextBlock lblStatusBanner;
        private TextBox txtConsoleLog;
        private Button btnBuild;
        private Button btnPush;
        private Button btnVerify;
        private Button btnRunEngine;
        private Button btnRunSetup;

        private bool isWorking = false;
        private string screenshotPath = null;

        public DashboardWindow(string[] args)
        {
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLowerInvariant() == "--screenshot" && i + 1 < args.Length)
                    {
                        screenshotPath = args[i + 1];
                        i++;
                    }
                }
            }

            projectDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            cscPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework64\v4.0.30319\csc.exe");
            if (!File.Exists(cscPath))
            {
                cscPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\v4.0.30319\csc.exe");
            }

            Title = "Matrix Wallpaper Engine — Developer Dashboard (Samuel Philip Schepen)";
            Width = 920;
            Height = 840;
            MinWidth = 800;
            MinHeight = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(12, 16, 24));
            Foreground = new SolidColorBrush(Color.FromRgb(225, 235, 248));
            FontFamily = new FontFamily("Segoe UI, Lucida Console");

            BuildUI();
            RefreshFileStatuses();
            CheckRemoteStatusAsync();

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                DispatcherTimer t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                t.Tick += (s, e) =>
                {
                    t.Stop();
                    try
                    {
                        int w = (int)Math.Max(ActualWidth, 920);
                        int h = (int)Math.Max(ActualHeight, 840);
                        RenderTargetBitmap rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(this);
                        PngBitmapEncoder enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(rtb));
                        using (FileStream fs = new FileStream(screenshotPath, FileMode.Create, FileAccess.Write))
                        {
                            enc.Save(fs);
                        }
                    }
                    catch { }
                    Application.Current.Shutdown();
                };
                t.Start();
            }
        }

        private void BuildUI()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Body
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(170) }); // Console Terminal

            // 1. Header Bar
            Border header = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 22, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 235, 160)),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(18, 12, 18, 12)
            };
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleStack = new StackPanel();
            TextBlock title = new TextBlock
            {
                Text = "⚡ MATRIX WALLPAPER ENGINE — DEVELOPER DASHBOARD",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 175))
            };
            TextBlock subTitle = new TextBlock
            {
                Text = "Developer: Samuel Philip Schepen • Friend Target: Bradley Taylor (Acer Celeron Laptop) • Repo: " + RepoOwnerRepo,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 180, 210)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleStack.Children.Add(title);
            titleStack.Children.Add(subTitle);
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            Border badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 48, 40)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 245, 170)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock
            {
                Text = "DEV CONSOLE",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 175))
            };
            Grid.SetColumn(badge, 1);
            headerGrid.Children.Add(badge);

            header.Child = headerGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // 2. Scrollable Body
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(18, 14, 18, 10)
            };
            StackPanel body = new StackPanel();

            // Status Banner Card
            Border bannerCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(18, 28, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };
            lblStatusBanner = new TextBlock
            {
                Text = "✔ Ready. Source Directory: " + projectDir,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 220, 255)),
                TextWrapping = TextWrapping.Wrap
            };
            bannerCard.Child = lblStatusBanner;
            body.Children.Add(bannerCard);

            // SECTION 1: Release Version & Notes
            AddSectionTitle(body, "1. RELEASE CONFIGURATION & VERSIONING", "Define the release version and changelog that Bradley's laptop will display when updating:");

            Grid verRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            verRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            verRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            verRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            verRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            verRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            txtVersion = new TextBox
            {
                Text = LoadExistingVersion(),
                Background = new SolidColorBrush(Color.FromRgb(20, 28, 40)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 175)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 220, 160)),
                BorderThickness = new Thickness(1.5),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(8, 4, 8, 4)
            };
            Grid.SetColumn(txtVersion, 0);
            verRow.Children.Add(txtVersion);

            Button btnBumpPatch = CreateMiniButton("+0.0.1 (Patch)", () => BumpVersion(0, 0, 1));
            Grid.SetColumn(btnBumpPatch, 2);
            verRow.Children.Add(btnBumpPatch);

            Button btnBumpMinor = CreateMiniButton("+0.1.0 (Minor)", () => BumpVersion(0, 1, 0));
            Grid.SetColumn(btnBumpMinor, 4);
            verRow.Children.Add(btnBumpMinor);

            body.Children.Add(verRow);

            TextBlock lblNotesHint = new TextBlock
            {
                Text = "Release Notes (shown on Bradley's laptop screen):",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(165, 190, 215)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            body.Children.Add(lblNotesHint);

            txtNotes = new TextBox
            {
                Text = "Performance boost for Celeron laptop, Eco Mode (30 FPS), zero-allocation GDI32 direct blit, and seamless GitHub auto-pull.",
                Background = new SolidColorBrush(Color.FromRgb(20, 28, 40)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1.2),
                FontSize = 11.5,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 44,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 14)
            };
            body.Children.Add(txtNotes);

            // SECTION 2: Local & Remote File Statuses
            AddSectionTitle(body, "2. EXECUTABLES & GITHUB STATUS", "Checksum and size comparison between your local build and what is published on GitHub:");

            Border statusBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 22, 32)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 56, 76)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 14)
            };
            StackPanel statusBoxStack = new StackPanel();

            lblLocalEngineStatus = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
            lblLocalSetupStatus = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
            lblRemoteStatus = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap };

            statusBoxStack.Children.Add(lblLocalEngineStatus);
            statusBoxStack.Children.Add(lblLocalSetupStatus);
            statusBoxStack.Children.Add(lblRemoteStatus);
            statusBox.Child = statusBoxStack;
            body.Children.Add(statusBox);

            // SECTION 3: 1-Click Action Buttons
            AddSectionTitle(body, "3. DEVELOPER ACTIONS", "Build, test, verify, and publish updates to GitHub in 1 click:");

            Grid actionsGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            btnBuild = CreateBigActionButton(
                "🔨 BUILD BOTH EXEs",
                "Compiles MatrixWallpaperEngine.exe & MatrixWallpaperSetup.exe with csc.exe, then updates version.json",
                Color.FromRgb(0, 245, 175),
                Color.FromRgb(14, 42, 34),
                Color.FromRgb(0, 215, 150),
                OnBuildClicked);
            Grid.SetColumn(btnBuild, 0);
            actionsGrid.Children.Add(btnBuild);

            btnPush = CreateBigActionButton(
                "🚀 1-CLICK PUSH TO GITHUB",
                "Runs git add, git commit -m 'Release v...', and git push origin main so Bradley receives the update!",
                Color.FromRgb(56, 189, 248),
                Color.FromRgb(16, 36, 56),
                Color.FromRgb(40, 160, 220),
                OnPushClicked);
            Grid.SetColumn(btnPush, 2);
            actionsGrid.Children.Add(btnPush);

            body.Children.Add(actionsGrid);

            Grid testGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            testGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            testGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            testGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            testGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            testGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            btnVerify = CreateMiniButton("🌐 Query GitHub version.json", CheckRemoteStatusAsync);
            Grid.SetColumn(btnVerify, 0);
            testGrid.Children.Add(btnVerify);

            btnRunEngine = CreateMiniButton("▶ Launch Wallpaper Engine", () => LaunchExe("MatrixWallpaperEngine.exe"));
            Grid.SetColumn(btnRunEngine, 2);
            testGrid.Children.Add(btnRunEngine);

            btnRunSetup = CreateMiniButton("▶ Launch Setup Wizard", () => LaunchExe("MatrixWallpaperSetup.exe"));
            Grid.SetColumn(btnRunSetup, 4);
            testGrid.Children.Add(btnRunSetup);

            body.Children.Add(testGrid);

            sv.Content = body;
            Grid.SetRow(sv, 1);
            root.Children.Add(sv);

            // 3. Live Console Terminal
            Border consoleCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(8, 10, 16)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(32, 44, 60)),
                BorderThickness = new Thickness(0, 2, 0, 0),
                Padding = new Thickness(14, 8, 14, 8)
            };
            Grid consoleGrid = new Grid();
            consoleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            consoleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid termHeader = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            termHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            termHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock termTitle = new TextBlock
            {
                Text = "> LIVE TERMINAL OUTPUT & BUILD LOGS",
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 170))
            };
            Grid.SetColumn(termTitle, 0);
            termHeader.Children.Add(termTitle);

            Button btnClear = new Button
            {
                Content = "Clear",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 165, 195)),
                BorderThickness = new Thickness(0),
                FontSize = 10,
                Cursor = Cursors.Hand
            };
            btnClear.Click += (s, e) => { txtConsoleLog.Text = ""; };
            Grid.SetColumn(btnClear, 1);
            termHeader.Children.Add(btnClear);

            Grid.SetRow(termHeader, 0);
            consoleGrid.Children.Add(termHeader);

            txtConsoleLog = new TextBox
            {
                IsReadOnly = true,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 240, 220)),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                Text = "Dashboard initialized. Ready to build and push.\r\n"
            };
            Grid.SetRow(txtConsoleLog, 1);
            consoleGrid.Children.Add(txtConsoleLog);

            consoleCard.Child = consoleGrid;
            Grid.SetRow(consoleCard, 2);
            root.Children.Add(consoleCard);

            Content = root;
        }

        private void AddSectionTitle(Panel parent, string title, string subtitle)
        {
            TextBlock t = new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 235, 250)),
                Margin = new Thickness(0, 4, 0, 1)
            };
            TextBlock sub = new TextBlock
            {
                Text = subtitle,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 165, 195)),
                Margin = new Thickness(0, 0, 0, 6),
                TextWrapping = TextWrapping.Wrap
            };
            parent.Children.Add(t);
            parent.Children.Add(sub);
        }

        private Button CreateMiniButton(string text, Action onClick)
        {
            Button b = new Button
            {
                Content = text,
                Background = new SolidColorBrush(Color.FromRgb(24, 34, 48)),
                Foreground = new SolidColorBrush(Color.FromRgb(140, 210, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };
            b.Click += (s, e) => { if (onClick != null) onClick(); };
            return b;
        }

        private Button CreateBigActionButton(string mainText, string subText, Color fg, Color bg, Color border, Action onClick)
        {
            Button b = new Button
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(border),
                BorderThickness = new Thickness(1.8),
                Padding = new Thickness(12, 10, 12, 10),
                Cursor = Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            StackPanel sp = new StackPanel();
            TextBlock t = new TextBlock
            {
                Text = mainText,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fg)
            };
            TextBlock s = new TextBlock
            {
                Text = subText,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(185, 205, 225)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            sp.Children.Add(t);
            sp.Children.Add(s);
            b.Content = sp;
            b.Click += (sObj, eObj) => { if (onClick != null) onClick(); };
            return b;
        }

        private void Log(string msg)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n";
                txtConsoleLog.AppendText(line);
                txtConsoleLog.ScrollToEnd();
            }));
        }

        private string LoadExistingVersion()
        {
            try
            {
                string p = Path.Combine(projectDir, "version.json");
                if (File.Exists(p))
                {
                    string json = File.ReadAllText(p);
                    Match m = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]+)\"");
                    if (m.Success) return m.Groups[1].Value;
                }
            }
            catch { }
            return "2.1.0";
        }

        private void BumpVersion(int majorDelta, int minorDelta, int patchDelta)
        {
            string[] parts = (txtVersion.Text ?? "2.1.0").Trim().Split('.');
            int maj = 2, min = 1, patch = 0;
            if (parts.Length > 0) int.TryParse(parts[0], out maj);
            if (parts.Length > 1) int.TryParse(parts[1], out min);
            if (parts.Length > 2) int.TryParse(parts[2], out patch);

            maj += majorDelta;
            min += minorDelta;
            patch += patchDelta;
            txtVersion.Text = string.Format("{0}.{1}.{2}", maj, min, patch);
        }

        private void RefreshFileStatuses()
        {
            string engineExe = Path.Combine(projectDir, "MatrixWallpaperEngine.exe");
            string setupExe = Path.Combine(projectDir, "MatrixWallpaperSetup.exe");

            if (File.Exists(engineExe))
            {
                FileInfo fi = new FileInfo(engineExe);
                string sha = AppUpdater.FileSha256(engineExe);
                lblLocalEngineStatus.Text = string.Format("• Local Engine: MatrixWallpaperEngine.exe ({0:F2} MB, Modified: {1})\r\n  SHA-256: {2}",
                    fi.Length / (1024.0 * 1024.0), fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"), sha);
                lblLocalEngineStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 170));
            }
            else
            {
                lblLocalEngineStatus.Text = "• Local Engine: MatrixWallpaperEngine.exe is NOT built yet.";
                lblLocalEngineStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 150, 100));
            }

            if (File.Exists(setupExe))
            {
                FileInfo fi = new FileInfo(setupExe);
                lblLocalSetupStatus.Text = string.Format("• Local Installer: MatrixWallpaperSetup.exe ({0:F2} MB, Modified: {1})",
                    fi.Length / (1024.0 * 1024.0), fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                lblLocalSetupStatus.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            }
            else
            {
                lblLocalSetupStatus.Text = "• Local Installer: MatrixWallpaperSetup.exe is NOT built yet.";
                lblLocalSetupStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 150, 100));
            }
        }

        private void CheckRemoteStatusAsync()
        {
            lblRemoteStatus.Text = "• Remote GitHub: Querying version.json from " + RepoOwnerRepo + "...";
            lblRemoteStatus.Foreground = new SolidColorBrush(Color.FromRgb(140, 210, 255));

            ThreadPool.QueueUserWorkItem(_ =>
            {
                string err;
                UpdateInfo info = AppUpdater.FetchLatest(out err);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (info == null)
                    {
                        lblRemoteStatus.Text = "• Remote GitHub: Couldn't reach or no release published yet (" + err + ")";
                        lblRemoteStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 90));
                        return;
                    }

                    string engineExe = Path.Combine(projectDir, "MatrixWallpaperEngine.exe");
                    string localSha = File.Exists(engineExe) ? AppUpdater.FileSha256(engineExe) : "";
                    bool matches = !string.IsNullOrEmpty(localSha) && string.Equals(localSha, info.Sha256, StringComparison.OrdinalIgnoreCase);

                    if (matches)
                    {
                        lblRemoteStatus.Text = string.Format("• Remote GitHub: v{0} ({1:F2} MB) — IN SYNC WITH LOCAL ✔ (Bradley gets latest version)",
                            info.Version, info.Size / (1024.0 * 1024.0));
                        lblRemoteStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 170));
                    }
                    else
                    {
                        lblRemoteStatus.Text = string.Format("• Remote GitHub: v{0} (Remote SHA: {1}...)\r\n  ⚠ DIFFERENT FROM LOCAL BUILD. Click '1-Click Push to GitHub' to release!",
                            info.Version, info.Sha256.Substring(0, Math.Min(12, info.Sha256.Length)));
                        lblRemoteStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 205, 70));
                    }
                }));
            });
        }

        private void SetControlsEnabled(bool enabled)
        {
            isWorking = !enabled;
            btnBuild.IsEnabled = enabled;
            btnPush.IsEnabled = enabled;
            btnVerify.IsEnabled = enabled;
            btnRunEngine.IsEnabled = enabled;
            btnRunSetup.IsEnabled = enabled;
        }

        private int RunProcessSync(string fileName, string args, string workingDir, Action<string> onOutput)
        {
            ProcessStartInfo psi = new ProcessStartInfo(fileName, args)
            {
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process p = Process.Start(psi))
            {
                p.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) onOutput(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) onOutput("[ERR] " + e.Data); };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                return p.ExitCode;
            }
        }

        private void OnBuildClicked()
        {
            if (isWorking) return;
            SetControlsEnabled(false);

            string ver = txtVersion.Text.Trim();
            string notes = txtNotes.Text.Trim();

            lblStatusBanner.Text = "⏳ Building MatrixWallpaperEngine.exe & MatrixWallpaperSetup.exe...";
            lblStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 60));
            Log("==================================================");
            Log("STARTING COMPILATION (csc.exe v4.0.30319)");
            Log("Target Version: " + ver);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                bool success = false;
                try
                {
                    string wpfLib = Path.Combine(Path.GetDirectoryName(cscPath), "WPF");

                    // 1. Compile MatrixWallpaperEngine.exe
                    string engineOut = Path.Combine(projectDir, "MatrixWallpaperEngine.exe");
                    string engineArgs = string.Format(
                        "/target:winexe /out:\"{0}\" /lib:\"{1}\" " +
                        "/reference:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,PresentationCore.dll,PresentationFramework.dll,WindowsBase.dll,System.Xaml.dll " +
                        "/resource:\"{2}\\matrix_logo.ico\",matrix_logo.ico " +
                        "/resource:\"{2}\\matrix_logo.png\",matrix_logo.png " +
                        "/resource:\"{2}\\meme_pointing.png\",meme_pointing.png " +
                        "/resource:\"{2}\\meme_pointer_single.png\",meme_pointer_single.png " +
                        "/resource:\"{2}\\meme_shocked.png\",meme_shocked.png " +
                        "/resource:\"{2}\\meme_gem_soyjak.png\",meme_gem_soyjak.png " +
                        "/resource:\"{2}\\sample_demo.mp4\",sample_demo.mp4 " +
                        "\"{2}\\MatrixEngineCore.cs\" \"{2}\\MatrixUpdater.cs\" \"{2}\\MatrixStudioUI.cs\"",
                        engineOut, wpfLib, projectDir);

                    Log("Compiling MatrixWallpaperEngine.exe...");
                    int code1 = RunProcessSync(cscPath, engineArgs, projectDir, s => Log("  " + s));
                    if (code1 != 0)
                    {
                        Log("ERROR CS: MatrixWallpaperEngine compilation failed with code " + code1);
                        return;
                    }
                    Log("✔ MatrixWallpaperEngine.exe compiled successfully!");

                    // 2. Compile MatrixWallpaperSetup.exe
                    string setupOut = Path.Combine(projectDir, "MatrixWallpaperSetup.exe");
                    string setupArgs = string.Format(
                        "/target:winexe /out:\"{0}\" " +
                        "/resource:\"{1}\",MatrixWallpaperEngine.exe " +
                        "/resource:\"{2}\\matrix_logo.ico\",matrix_logo.ico " +
                        "/resource:\"{2}\\matrix_logo.png\",matrix_logo.png " +
                        "/resource:\"{2}\\meme_pointing.png\",meme_pointing.png " +
                        "/resource:\"{2}\\meme_pointer_single.png\",meme_pointer_single.png " +
                        "/resource:\"{2}\\meme_shocked.png\",meme_shocked.png " +
                        "/resource:\"{2}\\meme_gem_soyjak.png\",meme_gem_soyjak.png " +
                        "/resource:\"{2}\\sample_demo.mp4\",sample_demo.mp4 " +
                        "\"{2}\\MatrixInstaller.cs\"",
                        setupOut, engineOut, projectDir);

                    Log("Compiling MatrixWallpaperSetup.exe...");
                    int code2 = RunProcessSync(cscPath, setupArgs, projectDir, s => Log("  " + s));
                    if (code2 != 0)
                    {
                        Log("ERROR CS: MatrixWallpaperSetup compilation failed with code " + code2);
                        return;
                    }
                    Log("✔ MatrixWallpaperSetup.exe compiled successfully!");

                    // 3. Compute SHA-256 and create version.json
                    string sha256 = AppUpdater.FileSha256(engineOut);
                    long size = new FileInfo(engineOut).Length;

                    string json = string.Format(
                        "{{\r\n" +
                        "  \"version\": \"{0}\",\r\n" +
                        "  \"sha256\": \"{1}\",\r\n" +
                        "  \"exe\": \"MatrixWallpaperEngine.exe\",\r\n" +
                        "  \"size\": {2},\r\n" +
                        "  \"notes\": \"{3}\"\r\n" +
                        "}}\r\n",
                        ver, sha256.ToLowerInvariant(), size, notes.Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " "));

                    File.WriteAllText(Path.Combine(projectDir, "version.json"), json, Encoding.UTF8);
                    Log("✔ Generated version.json (SHA-256: " + sha256 + ", Size: " + size + " bytes)");
                    success = true;
                }
                catch (Exception ex)
                {
                    Log("EXCEPTION: " + ex.ToString());
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetControlsEnabled(true);
                        RefreshFileStatuses();
                        if (success)
                        {
                            lblStatusBanner.Text = "🎉 BUILD SUCCESSFUL! Both EXEs & version.json are ready to push to GitHub.";
                            lblStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 175));
                        }
                        else
                        {
                            lblStatusBanner.Text = "⚠ Build failed. See logs below for compiler details.";
                            lblStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(255, 140, 110));
                        }
                    }));
                }
            });
        }

        private void OnPushClicked()
        {
            if (isWorking) return;
            SetControlsEnabled(false);

            string ver = txtVersion.Text.Trim();
            string notes = txtNotes.Text.Trim();
            string commitMsg = string.Format("Release v{0}: {1}", ver, notes);

            lblStatusBanner.Text = "⏳ Staging files and pushing to GitHub (origin main)...";
            lblStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            Log("==================================================");
            Log("STARTING GITHUB PUBLISH (" + RepoOwnerRepo + ")");
            Log("Commit message: " + commitMsg);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                bool pushOk = false;
                try
                {
                    // 1. git add
                    Log("Running git add...");
                    RunProcessSync("git", "add version.json MatrixWallpaperEngine.exe MatrixWallpaperSetup.exe MatrixEngineCore.cs MatrixUpdater.cs MatrixStudioUI.cs MatrixInstaller.cs MatrixDashboard.cs", projectDir, s => Log("  " + s));

                    // 2. git commit
                    Log("Running git commit...");
                    int codeCommit = RunProcessSync("git", "commit -m \"" + commitMsg.Replace("\"", "\\\"") + "\"", projectDir, s => Log("  " + s));
                    Log("Git commit code: " + codeCommit);

                    // 3. git push origin main
                    Log("Running git push origin main...");
                    int codePush = RunProcessSync("git", "push origin main", projectDir, s => Log("  " + s));
                    if (codePush == 0)
                    {
                        Log("🎉 PUSH SUCCESSFUL! The update is live on GitHub!");
                        pushOk = true;
                    }
                    else
                    {
                        Log("⚠ Git push exited with code " + codePush);
                    }
                }
                catch (Exception ex)
                {
                    Log("GIT EXCEPTION: " + ex.ToString());
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetControlsEnabled(true);
                        CheckRemoteStatusAsync();
                        if (pushOk)
                        {
                            lblStatusBanner.Text = "🚀 PUBLISHED TO GITHUB! Bradley's laptop will automatically detect and install this release.";
                            lblStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 175));
                        }
                        else
                        {
                            lblStatusBanner.Text = "⚠ Git push encountered an issue. See logs below.";
                            lblStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 90));
                        }
                    }));
                }
            });
        }

        private void LaunchExe(string exeName)
        {
            try
            {
                string p = Path.Combine(projectDir, exeName);
                if (!File.Exists(p))
                {
                    MessageBox.Show(exeName + " is not built yet! Click 'BUILD BOTH EXEs' first.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Process.Start(new ProcessStartInfo(p) { WorkingDirectory = projectDir });
                Log("Launched " + exeName);
            }
            catch (Exception ex)
            {
                Log("Failed to launch " + exeName + ": " + ex.Message);
            }
        }
    }

    public static class DashboardProgram
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application app = new Application();
            DashboardWindow win = new DashboardWindow(args);
            app.Run(win);
        }
    }
}
