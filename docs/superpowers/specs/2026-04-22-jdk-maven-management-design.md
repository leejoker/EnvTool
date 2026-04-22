# JDK & Maven Management Design

## Overview

为 EnvTool 添加两个独立窗口：JDK Management 和 Maven Management，实现版本管理、环境变量配置、仓库地址管理和源管理功能。

## Architecture

### 整体架构

- **JDK Management** — 扩展现有 `JpvmService`，复用其 Download/Install/Use/Remove 逻辑
- **Maven Management** — 新建 `MavenService`，管理本地配置和源

两个独立窗口，各自对应独立的 ViewModel 和 View。

### 复用策略

| 现有模块 | 用途 |
|---------|------|
| JpvmService | JDK 下载、安装、切换、卸载 |
| FileUtils | 文件操作（读写配置、目录遍历） |
| CmdUtils | 系统命令执行 |
| SysInfo | 系统/架构检测 |

## JDK Management 窗口

### 功能列表

| 功能 | 描述 |
|------|------|
| 查看已安装 JDK | DataGrid 展示本地 JDK 版本列表 |
| 安装 JDK | 从 gitee.com/monkeyNaive/jpvm 下载并安装 |
| 切换 JDK | 修改 JAVA_HOME 并更新 PATH |
| 卸载 JDK | 删除本地 JDK 目录 |
| 设置 JAVA_HOME | 环境变量配置 |

### UI 布局

```
┌─────────────────────────────────────────┐
│ JDK Management                      [X] │
├─────────────────────────────────────────┤
│ [本地版本] [可安装版本]                    │  ← TabControl
├─────────────────────────────────────────┤
│ DataGrid                                 │
│ ┌────────┬─────────┬───────────────┐   │
│ │发行版  │ 版本    │ 路径           │   │
│ ├────────┼─────────┼───────────────┤   │
│ │Zulu    │ 21     │ ~/.jpvm/jdks/..│   │
│ │...     │ ...    │ ...            │   │
│ └────────┴─────────┴───────────────┘   │
├─────────────────────────────────────────┤
│ [安装] [切换] [卸载] [刷新]               │
└─────────────────────────────────────────┘
```

- **本地版本 Tab**：显示已安装的 JDK，路径指向 `~/.jpvm/jdks/`
- **可安装版本 Tab**：从 `versions.json` 获取可选版本列表

### 数据流

```
JpvmService.Current() / JpvmService.DownloadVersionList()
       ↓
JdkManagementViewModel (ObservableCollection<JdkItem>)
       ↓
DataGrid 展示
       ↓
用户操作 → JpvmService.Install / JpvmService.Use / JpvmService.Remove
```

## Maven Management 窗口

### 功能列表

| 功能 | 描述 |
|------|------|
| 查看当前 Maven 版本 | 显示已安装 Maven 版本和 MAVEN_HOME |
| 配置 settings.xml | 管理仓库地址、认证信息、镜像配置 |
| 管理 Maven 源 | 添加/删除/切换仓库源（如阿里云 Maven 镜像） |
| 设置 MAVEN_HOME | 环境变量配置 |

### UI 布局

```
┌─────────────────────────────────────────┐
│ Maven Management                   [X] │
├─────────────────────────────────────────┤
│ 当前版本: Maven 3.9.6                     │
│ MAVEN_HOME: /usr/local/maven            │
├─────────────────────────────────────────┤
│ [源管理] [settings.xml]                  │  ← TabControl
├─────────────────────────────────────────┤
│ 源管理 Tab:                              │
│ ┌─────────────────┬───────────────┐    │
│ │ 源名称           │ URL           │    │
│ ├─────────────────┼───────────────┤    │
│ │ 阿里云 Maven    │ mirrors.ali.. │    │
│ │ Maven Central   │ repo.maven... │    │
│ └─────────────────┴───────────────┘    │
│ [添加源] [删除源] [设为默认]              │
├─────────────────────────────────────────┤
│ settings.xml Tab:                       │
│ ┌───────────────────────────────────┐  │
│ │ <localRepository>...</localRepo>  │  │
│ │ <mirrors>...</mirrors>            │  │
│ │ <servers>...</servers>            │  │
│ └───────────────────────────────────┘  │
│ [保存] [重置]                            │
└─────────────────────────────────────────┘
```

### 数据流

```
MavenService.GetVersion() / MavenService.GetSettings()
       ↓
MavenManagementViewModel
       ↓
TabControl 展示源列表 / settings.xml 内容
       ↓
用户操作 → MavenService.SaveSettings / MavenService.AddSource / MavenService.SetDefaultSource
```

## 关键实现细节

### JdkManagementViewModel

- `InstalledJdks: ObservableCollection<JdkItem>` — 本地 JDK 列表
- `AvailableJdks: ObservableCollection<JdkItem>` — 可安装版本列表
- `SelectedJdk: JdkItem` — 当前选中项
- `InstallCommand`: 调用 `JpvmService.Install`
- `UseCommand`: 调用 `JpvmService.Use`
- `RemoveCommand`: 调用 `JpvmService.Remove`

### MavenService (新建)

- `MavenHome: string` — MAVEN_HOME 路径
- `GetVersion()` — 检测当前 Maven 版本
- `GetSettings()` — 读取 ~/.m2/settings.xml
- `SaveSettings(content: string)` — 写入 settings.xml
- `GetSources()` — 解析并返回源列表
- `AddSource(source: MavenSource)` — 添加新源
- `RemoveSource(id: string)` — 删除源
- `SetDefaultSource(id: string)` — 设置默认源

### MavenManagementViewModel

- `CurrentVersion: string`
- `MavenHome: string`
- `Sources: ObservableCollection<MavenSource>`
- `SettingsContent: string` — settings.xml 原始内容
- `SaveCommand`, `AddSourceCommand`, `RemoveSourceCommand`, `SetDefaultCommand`

## 文件结构

```
EnvTool/
├── Services/
│   └── MavenService.fs          # 新建
├── ViewModels/
│   ├── JdkManagementViewModel.fs  # 扩展现有
│   └── MavenManagementViewModel.fs # 新建
├── Views/
│   ├── JdkManagementWindow.axaml   # 新建
│   ├── JdkManagementWindow.axaml.fs # 新建
│   ├── MavenManagementWindow.axaml  # 新建
│   └── MavenManagementWindow.axaml.fs # 新建
```

## 技术约束

- 使用 Avalonia 12.0 UI 框架
- 使用 ReactiveUI 进行 MVVM 绑定
- Maven settings.xml 解析使用手动 XML 解析，不引入额外依赖
- JDK 版本列表通过 `versions.json` 获取，复用 `JpvmService.DownloadVersionList`
