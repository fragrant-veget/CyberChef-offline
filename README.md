# CyberChef Offline · 一键离线版 CyberChef

**简体中文** | [English](README_EN.md)

[CyberChef](https://github.com/gchq/CyberChef) 的 **Windows 离线绿色包 + 一键启动器**。
下载解压，双击 `CyberChef_launcher.exe`，浏览器里就能用完整的 CyberChef —— **不联网、不装环境、不需要 Python / Node / 任何运行时**。

---

## 下载

到 [**Releases**](../../releases/latest) 页面下载：

| 文件 | 大小 | 说明 |
|---|---|---|
| `CyberChef_v11.5.0_offline_win64.zip` | ≈ 24 MB（解压后 ≈ 58 MB） | 完整离线包（CyberChef v11.5.0 + 启动器） |

- **系统要求**：Windows 10 / 11 x64（`.NET Framework 4.x` 系统自带），Edge / Chrome 任一即可

## 快速开始

1. 解压 zip，得到 `CyberChef_v11.5.0_offline_win64\` 文件夹
2. 双击里面的 **`CyberChef_launcher.exe`**
3. 弹出控制台窗口（显示 `http://127.0.0.1:8000/...`），随后自动打开 CyberChef 界面
4. 用完关掉控制台窗口即可停止服务

```
CyberChef_v11.5.0_offline_win64\
├─ CyberChef_launcher.exe      ← 双击这个
├─ CyberChef_v11.5.0.html      ← 应用入口（不要单独拷走）
├─ assets\  modules\  images\  ← 必须和 exe 放在一起
├─ README.txt                  ← 给最终用户看的说明
└─ LICENSE-CyberChef.txt       ← Apache-2.0
```

## 它做了什么

```
CyberChef_launcher.exe
  ├─ 扫描自身所在目录，找到 CyberChef_*.html（自动识别最高版本）
  ├─ 在 127.0.0.1 上起一个只读静态 HTTP 服务（8000 → 5500 → 8080 → 8888 → 3000，全占用则随机端口）
  └─ 用系统默认浏览器打开 http://127.0.0.1:<port>/CyberChef_vX.Y.Z.html
```

设计要点：

- **默认走系统浏览器标签页**：不弹 Chromium 应用窗口、不抢单实例锁，直接在你正在用的 Edge/Chrome 里新开一个标签页（最省心、兼容性最好）
- **只绑 `127.0.0.1`**，不监听外网，不触达防火墙入站规则
- 启动器是 `.NET Framework 4.x` 编译的**单文件 24 KB 程序**（系统自带运行时，内嵌图标 `src/CyberChef_launcher.ico`），源码在本仓库 `src/`
- **同一时间只允许一个实例**：再次双击只会把已在运行的地址再打开一次，不会起第二个服务占端口
- 服务实现为原始 `TcpListener`（不用 `HttpListener`，因此**不需要管理员权限**、不需要 URL ACL）
- 故意用 `http://` 而不是 `file://`：CyberChef 的 OCR / 图片处理 / 大文件操作依赖 Web Worker + WASM，`file://` 下多数 Chromium 会拦截
- 参数：
  - `--port=9000` 固定端口
  - `--no-open` 只起服务、不自动开浏览器
  - `--browser=app` 改用 Edge/Chrome 的 `--app` 独立窗口（会使用单独的配置目录 `%LOCALAPPDATA%\CyberChefOffline\browser-profile-*`，想要"像个独立软件"时用）

## 自己构建

只需要 Windows 自带的 .NET Framework 编译器（无需 Visual Studio / .NET SDK）：

```powershell
# 1) 编译启动器（产物: bin\CyberChef_launcher.exe）
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Target launcher

# 2) 准备 CyberChef 官方构建目录（解压官方 release zip 到 out\ 下），然后打包
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Target package -CyberChefDir .\out\CyberChef
```

或手动编译：

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /platform:anycpu /optimize+ ^
    /codepage:65001 /win32icon:src\CyberChef_launcher.ico ^
    /out:CyberChef_launcher.exe src\CyberChef_launcher.cs
```

## 更新到新版本 CyberChef

1. 从 [CyberChef Releases](https://github.com/gchq/CyberChef/releases) 下载新版官方 zip 并解压
2. 删除其中的 `*.gz` / `*.br`（**保留 `assets/tesseract/lang-data/*.traineddata.gz`**）
3. 把 `CyberChef_launcher.exe` 放进该目录，改名不用管 —— 启动器会自动挑 `CyberChef_*.html` 里版本号最高的那个

## 常见问题

<details>
<summary>控制台窗口一闪而过 / 提示 HTML not found</summary>

启动器没找到 `CyberChef_*.html`，通常是把 exe 单独拷出来了。请保持 exe 与 html、assets、modules、images 同目录。
</details>

<details>
<summary>浏览器显示「无法访问此网站」</summary>

等服务窗口打印出 URL 后再刷新；或手动访问窗口里显示的 `http://127.0.0.1:<port>/...`。端口被占用时启动器会自动换端口。
</details>

<details>
<summary>没有 Edge，能双击 html 直接用吗</summary>

能打开，但推荐做法仍是走启动器：`file://` 下部分 Web Worker/WASM 操作会被 Chromium 安全策略拦。
启动器找不到 Edge 时会回退到系统默认浏览器。
</details>

<details>
<summary>双击后弹出「打开文件 - 安全警告 / 无法验证发布者」，或者双击好像没反应</summary>

这是 Windows 给**从网上下载的 zip** 打的「来自 Internet」标记（MOTW），解压出来的 exe / html 也带着它。
两种解决办法：

- **推荐**：右键 zip → 属性 → 勾选「解除锁定」→ 确定，**然后再解压**（解压出来的文件就干净了）
- 已经解压了：在解压出的文件夹里打开 PowerShell，执行
  `Get-ChildItem -Recurse | Unblock-File`
  或者对 exe 右键 → 属性 → 解除锁定

没有代码签名的程序都会这样，不是包有问题。
</details>

<details>
<summary>弹出「Microsoft Edge 未响应，因为现有实例正在以提升的权限运行」，点"是"也没反应</summary>

这是 Chromium 的**单实例锁冲突**：启动器拉起的 Edge/Chrome 和你已经开着的浏览器权限级别不一致，浏览器拒绝加入现有实例，于是直接退出 —— 表现就是控制台黑窗口有了、浏览器没出来。

**v1.2.1 起默认不再拉独立的浏览器进程**：直接用系统默认浏览器开一个普通标签页，不存在单实例锁的问题，也不会弹出这个对话框。
如果你用的是旧包，可以这样救急：

1. 任务管理器里把所有 `msedge.exe` / `chrome.exe` 结束掉，再双击启动器；或
2. 升级到 v1.2.1；或
3. 手动打开控制台里打印的 `http://127.0.0.1:8000/...` 地址（服务本身是好的）
</details>

<details>
<summary>启动器会不会污染我日常的浏览器配置</summary>

默认模式不会——它只是在你日常浏览器里新开一个标签页，不建任何新配置。
只有加 `--browser=app` 时才会用单独的配置目录 `%LOCALAPPDATA%\CyberChefOffline\browser-profile-msedge`（或 `-chrome`）；删掉这个目录就相当于重置，你的收藏夹、扩展、登录状态都不受影响。
</details>

<details>
<summary>双击后控制台有了，但浏览器一直没动静</summary>

先看控制台里那行 `URL : http://127.0.0.1:xxxx/CyberChef_v11.5.0.html`，把它手动粘到浏览器地址栏——服务是好的，只是“自动开浏览器”这一步被系统拦了（常见于没有默认浏览器、或被安全软件拦截）。
启动器会自动回退：先试系统默认浏览器，失败再试 Edge/Chrome 的 app 窗口，最后把地址直接交给你手动打开。
</details>

<details>
<summary>为什么解压后 58 MB，zip 只有 24 MB</summary>

CyberChef 的 js 是未压缩文本（`assets/main.js` 13.6 MB + 23 个 modules），zip 压缩率很高。
</details>

## 许可证与致谢

- **CyberChef**：© GCHQ，[Apache-2.0](LICENSE-CyberChef.txt)（本包仅做精简与启动封装，未修改其代码）
- **本仓库启动器与脚本**：[MIT](LICENSE)
- 灵感来自 [Thyme-13/CyberChef_launcher](https://github.com/Thyme-13/CyberChef_launcher)（PyInstaller 方案）

> ⚠️ 本项目仅供合法的数据分析、教学与授权测试使用。请遵守所在地区法律与比赛规则。
