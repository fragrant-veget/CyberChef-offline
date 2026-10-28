========================================================================
 CyberChef 离线版 v11.5.0（Windows x64 绿色包）
 开箱即用 · 无需安装 · 无需联网 · 无需 Python / Node
========================================================================

【怎么用】

  双击  CyberChef_launcher.exe

  会弹出一个控制台窗口（显示访问地址），随后用你的默认浏览器打开 CyberChef
  （在已开着的 Edge / Chrome 里新开一个标签页，不会打断你正在用的窗口）。
  用完关掉那个控制台窗口即可（关窗口 = 停止本地服务）。

  想要"像一个独立软件"的无边框窗口，可以加参数：
        CyberChef_launcher.exe --browser=app
  这时启动器会给浏览器单独用一个配置目录
  （%LOCALAPPDATA%\CyberChefOffline\browser-profile-*），
  不会动你日常浏览器的收藏夹 / 扩展 / 登录状态。

【注意】

  - 必须保持 CyberChef_launcher.exe 和下面这些文件在同一目录：
        CyberChef_v11.5.0.html
        assets\      modules\      images\
    只拷贝 exe 或者只拷贝 html 都无法使用（v11 起官方不再提供单文件 HTML）。

  - 启动器只在 127.0.0.1（本机回环地址）提供服务，不对外网开放。
    如果 8000 端口被占用，会自动换 5500 / 8080 / 8888 / 3000；
    也可以手动指定端口： CyberChef_launcher.exe --port=9000

  - 同一时间只允许一个实例：重复双击只会把已启动的页面再打开一次。

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

  Q: 控制台有、浏览器一直不出来？
  A: 把控制台里那行 URL 手动粘到浏览器地址栏——服务是好的，
     只是"自动开浏览器"这一步被系统拦了（没有默认浏览器、或被安全软件拦截）。
     也可以试：  CyberChef_launcher.exe --browser=app

  Q: 双击后弹「打开文件 - 安全警告」，或者双击好像没反应？
  A: 这是 Windows 对「从网上下载的 zip」打的标记（MOTW）。
     解法一（推荐）：先右键 zip → 属性 → 勾选「解除锁定」→ 确定，然后再解压。
     解法二：在解压出的文件夹里执行  Get-ChildItem -Recurse | Unblock-File
     没有代码签名的程序都会这样，不是包坏了。

  Q: 弹「Microsoft Edge 未响应，因为现有实例正在以提升的权限运行」？
  A: 那是旧版（v1.1.0 及以前）拉独立浏览器进程时的单实例锁冲突。
     v1.2.1 起默认改用系统浏览器标签页，不会再弹这个框。
     如果还遇到：任务管理器结束所有 msedge.exe / chrome.exe 再启动，
     或手动打开控制台里的 http://127.0.0.1:8000/... 地址。

  Q: 杀毒软件报毒？
  A: 本启动器是 .NET Framework 编译的小程序（约 24 KB），
     源码公开在 GitHub 仓库 src/ 目录，可自行用 csc.exe 重新编译。

【关于内容/许可】

  CyberChef  © GCHQ，Apache-2.0（见 LICENSE-CyberChef.txt）
  本包只做精简与启动封装，未修改 CyberChef 代码。
  官方项目：https://github.com/gchq/CyberChef

  精简说明：移除 58 个 .gz / .br 预压缩副本（本地服务用不到），
  保留 tesseract 英文语言包 eng.traineddata.gz；模块与图标全部保留。
