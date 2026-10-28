# 发布指南（把本仓库上传到 GitHub）

本目录已经是**可直接 git 初始化的仓库骨架**：源码、构建脚本、README、许可证、截图都在，
`release\CyberChef_v11.5.0_offline_win64.zip`（24.3 MB）是**要作为 Release 资产上传的产物**（已被 `.gitignore` 排除，不进 git）。

> 建议仓库名：`cyberchef-offline`
> 建议描述：`One-click offline CyberChef for Windows — launcher + trimmed official standalone build. No install, no network, no Python.`

---

## 方式 A：网页 + 命令行（最稳）

### 1. 在 GitHub 建空仓库

<https://github.com/new> → 填仓库名 `cyberchef-offline` → **不要**勾选 README/.gitignore/License（本地已有）→ Create。

### 2. 本地初始化并推送

把 `<YOUR_NAME>` 换成你的 GitHub 用户名：

```powershell
cd "D:\Agent projects\Deepseek Harness\闲聊\github-cyberchef-offline"

git init -b main
git add .
git commit -m "CyberChef offline launcher + trimmed v11.5.0 standalone build"
git remote add origin https://github.com/fragrant-veget/cyberchef-offline.git
git push -u origin main
```

### 3. 创建 Release 并上传 zip

网页：仓库 → **Releases → Draft a new release**

- **Choose a tag**: `v11.5.0` → Create new tag on publish
- **Release title**: `CyberChef v11.5.0 offline (Windows x64)`
- **描述**：直接复制下面模板
- **Attach binaries**: 拖入 `release\CyberChef_v11.5.0_offline_win64.zip`
- Publish release

Release 描述模板：

```markdown
CyberChef **v11.5.0** 官方独立构建（精简版）+ 18 KB 一键启动器，Windows x64 离线可用。

## 下载
| 文件 | 大小 | SHA-256 |
|---|---|---|
| `CyberChef_v11.5.0_offline_win64.zip` | 24.26 MB | `D5EED23BCB3E3319D2EAAA5AFB91E60523563F38D84FCB5E3C0C9268D45B54EE` |

## 用法
解压 → 双击 `CyberChef_launcher.exe` → 浏览器自动打开。不联网、无需安装任何运行时。

## 相对官方包的改动
- 移除 58 个 `*.gz` / `*.br` 预压缩副本（-21 MB，本地服务用不到）
- 保留 tesseract 英文语言包 `eng.traineddata.gz`
- 移除重复的 `CyberChef_v11.4.0.html`
- 启动器重写（Windows 自带 .NET Framework 4.x 编译，无需管理员权限）

## 校验
- `CyberChef_launcher.exe`: `899574619ED731DC95769B7130E8578324D1035787EFD4C9BFBA6E27AED0483F`
- `CyberChef_v11.5.0.html`: `80917493...3903E9`
- 官方原版 zip: `F6478925...24CCCF`
```

### 4. 仓库设置（可选但推荐）

- **About** 右侧齿轮 → Description 用上面那句；**Topics** 填：
  `cyberchef` `offline` `forensics` `ctf` `windows` `portable` `single-file` `dfir`
- Settings → 勾选 **Releases** / **Discussions**（可选）

---

## 方式 B：gh CLI（一条命令）

已安装 [GitHub CLI](https://cli.github.com/) 且已 `gh auth login` 的话：

```powershell
cd "D:\Agent projects\Deepseek Harness\闲聊\github-cyberchef-offline"

git init -b main
git add .
git commit -m "CyberChef offline launcher + trimmed v11.5.0 standalone build"

gh repo create cyberchef-offline --public --source=. --remote=origin --push `
  --description "One-click offline CyberChef for Windows - launcher + trimmed official build"

gh release create v11.5.0 "release\CyberChef_v11.5.0_offline_win64.zip" `
  --title "CyberChef v11.5.0 offline (Windows x64)" `
  --notes-file release-notes.md
```

（`release-notes.md` 内容用上面的模板即可。）

---

## 上传前自检清单

- [ ] `git status` 里**没有** `release/`、`bin/`、`*.zip`（`.gitignore` 已排除；若已误 add，用 `git rm --cached release -r`）
- [ ] 仓库首页 README 里 `../../releases/latest` 链接能点
- [ ] Release 资产上传成功，下载后 SHA-256 与本文件上面的哈希表一致
- [ ] 找一台**没装 Python / Node** 的电脑（或虚拟机）解压试运行
- [ ] 首次运行会被 SmartScreen 拦一次（启动器未签名），点「更多信息 → 仍要运行」属预期

---

## 附：以后升级 CyberChef 版本

见 README「更新到新版本 CyberChef」。简言之：官方新版 zip → 删 `*.gz`/`*.br`（保留 `*traineddata.gz`）→ 放入旧启动器 → 重新打包 → 发新 Release（tag 用新版本号，例如 `v11.6.0`）。
`build.ps1 -Target package -CyberChefDir <目录> -Version 11.6.0` 会自动完成精简 + 打包 + 算哈希。

## 附：自动化（可选，未实测）

若想用 GitHub Actions 每次发版自动出包，可参考 `docs/github-actions-example.yml`。
**注意：该 workflow 我没有实际跑过 CI**，粘贴到 `.github/workflows/release.yml` 前请自行 review / 试跑一次。
