# PCL3 代码结构与构建说明

## 一、目录

| 路径 | 说明 |
| --- | --- |
| `pcl-2代码/` | 从 GitHub（Meloong-Git/PCL）下载的 PCL2 原始源码，只读参考，不要改 |
| `pcl3/` | PCL3 源码工作副本，所有修改都在这里 |
| `pcl3/Plain Craft Launcher 2/` | VB.NET 主工程（目录名沿用上游，程序集名已改为 `Plain Craft Launcher 3`） |
| `pcl3/PCLCS/` | C# 辅助库（UI 控件等） |
| `pcl3/MeloongCore/` | C# 基础库（`Shared` / `Wpf`） |
| `工具/roslyn/` | Roslyn 4.8 的 `vbc.exe`，VB 编译必须用它（Framework 自带 vbc 会报 BC30175/BC32025/BC36637） |
| `构建.ps1` / `构建.bat` | 一键构建脚本（bat 只是带 `pause` 的入口） |
| `pcl3-成品/` | 构建产物输出目录：`Plain Craft Launcher 3.<正式版本名>.<内测版本名>.exe` |
| `pcl3代码结构.md` | 本文件 |

## 二、版本规则

版本名为 `Plain Craft Launcher 3.<正式版本名>.<内测版本名>`，例如 `3.1.0.21`。

- 正式版本名（`VersionFormalName`）：默认 `1.0`，只在执行「正式版」构建时 +1。
- 内测版本名（`VersionInternalName`）：每次打包自动 +1（不论正式版还是内测版）。
- 这两个值定义在 `pcl3/Plain Craft Launcher 2/Modules/Base/ModBase.vb` 顶部，由 `构建.ps1` 自动读写。
- 显示用版本名 `VersionDisplay` 追加「正式版 / 内测版 / 开发版」后缀。

构建命令：

```powershell
powershell -ExecutionPolicy Bypass -File 构建.ps1          # 内测（Snapshot 配置）
powershell -ExecutionPolicy Bypass -File 构建.ps1 正式版    # 正式（Release 配置，正式版本名 +1）
```

也可直接双击 `构建.bat`（等同内测构建）。

## 三、构建链（务必按此顺序）

```text
dotnet build pcl3\PCLCS\PCLCS.csproj -c Release        # 先出 MeloongCore / MeloongCore.Wpf / PCLCS 三个 dll
        ↓  dll 被 Resources.resx 以 ResXFileRef 嵌入主程序
MSBuild.exe（Framework v4.0.30319）编译 VB 主工程
        ↓
pcl3\Plain Craft Launcher 2\bin\Plain Craft Launcher 3.exe
        ↓
复制到 pcl3-成品\Plain Craft Launcher 3.<正式>.<内测>.exe
```

关键参数（`构建.ps1` 已封装）：

- `/p:TargetFrameworkRootPath=%USERPROFILE%\.nuget\packages\microsoft.netframework.referenceassemblies.net48\1.0.3\build`（本机没装 .NET Framework 4.8 开发包）
- `/p:VbcToolPath=<工具\roslyn>`、`/p:VbcToolExe=vbc.exe`、`/p:UseHostCompilerIfAvailable=false`
- `/p:BuildProjectReferences=false`（C# 依赖已由 dotnet 单独构建，避免 MSBuild 去编译 csproj）
- .NET 9 SDK 位于 `C:\Users\ytzsy\.dotnet\dotnet.exe`（VB 需要 C# 13 编译器支持，.NET 8 SDK 不够）

`pcl3/Plain Craft Launcher 2/Plain Craft Launcher 2.vbproj` 里对三个 C# dll 用的是 `<Reference>`（`HintPath` 指向 `..\PCLCS\bin\...`，`Private=False`），**不是** ProjectReference，所以必须先跑 PCLCS 构建。

## 四、PCL3 相对 PCL2 的改动

