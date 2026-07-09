# MCNPC Toolkit

> MCNPC 拓展制作工具箱 - Minecraft 网易版 NPC DLC 拓展包制作工具  
> 支持**皮肤拓展**和**模型拓展**两种模式 · 纯 EXE 单文件运行

---

## 功能

### 皮肤拓展制作
- 添加 / 批量添加 PNG 贴图，自动生成 NPC 皮肤包
- 拖拽导入、ZIP 导入 / 导出
- 编辑、预览、批量编辑
- 一键打包为可直接导入游戏的 ZIP

### 模型拓展制作
- 添加 3D 模型（`.geo.json` + 贴图 + 动画）
- 配置碰撞箱、实体动画（idle/walk/walka/attack/death）、皮肤变体
- 自动生成行为包实体、资源包实体、sounds.json 注入、渲染控制器匹配
- 一键打包为可直接导入游戏的 ZIP

### 开发者工具箱
- 启动网易开发版互通 MC
- 一键生成 Minecraft 网易版模组完整框架（外部 Python 脚本）
- 批量生成 Minecraft 物品模板（外部 Python 脚本）

### MCStudio 项目配置管理
- 自动扫描 MCStudio 下载目录中的项目列表
- 搜索、更改项目源代码路径、查看配置目录
- 查看测试存档列表：匹配 MC_GAME 配置，显示存档名/版本号
- 修改游戏版本、打开存档文件夹

### 存档全局配置
- FeverGames 正式端 / 测试端自由切换
- 浏览玩家 config 文件，按修改时间排序
- 搜索、打开文件、资源管理器定位

### 其他
- **3D 文字**：WebView2 嵌入在线工具
- **字体设置**：系统字体下拉选择 + 自定义字体文件

### UI 特性
- PCL2 视觉风格：无边框圆角窗口、卡片式布局、蓝色主题
- 流畅动画：页面卡片错峰入场、弹窗旋转下落、Logo 方块浮动
- 自定义控件：MyButton / MyIconButton / MyTextBox / MySlider / MyMsgBox / LogoIcon
- 半透明卡片 + 自定义背景图支持
- 跟随系统主题色 / 手动调色

---

## 下载

[▶ 版本 2.0 下载](../../releases/tag/v2.0.0)

解压后直接运行 `NpcSkinMaker.exe`，无需安装任何依赖。

> 仅需 Windows 10 1903+ 系统（自带 .NET Framework 4.8）。

---

## 技术栈

| 项 | 值 |
|---|---|
| 框架 | WPF + .NET Framework 4.8 |
| 语言 | C#（PolySharp 支持最新语法） |
| 发布 | 单 EXE 文件（Costura.Fody 嵌入所有依赖） |
| 编译 | `dotnet build`（需 .NET 8 SDK） |

## 编译

```bash
dotnet build NpcSkinMaker/NpcSkinMaker.csproj -c Release
```

输出：`NpcSkinMaker/bin/Release/net48/NpcSkinMaker.exe`（约 760KB，单文件）

## 项目结构

```
├── App.xaml / .cs                # 全局资源
├── MainWindow.xaml / .cs          # 主窗口 + 导航 + 功能切换
├── Animation/AniHelper.cs         # 动画系统
├── Theme/ThemeManager.cs          # 主题色阶 + 系统色跟随
├── Controls/                      # 自定义控件库
├── Core/                          # 业务逻辑（皮肤+模型+设置）
├── Pages/                         # 页面
│   ├── PageHome / PageModelHome   # 皮肤/模型首页
│   ├── Page3DText                 # 3D 文字
│   ├── PageMcTools                # 开发者工具箱
│   ├── PageSettings               # 设置
│   ├── PageMcStudioConfig         # MCStudio 项目配置
│   ├── PageSaveConfig             # 存档全局配置
│   └── PageTestSaves              # 测试存档列表
├── Dialogs/                       # 对话框
└── Resources/                     # 模板（嵌入资源）
```

## 作者

大果喵 (DaGuoNeko)
