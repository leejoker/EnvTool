# CLAUDE.md

本项目使用中文进行交互。所有回复、注释和文档均使用中文。

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the main application
dotnet build EnvTool/EnvTool.fsproj

# Run tests
dotnet test Tests/Tests.fsproj

# Run a single test
dotnet test Tests/Tests.fsproj --filter "JpvmInstallTest"

# Publish for Windows x64
dotnet publish EnvTool/EnvTool.fsproj -c Release -r win-x64 --self-contained
```

## Architecture

This is a cross-platform (Windows/Linux/macOS) desktop environment toolbox built with:
- **Avalonia 12.0** - UI framework
- **ReactiveUI.Avalonia** - Reactive MVVM bindings
- **F#** - Primary language

### Project Structure

- `EnvTool/DataModels/` - Data models (ProxyConfigModel)
- `EnvTool/Services/` - Business logic services:
  - `JpvmService` - JDK version management (install, use, remove, current)
  - `ProxyConfigService` - Proxy configuration persistence
  - `HysteriaService` - Hysteria proxy process management
  - `StatementService` - Global statements management
- `EnvTool/Utils/` - Utilities: CmdUtils, SysInfo, FileUtils, ProxyUtils
- `EnvTool/ViewModels/` - MVVM ViewModels using ReactiveUI
- `EnvTool/Views/` - Avalonia views (.axaml + .axaml.fs code-behind)

### Platform Detection

Uses conditional compilation constants: `Windows`, `OSX`, `Linux`. Check with `#if Windows` or `SysOS`/`SysArch` from `SysInfo` module.

### Key Services

- `JpvmModule` (JpvmService.fs) - Manages JPVM_HOME (~/.jpvm), downloads JDKs from gitee.com/monkeyNaive/jpvm, sets JAVA_HOME and PATH
- `ProxyUtils` - System proxy, git proxy, and Hysteria proxy controls
- `MainViewModel` - Orchestrates boot-up, system proxy, git proxy, and hysteria proxy toggles