1. 移除遥测：`ModMain.vb` 的 `Telemetry` 整体删除，并清理 `FormMain`、`PclLogger`、`ModNet`、`ModLaunch`、`ModCrash`、`PageLinkMain`、`PageSetupSystem`、`Settings` 中的调用与开关。
2. `ModSecret.vb`：删除 `ClsBaseUrl`、`InputPotatoCode`/`GeneratePotatoCode`，UA/Referer 改 `PCL3`；`ThemeCheckOne` 恒真、`ThemeUnlock` 恒假、`ThemeCheckAll` 空实现（赞助与主题解锁彻底去掉）。
3. 移除 MC 下载限制：删除 `RestrictMcDownload` 常量及其 4 处判断（`PageDownloadInstall`、`ModModpack`×2、`PageDownloadModify`、`PageDownloadCompDetail`、`FormMain`），下载与整合包安装恢复。
4. 主题配色引擎：开源版 `ThemeRefresh` 只有 HSL→颜色计算，缺主题 ID→色相的映射（在闭源部分），导致切换主题无效果。已在 `ModSecret.vb` 补上 `ThemeApply(Id)`（0~11 固定配色、12 滑稽彩自动循环、13 欧皇彩随机、14 自定义读取 `UiLauncherHue/Sat/Delta/Light` 设置），并在 `PageSetupUI.xaml.vb` 的 `HSL_Change` 里保存四个滑条到设置、`SliderLoad` 里回读，自定义主题可用。
5. UI：`PageSetupUI` 删除赞助横幅与主题锁定遮罩，全部主题直接可选；`PageOtherAbout` 删除赞助按钮，隐私说明改为「不上传任何数据」；`PageSetupSystem` 删除赞助卡片。
6. 版本/命名：程序集名、窗口标题、`PathImage`、pack URI、`xmlns` 等 `Plain Craft Launcher 2` → `Plain Craft Launcher 3`；`ModBase.vb` 新增 `ProductName` / `VersionFormalName` / `VersionInternalName` / `VersionDisplay`。
7. 更新日志里「删除 MC 下载」的条目改为 PCL3 自己的改动说明。
8. 新增「更衣室」（设置 → 更衣室，见第七节）：3D 预览皮肤/披风，管理皮肤库，离线/正版均可换肤。
9. 设置 → 启动 新增「登录方式」卡片（`CardLogin`）：下拉选 正版/离线/统一通行证/Authlib，附离线玩家名与第三方服务器信息；写入全局 `LoginPageType`/`LoginType`，并同步当前选中版本的 `VersionServerLogin`，改完立即刷新启动页登录面板。原「离线皮肤」卡片已删除（功能迁入更衣室，`LaunchSkinType` 设置保留供启动逻辑使用）。
10. 设置 → 其他 新增「开发者模式」卡片（`CardDeveloper`）：版本/系统信息展示 + 打开日志文件、打开 PCL 文件夹、导出诊断信息（存桌面）、分析崩溃报告（内置 `CrashAnalyzer`）、打开版本文件夹、打开 mods 文件夹、打开版本 JSON 七个工具，面向模组开发者。
11. 百宝箱（更多 → 百宝箱，`PageOtherTest`）：开源版只有占位符，已按官方 PCL2 exe 反向解析补齐——「百宝箱」卡片（今日人品 / 内存优化 / 清理游戏垃圾 / 千万别点）、「回声洞」卡片（点击换留言，投稿仍指向 jinshuju）、「下载自定义文件」卡片（下载地址/保存到/文件名，`StartCustomDownload` 走多线程下载引擎）。`Jrrp`/`MemoryOptimize`/`RubbishClear`/`GetRandomHint` 等共享函数放在 `PageOtherTest` 中供 `ModEvent`/`ModMain` 调用。
12. PCL2 哀悼日（每年 10 月 20 日）：`ModSecret.IsMourningDay`（环境变量 `PCL3_FORCE_MOURNING=1` 可强制启用以便调试）；`ThemeRefresh` 在该日强制 `ColorSat = 0`（全 UI 灰色，对滑稽彩/欧皇彩/自定义主题同样生效），`FormMain` 启动加载池（EULA 之后）弹出「今天是 PCL2 的下架日期，默哀」提示。
13. AI 助手（`Modules/ModAi.vb`，设置 → 其他「AI 助手」卡片）：设置项 `ToolApiKey`（加密）/`ToolAiUrl`（默认 DeepSeek）/`ToolAiModel`（默认 deepseek-chat），OpenAI 兼容 Chat Completions 接口。功能：① 启动失败的两类弹窗（`ModLaunch` 的 `$` 错误与未知错误分支）与崩溃分析（`ModCrash.Output`）后均可选「让 AI 分析报错原因」；② 卡片内「测试连接 / AI 分析本次日志 / AI 模组推荐 / 新手入门教学」四个按钮。回答弹窗带「复制结果」按钮；所有请求共享一把并发锁（`Interlocked`），进行中会提示稍候；日志发送上限约 12000 字符，超长截断；请求失败时显示服务端返回的原始错误（如 Key 无效、余额不足）。
14. `构建.ps1`：输出成品后自动删除 `pcl3-成品` 里的旧版 exe，只保留最新一次构建；**构建失败时自动回滚版本号**，失败的构建不再消耗内测版本号。
15. 哀悼日的背景图片也会灰度化：`ModSecret.GrayscaleImage`（GDI+ 颜色矩阵，0.299/0.587/0.114 亮度加权），`PageSetupUI.BackgroundRefresh` 在灰度生效时对背景图调用它。已实测四色彩图会完整变灰。
16. 「你知道吗」中已过时的离线皮肤提示改为指向更衣室；设置 → 启动「登录方式」的 ToolTip 与实际行为（同步当前选中版本）对齐。
17. 彩蛋卡片（设置 → 其他，`CardEaster`，位于 调试选项 与 开发者模式 之间）：
    - 「哀悼日灰色色调」开关（`SystemEasterMourningGray`，默认开）：关闭后哀悼日保持彩色（`ModSecret.IsMourningGray` 属性同时控制主题灰度与背景图灰度），哀悼弹窗仍会弹出，文案自动适配。
    - 「滑稽彩轮换速度」滑条（`SystemEasterHueStep`，1~30，默认 3）：控制滑稽彩（250ms 时钟）与真·滑稽彩（50ms 时钟）的色相步进。
    - 四个体验按钮：滑稽彩（主题 12）、欧皇彩（主题 13，重复点击重新随机）、真·滑稽彩（`ThemeDontClick=2`，**可再次点击停止并恢复原主题**，比原版多了退出方式）、PLC 彩蛋（标题 Logo 换手绘 PLC，可再次点击恢复）。

