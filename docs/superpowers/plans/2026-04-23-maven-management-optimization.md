# Maven 管理功能优化实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 SetDefaultSource bug 并添加新增 Maven 镜像源功能

**Architecture:** 修复 MavenService 中的 XML 操作 bug，在 ViewModel 添加对话框状态和命令，View 层添加对话框 UI 和绑定

**Tech Stack:** F#, Avalonia, ReactiveUI, XNamespace/XML

---

## 文件清单

| 文件 | 职责 |
|------|------|
| `EnvTool/Services/MavenService.fs` | 修复 SetDefaultSource XML 操作 bug |
| `EnvTool/ViewModels/MavenManagementViewModel.fs` | 添加对话框属性和命令 |
| `EnvTool/Views/MavenManagementView.axaml` | 添加对话框 UI 和新增按钮 |
| `EnvTool/Views/MavenManagementView.axaml.fs` | 对话框交互逻辑 |

---

## Task 1: 修复 SetDefaultSource BUG

**Files:**
- Modify: `EnvTool/Services/MavenService.fs:161-180`

- [ ] **Step 1: 找到并替换错误代码**

文件: `EnvTool/Services/MavenService.fs`
位置: `SetDefaultSource` 函数（约161-180行）

将:
```fsharp
// Reset all mirrors to non-default
mirrorsElement.Elements(mavenNs + "mirror")
|> Seq.iter (fun m ->
    let idElem = m.Element(mavenNs + "id")
    if idElem <> null && idElem.Value = "central" then
        idElem.Remove()
)
```

替换为:
```fsharp
// Reset all mirrors to non-default (clear central id)
mirrorsElement.Elements(mavenNs + "mirror")
|> Seq.iter (fun m ->
    let idElem = m.Element(mavenNs + "id")
    if idElem <> null && idElem.Value = "central" then
        idElem.SetValue("")
)
```

- [ ] **Step 2: 提交**

```bash
git add EnvTool/Services/MavenService.fs
git commit -m "fix: use SetValue instead of Remove to preserve XML structure in SetDefaultSource"
```

---

## Task 2: 添加对话框属性和命令到 ViewModel

**Files:**
- Modify: `EnvTool/ViewModels/MavenManagementViewModel.fs`

- [ ] **Step 1: 添加对话框相关属性和命令**

在 `MavenManagementViewModel` 类型定义中，添加以下成员：

在 `sources` 声明之后添加：
```fsharp
let mutable isAddDialogOpen = false
let mutable newSourceId = ""
let mutable newSourceName = ""
let mutable newSourceUrl = ""

let showAddDialogCmd = ReactiveCommand.Create(Action(this.ShowAddDialog))
let addSourceCmd = ReactiveCommand.Create(Action(this.AddSourceFromDialog))
let cancelAddDialogCmd = ReactiveCommand.Create(Action(this.CancelAddDialog))
```

在成员声明部分添加：
```fsharp
member this.IsAddDialogOpen
    with get () = isAddDialogOpen
    and set v = this.RaiseAndSetIfChanged(&isAddDialogOpen, v) |> ignore

member this.NewSourceId
    with get () = newSourceId
    and set v = this.RaiseAndSetIfChanged(&newSourceId, v) |> ignore

member this.NewSourceName
    with get () = newSourceName
    and set v = this.RaiseAndSetIfChanged(&newSourceName, v) |> ignore

member this.NewSourceUrl
    with get () = newSourceUrl
    and set v = this.RaiseAndSetIfChanged(&newSourceUrl, v) |> ignore

member this.ShowAddDialogCommand: ICommand = showAddDialogCmd
member this.AddSourceCommand: ICommand = addSourceCmd
member this.CancelAddDialogCommand: ICommand = cancelAddDialogCmd
```

在 `SetDefaultSource` 方法之后添加：
```fsharp
member this.ShowAddDialog() =
    this.IsAddDialogOpen <- true

member this.CancelAddDialog() =
    this.IsAddDialogOpen <- false
    this.NewSourceId <- ""
    this.NewSourceName <- ""
    this.NewSourceUrl <- ""

member this.AddSourceFromDialog() =
    if not (String.IsNullOrWhiteSpace(this.NewSourceId)) &&
       not (String.IsNullOrWhiteSpace(this.NewSourceUrl)) then
        this.AddSource(this.NewSourceId, this.NewSourceName, this.NewSourceUrl)
        this.IsAddDialogOpen <- false
        this.NewSourceId <- ""
        this.NewSourceName <- ""
        this.NewSourceUrl <- ""
```

