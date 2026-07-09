# MCNPC 拓展制作工具箱 架构说明文档

> 本文档供 AI 或开发者快速理解项目结构、核心设计决策与已知陷阱，以便继续开发或修复 bug。

---

## 1. 项目概览

### 1.1 这是什么

一个用 **WPF (.NET Framework 4.8) + C#** 编写的「MCNPC 拓展制作工具箱」，支持两种功能模式：
- **皮肤拓展制作**：将多张 PNG 皮肤贴图打包成 NPC DLC 皮肤包 ZIP
- **模型拓展制作**：将 3D 模型（.geo.json + 贴图 + 动画）打包成 NPC DLC 模型拓展包 ZIP

附加功能：
- **3D 文字**：通过 WebView2 嵌入 Cube 3D Text (https://3dtext.easecation.net/)
- **开发者工具箱**：启动网易开发版 MC、生成 MOD 框架、批量生成物品模板（调用外部 Python 脚本）
- **MCStudio 项目配置管理**：扫描 MCStudio 下载目录中的项目，支持搜索、修改 CustomWorkDir、查看测试存档列表
- **存档全局配置**：浏览 FeverGames 正式端/测试端玩家 config 文件，支持搜索、打开、资源管理器定位
- **字体设置**：系统字体下拉选择 + 自定义字体文件（.ttf/.otf）

视觉风格完全仿照 **PCL2 (Plain Craft Launcher 2)**：无边框圆角窗口、卡片式布局、蓝色主题、流畅动画、自定义控件库、半透明背景。

### 1.2 原项目

- 皮肤功能：从 `C:\Users\cat\Desktop\newexe\自动NPC皮肤打包项目` 的 Python+PyQt5 版本 1:1 移植业务逻辑
- 模型功能：从 `C:\Users\cat\Desktop\sub_npc_models_test\NPC模型拓展制作器` 的 Python+PyQt5 版本 1:1 移植业务逻辑
- UI 全部用 WPF 重写并增强，采用 PCL2 视觉风格

### 1.3 编译环境

| 项 | 值 |
|---|---|
| 框架 | .NET Framework 4.8 (net48) |
| 语言版本 | **C# latest**（通过 PolySharp 1.15.0 源生成器支持最新语法特性） |
| 项目格式 | SDK-style csproj (`Microsoft.NET.Sdk.WindowsDesktop`) |
| 编译器 | .NET 8 SDK 自带的 MSBuild 17.9 (`dotnet build`) |
| 第三方依赖 | Newtonsoft.Json 13.0.3、Ookii.Dialogs.Wpf 1.2.0、PolySharp 1.15.0、Microsoft.Web.WebView2 1.0.4078.44、Costura.Fody 6.2.0、Fody 6.9.3、Microsoft.NETFramework.ReferenceAssemblies 1.0.3（均通过 NuGet） |
| 编译命令 | `dotnet build NpcSkinMaker.csproj -c Debug` |
| 输出路径 | `bin\Debug\net48\NpcSkinMaker.exe` |

### 1.4 依赖打包（Costura.Fody）

通过 `Costura.Fody` 将大多数托管 DLL 嵌入 exe：

- **嵌入的 DLL**：Newtonsoft.Json.dll、Ookii.Dialogs.Wpf.dll、Microsoft.Web.WebView2.Wpf.dll
- **排除的 DLL**（不嵌入）：Microsoft.Web.WebView2.WinForms.dll
- **原生 DLL 嵌入**：WebView2Loader.dll (x64) 通过 `FodyWeavers.xml` 的 `Unmanaged64Assemblies` 配置嵌入
- **构建后步骤**：`ForceX64WebView2` target 强制将 x64 版 WebView2Loader.dll 复制到输出目录，防止 NuGet 自动复制 x86 版本

配置文件：
- `FodyWeavers.xml`：Costura 配置（嵌入/排除规则）
- `FodyWeavers.xsd`：XML schema（Fody 自动生成）

### 1.5 WebView2 运行时要求

Page3DText 使用 WebView2 嵌入网页。运行时需要：
- **Windows 11**：自带 WebView2 Runtime
- **Windows 10**：通常需要安装 [WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703)
- `WebView2Loader.dll` 已通过 Costura 嵌入 exe，无需额外分发

### 1.6 C# 语法支持

项目已升级为 **C# latest** + **PolySharp** 源生成器，支持所有现代 C# 语法（字符串插值、null 条件、表达式体成员、`nameof`、`out var`、模式匹配、元组等）。新代码建议使用现代语法。

### 1.7 已移除的内容

- `cube3d/` 本地离线文件：3D 文字功能改为在线加载 https://3dtext.easecation.net/
- `ARCHITECTURE.md` 已加入 `.gitignore`，不再被 Git 跟踪

---

## 2. 项目结构

```
NpcSkinMaker/
├── NpcSkinMaker.sln
└── NpcSkinMaker/
    ├── NpcSkinMaker.csproj          # 项目文件（SDK-style）
    ├── FodyWeavers.xml              # Costura.Fody 配置（DLL嵌入）
    ├── FodyWeavers.xsd              # Fody XML Schema
    ├── App.xaml / .cs               # 应用入口，全局资源（颜色/画刷/字体/全局样式）
    ├── MainWindow.xaml / .cs        # 无边框主窗口 + 导航 + NavButton/TabButton + NavIndicator

    ├── Animation/
    │   └── AniHelper.cs             # 动画工具类（颜色/透明度/缩放/位移/页面动画/弹窗动画/AniDouble/AniThickness）

    ├── Theme/
    │   └── ThemeManager.cs          # HSL 色阶生成 + 运行时换色 + 系统主题色检测

    ├── Controls/                    # 自定义控件库
    │   ├── MyButton.xaml/.cs        # 圆角文字按钮
    │   ├── MyIconButton.xaml/.cs    # SVG path 图标按钮
    │   ├── MyTextBox.xaml/.cs       # 圆角输入框（含 Hint + Password 模式）
    │   ├── MyCard.xaml/.cs          # 卡片容器（当前未在页面中使用）
    │   ├── MyHint.xaml/.cs          # Toast 通知条
    │   ├── MyMsgBox.xaml/.cs        # PCL2 风格自定义消息框
    │   ├── MySlider.xaml/.cs        # 自定义滑块（Line + Ellipse，无 WPF Slider 默认样式）
    │   ├── LogoIcon.xaml/.cs        # Minecraft 方块 Logo（自带浮动动画，DependencyProperty 支持）
    │   ├── MyResizer.cs             # Win32 窗口缩放器（最小尺寸限制，无16:9比例锁）
    │   ├── MyScrollViewer.cs        # 自定义 ScrollViewer（使用全局 ScrollBar 样式）
    │   ├── DialogAnimationHelper.cs # 对话框动画辅助（遮罩+同步主窗口+圆角裁剪）
    │   └── BlurHelper.cs            # Win32 窗口毛玻璃效果（当前未启用）

    ├── Core/                        # 业务逻辑（从 Python 移植）
    │   ├── SkinData.cs              # 皮肤数据模型
    │   ├── SkinManager.cs           # 皮肤增删改查（含 ImportFromDict 支持 JArray/JObject）
    │   ├── PackageBuilder.cs        # 皮肤 ZIP 打包核心流程
    │   ├── ModelEntry.cs            # 模型数据模型 + ModelTexture/ModelSkin
    │   ├── ModelManager.cs          # 模型增删改查（含上移/下移）
    │   ├── ModelPackageBuilder.cs   # 模型 ZIP 打包核心流程
    │   ├── Utils.cs                 # 工具函数（UUID/PNG验证/文件操作/解压/重命名/文本替换）
    │   ├── Logger.cs                # 日志写入 %LocalAppData%\Temp\NPC_SkinMaker\
    │   └── AppSettings.cs           # 设置持久化 JSON（含 UseSystemAccent / BgImagePath）

    ├── Pages/                       # 页面（UserControl）
    │   ├── PageHome.xaml/.cs        # 皮肤首页：皮肤列表管理
    │   ├── PageModelHome.xaml/.cs   # 模型首页：模型列表管理
    │   ├── Page3DText.xaml/.cs      # 3D 文字：WebView2 嵌入在线工具
    │   ├── PageMcTools.xaml/.cs     # 开发者工具箱：3张卡片（启动MC/生成MOD框架/生成物品模板）
    │   ├── PageSettings.xaml/.cs    # 设置页（主题/背景/输出目录/字体）
    │   ├── PageAbout.xaml/.cs       # 关于页
    │   ├── PageMcStudioConfig.xaml/.cs  # MCStudio 项目配置管理
    │   ├── PageSaveConfig.xaml/.cs      # 存档全局配置（FeverGames）
    │   └── PageTestSaves.xaml/.cs       # 测试存档列表

    ├── Dialogs/                     # 对话框（Window）
    │   ├── AddSkinDialog.xaml/.cs
    │   ├── BatchAddSkinDialog.xaml/.cs
    │   ├── BatchEditDialog.xaml/.cs
    │   ├── EditSkinDialog.xaml/.cs
    │   ├── PreviewSkinDialog.xaml/.cs
    │   └── EditModelDialog.xaml/.cs

    ├── Resources/
    │   ├── template.zip             # 皮肤打包模板（EmbeddedResource）
    │   ├── template_models.zip      # 模型打包模板（EmbeddedResource）
    │   └── icon.ico                 # 应用图标

    └── Properties/
        └── AssemblyInfo.cs
```

---

## 3. 核心架构

### 3.1 全局架构图

```
App.xaml (全局资源: 颜色/画刷/字体/样式)
  └── MainWindow (无边框窗口，最小 900×550，支持最大化)
        ├── PanTitle (标题栏: LogoIcon + 标题 + 功能切换按钮 + 最小化/最大化/关闭)
        │   └── PanTopTabs (功能切换: 皮肤拓展制作 | 模型拓展制作)
        ├── PanMainLeft (左侧导航: NavButton × 5 + NavIndicator 滑动指示条)
        │   ├── 首页      -> index 0: PageHome(皮肤) 或 PageModelHome(模型) (每次新建)
        │   ├── 3D 文字   -> index 1: Page3DText (WebView2, 缓存)
        │   ├── 开发者工具箱 -> index 2: PageMcTools (不缓存，状态经 AppSettings 持久化)
        │   ├── 设置      -> index 3: PageSettings (每次新建)
        │   └── 关于      -> index 4: PageAbout (每次新建)
        └── PanRightContent (右侧内容区: 页面切换容器)
            └── (层叠: ImgBackground 背景层 + 内容层)
```

MainWindow 持有:
- `SkinManager` / `PackageBuilder` - 皮肤业务
- `ModelManager` / `ModelPackageBuilder` - 模型业务
- `AppSettings` - 用户设置持久化（含 ModScriptPath, ModOutDir, ModName, ModHelp, ModHud, ModWorldData, ModSetting, ItemScriptPath, McPath）
- `_currentTool` (0=皮肤, 1=模型, 控制 index=0 加载哪个页面)
- `_currentPageIndex` (0-4)
- `_page3DText` / `_pageMcTools` - 缓存页面实例（避免重复初始化 WebView2）

### 3.2 单例访问

`MainWindow` 通过静态属性 `Instance` 暴露全局访问：

```csharp
MainWindow.Instance.SkinManager
MainWindow.Instance.ModelManager
MainWindow.Instance.PackageBuilder
MainWindow.Instance.ModelPackageBuilder
MainWindow.Instance.Settings
```

### 3.3 功能切换

顶部标题栏中间嵌入两个 `TabButton`（皮肤拓展制作 / 模型拓展制作）。点击切换 `_currentTool`，强制重新加载 index=0 页面（`PageHome` 或 `PageModelHome`）。

---

## 4. 主题与颜色系统

### 4.1 8 级色阶（仿 PCL2）

在 `App.xaml` 中定义 8 个 `ColorObject` + 对应的 `ColorBrush`，通过 `ThemeManager.Apply(hue, sat)` 在运行时用 HSL 重新计算替换。

| Key | 用途 | HSL (hue=210, sat=85) |
|-----|------|----------------------|
| ColorObject1 | 深色文字/图标 | sat×0.2, L=25 |
| ColorObject2 | 标题栏强调色 | sat, L=45 |
| ColorObject3 | 焦点/悬停/指示条 | sat, L=55 |
| ColorObject4 | 悬停 | sat, L=65 |
| ColorObject5-8 | 渐浅背景 | sat, L=80/91/95/97 |

另有 8 级灰阶、红色系列、以及 `HalfWhite`/`SemiWhite`/`White`/`SemiTransparent`。

### 4.2 系统主题色检测

`ThemeManager.DetectSystemAccent()` 从注册表 `HKCU\Software\Microsoft\Windows\DWM\AccentColor` 读取 Windows 系统主题色（ABGR 格式），转换为 HSL 后应用。

`AppSettings.UseSystemAccent` 开关（默认 `true`）控制是否跟随系统主题。在 `PageSettings` 中通过 CheckBox 切换。开启时色相/饱和度滑块被禁用。

### 4.3 冻结画笔陷阱（已修复）

XAML 中 `{StaticResource ColorBrushN}` 创建的 `SolidColorBrush` 是 Frozen 的，运行时不能修改 `.Color`。`ThemeManager.UpdateBrushes()` 创建新画笔替换；`AniHelper.AniColor()` 检测 frozen 并替换为可变副本。

### 4.4 XAML 中颜色绑定

- 用 `{DynamicResource ColorBrushN}` 绑定（支持运行时替换）
- 代码中用 `Application.Current.TryFindResource("ColorBrushN")` 获取
- 不要用 `{StaticResource}` 绑定画笔（会冻结）

---

## 5. 自定义控件体系

### 5.1 控件继承关系

```
Border (WPF)
  ├── MyButton        (文字按钮)
  ├── MyIconButton    (图标按钮)
  ├── MyTextBox       (输入框)
  ├── MyHint          (Toast 提示)
  ├── MyCard          (卡片容器, 未使用)
  ├── MySlider        (自定义滑块)
  ├── NavButton       (导航按钮, MainWindow.xaml.cs)
  └── TabButton       (功能切换按钮, MainWindow.xaml.cs)

UserControl (WPF)
  └── LogoIcon        (MC 方块 Logo，自带浮动动画)

ScrollViewer (WPF)
  └── MyScrollViewer  (使用全局 ScrollBar 样式)

Window (WPF)
  └── MyMsgBox        (自定义消息框)
```

### 5.2 MySlider

自定义滑块，由 Line（前景+背景）+ Ellipse（圆点）构成，不使用 WPF 原生 Slider。属性：`Value`、`MaxValue`（DependencyProperty）。事件：`ValueChanged` (Action&lt;double&gt;)。支持点击轨道跳转、拖拽滑块、键盘方向键微调。拖拽时圆点缩放到 1.3（100ms），释放后回弹（200ms back-ease）。属性变化时前景宽度播放 50ms 动画过渡。

### 5.3 LogoIcon

Minecraft 方块 Logo 的重用 UserControl，自带浮动动画。DependencyProperty：`Stroke`(Brush)、`Fill`(Brush)、`StrokeThickness`(double)，均支持 `DynamicResource` 绑定。浮动动画：Y 轴 ±1.5px，1700ms 周期，SineEase 缓动，无限循环往复。下方有黑色渐变径向阴影。用法：`<local:LogoIcon Width="20" Height="20" Stroke="White" Fill="#22FFFFFF" />`

### 5.4 控件事件模型

**MyButton 和 MyIconButton**：都定义了 `public new event RoutedEventHandler Click;`，通过 `MouseLeftButtonUp` 触发。`MyIconButton` 的 `MouseLeftButtonUp` 在触发 `Click` 后设置 `e.Handled = true`。

- 订阅 `BtnMin.Click` / `BtnMax.Click` / `BtnClose.Click`（Click 在 Handled 之前触发）
- 不要订阅 `BtnMin.MouseLeftButtonUp`（会被 Handled 阻断）

**TabButton**：必须设置 `MouseLeftButtonDown += (s, e) => { e.Handled = true; }` 阻止 DragMove 吞掉点击事件。
**NavButton**：同理，阻止 DragMove 冒泡。

### 5.5 DependencyProperty 解析陷阱（已修复）

`AniHelper.GetDependencyProperty()` 用反射在目标对象实际类型上查找 `XxxProperty` 字段（含 `FlattenHierarchy`），兼容 Border 子类。

### 5.6 MyMsgBox 自定义消息框

替代系统 `MessageBox.Show()`，拥有 PCL2 风格 + 黑色半透明遮罩 + 动画。

调用方式：
```csharp
MyMsgBox.Show("消息", "标题", MyMsgBox.MsgType.Warning);
var result = MyMsgBox.ShowYesNo("确定删除？", "确认", MyMsgBox.MsgType.Question);
var input = MyMsgBox.Prompt("请输入：", "输入", "默认值");
```

消息类型：Info(蓝)、Warning(黄)、Error(红)、Question(蓝)，决定标题和分隔线颜色。

**MyMsgBox.Prompt**：输入对话框，复用 MyMsgBox 的遮罩/动画机制。在消息区域插入 MyTextBox，按钮替换为 OK/Cancel。返回用户输入字符串（取消返回 null）。`CloseWithAnimation` 已改为 public 以支持 Prompt 的关闭逻辑。

**禁止使用 `MessageBox.Show()`**。

### 5.7 CardBackgroundBrush

`App.xaml` 中定义了 `CardBackgroundBrush` 资源（`#99FFFFFF` = 60% 不透明度）：
- 所有页面的卡片通过 `{DynamicResource CardBackgroundBrush}` 引用
- 左侧边栏使用 `#E6FFFFFF`（90% 不透明度），让背景图片透过

---

## 6. 无边框窗口系统

### 6.1 窗口样式

```xml
WindowStyle="None"
AllowsTransparency="True"
ResizeMode="CanResize"
Background="{x:Null}"
```

### 6.2 结构层次

```
Window
└── Grid (PanMain, Margin=10 透明边距, 带 DropShadowEffect 窗口阴影)
    ├── Grid.RenderTransform (TransformGroup: RotateTransform + TranslateTransform)
    ├── ImgBackground (Image 层: 自定义背景图片, 跨越所有行/列, Opacity=0.3, UniformToFill)
    ├── 8× Rectangle (缩放手柄)
    └── Border (BorderForm, CornerRadius=6, 圆角裁剪)
        └── Grid (2 行)
            ├── Row 0: Border (PanTitle, 标题栏, 背景=ColorBrush2 渐变)
            │   ├── LogoIcon + 标题文字
            │   ├── PanTopTabs (StackPanel: TabButton × 2, 居中)
            │   └── MyIconButton × 3 (最小化/最大化/关闭, 右对齐)
            └── Row 1: Grid (2 列)
                ├── Col 0: Border (PanMainLeft, 左侧导航, 白色)
                │   └── StackPanel (PanLeftItems: NavButton × 5)
                │   └── NavIndicator (3px 宽, ColorBrush3, CornerRadius=1.5)
                └── Col 1: Border (PanMainRight, 右侧内容, Background=Transparent)
                    └── Grid (PanRightContent, 页面容器)
```

### 6.3 窗口缩放

通过 Win32 `SendMessage(WM_SYSCOMMAND, SC_SIZE)` 原生缩放。

| 消息 | 作用 |
|------|------|
| `WM_SIZING` (0x0214) | 实时修正窗口 RECT，仅强制不小于最小尺寸 900×550 |
| `WM_GETMINMAXINFO` (0x0024) | 限制最小 900×550，最大化时占满工作区（避开任务栏） |

**边缘拖拽不再锁定 16:9 比例**，可以自由拉伸。仅强制最小尺寸。初始窗口尺寸 1100×619。

缩放计算使用 `BorderForm.PointToScreen()` 获取精确的屏幕位置。

### 6.4 最大化按钮

标题栏有最大化/还原按钮（`BtnMax`），点击调用 `ToggleMaximize()`：

- **最大化**：保存当前位置和尺寸到 `_storedLeft/Top/Width/Height`，设置 `WindowState=Maximized`。手动调整 `Left/Top/Width/Height` 为当前屏幕工作区尺寸，图标切换为 `IconRestore`。
- **还原**：恢复存储的位置和尺寸，`WindowState=Normal`，图标切换为 `IconMaximize`。
- **无动画**：直接切换状态，不使用动画过渡。

### 6.5 标题栏拖拽

`PanTitle.MouseLeftButtonDown` 绑定 `DragMove()`。`MyIconButton` 和 `TabButton` 都设置 `MouseLeftButtonDown` 的 `e.Handled = true` 阻止冒泡。

### 6.6 入场动画

窗口加载时从 `-4°` 旋转 + `Y=60` 偏移归位（500ms）。

### 6.7 背景图片

主窗口支持自定义背景图片：

- `MainWindow` 中 `ImgBackground`（Image 元素）跨越所有行/列，位于内容层之下
- `Opacity=0.3`，`Stretch=UniformToFill`
- `AppSettings.BgImagePath` 存储路径
- `PageSettings` 提供浏览/清除背景图片的按钮
- 页面和右侧面板的背景色已设为 `Transparent`，让背景图片透过半透明卡片得以显示

---

## 7. 页面导航系统

### 7.1 导航流程

```
NavButton.MouseLeftButtonUp -> Selected -> NavigateToPage(index)
  -> 更新 NavButton.IsSelected
  -> NavIndicator 滑动动画（100ms AniThickness）
  -> 旧页面整体淡出
  -> PanRightContent.Clear + Add(newPage)
  -> 新页面 Loaded 事件触发 PageEnterAnimation
```

页面切换使用 `_isTransitioning` 锁防止并发切换。`Page3DText` 缓存为字段（避免重复初始化 WebView2）。`PageMcTools` 不再缓存实例，但其表单状态通过 `AppSettings` 持久化，切换页面不丢失输入。

### 7.2 导航列表

| index | 名称 | _currentTool=0 (皮肤) | _currentTool=1 (模型) | 位置 | 缓存? |
|-------|------|----------------------|----------------------|------|-------|
| 0 | 首页 | PageHome (皮肤列表) | PageModelHome (模型列表) | 标题栏 | 否 |
| 1 | 3D 文字 | Page3DText | Page3DText | 侧栏 | 是 |
| 2 | 开发者工具箱 | PageMcTools | PageMcTools | 侧栏 | 否（状态持久化至 AppSettings） |
| 3 | 设置 | PageSettings | PageSettings | 标题栏 | 否 |
| 4 | 关于 | PageAbout | PageAbout | 标题栏 | 否 |
| 5 | MCStudio项目配置管理 | PageMcStudioConfig | PageMcStudioConfig | 侧栏 | 否 |
| 6 | 存档全局配置 | PageSaveConfig | PageSaveConfig | 侧栏 | 否 |
| 7 | 测试存档列表 | PageTestSaves | PageTestSaves | 程序导航 | 否 |

### 7.2.1 SwitchTool 与页面动画

`SwitchTool`（顶部 TabButton 切换皮肤/模型）不再清除 `_pageMcTools` 缓存字段。页面切换动画使用 `DispatcherPriority.Render`：先将 `newPage.Opacity = 1`，再在 Render 优先级回调中触发入场动画，确保新页面已布局完毕再播放。

### 7.3 NavIndicator 滑动指示条

`NavIndicator` 是一个位于左侧导航栏的蓝色竖条（`Width=3`, `Background=ColorBrush3`, `CornerRadius=1.5`）。

当 NavigateToPage 切换页面时，通过 `AniHelper.AniThickness()` 播放 100ms 动画将 NavIndicator 的 `Margin.Top` 滑动到选中按钮位置，高度固定 28px。

计算方式：
```csharp
var transform = targetBtn.TransformToAncestor(PanMainLeft);
var pos = transform.Transform(new Point(0, 0));
double targetY = pos.Y + (targetBtn.ActualHeight - 28) / 2;
```

### 7.4 NavButton / TabButton

定义在 `MainWindow.xaml.cs` 中，继承 `Border`：
- **NavButton**：左侧导航，图标+文字，使用 `Loaded` 事件设置内容。图标通过 `MyIconButton` 常量（`IconHome`, `IconCreeper`, `IconSettings`, `IconInfo`）
- **TabButton**：顶部切换栏，文字按钮，适配深色标题栏背景（半透明白色文字）

---

## 8. 动画系统 (AniHelper)

### 8.1 动画类型

| 方法 | 作用 |
|------|------|
| `AniColor` / `AniColorByResource` | 颜色渐变（含 frozen brush 检测） |
| `AniOpacity` | 透明度渐变 |
| `AniScale` | 缩放（支持 back-ease） |
| `AniTranslate` / `AniTranslateX` / `AniTranslateY` | 位移（支持 back-ease） |
| `AniHeight` | 高度变化 |
| `AniDouble` | 对任意 DependencyProperty 做 Double 动画 |
| `AniThickness` | Margin 厚度动画（用于滑块/指示条位移） |
| `PageEnterAnimation` | 页面入场：直接子元素从 Y=-40 下落 + 淡入，90ms 错峰 |
| `PageExitAnimation` | 页面退场：元素整体淡出 + 微微上飘 |
| `LeftPanelEnterAnimation` | 左侧栏滑出：每个按钮从 X=-25 滑入 |
| `DialogEnterAnimation` / `DialogExitAnimation` | 弹窗入场/退场 |

### 8.2 页面入场动画（核心变更）

通过页面 `Loaded` 事件触发。使用 `FindChildByName("PanContent")` 查找内容容器，对其**直接子元素**（card Border）做动画，不递归深入。卡片从 Y=-40 下落（450ms, BackEase Amp=0.3），同时淡入（200ms），元素间错峰 90ms。自动跳过 ScrollViewer 和 Collapsed 元素。

### 8.3 动画参数速查

| 场景 | 初始偏移 | 目标 | 时长 | 缓动 | 错峰 |
|------|---------|------|------|------|------|
| 页面入场 | Y=-40 | Y=0 | 450ms | BackEase (Amp=0.3) | 90ms/元素 |
| 页面退场 | Y=0 | Y=-8 | 100ms | QuadraticEase Out | 同时 |
| 左侧栏滑出 | X=-25 | X=0 | 300ms | BackEase (Amp=0.3) | 递减 |
| 弹窗入场 | Rot=-4°, Y=40 | 0, 0 | 300ms | BackEase + QuadEase | 延迟60ms |
| NavIndicator | Margin.Top | 目标Y | 100ms | QuadraticEase Out | 0 |

---

## 9. 对话框与遮罩系统

### 9.1 DialogAnimationHelper

所有对话框通过 `DialogAnimationHelper.Setup(this)` 统一处理。

**遮罩层**：在对话框窗口内创建 Grid 包裹层，底部加入黑色 50% 不透明遮罩（`#80000000`）。Loaded 时添加圆角 Clip（`RectangleGeometry RadiusX=6, RadiusY=6`，匹配主窗口 CornerRadius）。对话框内容居中显示在遮罩之上。

**同步主窗口**：`SyncWithMainWindow(dialog)` 通过 `BorderFormEl.PointToScreen()` + `ActualWidth/Height` 将对话框窗口定位到与主窗口完全重叠，`WindowStartupLocation=Manual`。

**不可拖拽**：`DragMove` 已移除，对话框固定居中在遮罩层上方。

**入场**：旋转 -4°->0°(300ms) + Y=40->0(300ms,回弹) + 淡入(120ms,延迟60ms) + 遮罩淡入(200ms)。
**退场**：旋转->+6°(150ms) + Y->20(150ms) + 淡出(80ms,延迟20ms) + 遮罩淡出(200ms)。

### 9.2 PCL2 风格对话框设计规范

| 元素 | 规范 |
|------|------|
| 窗口背景 | `#FBFBFB`，圆角 7px |
| 阴影 | `#343D4A`，BlurRadius=20，ShadowDepth=4，Opacity=0.25 |
| 标题 | FontSize=20，FontWeight=Bold，颜色=ColorBrush2，下方 2px 彩色分隔线 |
| 正文 | FontSize=15（消息框）或 13（表单），灰色 `#5C5C5C` |
| 按钮 | MinWidth=100，Padding=20,4，间隔 12px |

---

## 10. 业务逻辑层 (Core)

### 10.1 皮肤模块

**SkinData**：`Id, TexturePath, Name, Author, OriginalId, OriginalTexture, FromImport`

**SkinManager**：AddSkin, UpdateSkin, RemoveSkin, GetSkin, GetAllSkins, GetSkinCount, ClearSkins, ExportToDict, ImportFromDict。最多 1000 个。

**ImportFromDict 修复**：数据来自 `Newtonsoft.Json` 反序列化，嵌套的数组和对象实际类型是 `JArray` / `JObject`（而非 `List<object>` / `Dictionary<string, object>`）。方法已按 `JArray` / `JObject` 双路径处理。

**PackageBuilder** 10 步：生成包名 -> 解压 template.zip -> 重命名资源包/行为包文件夹 -> 更新 UUID -> 生成 npcskinlist.json -> 复制贴图 -> 重命名脚本目录 -> 创建 ZIP -> 清理临时目录。

### 10.2 导入 ZIP 灵活配置查找

ImportZip 不再硬编码配置文件名。查找逻辑：
1. 优先查找 `*_skindlc.json` 文件
2. 其次查找 `npcsklist.json` 文件
3. 兜底扫描 `modconfigs/` 目录查找任意 `.json` 文件
4. 贴图提取使用**文件名匹配**而非完整路径匹配，兼容不同目录结构

### 10.3 模型模块

**ModelEntry**：DisplayName, CustomName, GeoPath, Textures, AnimationList, SkinList, CollisionWidth/Height, 5 种动画字段, EnableAttachables

**ModelPackageBuilder** 8 步：解压 template_models.zip -> 重命名/UUID -> sounds.json 写入 -> 模型配置 npcmodelslist -> 实体 JSON 文本替换（identifier/collision_box）-> 贴图/UI图/geo 复制 -> 脚本目录重命名 -> 清理+打包。渲染控制器按贴图数量 1-8 映射，动画回退链 idle->pass, walk->idle, walka->walk, attack->pass, death->default.death。

### 10.4 模板嵌入资源

每次启动从嵌入资源覆盖提取到 `%LocalAppData%\NPC_SkinMaker\`。通过 `EmbeddedResource` 编译进 exe。

### 10.5 Utils / Logger / AppSettings

- `Utils`：GenerateUuid, GenerateShortUid, GeneratePackageName, ValidatePngFile, SanitizeFilename, CopyDirectory, RenameDirectory, ReplaceInFile
- `Logger`：写入 `%LocalAppData%\Temp\NPC_SkinMaker\npc_skin_maker_*.log`
- `AppSettings`：持久化到 `%LocalAppData%\NPC_SkinMaker\settings.json`
  - 字段：`LastOutputDir`, `ThemeHue`, `ThemeSat`, `UseSystemAccent`, `BgImagePath`, `ModScriptPath`, `ModOutDir`, `ModName`, `ModHelp`, `ModHud`, `ModWorldData`, `ModSetting`, `ItemScriptPath`, `McPath`

### 10.6 PageMcTools 外部脚本调用

两个功能均调用外部 Python 脚本（Python 3 + argparse，支持 `--no-interactive` 和 `--output` 参数，配置通过 stdin 逐行发送）。

**GenerateMod**（生成 MOD 框架）：
- 脚本：`一键生成完整MOD.py`
- 调用：`python 一键生成完整MOD.py --no-interactive --output <ModOutDir>`
- stdin 发送：MOD 名称、脚本路径、4 个复选框状态（ModHelp/ModHud/ModWorldData/ModSetting）

**GenerateItem**（批量生成物品模板）：
- 脚本：`autoMinecraftitem.py`
- 调用：`python autoMinecraftitem.py --no-interactive --output <ModOutDir>`
- stdin 发送：namespace、name、cnname、tab、start/end index、物品类型、自定义类型
- 额外参数（耐久度、伤害值等）通过 `MyMsgBox.Prompt` 弹窗输入

---

## 11. 页面详解

### 11.1 PageHome（皮肤首页）

操作按钮：添加皮肤 / 批量添加 / 批量编辑 / 导入ZIP / 导出ZIP / 清空列表。皮肤列表每行：序号 + 缩略图 + 名称 + 作者 + 编辑/预览/删除按钮。支持拖拽 PNG 导入。

### 11.2 PageModelHome（模型首页）

操作按钮：添加模型 / 上移 / 下移 / 清空 / 导出ZIP。模型列表每行：序号 + 名称 + 标识符 + 来源 + 贴图数 + 动画数 + 编辑/删除按钮。

### 11.3 Page3DText（3D 文字）

通过 **WebView2** 嵌入 Cube 3D Text 在线工具：

- XAML 中放置 `<wpf:WebView2 x:Name="WebView" />`
- 初始化时创建 `CoreWebView2Environment`，调用 `EnsureCoreWebView2Async`
- 加载 `https://3dtext.easecation.net/`
- `NavigationCompleted` 事件中隐藏"加载中..."指示器
- 页面实例在 MainWindow 中缓存，避免重复初始化 WebView2 环境
- 左侧导航使用 Creeper 图标（`MyIconButton.IconCreeper`）

### 11.4 PageMcTools（开发者工具箱）

3 张卡片，所有表单字段持久化到 `AppSettings`（ModName, ModHelp, ModHud, ModWorldData, ModSetting, ItemScriptPath, McPath, ModScriptPath, ModOutDir）。

**卡片 1 - 启动网易开发版互通MC**：选择 MC 路径（McPath 保存到设置），点击启动后显示加载弹窗并检测游戏进程窗口。

**卡片 2 - 一键生成Minecraft网易版模组完整框架**：输入脚本路径（ModScriptPath）、MOD 名称（ModName）、4 个复选框（ModHelp/ModHud/ModWorldData/ModSetting）。点击生成调用 Python 脚本（见 10.6）。

**卡片 3 - 批量生成Minecraft物品模板**：输入 namespace、name、cnname、tab、起始/结束 index、物品类型单选按钮、自定义类型复选框、脚本路径（ItemScriptPath）。点击生成时先通过 `MyMsgBox.Prompt` 询问额外参数（耐久度、伤害值等），再调用 Python 脚本（见 10.6）。

### 11.5 PageSettings

输出目录选择、主题色（系统主题开关 + 手动色相/饱和度 MySlider）、背景图片（浏览/清除）。系统主题开启时禁用手动滑块。MySlider 替代 WPF Slider，无动画延迟。

### 11.6 PageAbout

版本信息、使用说明。

---

## 12. 全局样式 (App.xaml)

| 样式 | 说明 |
|------|------|
| TextBlock / Label | 微软雅黑，FontSize=13，ColorBrush1 |
| ScrollBar | 8px 宽，圆角，半透明 |
| ScrollViewer | 自定义滚动条模板 |
| TabControl | PCL2 风格：透明背景、无边框 |
| TabItem | PCL2 风格扁平圆角标签 |
| CheckBox | PCL2 风格：18×18 圆角方框 |
| CardBackgroundBrush | `#99FFFFFF`（60% 不透明度）|
| Slider | 自定义细条轨道 + 圆形滑块样式 |
| SelectableText | 只读 TextBox 样式 |

字体：`Microsoft YaHei UI, Microsoft YaHei`

全局工具：`MyMsgBox.Prompt` - 复用 MyMsgBox 遮罩的输入对话框，提供 OK/Cancel 按钮和 MyTextBox 输入区域。

---

## 13. 已修复的 Bug 记录

| # | Bug | 方案 |
|---|-----|------|
| 13.1 | NameScope 冲突 | 页面用普通 Border 代替 MyCard |
| 13.2 | 冻结画笔异常 | ThemeManager 创建新画笔；AniHelper 检测 frozen |
| 13.3 | DependencyProperty 解析错误 | 反射查找实际类型的 XxxProperty |
| 13.4 | 关闭/最小化按钮不工作 | 订阅 Click 事件而非 MouseLeftButtonUp |
| 13.5 | 窗口拖动变全屏 | Win32 SendMessage(WM_SYSCOMMAND, SC_SIZE) |
| 13.6 | 导航文字不显示 | 用 Loaded 事件而非 OnInitialized |
| 13.7 | template.zip 未嵌入 | 改为 EmbeddedResource |
| 13.8 | PackageBuilder 空引用 | 增加 null 检查 |
| 13.9 | TabButton 点击无效 | MouseLeftButtonDown 设 e.Handled=true |
| 13.10 | MyMsgBox 遮罩未覆盖全屏 | WindowState=Maximized + Topmost=True |
| 13.11 | ImportFromDict 类型转换错误 | 按 JArray/JObject 路径处理 Newtonsoft 类型 |
| 13.12 | WebView2Loader.dll x86/x64 不匹配 | NuGet 默认复制 x86，post-build target (ForceX64WebView2) 强制复制 x64 |
| 13.13 | ImportFromDict JArray/JObject 类型不匹配 | Newtonsoft.Json 反序列化返回 JArray 而非 List&lt;object&gt;，按 JArray/JObject 处理 |
| 13.14 | PageMcTools 页面切换丢状态 | 将所有表单字段持久化到 AppSettings |
| 13.15 | Prompt 对话框出现在主窗口外 | 复用 MyMsgBox 遮罩层，与主窗口对齐 |

---

## 14. 开发注意事项

### 14.1 添加新页面

1. 在 `Pages/` 创建 `PageXxx.xaml` + `.xaml.cs`
2. 在 `MainWindow.InitializeNavigation()` 添加导航按钮 + `_navButtons` 列表
3. 在 `NavigateToPage()` 添加 case 分支
4. 如需缓存页面（如 WebView2），在 MainWindow 声明字段并缓存

### 14.2 添加新对话框

1. 在 `Dialogs/` 创建 `XxxDialog.xaml` + `.xaml.cs`
2. XAML 遵循 PCL2 设计规范（见 9.2）
3. 构造函数末尾调用 `DialogAnimationHelper.Setup(this);`
4. 关闭用 `DialogAnimationHelper.PlayExitAnimationAndClose(this);`
5. **禁止使用 `MessageBox.Show()`**

### 14.3 添加新功能模块

1. `Core/` 创建 XxxEntry.cs + XxxManager.cs + XxxPackageBuilder.cs
2. `Resources/` 添加 template_xxx.zip（EmbeddedResource）
3. `Pages/` 创建 PageXxxHome.xaml/.cs
4. `Dialogs/` 创建 EditXxxDialog.xaml/.cs
5. MainWindow 添加初始化 + 导航注册 + NavigateToPage 分支
6. MainWindow 持有 XxxManager + XxxPackageBuilder 属性

### 14.4 添加新控件

项目为 SDK-style csproj，`.cs` 和 `.xaml` 文件自动包含，无需手动编辑 csproj。

### 14.5 WebView2 注意事项

- Page3DText 的 WebView2 初始化是异步的，需要 `async void` 或 `async` Loaded handler
- 页面缓存可避免重复初始化 WebView2 环境（开销较大）
- 必须确保 `WebView2Loader.dll` (x64) 被正确复制到输出目录（`ForceX64WebView2` post-build target）

### 14.6 外部 Python 脚本约定

PageMcTools 调用的两个外部 Python 脚本均已升级为 Python 3 + argparse：
- 统一参数：`--no-interactive`（非交互模式）、`--output <dir>`（输出目录）
- 配置数据通过 stdin 逐行发送（每行一个键值）
- 调用前由 C# 端构造配置文本并写入进程 stdin

---

## 15. 文件路径速查

| 文件 | 路径 |
|------|------|
| 项目根 | `C:\Users\cat\Desktop\newexe\NpcSkinMaker\` |
| 主窗口 | `NpcSkinMaker\MainWindow.xaml(.cs)` |
| 动画工具 | `NpcSkinMaker\Animation\AniHelper.cs` |
| 主题管理 | `NpcSkinMaker\Theme\ThemeManager.cs` |
| Fody 配置 | `NpcSkinMaker\FodyWeavers.xml` |
| 消息框 | `NpcSkinMaker\Controls\MyMsgBox.xaml(.cs)` |
| 自定义滑块 | `NpcSkinMaker\Controls\MySlider.xaml(.cs)` |
| Logo 图标 | `NpcSkinMaker\Controls\LogoIcon.xaml(.cs)` |
| 毛玻璃 | `NpcSkinMaker\Controls\BlurHelper.cs` |
| 对话框动画 | `NpcSkinMaker\Controls\DialogAnimationHelper.cs` |
| 窗口缩放器 | `NpcSkinMaker\Controls\MyResizer.cs` |
| 皮肤打包 | `NpcSkinMaker\Core\PackageBuilder.cs` |
| 模型打包 | `NpcSkinMaker\Core\ModelPackageBuilder.cs` |
| 皮肤管理 | `NpcSkinMaker\Core\SkinManager.cs` |
| 模型管理 | `NpcSkinMaker\Core\ModelManager.cs` |
| 模型数据 | `NpcSkinMaker\Core\ModelEntry.cs` |
| 设置 | `NpcSkinMaker\Core\AppSettings.cs` |
| 3D 文字页 | `NpcSkinMaker\Pages\Page3DText.xaml(.cs)` |
| 日志 | `%LocalAppData%\Temp\NPC_SkinMaker\npc_skin_maker_*.log` |
| 用户设置 | `%LocalAppData%\NPC_SkinMaker\settings.json` |
| 模板缓存 | `%LocalAppData%\NPC_SkinMaker\template.zip` / `template_models.zip` |
| 编译输出 | `NpcSkinMaker\bin\Debug\net48\NpcSkinMaker.exe` |
| MOD 生成脚本 | `C:\Users\cat\Desktop\pyMOD模板\自动创建Minecraft模组完整版\一键生成完整MOD.py` |
| 物品生成脚本 | `C:\Users\cat\Desktop\pyMOD模板\批量自动生成Minecraft物品模板\autoMinecraftitem.py` |

---

## 16. 编译与运行

```bash
dotnet build "C:\Users\cat\Desktop\newexe\NpcSkinMaker\NpcSkinMaker\NpcSkinMaker.csproj" -c Debug
"C:\Users\cat\Desktop\newexe\NpcSkinMaker\NpcSkinMaker\bin\Debug\net48\NpcSkinMaker.exe"
```

编译产物（bin\Debug\net48\）：
- `NpcSkinMaker.exe` -- 主程序（Newtonsoft.Json、Ookii.Dialogs.Wpf、WebView2.WPF 已通过 Costura 嵌入）
- `WebView2Loader.dll` -- WebView2 原生 x64 DLL（Costura 嵌入，post-build target 保证 x64 版本）
- `PolySharp.dll` -- 源生成器运行时不需分发
- `Microsoft.Web.WebView2.WinForms.dll` -- 排除，不嵌入

### 16.1 系统要求

- Windows 7 SP1+，.NET Framework 4.8+
- **WebView2 Runtime**：Win11 自带，Win10 通常需手动安装（Page3DText 依赖）
- 不需要 SDK/VS/Developer Pack

### 16.2 分发

拷贝编译输出目录所有文件即可运行（大部分托管 DLL 已嵌入 exe）。主要外部依赖只有 `WebView2Loader.dll`。

---

## 17. 运行时部署与数据存储

```
%LocalAppData%\NPC_SkinMaker\
├── settings.json      ← 用户设置（LastOutputDir, ThemeHue, ThemeSat, UseSystemAccent, BgImagePath）
├── template.zip       ← 皮肤模板（每次启动覆盖提取）
└── template_models.zip ← 模型模板（每次启动覆盖提取）

%LocalAppData%\Temp\NPC_SkinMaker\
└── npc_skin_maker_*.log ← 日志
```

不碰注册表（除读取系统主题色），不写 exe 所在目录，按用户隔离，绿色便携。

### 17.1 列表不持久化

皮肤列表和模型列表都只在内存中，关闭程序后清空。如需持久化，可在 Manager 中添加保存/加载 JSON 的逻辑。

---

*文档更新时间：2026-07-10*
*项目名称：MCNPC 拓展制作工具箱（v2.0）*
*视觉风格基于 PCL2，业务逻辑 1:1 移植自两个 Python 版本*
*动画参数复刻自 PCL2 ModAnimation.vb / MyPageRight.vb / MyPageLeft.vb / MyMsgText.xaml.vb*
