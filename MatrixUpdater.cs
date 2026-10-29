using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MatrixWallpaperEngine
{
    // What the developer published on GitHub (version.json in the repo root)
    public class UpdateInfo
    {
        public string Version = "";
        public string Sha256 = "";
        public string ExeName = "MatrixWallpaperEngine.exe";
        public string Notes = "";
        public long Size = 0;
    }

    // Tiny rolling log (%LocalAppData%\MatrixWallpaperEngine\app.log) so remote problems can be diagnosed
    public static class AppLog
    {
        private static readonly object gate = new object();

        public static void Write(string msg)
        {
            try
            {
                lock (gate)
                {
                    string p = Path.Combine(AppUpdater.DataDir, "app.log");
                    FileInfo fi = new FileInfo(p);
                    if (fi.Exists && fi.Length > 200 * 1024)
                    {
                        try { File.Delete(p); } catch { }
                    }
                    File.AppendAllText(p, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + "\r\n");
                }
            }
            catch { }
        }

        public static void Write(Exception ex)
        {
            Write(ex == null ? "(null exception)" : ex.ToString());
        }
    }

    // Safe, checksum-verified self-updater. No open ports, no passwords, nothing runs unless the
    // downloaded file's SHA-256 matches what version.json (in YOUR repo) says it should be.
    public static class AppUpdater
    {
        public const string Repo = "BrassyFrogy/matrix-wallpaper";
        public const string Branch = "main";
        public const string RepoUrl = "https://github.com/" + Repo;

        private static string cachedSelfSha = null;

        public static string RawBase
        {
            get { return "https://raw.githubusercontent.com/" + Repo + "/" + Branch + "/"; }
        }

        public static string DataDir
        {
            get
            {
                string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MatrixWallpaperEngine");
                try { if (!Directory.Exists(d)) Directory.CreateDirectory(d); } catch { }
                return d;
            }
        }

        public static string SelfPath
        {
            get
            {
                try { return Assembly.GetEntryAssembly().Location; }
                catch { return ""; }
            }
        }

        public static string ToHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }

        public static string Sha256Hex(byte[] data)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(data));
            }
        }

        public static string FileSha256(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (SHA256 sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(fs));
            }
        }

        // Hash of the running program (computed once and cached)
        public static string SelfSha()
        {
            if (cachedSelfSha == null)
            {
                try { cachedSelfSha = FileSha256(SelfPath); }
                catch { cachedSelfSha = ""; }
            }
            return cachedSelfSha;
        }

        private static void EnableTls()
        {
            try
            {
                // Tls (192) | Tls11 (768) | Tls12 (3072) — GitHub needs TLS 1.2 on older Windows
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)(192 | 768 | 3072);
                ServicePointManager.Expect100Continue = false;
            }
            catch { }
        }

        public static byte[] Download(string url, int timeoutMs, Action<int> progress)
        {
            EnableTls();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "MatrixWallpaperEngine-Updater/4.0";
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);
            req.Headers.Add("Cache-Control", "no-cache");

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream s = resp.GetResponseStream())
            using (MemoryStream ms = new MemoryStream())
            {
                long total = resp.ContentLength;
                long got = 0;
                byte[] buf = new byte[32768];
                int r;
                while ((r = s.Read(buf, 0, buf.Length)) > 0)
                {
                    ms.Write(buf, 0, r);
                    got += r;
                    if (got > 80L * 1024 * 1024) throw new IOException("File too large");
                    if (progress != null && total > 0) progress((int)(got * 100 / total));
                }
                return ms.ToArray();
            }
        }

        private static string JsonValue(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(?:\"((?:[^\"\\\\]|\\\\.)*)\"|([0-9]+))");
            if (!m.Success) return "";
            string v = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            return v.Replace("\\n", "\n").Replace("\\r", "").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        // Tiny request (~200 bytes) — the big EXE is only downloaded when the checksum actually differs.
        public static UpdateInfo FetchLatest(out string error)
        {
            error = "";
            try
            {
                string url = RawBase + "version.json?cb=" + DateTime.UtcNow.Ticks;
                string json = Encoding.UTF8.GetString(Download(url, 8000, null));

                UpdateInfo info = new UpdateInfo();
                info.Version = JsonValue(json, "version");
                info.Sha256 = JsonValue(json, "sha256").ToLowerInvariant();
                info.Notes = JsonValue(json, "notes");
                string exe = JsonValue(json, "exe");
                if (!string.IsNullOrEmpty(exe) && exe.IndexOf('/') < 0 && exe.IndexOf('\\') < 0) info.ExeName = exe;
                long size;
                if (long.TryParse(JsonValue(json, "size"), out size)) info.Size = size;

                if (info.Sha256.Length != 64)
                {
                    error = "The update info on GitHub is incomplete.";
                    return null;
                }
                return info;
            }
            catch (WebException wex)
            {
                HttpWebResponse r = wex.Response as HttpWebResponse;
                if (r != null && r.StatusCode == HttpStatusCode.NotFound)
                    error = "No update has been published yet.";
                else
                    error = "Couldn't reach the update server. Check your internet and try again.";
                AppLog.Write("FetchLatest: " + wex.Message);
                return null;
            }
            catch (Exception ex)
            {
                error = "Update check failed: " + ex.Message;
                AppLog.Write(ex);
                return null;
            }
        }

        public static bool IsNewer(UpdateInfo info)
        {
            if (info == null) return false;
            string self = SelfSha();
            if (string.IsNullOrEmpty(self)) return false;
            return !string.Equals(info.Sha256, self, StringComparison.OrdinalIgnoreCase);
        }

        // Downloads, verifies the checksum, then swaps the running EXE (Windows lets you rename a running EXE).
        public static bool DownloadAndInstall(UpdateInfo info, Action<int> progress, out string error)
        {
            error = "";
            string self = SelfPath;
            string tmp = self + ".new";
            string old = self + ".old.exe";
            try
            {
                byte[] data = Download(RawBase + info.ExeName + "?cb=" + DateTime.UtcNow.Ticks, 60000, progress);
                if (data.Length < 65536 || data[0] != (byte)'M' || data[1] != (byte)'Z')
                {
                    error = "The downloaded file wasn't a valid program.";
                    return false;
                }
                if (!string.Equals(Sha256Hex(data), info.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    error = "The download was corrupted. Please try again in a minute.";
                    return false;
                }

                File.WriteAllBytes(tmp, data);
                try { if (File.Exists(old)) File.Delete(old); } catch { }
                File.Move(self, old);
                try
                {
                    File.Move(tmp, self);
                }
                catch
                {
                    try { File.Move(old, self); } catch { }
                    throw;
                }
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Write(ex);
                error = "Couldn't install the update: " + ex.Message;
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                return false;
            }
        }

        public static void RestartSelf()
        {
            string self = SelfPath;
            ProcessStartInfo psi = new ProcessStartInfo(self, "--updated");
            psi.WorkingDirectory = Path.GetDirectoryName(self);
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        public static void CleanupLeftovers()
        {
            try
            {
                string self = SelfPath;
                if (string.IsNullOrEmpty(self)) return;
                try { if (File.Exists(self + ".old.exe")) File.Delete(self + ".old.exe"); } catch { }
                try { if (File.Exists(self + ".new")) File.Delete(self + ".new"); } catch { }
            }
            catch { }
        }
    }
}
