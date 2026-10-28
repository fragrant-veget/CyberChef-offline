========================================================================
 CyberChef 离线版 v11.5.0（Windows x64 绿色包）
 开箱即用 · 无需安装 · 无需联网 · 无需 Python / Node
========================================================================

【怎么用】

  双击  CyberChef_launcher.exe

  会弹出一个控制台窗口（显示访问地址），随后自动打开 CyberChef 界面。
  用完关掉那个控制台窗口即可（关窗口 = 停止本地服务）。

  如果你用 Edge / Chrome 打开，会是独立的无边框应用窗口；
  没有 Edge 时自动回退到系统默认浏览器。
  启动器给浏览器单独用了一个配置目录（%LOCALAPPDATA%\CyberChefOffline\browser-profile-*），
  不会动你日常浏览器的收藏夹 / 扩展 / 登录状态。

【注意】

  - 必须保持 CyberChef_launcher.exe 和下面这些文件在同一目录：
        CyberChef_v11.5.0.html
        assets\      modules\      images\
    只拷贝 exe 或者只拷贝 html 都无法使用（v11 起官方不再提供单文件 HTML）。

  - 启动器只在 127.0.0.1（本机回环地址）提供服务，不对外网开放。
    如果 8000 端口被占用，会自动换 5500 / 8080 / 8888 / 3000；
    也可以手动指定端口： CyberChef_launcher.exe --port=9000

  - 首次在别人的电脑上运行可能出现 Windows SmartScreen
    「Windows 已保护你的电脑」→ 点「更多信息」→「仍要运行」。
    这是没有代码签名的正常现象。

  - 想直接双击 html 也可以（file:// 模式能打开），
    但部分依赖 Web Worker 的操作（OCR、图片处理、大文件）在 file:// 下可能受限，
    所以推荐用 launcher 走 http://。

【常见问题】

  Q: 控制台窗口一闪而过？
  A: 说明 html 或 assets 没找齐，窗口里会打印具体缺什么。

  Q: 浏览器显示「无法访问此网站」？
  A: 等服务窗口打印出 URL 后再刷新；或改用 http://127.0.0.1:8000/ 手动打开。

  Q: 双击后弹「打开文件 - 安全警告」，或者双击好像没反应？
  A: 这是 Windows 对「从网上下载的 zip」打的标记（MOTW）。
     解法一（推荐）：先右键 zip → 属性 → 勾选「解除锁定」→ 确定，然后再解压。
     解法二：在解压出的文件夹里执行  Get-ChildItem -Recurse | Unblock-File
     没有代码签名的程序都会这样，不是包坏了。

  Q: 弹「Microsoft Edge 未响应，因为现有实例正在以提升的权限运行」，点“是”也没反应？
  A: 这是浏览器单实例锁冲突。1.1.0 起启动器已规避（给浏览器单独配置目录）。
     旧包救急：任务管理器结束所有 msedge.exe / chrome.exe 再启动，
     或者改用默认浏览器：  CyberChef_launcher.exe --browser=default

  Q: 杀毒软件报毒？
  A: 本启动器是 .NET Framework 编译的小程序（约 21 KB），
     源码公开在 GitHub 仓库 src/ 目录，可自行用 csc.exe 重新编译。

【关于内容/许可】

  - CyberChef 本体：GCHQ 出品，Apache-2.0，
    项目地址 https://github.com/gchq/CyberChef
    本包为官方 v11.5.0 独立构建（standalone build）的精简版：
    删除了 Web 服务器用的 .gz / .br 预压缩副本（本地服务用不到），
    保留了 tesseract 英文语言包 assets/tesseract/lang-data/eng.traineddata.gz。
  - 启动器源码与打包脚本：见本项目的 GitHub 仓库，MIT 许可。
  - 完整许可文本：LICENSE-CyberChef.txt

  校验值（SHA256）
    CyberChef_launcher.exe :
      __LAUNCHER_SHA256__
    CyberChef_v11.5.0.html :
      809174936FD5F895E23F45E6D06448B6DC6F8F115CF7ACC969029F3D894903E9

========================================================================

