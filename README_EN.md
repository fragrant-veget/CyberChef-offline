# CyberChef Offline · One-Click Offline CyberChef

[简体中文](README.md) | **English**

A **portable Windows offline bundle + one-click launcher** for [CyberChef](https://github.com/gchq/CyberChef).
Download, extract, double-click `CyberChef_launcher.exe`, and the full CyberChef opens in your browser — **no internet, no installs, no Python / Node / any runtime required**.

---

## Download

Grab it from the [**Releases**](../../releases/latest) page:

| File | Size | Description |
|---|---|---|
| `CyberChef_v11.5.0_offline_win64.zip` | ~24 MB (~58 MB extracted) | Full offline bundle (CyberChef v11.5.0 + launcher) |

- **Requirements**: Windows 10 / 11 x64 (`.NET Framework 4.x` is built into Windows), plus Edge or Chrome

## Quick Start

1. Extract the zip — you get a `CyberChef_v11.5.0_offline_win64\` folder
2. Double-click **`CyberChef_launcher.exe`** inside it
3. A console window appears (showing `http://127.0.0.1:8000/...`) and CyberChef opens automatically
4. Close the console window when you are done — that stops the local server

```
CyberChef_v11.5.0_offline_win64\
├─ CyberChef_launcher.exe      ← double-click this
├─ CyberChef_v11.5.0.html      ← app entry point (do not copy it out alone)
├─ assets\  modules\  images\  ← must stay next to the exe
├─ README.txt                  ← end-user notes (Chinese)
└─ LICENSE-CyberChef.txt       ← Apache-2.0
```

## What the Launcher Does

```
CyberChef_launcher.exe
  ├─ scans its own directory for CyberChef_*.html (picks the highest version)
  ├─ starts a read-only static HTTP server on 127.0.0.1 (8000 → 5500 → 8080 → 8888 → 3000, random if all are taken)
  └─ opens http://127.0.0.1:<port>/CyberChef_vX.Y.Z.html in your default browser
```

Design notes:

- **Opens a tab in your default browser by default** — no Chromium app window, no single-instance lock clash, works inside the Edge/Chrome you already have open
- **Binds `127.0.0.1` only** — never exposed to the LAN, no inbound firewall rule
- The launcher is a **single 24 KB executable** built against `.NET Framework 4.x` (runtime ships with Windows; icon embedded from `src/CyberChef_launcher.ico`). Source lives in `src/`
- **Single instance**: a second double-click just reopens the running URL instead of starting a second server
- The server is a raw `TcpListener` (not `HttpListener`), so it needs **no administrator rights** and no URL ACL
- Uses `http://` on purpose instead of `file://`: CyberChef's OCR / image / large-file operations rely on Web Workers + WASM, which most Chromium builds block under `file://`
- Options:
  - `--port=9000` pin the port
  - `--no-open` start the server only, do not open a browser
  - `--browser=app` use the Edge/Chrome `--app` window instead (runs in its own profile dir `%LOCALAPPDATA%\CyberChefOffline\browser-profile-*`, for a more "standalone app" feel)

## Build It Yourself

Only the .NET Framework compiler that ships with Windows is needed — no Visual Studio, no .NET SDK:

```powershell
# 1) Build the launcher (output: bin\CyberChef_launcher.exe)
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Target launcher

# 2) Unpack the official CyberChef release zip into out\, then package the bundle
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Target package -CyberChefDir .\out\CyberChef
```

Or compile manually:

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /platform:anycpu /optimize+ ^
    /codepage:65001 /win32icon:src\CyberChef_launcher.ico ^
    /out:CyberChef_launcher.exe src\CyberChef_launcher.cs
```

## Updating to a Newer CyberChef

1. Download and extract the newer official zip from [CyberChef Releases](https://github.com/gchq/CyberChef/releases)
2. Delete the `*.gz` / `*.br` files in it (**keep `assets/tesseract/lang-data/*.traineddata.gz`**)
3. Drop `CyberChef_launcher.exe` into that folder — no renaming needed: the launcher picks the highest `CyberChef_*.html` version automatically

## FAQ

<details>
<summary>Console window flashes and closes / "HTML not found"</summary>

The launcher cannot find `CyberChef_*.html` — usually because the exe was copied out on its own. Keep the exe in the same folder as the html, `assets`, `modules` and `images`.
</details>

<details>
<summary>Browser says "This site can't be reached"</summary>

Wait until the console prints the URL, then reload — or open `http://127.0.0.1:<port>/...` from that window manually. If the port is busy the launcher automatically tries the next one.
</details>

<details>
<summary>No Edge installed — can I just double-click the html?</summary>

It opens, but using the launcher is still recommended: some Web Worker / WASM operations are blocked by Chromium's security policy under `file://`.
If Edge is missing, the launcher falls back to the system default browser.
</details>

<details>
<summary>Windows shows "Open File - Security Warning / unknown publisher", or double-click seems to do nothing</summary>

That is the Mark-of-the-Web (MOTW) tag Windows puts on files extracted from a downloaded zip.
Either right-click the **zip** → Properties → check **Unblock** → OK, and then extract; or, after extracting, run
`Get-ChildItem -Recurse | Unblock-File` in the folder. Unsigned executables always trigger this — the bundle itself is fine.
</details>

<details>
<summary>"Microsoft Edge is not responding because the existing instance is running with elevated privileges"</summary>

Chromium's single-instance lock: the browser started by the launcher has a different integrity level than the one already running, so it refuses to join and exits — console appears, no browser window.

**Since v1.2.1 the launcher no longer starts a separate browser process**: it just opens a normal tab in your default browser, so this dialog cannot happen.
For older packages: kill all `msedge.exe` / `chrome.exe` and start the launcher again, upgrade to v1.2.1, or simply open the `http://127.0.0.1:8000/...` URL printed in the console.
</details>

<details>
<summary>Does the launcher pollute my daily browser profile?</summary>

By default, no — it opens a tab in your existing browser and creates no profile at all.
Only with `--browser=app` does it use a separate profile dir `%LOCALAPPDATA%\CyberChefOffline\browser-profile-msedge` (or `-chrome`). Delete that folder to reset it; bookmarks, extensions and logins are untouched.
</details>

<details>
<summary>The console window appears but no browser ever does</summary>

Copy the `URL : http://127.0.0.1:xxxx/CyberChef_v11.5.0.html` line from the console into your browser — the server itself is fine, only the "open a browser" step got blocked (no default browser set, security software, etc.).
The launcher falls back automatically: default browser first, then Edge/Chrome app mode, then it leaves the URL for you.
</details>

<details>
<summary>Why is it 58 MB extracted but only 24 MB zipped?</summary>

CyberChef's JavaScript is shipped as uncompressed text (`assets/main.js` is 13.6 MB, plus 23 module bundles), so zip compression is very effective.
</details>

## License & Credits

- **CyberChef**: © GCHQ, [Apache-2.0](LICENSE-CyberChef.txt) — this bundle only trims the official build and wraps it with a launcher; its code is unmodified
- **Launcher and scripts in this repo**: [MIT](LICENSE)
- Inspired by [Thyme-13/CyberChef_launcher](https://github.com/Thyme-13/CyberChef_launcher) (PyInstaller approach)

> ⚠️ This project is intended for lawful data analysis, education and authorized testing only. Follow the laws and competition rules that apply to you.