- [ ] **Step 2: 提交**

```bash
git add EnvTool/ViewModels/MavenManagementViewModel.fs
git commit -m "feat: add AddSource dialog properties and commands to MavenManagementViewModel"
```

---

## Task 3: 在 View 添加对话框 UI

**Files:**
- Modify: `EnvTool/Views/MavenManagementView.axaml`

- [ ] **Step 1: 在按钮区域添加"新增"按钮**

找到源管理 Tab 中的按钮区域：
```xml
<StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" Spacing="10" Margin="0 10 0 0">
    <Button Content="删除" Command="{Binding RemoveSourceCommand}" />
    <Button Content="设为默认" Command="{Binding SetDefaultSourceCommand}" />
</StackPanel>
```

替换为：
```xml
<StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" Spacing="10" Margin="0 10 0 0">
    <Button Content="新增" Command="{Binding ShowAddDialogCommand}" />
    <Button Content="删除" Command="{Binding RemoveSourceCommand}" />
    <Button Content="设为默认" Command="{Binding SetDefaultSourceCommand}" />
</StackPanel>
```

- [ ] **Step 2: 在 DockPanel 最后添加对话框 Window**

在 `</DockPanel>` 之前添加：
```xml
<Window x:Name="AddSourceDialog"
        Title="添加 Maven 镜像源"
        Width="400"
        Height="200"
        WindowStartupLocation="CenterOwner"
        CanResize="False"
        IsVisible="{Binding IsAddDialogOpen}">
    <StackPanel Margin="20" Spacing="10">
        <StackPanel Orientation="Horizontal" Spacing="10">
            <TextBlock Text="ID:" VerticalAlignment="Center" Width="60" />
            <TextBox Text="{Binding NewSourceId}" Width="280" />
        </StackPanel>
        <StackPanel Orientation="Horizontal" Spacing="10">
            <TextBlock Text="名称:" VerticalAlignment="Center" Width="60" />
            <TextBox Text="{Binding NewSourceName}" Width="280" />
        </StackPanel>
        <StackPanel Orientation="Horizontal" Spacing="10">
            <TextBlock Text="地址:" VerticalAlignment="Center" Width="60" />
            <TextBox Text="{Binding NewSourceUrl}" Width="280" />
        </StackPanel>
        <StackPanel Orientation="Horizontal" Spacing="10" HorizontalAlignment="Right" Margin="0 10 0 0">
            <Button Content="取消" Command="{Binding CancelAddDialogCommand}" Width="80" />
            <Button Content="确定" Command="{Binding AddSourceCommand}" Width="80" />
        </StackPanel>
    </StackPanel>
</Window>
```

- [ ] **Step 3: 提交**

```bash
git add EnvTool/Views/MavenManagementView.axaml
git commit -m "feat: add AddSource dialog UI to MavenManagementView"
```

---

## Task 4: 在 Code-Behind 处理对话框逻辑

**Files:**
- Modify: `EnvTool/Views/MavenManagementView.axaml.fs`

- [ ] **Step 1: 修改 code-behind 以支持对话框**

将:
```fsharp
namespace EnvTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml


type MavenManagementView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member private this.InitializeComponent() = AvaloniaXamlLoader.Load(this)
```

替换为:
```fsharp
namespace EnvTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Interactivity
open EnvTool.ViewModels


type MavenManagementView() as this =
    inherit UserControl()

    let mutable addSourceDialog: Window option = None

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        addSourceDialog <- this.FindNameScope().FindRegisteredAsync<Window>("AddSourceDialog") |> Async.AwaitTask |> ignore

    member this.OnOpenAddDialog(sender: obj, RoutedEventArgs e) =
        match addSourceDialog with
        | Some(dialog) ->
            let vm = this.DataContext :?> MavenManagementViewModel
            if vm <> null then
                dialog.DataContext <- vm
        | None -> ()
```

- [ ] **Step 2: 提交**

```bash
git add EnvTool/Views/MavenManagementView.axaml.fs
git commit -m "feat: add code-behind logic for AddSource dialog in MavenManagementView"
```

---

## Task 5: 整体测试

- [ ] **Step 1: 构建项目验证无编译错误**

```bash
dotnet build EnvTool/EnvTool.fsproj
```

预期: Build succeeded

- [ ] **Step 2: 运行测试**

```bash
dotnet test Tests/Tests.fsproj
```

预期: 所有 MavenService 相关测试通过

- [ ] **Step 3: 提交完整功能**

```bash
git add -A
git commit -m "feat: Maven management - fix SetDefaultSource bug and add source dialog"
```