## 五之一、安全加固记录（2026-10-24 审计）

0. **内置防火墙（`Modules/ModFirewall.vb`，设置 → 安全）**：三层防护。
   - **启动前模组扫描**（`ScanJar`/`ScanMods`）：解压 mods 下全部 Jar 的 .class 条目（单条目 16MB / 单文件 96MB 限额），在字节码常量池中搜索危险特征并加权评分：已知恶意家族特征（fractureiser/.reflexa/dltlbm，10 分）、浏览器/钱包凭据路径（10）、命令执行+持久化路径组合（10）、命令执行+脚本解释器（8）、Base64 解密+defineClass 动态加载（6）、Runtime/ProcessBuilder（5）、javax.script（4）、内嵌原生库（3）、缺模组元数据（2）等。≥10 判恶意、≥6 可疑、≥3 注意。结果按「文件名+大小+修改时间」哈希缓存于 `PathTemp\Firewall\Cache\`，二次启动零开销。
   - **启动门禁**（`GateLaunch`，挂在 `McLaunchStart` 预检测之后；导出启动脚本时跳过）：可疑/恶意文件弹窗列出具体特征；模式可选「仅提示」或「阻止启动」，均提供 信任并启动（写入 `SystemSafeWhitelist` 白名单，¨ 分隔）/ 无视风险 / 取消。防火墙自身异常时放行并记日志（不因保护机制杀死正常启动）。仅“注意”级只气泡提示不拦截。
   - **运行时监控**（`WatchStart`，游戏进程启动后挂后台线程）：①每 2 秒用 WMI 枚举进程树，发现 Java 后代中出现 powershell/cmd/wscript/mshta/certutil 等脚本与下载工具即弹窗，可一键结束游戏与全部可疑进程；②每 15 秒对比 注册表 HKCU/HKLM Run 键 + 用户/公共启动文件夹快照，发现新增自启项弹持久化警报。游戏退出后监控线程自动结束。
   - 设置项：`SystemSafeEnabled`（总开关，默认开）/`SystemSafeScan`/`SystemSafeWatch`/`SystemSafeMode`(0 提示 1 阻止)/`SystemSafeWhitelist`/`SystemSafeLastScan`；日志在 `PathTemp\Firewall\Log.txt`；页面 `PageSetupSafe`（导航 ItemSafe，Tag=5，盾牌图标，PageSubType.SetupSafe=5）。
   - 布局（2026-10-25 按设置页规范调整）：发现威胁改为两个 MyRadioBox（Width=110，仿内存分配卡片）；三个按钮统一 MinWidth=140/Height=35（上游标准规格）；扫描状态文字纳入标签 Grid（ColumnSpan=2）；复选框文案精简为「启用防火墙/启用」，详细说明都在 ToolTip 与说明文字中。
   - 实现注意：模块内属性不能叫 `LogFile`（会与 ModCrash 的 `For Each LogFile` 循环变量撞名报 BC30039）；`List(Of T).Count(谓词)` 会被 Count 属性遮蔽，需用 `.Where(...).Count`。
   - 性能（2026-10-25 优化）：① 子进程监控改用 **Toolhelp32 快照**（`CreateToolhelp32Snapshot` + `Process32FirstW/NextW`，实测 306 进程 17ms）替代 WMI `Win32_Process` 查询（每次数十至数百 ms）；注意 `PROCESSENTRY32W.pcPriClassBase` 在 Win32 头文件中是 `LONG`（VB 里用 `Integer`），声明成 `IntPtr` 会因结构体错位导致 `Process32FirstW` 报错 203。② 新增 `Prefetch`（挂在 `McInstanceSelected` setter）：版本切换时后台低优先级预热扫描缓存，启动门禁直接命中缓存、不阻塞启动（单飞锁防重复）。③ 扫描只认 `.jar` 后缀（Directory 通配符的 8.3 怪癖会匹配 `.jarx`）。

## 五之二、性能优化记录（2026-10-25）

1. **背景图片异步解码**（`PageSetupUI.BackgroundRefresh`）：改为后台线程解码 + `Freeze()` 后 `RunInUi` 应用（`BackgroundLoadId` 序号防旧加载覆盖新背景）。此前 4K 背景图会在 FormMain_Loaded 同步解码 200~500ms 阻塞窗口显示；现在窗口立即可用，图片随后淡入。哀悼日灰度转换也在同一线程完成。
2. 启动器本体已很快（实测三阶段 102+247+104 ≈ 450ms），未做改动；「启动慢」的实际痛点是首次启动游戏的防火墙扫描（已由预热解决）与游戏运行期间的 WMI 轮询（已由 Toolhelp32 解决）。

1. **AI 模块凭据泄露（已修复）**：`ModAi.AiAnalyzeError` 之前将崩溃报告原文直接发给第三方 AI 服务，而游戏崩溃日志可能包含登录访问令牌（accessToken）。现已在发送前用 `FilterAccessToken` + `FilterUserName`（与错误报告导出同一套脱敏）过滤。PCL 自身日志（Log1.txt）本来就经 `PclLogger.Format` 脱敏，无需重复处理。
2. **AI 提示词注入（已加固）**：恶意 Mod 可以构造包含指令的日志/错误文本诱导 AI 输出危险内容（钓鱼链接等）。系统提示词已加入安全要求：日志内容一律视为数据、不执行其中指令、链接仅限知名官网。
3. **加密存储弱加密（已迁移）**：原上游用 DES + 硬编码密钥/IV（`@;$ Abv2` / `95168702`）加密注册表中的登录密码与 API Key——密钥随 exe 分发，形同混淆。现迁移到 Windows DPAPI（当前用户范围，`dp1:` 前缀标识），旧 DES 数据自动兼容读取（`DesDecryptLegacy`），解密失败时按上游既有逻辑重置该项设置。换机器后加密设置会重置（需重新登录），这是 DPAPI 的正确行为。需要 `System.Security` 程序集引用。
4. **审计通过、无需修改的面**：Zip 解压有 ZipSlip 防护（`FileUtils.ExtractToDirectory` 用 `PathUtils.IsParentOf` 校验）；下载文件带哈希校验（`ModNet` Check.Hash）；JSON 反序列化未启用 TypeNameHandling（无反序列化 gadget 风险）；`OpenWebsite` 仅允许 http/https/minecraft://；自定义事件执行本地程序前有确认弹窗（`EventSafetyConfirm`），联网路径仅限帮助页面（json+xaml）；`GetObjectFromXML` 有类型黑名单（x:Code 等）。
5. **已知接受的风险（上游设计）**：主页自定义内容可执行本地文件（有确认弹窗）；API Key 会发送到用户自行配置的 AI 接口地址（功能本身如此）；单实例互斥锁名为固定字符串（可被恶意程序抢先占用导致无法启动，属本地攻击者场景，意义不大）。

## 五、注意事项

- 源码为 **UTF-8 无 BOM**；用 PowerShell 改文件必须 `[IO.File]::ReadAllText/WriteAllText(..., New-Object System.Text.UTF8Encoding($false))`，否则会写入 BOM。**严禁用 `Get-Content`/`Set-Content`/`-replace` 管道改含中文的源码文件**：PS 5.1 默认按 GBK 读写，UTF-8 中文会被转成乱码且吞掉部分换行（2026-10-23 的 ModBase.vb 事故即由此造成，修复方法：把乱码文本用 `[Text.Encoding]::GetEncoding(936).GetBytes` 编回字节后按 UTF-8 解码可恢复大部分内容，再对照 `pcl-2代码` 原版补损坏行）。
- `构建.ps1` / `构建.bat` 含中文，PowerShell 5.1 要求 **UTF-8 带 BOM**，否则中文变乱码、路径找不到。重写脚本后先用 `Parser::ParseFile` 检查语法。
- 用 `[Text.Encoding]::UTF8.GetString(...)` 读再带 BOM 写回会得到**双 BOM**（`EF BB BF EF BB BF`），脚本会解析失败，务必先 `TrimStart([char]0xFEFF)`。
- MSBuild 会提示 `ToolsVersion="15.0" 无法识别`，属无害警告（自动回退到 4.0 工具集）。
- `ResXFileRef` 引用的是 `..\PCLCS\bin\*.dll`，改完 C# 代码后不重建 PCLCS 就会嵌入旧 dll。
- 修改代码后必须重新打包 exe（用户明确要求每次改动都出成品）。
- **已知怪问题（2026-10-24）**：`Settings.vb` 的 `Entries` 集合中，在 `ToolModLocalNameStyle` 与 `SystemEasterMourningGray` 两行之间**插入任何新行（哪怕空行）**，都会让 MSBuild 的 wpftmp 临时编译（XAML GenerateTemporaryTargetAssembly）在 Settings.vb(124,75) 报 BC30201 并级联到集合结束；同样的行插到文件末尾则完全正常；单独用 vbc 编译该文件也正常。直接原因是 vbc 在 MSBuild 环境下把文件按 GBK 回退解码、部分换行被吞（典型症状），但文件本身是合法 UTF-8 无 BOM，且未传 `/codepage`。规避方法：**新增 Setting 条目紧跟现有条目添加，不要在该位置插入空行或注释行**；如遇同类级联语法错误，优先尝试移动/删除可疑位置的空行。
- 反向解析官方 exe 的 UI（提取 BAML）：官方 exe 的 `.g.resources` 为明文，魔数 `BEEFCACE` 后 12 字节起跳过 headerSize；名字区条目为「7bit 字节长度 + UTF-16LE 名 + int32 数据偏移」，数据区条目为「7bit 类型索引 + int32 长度 + 内容」，BAML 内属性值为 7bit 长度 + UTF-8，直接解出字符串即可还原页面结构（本次百宝箱即由此恢复）。

## 六、更衣室（新功能）

入口：启动页点击人物头像，或 设置 → 更衣室（左侧列表 `ItemSkin`，`Tag="4"`，对应 `PageSubType.SetupSkin = 4`，必须与 `FrmSetupLeft.PanItem` 索引一致）。

| 文件 | 说明 |
| --- | --- |
| `Pages/PageSetup/PageSetupSkin.xaml(.vb)` | 更衣室页面：左侧上方 `皮肤`/`披风` 切换，下方对应列表（皮肤显示头像 + 名称 + Steve/Alex 模型；披风显示名称与「使用中」），右侧 3D 预览 + 操作按钮 + 提示；按登录方式限制（离线可切换，其它只读查看） |
| `Pages/PageSetup/MySkinViewer.xaml(.vb)` | 3D 预览控件：`SetSkin(路径, IsSlim)`、`SetCape(路径)`、`ResetView()`；拖动旋转、滚轮缩放；正交相机；64x32 旧皮肤左肢镜像，64x64 支持 overlay；披风为背后薄片 |
| `Modules/Minecraft/ModSkinRoom.vb` | `SkinRoomFolder`、`SkinRoomCape`、`SkinRoomGetCapes`、`SkinRoomGetCapeTexture`、`SkinRoomApplySkin`、`SkinRoomApplyCape`、`SkinRoomIsSlim` |

- 相机：`OrthographicCamera Position="0,0,100" LookDirection="0,0,-1"`，`UpdateCamera` 里 `Width = 44 * Zoom * ActualWidth / ActualHeight`（`Zoom=1` 时可视高度 44 单位，32 单位角色不裁切）；滚轮 `Zoom` 限 0.5~2.5。
- 面顺序固定为 `Faces(0)=顶`、`(1)=底`、`(2)=-X`、`(3)=+Z 前`、`(4)=+X`、`(5)=-Z 后`；`+X` 是角色左手侧，所以左臂/左腿放在 +X、右臂/右腿放在 -X。
- 底面几何顶点仍按 `(X1,Y1,Z1)→(X2,Y1,Z1)→(X2,Y1,Z2)→(X1,Y1,Z2)` 让法线朝下，但贴图 UV 用自定义 `(0,0),(1,0),(1,1),(0,1)`（Flip 时反向），否则底面会上下颠倒。
- **overlay 必须在所有基础部件之后绘制**（`PendingOverlays` 暂存，`Rebuild` 末尾统一加入 `Models.Children`）。WPF 对半透明材质同样写深度，若 overlay 先画会把更早/更晚的基础部件挡出「背景色白缝」，表现为脖子、腰部、腋下、胯下断裂。
- 各部件间留 `Eps = 0.03` 的微小间隙（头 +Y、腿 -Y、手臂向外、两腿内缘 ±0.01），避免共面深度冲突导致接缝闪烁。
- 旧版 64x32 左肢 = `MirrorFaces(右肢)`：所有面 `Flip` 并交换 `Faces(2)`/`Faces(4)`，不做 X 坐标重映射；左腿 overlay 直接 `OffsetFaces(左腿, -16, 0)`（不镜像）。
- 披风外层面 `(1,1,10,16)` 贴 -Z（外侧），其余面用内层 `(12,1,10,16)`；盒体 `(-5,-8,-3.4)~(5,8,-2.6)`。
- 拖动：`AngleY += ΔX * 0.6`，`AngleX += ΔY * 0.6`（限制 -60~60；向下拖显示头顶、向右拖显示左侧）。
- 登录方式门控：`IsOfflineLogin`（`LoginType = McLoginType.Legacy`）时才显示导入/应用/披风应用/刷新按钮；非离线列表只放一条只读的当前皮肤，点击预览不切换。

- 数据位置：皮肤库 `%APPDATA%\PCL\Skins\*.png`；离线皮肤 `%APPDATA%\PCL\CustomSkin.png`（应用时同时置 `LaunchSkinType = 4` 并刷新 `PageLaunchLeft.SkinLegacy`）；披风纹理缓存 `PathTemp\Cache\Cape\<id>.png`。
- 正版接口：皮肤 `POST https://api.minecraftservices.com/minecraft/profile/skins`（multipart `variant=classic/slim` + `file`）；披风 `PUT/DELETE .../profile/capes/active`；列表读 `Settings("CacheMsV2ProfileJson").capes`。
- 细手臂判定：64x64 皮肤右臂第 54 列透明（`GetPixel(54, 20).A = 0` 或第 55 列透明）即视为 Alex/slim。
- 内置皮肤（`PathImage & "Skins/"`）不是真实文件，应用/保存前先导出到 `PathTemp\SkinApply.png`。
- 皮肤库列表左键预览、右键删除；「导入皮肤」接受 64x32 或 64x64 的 png/jpg/jpeg/webp。
- 选项卡通过 `Handles ItemSkin.Check` / `Handles ItemCape.Check` 切 `PanSkin`/`PanCape`（按钮区）与 `PanSkinScroll`/`PanCapeScroll`（左列表）；`Checked="True"` 会在 XAML 加载时触发事件，处理器需判空。
- 页面刷新用 `Handles Me.PageEnter`（`Me.Loaded` 在缓存页面再次进入时不一定触发）。

## 七、测试方式

- 直接运行 `pcl3-成品` 里的 exe，日志在 exe 同目录 `PCL\Log1.txt`，首行会显示版本号。
- 验证下载：进入「下载」页，日志应出现 `DlClientList Main ... Finished`，界面不应再出现「为遵守相关法律法规…」提示。
- 验证主题：设置 → 个性化，所有主题（含玄素黑等隐藏主题）均可直接点击选中，无赞助横幅。
