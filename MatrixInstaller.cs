using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MatrixWallpaperInstaller
{
    public class InstallerForm : Form
    {
        private CheckBox chkDesktopShortcut;
        private CheckBox chkStartMenuShortcut;
        private CheckBox chkLaunchNow;
        private CheckBox chkRunOnStartup;
        private TextBox txtInstallDir;
        private Button btnInstall;
        private Button btnUninstall;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Icon appIcon;

        public InstallerForm(string screenshotPath)
        {
            try { SetProcessDPIAware(); } catch { }
            LoadEmbeddedIcon();
            InitializeComponent();

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                Timer t = new Timer { Interval = 900 };
                t.Tick += (s, e) =>
                {
                    t.Stop();
                    try
                    {
                        using (Bitmap bmp = new Bitmap(this.Width, this.Height))
                        {
                            this.DrawToBitmap(bmp, new Rectangle(0, 0, this.Width, this.Height));
                            bmp.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    catch { }
                    Application.Exit();
                };
                t.Start();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private void LoadEmbeddedIcon()
        {
            try
            {
                appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null) this.Icon = appIcon;
            }
            catch { }
        }

        private Image LoadEmbeddedLogoPng()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream("matrix_logo.png"))
                {
                    if (s != null)
                    {
                        using (Image raw = Image.FromStream(s))
                        {
                            Bitmap clean = new Bitmap(raw.Width, raw.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                            using (Graphics g = Graphics.FromImage(clean))
                            {
                                g.DrawImage(raw, 0, 0, raw.Width, raw.Height);
                            }
                            return clean;
                        }
                    }
                }
            }
            catch { }
            return appIcon != null ? appIcon.ToBitmap() : null;
        }

        private void InitializeComponent()
        {
            this.Text = "Matrix & ASCII Fluid Wallpaper Engine — Setup";
            this.Size = new Size(560, 470);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(13, 17, 24);
            this.ForeColor = Color.FromArgb(235, 242, 248);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Top Banner
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(19, 26, 36)
            };

            PictureBox logoBox = new PictureBox
            {
                Location = new Point(22, 16),
                Size = new Size(52, 52),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = LoadEmbeddedLogoPng()
            };

            Label lblTitle = new Label
            {
                Text = "MATRIX & ASCII FLUID WALLPAPER",
                Location = new Point(88, 16),
                Size = new Size(440, 26),
                Font = new Font("Segoe UI", 14.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 140),
                UseMnemonic = false
            };

            Label lblSub = new Label
            {
                Text = "Easy 1-Click Installer — Ready for Any Windows PC",
                Location = new Point(90, 44),
                Size = new Size(440, 22),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(165, 188, 210),
                UseMnemonic = false
            };

            header.Controls.Add(logoBox);
            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSub);
            this.Controls.Add(header);

            int y = 104;

            Label lblFolder = new Label
            {
                Text = "Install Location:",
                Location = new Point(26, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(190, 208, 225)
            };
            this.Controls.Add(lblFolder);
            y += 24;

            string defaultDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MatrixWallpaperEngine");

            txtInstallDir = new TextBox
            {
                Text = defaultDir,
                Location = new Point(26, y),
                Size = new Size(405, 27),
                BackColor = Color.FromArgb(22, 29, 40),
                ForeColor = Color.FromArgb(0, 245, 140),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtInstallDir);

            Button btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(439, y - 1),
                Size = new Size(85, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 40, 54),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(65, 85, 110);
            btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog { SelectedPath = txtInstallDir.Text })
                {
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                        txtInstallDir.Text = fbd.SelectedPath;
                }
            };
            this.Controls.Add(btnBrowse);
            y += 42;

            chkDesktopShortcut = new CheckBox
            {
                Text = "Create a Desktop Shortcut (Recommended)",
                Location = new Point(28, y),
                Size = new Size(480, 24),
                Checked = true,
                ForeColor = Color.White
            };
            this.Controls.Add(chkDesktopShortcut);
            y += 30;

            chkStartMenuShortcut = new CheckBox
            {
                Text = "Add to Windows Start Menu Programs",
                Location = new Point(28, y),
                Size = new Size(480, 24),
                Checked = true,
                ForeColor = Color.White
            };
            this.Controls.Add(chkStartMenuShortcut);
            y += 30;

            chkRunOnStartup = new CheckBox
            {
                Text = "Start automatically when Windows logs in (Optional)",
                Location = new Point(28, y),
                Size = new Size(480, 24),
                Checked = false,
                ForeColor = Color.FromArgb(200, 215, 230)
            };
            this.Controls.Add(chkRunOnStartup);
            y += 30;

            chkLaunchNow = new CheckBox
            {
                Text = "Launch Matrix Wallpaper Engine right after installing",
                Location = new Point(28, y),
                Size = new Size(480, 24),
                Checked = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 245, 145)
            };
            this.Controls.Add(chkLaunchNow);
            y += 38;

            progressBar = new ProgressBar
            {
                Location = new Point(26, y),
                Size = new Size(498, 16),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            this.Controls.Add(progressBar);
            y += 22;

            lblStatus = new Label
            {
                Text = "Ready to install. Click 'INSTALL NOW' below.",
                Location = new Point(26, y),
                Size = new Size(498, 22),
                ForeColor = Color.FromArgb(150, 200, 245)
            };
            this.Controls.Add(lblStatus);
            y += 30;

            btnInstall = new Button
            {
                Text = "⚡  INSTALL NOW (1-CLICK)",
                Location = new Point(26, y),
                Size = new Size(355, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 175, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 145);
            btnInstall.Click += OnInstallClick;
            this.Controls.Add(btnInstall);

            btnUninstall = new Button
            {
                Text = "Uninstall",
                Location = new Point(394, y),
                Size = new Size(130, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 48, 60),
                ForeColor = Color.FromArgb(220, 225, 235),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnUninstall.FlatAppearance.BorderColor = Color.FromArgb(75, 90, 110);
            btnUninstall.Click += OnUninstallClick;
            this.Controls.Add(btnUninstall);
        }

        private void ExtractEmbeddedResource(string resourceName, string outPath)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(resourceName))
            {
                if (s == null)
                    throw new Exception("Missing embedded payload: " + resourceName);
                using (FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
                {
                    s.CopyTo(fs);
                }
            }
        }

        private void CreateShortcut(string shortcutPath, string targetExe, string workDir, string iconPath, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                Type scType = shortcut.GetType();
                scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetExe });
                scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workDir });
                scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    scType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconPath });
                }
                scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            catch { }
        }

        private void StopRunningWallpaperProcesses()
        {
            try
            {
                Process[] running = Process.GetProcessesByName("MatrixWallpaperEngine");
                for (int i = 0; i < running.Length; i++)
                {
                    try
                    {
                        running[i].Kill();
                        running[i].WaitForExit(1500);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void PropagateGitHubChannelToInstalledApp(string targetDir, string installedExePath)
        {
            try
            {
                string setupExe = Application.ExecutablePath;
                if (!File.Exists(setupExe)) return;
                using (FileStream fs = new FileStream(setupExe, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Length <= 256) return;
                    fs.Seek(-256, SeekOrigin.End);
                    byte[] trailer = new byte[256];
                    int r = fs.Read(trailer, 0, 256);
                    if (r == 256 &&
                        trailer[0] == (byte)'M' && trailer[1] == (byte)'W' &&
                        trailer[2] == (byte)'P' && trailer[3] == (byte)'_' &&
                        trailer[4] == (byte)'G' && trailer[5] == (byte)'H' &&
                        trailer[6] == (byte)'0' && trailer[7] == (byte)'1')
                    {
                        int len = 0;
                        while (len < 248 && trailer[8 + len] != 0) len++;
                        if (len > 0)
                        {
                            string url = System.Text.Encoding.UTF8.GetString(trailer, 8, len).Trim();
                            File.WriteAllText(Path.Combine(targetDir, "github_channel.cfg"), url, System.Text.Encoding.UTF8);
                            using (FileStream outFs = new FileStream(installedExePath, FileMode.Append, FileAccess.Write))
                            {
                                outFs.Write(trailer, 0, 256);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void OnInstallClick(object sender, EventArgs e)
        {
            try
            {
                btnInstall.Enabled = false;
                StopRunningWallpaperProcesses();
                string targetDir = txtInstallDir.Text.Trim();
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                progressBar.Value = 20;
                lblStatus.Text = "Extracting application files...";
                Application.DoEvents();

                string exePath = Path.Combine(targetDir, "MatrixWallpaperEngine.exe");
                string icoPath = Path.Combine(targetDir, "matrix_logo.ico");
                string pngPath = Path.Combine(targetDir, "matrix_logo.png");
                string mp4Path = Path.Combine(targetDir, "sample_demo.mp4");

                ExtractEmbeddedResource("MatrixWallpaperEngine.exe", exePath);
                PropagateGitHubChannelToInstalledApp(targetDir, exePath);
                progressBar.Value = 45;
                ExtractEmbeddedResource("matrix_logo.ico", icoPath);
                ExtractEmbeddedResource("matrix_logo.png", pngPath);
                ExtractEmbeddedResource("meme_pointing.png", Path.Combine(targetDir, "meme_pointing.png"));
                ExtractEmbeddedResource("meme_shocked.png", Path.Combine(targetDir, "meme_shocked.png"));
                ExtractEmbeddedResource("meme_pointer_single.png", Path.Combine(targetDir, "meme_pointer_single.png"));
                ExtractEmbeddedResource("meme_gem_soyjak.png", Path.Combine(targetDir, "meme_gem_soyjak.png"));
                progressBar.Value = 80;
                ExtractEmbeddedResource("sample_demo.mp4", mp4Path);

                if (chkDesktopShortcut.Checked)
                {
                    string desktopLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Matrix Wallpaper Engine.lnk");
                    CreateShortcut(desktopLink, exePath, targetDir, icoPath, "Matrix & ASCII Fluid Wallpaper Engine");
                }

                if (chkStartMenuShortcut.Checked)
                {
                    string startMenuLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Matrix Wallpaper Engine.lnk");
                    CreateShortcut(startMenuLink, exePath, targetDir, icoPath, "Matrix & ASCII Fluid Wallpaper Engine");
                }

                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null)
                    {
                        if (chkRunOnStartup.Checked)
                            runKey.SetValue("MatrixWallpaperEngine", "\"" + exePath + "\"");
                        else
                            runKey.DeleteValue("MatrixWallpaperEngine", false);
                    }
                }

                progressBar.Value = 100;
                lblStatus.Text = "✅ Installed successfully to " + targetDir;
                lblStatus.ForeColor = Color.FromArgb(0, 255, 145);

                if (chkLaunchNow.Checked)
                {
                    ProcessStartInfo psi = new ProcessStartInfo(exePath) { WorkingDirectory = targetDir };
                    Process.Start(psi);
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show(this, "Matrix & ASCII Fluid Wallpaper Engine has been installed!\nYou can open it anytime from your Desktop or Start Menu.", "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnInstall.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                btnInstall.Enabled = true;
                lblStatus.Text = "Error: " + ex.Message;
                lblStatus.ForeColor = Color.FromArgb(255, 95, 105);
            }
        }

        private void OnUninstallClick(object sender, EventArgs e)
        {
            try
            {
                StopRunningWallpaperProcesses();
                string targetDir = txtInstallDir.Text.Trim();
                string desktopLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Matrix Wallpaper Engine.lnk");
                string startMenuLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Matrix Wallpaper Engine.lnk");

                if (File.Exists(desktopLink)) File.Delete(desktopLink);
                if (File.Exists(startMenuLink)) File.Delete(startMenuLink);

                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null) runKey.DeleteValue("MatrixWallpaperEngine", false);
                }

                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }

                progressBar.Value = 100;
                lblStatus.Text = "Uninstalled cleanly.";
                MessageBox.Show(this, "Matrix Wallpaper Engine and its shortcuts have been removed.", "Uninstalled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Uninstall note: " + ex.Message;
            }
        }
    }

    static class SetupProgram
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => { };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => { };
            }
            catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string shot = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--screenshot" && i + 1 < args.Length)
                    shot = args[++i];
            }
            Application.Run(new InstallerForm(shot));
        }
    }
}
