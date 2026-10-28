// CyberChef offline launcher (folder mode).
// Serves the folder that contains this .exe over a loopback HTTP server and opens
// the CyberChef standalone HTML build in an app-mode (or default) browser window.
//
// Why a launcher instead of double-clicking the .html?
//   The standalone build needs to be loaded over http:// for Web Workers / WASM
//   (OCR, compression, image ops) to work reliably; file:// is blocked by most
//   Chromium-based browsers for worker scripts.
//
// Build (no SDK needed, .NET Framework 4.x ships with Windows 10/11):
//   %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /platform:anycpu ^
//       /optimize+ /codepage:65001 /out:CyberChef_launcher.exe src\CyberChef_launcher.cs
//
// Runtime deps: .NET Framework 4.x (built in) + any browser (Edge/Chrome preferred).
// No network access, no admin rights, no installation.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static class Program
{
    private const string Title = "CyberChef Offline Launcher";
    private const string LauncherVersion = "1.2.0";

    // Same preference order as the classic Thyme-13/CyberChef_launcher project.
    private static readonly int[] PreferredPorts = new int[] { 8000, 5500, 8080, 8888, 3000 };

    private static string _root;
    private static string _entry;   // file name of the CyberChef html, e.g. CyberChef_v11.5.0.html

    // "default" = a normal tab in the system default browser (most reliable everywhere).
    // "app"     = a chrome-less Edge/Chrome --app window (opt-in via --browser=app).
    private static string _browserMode = "default";

    // Single-instance guard: a second double-click just re-opens the running page
    // instead of starting a second server and a second browser profile.
    private static Mutex _instanceMutex;

    private static void Main(string[] args)
    {
        bool noOpen = false;
        int forcedPort = 0;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (string.Equals(a, "--no-open", StringComparison.OrdinalIgnoreCase)) noOpen = true;
            else if (a.StartsWith("--port=", StringComparison.OrdinalIgnoreCase))
            {
                int p;
                if (int.TryParse(a.Substring(7), out p) && p > 0 && p < 65536) forcedPort = p;
            }
            else if (a.StartsWith("--browser=", StringComparison.OrdinalIgnoreCase))
            {
                _browserMode = string.Equals(a.Substring(10), "default", StringComparison.OrdinalIgnoreCase)
                    ? "default" : "app";
            }
            else if (string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Usage: CyberChef_launcher.exe [--port=N] [--no-open] [--browser=default|app]");
                Console.WriteLine("  --port=N           bind 127.0.0.1:N (default: first free of 8000,5500,8080,8888,3000)");
                Console.WriteLine("  --no-open          start the server only, do not open a browser");
                Console.WriteLine("  --browser=default  open a normal tab in the default browser (default)");
                Console.WriteLine("  --browser=app      open a chrome-less Edge/Chrome window instead");
                return;
            }
        }

        Console.Title = Title;
        try
        {
            _root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            _entry = FindEntryHtml(_root);

            if (_entry == null)
            {
                Fail("CyberChef html file not found next to this launcher.\n\n" +
                     "Folder: " + _root + "\n\n" +
                     "The launcher must stay in the same folder as CyberChef_vX.Y.Z.html,\n" +
                     "assets/, modules/ and images/.");
                return;
            }

            string previousUrl = AcquireSingleInstance();
            if (previousUrl != null)
            {
                if (previousUrl.Length > 0)
                {
                    Console.WriteLine("  CyberChef Offline Launcher is already running.");
                    Console.WriteLine("  URL   : " + previousUrl);
                    if (!noOpen) OpenDefaultBrowser(previousUrl);
                }
                else
                {
                    Console.WriteLine("  CyberChef Offline Launcher is already running,");
                    Console.WriteLine("  but its URL is unknown. Close the other window and try again.");
                }
                return;
            }

            int port = StartServer(forcedPort);
            string url = "http://127.0.0.1:" + port + "/" + Uri.EscapeDataString(_entry);

            Console.WriteLine("============================================================");
            Console.WriteLine("  " + Title + "  v" + LauncherVersion);
            Console.WriteLine("============================================================");
            Console.WriteLine("  URL   : " + url);
            Console.WriteLine("  Files : " + _root);
            Console.WriteLine("  Entry : " + _entry);
            Console.WriteLine();
            Console.WriteLine("  Keep THIS window open, close it to stop the server.");
            Console.WriteLine("  (Ctrl+C also stops it.)");
            Console.WriteLine("============================================================");
            Console.WriteLine();

            if (!noOpen) OpenBrowser(url);
            else Console.WriteLine("  --no-open: open the URL above manually.");

            for (;;) Thread.Sleep(60000);
        }
        catch (Exception ex)
        {
            Fail("Unexpected error:\n\n" + ex.Message);
        }
    }

    private static void Fail(string message)
    {
        Console.WriteLine();
        Console.WriteLine("ERROR: " + message);
        Console.WriteLine();
        try
        {
            Console.Write("Press Enter to exit...");
            Console.ReadLine();
        }
        catch { }
    }

    // -------------------------------------------------------------- file lookup

    private static string FindEntryHtml(string dir)
    {
        // Exact legacy names first, then the highest-versioned CyberChef_*.html found.
        string[] preferred = new string[] { "index.html", "CyberChef.html" };
        for (int i = 0; i < preferred.Length; i++)
        {
            string p = Path.Combine(dir, preferred[i]);
            if (File.Exists(p)) return preferred[i];
        }

        string[] candidates = Directory.GetFiles(dir, "CyberChef*.html");
        string best = null;
        Version bestVer = null;

        for (int i = 0; i < candidates.Length; i++)
        {
            string name = Path.GetFileName(candidates[i]);
            Version v = ParseVersion(name);
            if (best == null || CompareVersion(v, bestVer) > 0)
            {
                best = name;
                bestVer = v;
            }
        }

        return best;
    }

    private static Version ParseVersion(string fileName)
    {
        // CyberChef_v11.5.0.html -> 11.5.0 ; anything unparseable sorts lowest.
        int i = fileName.IndexOf('v');
        if (i < 0) return new Version(0, 0);
        int start = i + 1;
        int end = start;
        while (end < fileName.Length &&
               (char.IsDigit(fileName[end]) || fileName[end] == '.')) end++;
        string num = fileName.Substring(start, end - start).Trim('.');
        if (num.Length == 0) return new Version(0, 0);
        try { return new Version(num); }
        catch { return new Version(0, 0); }
    }

    private static int CompareVersion(Version a, Version b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;
        return a.CompareTo(b);
    }

    // -------------------------------------------------------------- http server

    private static int StartServer(int forcedPort)
    {
        TcpListener listener = null;

        if (forcedPort > 0)
        {
            listener = new TcpListener(IPAddress.Loopback, forcedPort);
            try { listener.Start(); }
            catch (Exception ex)
            {
                throw new Exception("Port " + forcedPort + " is not available: " + ex.Message);
            }
        }
        else
        {
            for (int i = 0; i < PreferredPorts.Length && listener == null; i++)
            {
                try
                {
                    TcpListener l = new TcpListener(IPAddress.Loopback, PreferredPorts[i]);
                    l.Start();
                    listener = l;
                }
                catch { /* try next port */ }
            }

            if (listener == null)
            {
                // Every preferred port is busy: let the OS pick a free one.
                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
            }
        }

        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        Thread t = new Thread(delegate() { AcceptLoop(listener); });
        t.IsBackground = true;
        t.Start();

        return port;
    }

    private static void AcceptLoop(TcpListener listener)
    {
        for (;;)
        {
            TcpClient client;
            try { client = listener.AcceptTcpClient(); }
            catch { continue; }

            Thread th = new Thread(delegate() { HandleClient(client); });
            th.IsBackground = true;
            th.Start();
        }
    }

    private static void HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            {
                client.ReceiveTimeout = 15000;
                client.SendTimeout = 60000;

                NetworkStream ns = client.GetStream();

                string requestLine = ReadLine(ns);
                if (string.IsNullOrEmpty(requestLine)) return;

                string h;
                while ((h = ReadLine(ns)) != null && h.Length > 0) { }

                string[] parts = requestLine.Split(' ');
                if (parts.Length < 3) { WriteText(ns, "400 Bad Request", "bad request"); return; }

                string method = parts[0].ToUpperInvariant();
                string target = parts[1];

                if (method != "GET" && method != "HEAD")
                {
                    WriteText(ns, "405 Method Not Allowed", "method not allowed");
                    return;
                }

                int cut = target.IndexOf('?');
                if (cut >= 0) target = target.Substring(0, cut);
                cut = target.IndexOf('#');
                if (cut >= 0) target = target.Substring(0, cut);

                try { target = Uri.UnescapeDataString(target); }
                catch { /* keep raw target */ }

                if (target.Length == 0 || target == "/") target = "/" + _entry;
                if (target.StartsWith("/", StringComparison.Ordinal)) target = target.Substring(1);
                target = target.Replace('/', Path.DirectorySeparatorChar);

                string full = Path.GetFullPath(Path.Combine(_root, target));
                string rootFull = Path.GetFullPath(_root) + Path.DirectorySeparatorChar;
                if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(full, Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
                {
                    WriteText(ns, "403 Forbidden", "forbidden");
                    return;
                }

                if (!File.Exists(full))
                {
                    WriteText(ns, "404 Not Found", "not found: " + target);
                    return;
                }

                byte[] body = File.ReadAllBytes(full);
                WriteResponse(ns, "200 OK", ContentTypeFor(Path.GetExtension(full)), body, method == "HEAD");
            }
        }
        catch
        {
            // client went away / timeout: ignore
        }
    }

    private static string ReadLine(Stream s)
    {
        List<byte> buf = new List<byte>(128);
        int b;
        while ((b = s.ReadByte()) != -1)
        {
            if (b == 10) break;
            if (b != 13) buf.Add((byte)b);
            if (buf.Count > 16384) break;
        }
        if (b == -1 && buf.Count == 0) return null;
        return Encoding.UTF8.GetString(buf.ToArray());
    }

    private static void WriteText(Stream s, string status, string text)
    {
        WriteResponse(s, status, "text/plain; charset=utf-8", Encoding.UTF8.GetBytes(text), false);
    }

    private static void WriteResponse(Stream s, string status, string contentType, byte[] body, bool headOnly)
    {
        string header =
            "HTTP/1.1 " + status + "\r\n" +
            "Content-Type: " + contentType + "\r\n" +
            "Content-Length: " + body.Length + "\r\n" +
            "Cache-Control: max-age=600\r\n" +
            "Connection: close\r\n\r\n";

        byte[] hb = Encoding.ASCII.GetBytes(header);
        s.Write(hb, 0, hb.Length);
        if (!headOnly && body.Length > 0) s.Write(body, 0, body.Length);
        s.Flush();
    }

    private static string ContentTypeFor(string ext)
    {
        switch (ext.ToLowerInvariant())
        {
            case ".html": case ".htm": return "text/html; charset=utf-8";
            case ".js": return "application/javascript; charset=utf-8";
            case ".mjs": return "application/javascript; charset=utf-8";
            case ".css": return "text/css; charset=utf-8";
            case ".json": return "application/json; charset=utf-8";
            case ".map": return "application/json; charset=utf-8";
            case ".png": return "image/png";
            case ".jpg": case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".svg": return "image/svg+xml";
            case ".ico": return "image/x-icon";
            case ".bmp": return "image/bmp";
            case ".webp": return "image/webp";
            case ".ttf": return "font/ttf";
            case ".otf": return "font/otf";
            case ".woff": return "font/woff";
            case ".woff2": return "font/woff2";
            case ".wasm": return "application/wasm";
            case ".gz": return "application/gzip";
            case ".zip": return "application/zip";
            case ".txt": case ".md": return "text/plain; charset=utf-8";
            default: return "application/octet-stream";
        }
    }

    // ------------------------------------------------------------------ browser

    private static void OpenBrowser(string url)
    {
        if (_browserMode == "default")
        {
            OpenDefaultBrowser(url);
            return;
        }

        string browser = FindBrowser();
        if (browser == null)
        {
            Console.WriteLine("  No Edge/Chrome found - falling back to the default browser.");
            OpenDefaultBrowser(url);
            return;
        }

        // A private --user-data-dir keeps this launcher out of the user's regular
        // browser profile. That also avoids the Chromium "process singleton" clash
        // when the user's normal browser is already running with a different
        // integrity level (classic "现有实例正在以提升的权限运行" dialog).
        string profile = GetPrivateProfileDir(browser);
        try { Directory.CreateDirectory(profile); }
        catch (Exception ex) { Console.WriteLine("  (private profile dir: " + ex.Message + ")"); }

        // A force-killed browser can leave a stale singleton lockfile behind, which makes
        // the next Chromium start abort with "Lock file can not be created".
        try
        {
            string lockFile = Path.Combine(profile, "lockfile");
            if (File.Exists(lockFile)) File.Delete(lockFile);
        }
        catch { }

        Console.WriteLine("  Browser: " + browser);
        Console.WriteLine("  Profile: " + profile);

        Process p = null;
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = browser;
            psi.Arguments =
                "--app=\"" + url + "\" " +
                "--user-data-dir=\"" + profile + "\" " +
                "--no-first-run --no-default-browser-check --window-size=1500,950";
            psi.UseShellExecute = false;
            p = Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine("  Could not start " + Path.GetFileName(browser) + ": " + ex.Message);
        }

        if (p != null && WaitForCyberChefWindow(9000))
        {
            Console.WriteLine("  App window opened.");
            return;
        }

        Console.WriteLine("  App-mode window did not appear - opening a normal browser tab instead.");
        OpenDefaultBrowser(url);
    }

    private static void OpenDefaultBrowser(string url)
    {
        try
        {
            Process.Start(url);
            Console.WriteLine("  Opened the URL in the default browser.");
        }
        catch
        {
            Console.WriteLine("  Could not start a browser. Open this URL manually:");
            Console.WriteLine("    " + url);
        }
    }

    private static bool WaitForCyberChefWindow(int timeoutMs)
    {
        int waited = 0;
        while (waited < timeoutMs)
        {
            if (FindCyberChefWindow()) return true;
            Thread.Sleep(250);
            waited += 250;
        }
        return FindCyberChefWindow();
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private static bool FindCyberChefWindow()
    {
        bool found = false;
        try
        {
            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd)) return true;

                // Only Chromium content windows count. This deliberately ignores our own
                // console window ("CyberChef Offline Launcher"), which also contains the
                // word "CyberChef" and used to make the launcher report success even when
                // the browser had already aborted.
                StringBuilder cls = new StringBuilder(120);
                GetClassName(hWnd, cls, cls.Capacity);
                if (!string.Equals(cls.ToString(), "Chrome_WidgetWin_1", StringComparison.Ordinal))
                    return true;

                StringBuilder sb = new StringBuilder(300);
                GetWindowText(hWnd, sb, sb.Capacity);
                if (sb.ToString().IndexOf("CyberChef", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = true;
                    return false;   // stop enumerating
                }
                return true;
            }, IntPtr.Zero);
        }
        catch { }
        return found;
    }

    // Returns null when this process became the single instance (and now owns the mutex),
    // otherwise the URL of the already-running instance (or "" when that URL is unknown).
    private static string AcquireSingleInstance()
    {
        try
        {
            bool createdNew;
            _instanceMutex = new Mutex(true, @"Local\CyberChefOfflineLauncher", out createdNew);
            if (createdNew) return null;

            string url = ProbeRunningServer();
            return url == null ? "" : url;
        }
        catch
        {
            return null;   // if the mutex cannot be created, keep working anyway
        }
    }

    // Looks for an already-running launcher by probing the ports we normally bind.
    // Safer than a state file (which can be unwritable or stale).
    private static string ProbeRunningServer()
    {
        for (int i = 0; i < PreferredPorts.Length; i++)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult ar = client.BeginConnect(IPAddress.Loopback, PreferredPorts[i], null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(300);
                    if (!ok) continue;
                    client.EndConnect(ar);
                    return "http://127.0.0.1:" + PreferredPorts[i] + "/" + Uri.EscapeDataString(_entry);
                }
            }
            catch { }
        }

        return null;
    }

    private static string GetPrivateProfileDir(string browser)
    {
        string la = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        la = string.IsNullOrEmpty(la)
            ? Path.Combine(Path.GetTempPath(), "CyberChefOffline")
            : Path.Combine(la, "CyberChefOffline");

        return Path.Combine(la, "browser-profile-" + Path.GetFileNameWithoutExtension(browser).ToLowerInvariant());
    }

    private static string FindBrowser()
    {
        string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string la = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string[] candidates = new string[]
        {
            Path.Combine(pf86, "Microsoft\\Edge\\Application\\msedge.exe"),
            Path.Combine(pf, "Microsoft\\Edge\\Application\\msedge.exe"),
            Path.Combine(pf, "Google\\Chrome\\Application\\chrome.exe"),
            Path.Combine(pf86, "Google\\Chrome\\Application\\chrome.exe"),
            Path.Combine(la, "Google\\Chrome\\Application\\chrome.exe")
        };

        for (int i = 0; i < candidates.Length; i++)
            if (File.Exists(candidates[i])) return candidates[i];

        return null;
    }
}
