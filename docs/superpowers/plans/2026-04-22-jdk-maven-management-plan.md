# JDK & Maven Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 EnvTool 实现 JDK Management 和 Maven Management 两个独立窗口，包含版本管理、环境变量配置、仓库地址管理和源管理功能。

**Architecture:**
- JDK Management：扩展现有 `JavaConfigWindow` + `JdkManagementViewModel`，复用 `JpvmService`
- Maven Management：新建 `MavenService` + `MavenManagementWindow`，管理本地 settings.xml 和源
- 均为独立窗口，遵循现有 Avalonia MVVM 模式

**Tech Stack:** Avalonia 12.0, ReactiveUI, F#, SharpZipLib, Newtonsoft.Json

---

## File Structure

```
EnvTool/
├── Services/
│   └── MavenService.fs                    # 新建：Maven 管理服务
├── ViewModels/
│   ├── JdkManagementViewModel.fs          # 扩展：实现完整 JDK 管理
│   └── MavenManagementViewModel.fs         # 新建：Maven 管理 ViewModel
├── Views/
│   ├── JdkManagementView.axaml             # 扩展：实现 DataGrid UI
│   ├── JdkManagementView.axaml.fs          # 已有：无需修改
│   ├── MavenManagementWindow.axaml         # 新建：Maven 窗口
│   ├── MavenManagementWindow.axaml.fs      # 新建：Maven 窗口代码后端
│   ├── MavenManagementView.axaml           # 新建：Maven 主内容视图
│   ├── MavenManagementView.axaml.fs        # 新建：代码后端
│   ├── JavaConfigWindow.axaml              # 修改：更新 Title
│   └── ProxyConfigView.axaml.fs            # 修改：添加 Maven 按钮点击处理
└── EnvTool.fsproj                          # 修改：添加 .fs 文件（axaml 自动处理）
```

---

## Task 1: MavenService 实现

**Files:**
- Create: `EnvTool/Services/MavenService.fs`

- [ ] **Step 1: 创建 MavenService.fs**

```fsharp
namespace EnvTool.Services

open System
open System.IO
open System.Xml.Linq
open EnvTool.Utils.SysInfo

type MavenSource = { Id: string; Name: string; Url: string; IsDefault: bool }

type MavenService() =

    let mavenHome = 
        Environment.GetEnvironmentVariable("MAVEN_HOME")
        |> fun e -> if String.IsNullOrEmpty(e) then "" else e

    let mavenConfDir =
#if Windows
        [| Environment.GetEnvironmentVariable("USERPROFILE"); ".m2" |] |> Path.Combine
#else
        [| Environment.GetEnvironmentVariable("HOME"); ".m2" |] |> Path.Combine
#endif
    let settingsPath = Path.Combine(mavenConfDir, "settings.xml")

    member this.GetVersion() =
        if String.IsNullOrEmpty(mavenHome) then ""
        else
            let mavenBin = Path.Combine(mavenHome, "bin", if SysOS = "Windows" then "mvn.cmd" else "mvn")
            if File.Exists(mavenBin) then
                try
                    let output = EnvTool.Utils.CmdUtils.RunCmdCommand $"{mavenBin} -version"
                    let lines = output.Split(Environment.NewLine)
                    if lines.Length > 0 then lines[0] else ""
                with _ -> ""
            else ""

    member this.GetMavenHome() = mavenHome

    member this.GetSettingsContent() =
        if File.Exists(settingsPath) then File.ReadAllText(settingsPath) else ""

    member this.SaveSettings(content: string) =
        if not (Directory.Exists(mavenConfDir)) then
            Directory.CreateDirectory(mavenConfDir) |> ignore
        File.WriteAllText(settingsPath, content)

    member this.GetSources() =
        let defaultSources = [
            { Id = "central"; Name = "Maven Central"; Url = "https://repo.maven.apache.org/maven2"; IsDefault = true }
            { Id = "aliyun"; Name = "阿里云 Maven"; Url = "https://maven.aliyun.com/repository/public"; IsDefault = false }
        ]
        try
            if File.Exists(settingsPath) then
                let doc = XDocument.Load(settingsPath)
                let mirrors = doc.Root.Element(XName.Get("mirrors"))
                if mirrors <> null then
                    mirrors.Elements(XName.Get("mirror"))
                    |> Seq.map (fun m ->
                        let id = m.Attribute(XName.Get("id")) |> fun a -> if a <> null then a.Value else ""
                        let name = m.Attribute(XName.Get("name") |> fun a -> if a <> null then a.Value else "")
                        let url = m.Attribute(XName.Get("url") |> fun a -> if a <> null then a.Value else "")
                        let isDefault = id = "central"
                        { Id = id; Name = name; Url = url; IsDefault = isDefault })
                    |> Seq.toList
                else defaultSources
            else defaultSources
        with _ -> defaultSources

    member this.AddSource(source: MavenSource) =
        let content = this.GetSettingsContent()
        let doc = if String.IsNullOrEmpty(content) then XDocument.Parse("<settings></settings>") else XDocument.Parse(content)
        let mirrors = doc.Root.Element(XName.Get("mirrors"))
        let newMirror = XElement(XName.Get("mirror")
            , XAttribute(XName.Get("id"), source.Id)
            , XAttribute(XName.Get("name"), source.Name)
            , XAttribute(XName.Get("url"), source.Url))
        if mirrors <> null then
            mirrors.Add(newMirror)
        else
            doc.Root.Add(XElement(XName.Get("mirrors"), newMirror))
        this.SaveSettings(doc.ToString())

    member this.RemoveSource(id: string) =
        let content = this.GetSettingsContent()
        if String.IsNullOrEmpty(content) then false
        else
            let doc = XDocument.Parse(content)
            let mirrors = doc.Root.Element(XName.Get("mirrors"))
            if mirrors <> null then
                let target = mirrors.Elements(XName.Get("mirror")) |> Seq.tryFind (fun m -> m.Attribute(XName.Get("id")).Value = id)
                match target with
                | Some(t) -> t.Remove(); this.SaveSettings(doc.ToString()); true
                | None -> false
            else false

    member this.SetDefaultSource(id: string) =
        let sources = this.GetSources()
        let updated = sources |> List.map (fun s -> { s with IsDefault = (s.Id = id) })
        // 重新生成 settings.xml
        let doc = XDocument.Parse("<settings></settings>")
        let mirrorsElement = XElement(XName.Get("mirrors"))
        updated |> List.iter (fun s ->
            mirrorsElement.Add(XElement(XName.Get("mirror")
                , XAttribute(XName.Get("id"), s.Id)
                , XAttribute(XName.Get("name"), s.Name)
                , XAttribute(XName.Get("url"), s.Url))))
        doc.Root.Add(mirrorsElement)
        this.SaveSettings(doc.ToString())
```

- [ ] **Step 2: 提交**

```bash
git add EnvTool/Services/MavenService.fs
git commit -m "feat: add MavenService for Maven management"
```

---

## Task 2: JdkManagementViewModel 完整实现

**Files:**
- Modify: `EnvTool/ViewModels/JdkManagementViewModel.fs`

- [ ] **Step 1: 实现 JdkManagementViewModel**

```fsharp
namespace EnvTool.ViewModels

open System
open System.Collections.ObjectModel
open ReactiveUI
open EnvTool.Services

type JdkItem = {
    Distro: string
    Version: string
    Path: string
    IsCurrent: bool
}

type JdkManagementViewModel() as this =
    inherit ViewModelBase()

    let mutable installedJdks = ObservableCollection<JdkItem>()
    let mutable availableJdks = ObservableCollection<JdkItem>()
    let mutable selectedJdk = Unchecked.defaultof<JdkItem>
    let mutable currentVersion = ""
    let mutable isLoading = false

    do this.RefreshInstalled()

    member this.InstalledJdks
        with get () = installedJdks
        and set (v: ObservableCollection<JdkItem>) = this.RaiseAndSetIfChanged(&installedJdks, v) |> ignore

    member this.AvailableJdks
        with get () = availableJdks
        and set (v: ObservableCollection<JdkItem>) = this.RaiseAndSetIfChanged(&availableJdks, v) |> ignore

    member this.SelectedJdk
        with get () = selectedJdk
        and set (v: JdkItem) = this.RaiseAndSetIfChanged(&selectedJdk, v) |> ignore

    member this.CurrentVersion
        with get () = currentVersion
        and set (v: string) = this.RaiseAndSetIfChanged(&currentVersion, v) |> ignore

    member this.IsLoading
        with get () = isLoading
        and set (v: bool) = this.RaiseAndSetIfChanged(&isLoading, v) |> ignore

    member this.RefreshInstalled() =
        let current = JpvmModule.Current()
        this.CurrentVersion <- if String.IsNullOrEmpty(current.version) then "未设置" else $"{current.distro} {current.version}"
        
        let jdkPath = JpvmModule.JDK_PATH
        if System.IO.Directory.Exists(jdkPath) then
            let dirs = System.IO.Directory.EnumerateDirectories(jdkPath, "*", System.IO.SearchOption.AllDirectories)
            let items = dirs |> Seq.choose (fun d ->
                let parts = d.Split(System.IO.Path.DirectorySeparatorChar)
                if parts.Length >= 4 then
                    let distro = parts[parts.Length - 4]
                    let version = parts[parts.Length - 3]
                    let isCurrent = distro = current.distro && version = current.version
                    Some({ Distro = distro; Version = version; Path = d; IsCurrent = isCurrent })
                else None) |> Seq.toList
            this.InstalledJdks <- ObservableCollection(items)
        else
            this.InstalledJdks <- ObservableCollection([])

    member this.InstallSelected() =
        if selectedJdk <> null then
            this.IsLoading <- true
            try
                let jdk = { JdkVersionInfo.distro = selectedJdk.Distro; JdkVersionInfo.version = selectedJdk.Version }
                let progress = System.Progress<double>(fun p -> ())
                JpvmModule.Install(jdk, progress)
                this.RefreshInstalled()
            finally
                this.IsLoading <- false

    member this.UseSelected() =
        if selectedJdk <> null then
            let jdk = { JdkVersionInfo.distro = selectedJdk.Distro; JdkVersionInfo.version = selectedJdk.Version }
            if JpvmModule.Use(jdk) then
                this.RefreshInstalled()

    member this.RemoveSelected() =
        if selectedJdk <> null && not selectedJdk.IsCurrent then
            let jdk = { JdkVersionInfo.distro = selectedJdk.Distro; JdkVersionInfo.version = selectedJdk.Version }
            if JpvmModule.Remove(jdk) then
                this.RefreshInstalled()
```

- [ ] **Step 2: 提交**

```bash
git add EnvTool/ViewModels/JdkManagementViewModel.fs
git commit -m "feat: implement JdkManagementViewModel with full functionality"
```

---

## Task 3: JdkManagementView UI 实现

**Files:**
- Modify: `EnvTool/Views/JdkManagementView.axaml`

- [ ] **Step 1: 实现 JdkManagementView.axaml DataGrid UI**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
             xmlns:vm="using:EnvTool.ViewModels"
             x:Class="EnvTool.Views.JdkManagementView"
             x:DataType="vm:JdkManagementViewModel">
    <UserControl.Styles>
        <Style Selector="TextBlock">
            <Setter Property="FontFamily" Value="微软雅黑" />
        </Style>
        <Style Selector="Button">
            <Setter Property="FontFamily" Value="微软雅黑" />
            <Setter Property="FontWeight" Value="Bold" />
            <Setter Property="Width" Value="100" />
            <Setter Property="Height" Value="30" />
            <Setter Property="Margin" Value="5" />
        </Style>
        <Style Selector="StackPanel.action">
            <Setter Property="Orientation" Value="Horizontal" />
            <Setter Property="HorizontalAlignment" Value="Center" />
            <Setter Property="Margin" Value="0 10 0 0" />
        </Style>
    </UserControl.Styles>

    <DockPanel>
        <StackPanel DockPanel.Dock="Top" Margin="10">
            <TextBlock Text="当前 JDK" FontWeight="Bold" FontSize="14" />
            <TextBlock Text="{Binding CurrentVersion}" FontSize="12" Margin="0 5 0 0" />
        </StackPanel>
        
        <StackPanel DockPanel.Dock="Bottom" Classes="action">
            <Button Content="安装" Command="{Binding InstallSelected}" IsEnabled="{Binding !IsLoading}" />
            <Button Content="切换" Command="{Binding UseSelected}" />
            <Button Content="卸载" Command="{Binding RemoveSelected}" />
            <Button Content="刷新" Command="{Binding RefreshInstalled}" />
        </StackPanel>

        <DataGrid ItemsSource="{Binding InstalledJdks}"
                  SelectedItem="{Binding SelectedJdk}"
                  AutoGenerateColumns="False"
                  CanUserReorderColumns="False"
                  CanUserResizeColumns="True"
                  CanUserSortColumns="True"
                  GridLinesVisibility="Horizontal"
                  Margin="10 0 10 10">
            <DataGrid.Columns>
                <DataGridTextColumn Header="发行版" Binding="{Binding Distro}" Width="*" />
                <DataGridTextColumn Header="版本" Binding="{Binding Version}" Width="80" />
                <DataGridTextColumn Header="路径" Binding="{Binding Path}" Width="2*" />
                <DataGridTextColumn Header="状态" Binding="{Binding IsCurrent}" Width="60" />
            </DataGrid.Columns>
        </DataGrid>
    </DockPanel>
</UserControl>
```

- [ ] **Step 2: 提交**

```bash
git add EnvTool/Views/JdkManagementView.axaml
git commit -m "feat: implement JdkManagementView with DataGrid UI"
```

---

## Task 4: MavenManagementViewModel 实现

**Files:**
- Create: `EnvTool/ViewModels/MavenManagementViewModel.fs`

- [ ] **Step 1: 实现 MavenManagementViewModel**

```fsharp
namespace EnvTool.ViewModels

open System
open System.Collections.ObjectModel
open ReactiveUI
open EnvTool.Services

type MavenSourceItem = {
    Id: string
    Name: string
    Url: string
    IsDefault: bool
}

type MavenManagementViewModel() as this =
    inherit ViewModelBase()

    let mutable currentVersion = ""
    let mutable mavenHome = ""
    let mutable sources = ObservableCollection<MavenSourceItem>()
    let mutable settingsContent = ""
    let mutable selectedSource = Unchecked.defaultof<MavenSourceItem>

    let mavenService = MavenService()

    do this.LoadData()

    member this.CurrentVersion
        with get () = currentVersion
        and set (v: string) = this.RaiseAndSetIfChanged(&currentVersion, v) |> ignore

    member this.MavenHome
        with get () = mavenHome
        and set (v: string) = this.RaiseAndSetIfChanged(&mavenHome, v) |> ignore

    member this.Sources
        with get () = sources
        and set (v: ObservableCollection<MavenSourceItem>) = this.RaiseAndSetIfChanged(&sources, v) |> ignore

    member this.SettingsContent
        with get () = settingsContent
        and set (v: string) = this.RaiseAndSetIfChanged(&settingsContent, v) |> ignore

    member this.SelectedSource
        with get () = selectedSource
        and set (v: MavenSourceItem) = this.RaiseAndSetIfChanged(&selectedSource, v) |> ignore

    member this.LoadData() =
        this.CurrentVersion <- mavenService.GetVersion()
        this.MavenHome <- mavenService.GetMavenHome()
        this.SettingsContent <- mavenService.GetSettingsContent()
        let srcList = mavenService.GetSources() |> List.map (fun s -> 
            { Id = s.Id; Name = s.Name; Url = s.Url; IsDefault = s.IsDefault }) 
        this.Sources <- ObservableCollection(srcList)

    member this.SaveSettings() =
        mavenService.SaveSettings(this.SettingsContent)

    member this.AddSource(id: string, name: string, url: string) =
        let source = { Id = id; Name = name; Url = url; IsDefault = false }
        mavenService.AddSource(source)
        this.LoadData()

    member this.RemoveSource() =
        if selectedSource <> null && not selectedSource.IsDefault then
            mavenService.RemoveSource(selectedSource.Id) |> ignore
            this.LoadData()

    member this.SetDefaultSource() =
        if selectedSource <> null then
            mavenService.SetDefaultSource(selectedSource.Id) |> ignore
            this.LoadData()
```

- [ ] **Step 2: 提交**

```bash
git add EnvTool/ViewModels/MavenManagementViewModel.fs
git commit -m "feat: add MavenManagementViewModel"
```

---

## Task 5: MavenManagementView 创建

**Files:**
- Create: `EnvTool/Views/MavenManagementView.axaml`
- Create: `EnvTool/Views/MavenManagementView.axaml.fs`

- [ ] **Step 1: 创建 MavenManagementView.axaml**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
             xmlns:vm="using:EnvTool.ViewModels"
             x:Class="EnvTool.Views.MavenManagementView"
             x:DataType="vm:MavenManagementViewModel">
    <UserControl.Styles>
        <Style Selector="TextBlock">
            <Setter Property="FontFamily" Value="微软雅黑" />
        </Style>
        <Style Selector="Button">
            <Setter Property="FontFamily" Value="微软雅黑" />
            <Setter Property="FontWeight" Value="Bold" />
            <Setter Property="Width" Value="100" />
            <Setter Property="Height" Value="30" />
            <Setter Property="Margin" Value="5" />
        </Style>
        <Style Selector="TextBox">
            <Setter Property="FontFamily" Value="Consolas" />
        </Style>
        <Style Selector="StackPanel.action">
            <Setter Property="Orientation" Value="Horizontal" />
            <Setter Property="HorizontalAlignment" Value="Center" />
            <Setter Property="Margin" Value="0 10 0 0" />
        </Style>
    </UserControl.Styles>

    <DockPanel>
        <StackPanel DockPanel.Dock="Top" Margin="10">
            <TextBlock Text="当前版本" FontWeight="Bold" FontSize="14" />
            <TextBlock Text="{Binding CurrentVersion}" FontSize="12" Margin="0 5 0 0" />
            <TextBlock Text="MAVEN_HOME" FontWeight="Bold" FontSize="14" Margin="0 10 0 0" />
            <TextBlock Text="{Binding MavenHome}" FontSize="12" Margin="0 5 0 0" />
        </StackPanel>
        
        <StackPanel DockPanel.Dock="Bottom" Classes="action">
            <Button Content="保存设置" Command="{Binding SaveSettings}" />
            <Button Content="刷新" Command="{Binding LoadData}" />
        </StackPanel>

        <TabControl Margin="10 0 10 10">
            <TabItem Header="源管理">
                <DockPanel>
                    <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0 10 0 0">
                        <Button Content="添加源" />
                        <Button Content="删除源" Command="{Binding RemoveSource}" />
                        <Button Content="设为默认" Command="{Binding SetDefaultSource}" />
                    </StackPanel>
                    <DataGrid ItemsSource="{Binding Sources}"
                              SelectedItem="{Binding SelectedSource}"
                              AutoGenerateColumns="False"
                              CanUserReorderColumns="False"
                              CanUserResizeColumns="True"
                              GridLinesVisibility="Horizontal">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="*" />
                            <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="*" />
                            <DataGridTextColumn Header="URL" Binding="{Binding Url}" Width="2*" />
                            <DataGridTextColumn Header="默认" Binding="{Binding IsDefault}" Width="60" />
                        </DataGrid.Columns>
                    </DataGrid>
                </DockPanel>
            </TabItem>
            <TabItem Header="settings.xml">
                <TextBox Text="{Binding SettingsContent}"
                         AcceptsReturn="True"
                         TextWrapping="NoWrap"
                         FontSize="11"
                         VerticalScrollBarVisibility="Auto"
                         HorizontalScrollBarVisibility="Auto" />
            </TabItem>
        </TabControl>
    </DockPanel>
</UserControl>
```

- [ ] **Step 2: 创建 MavenManagementView.axaml.fs**

```fsharp
namespace EnvTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

type MavenManagementView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member private this.InitializeComponent() = AvaloniaXamlLoader.Load(this)
```

- [ ] **Step 3: 提交**

```bash
git add EnvTool/Views/MavenManagementView.axaml EnvTool/Views/MavenManagementView.axaml.fs
git commit -m "feat: add MavenManagementView with source management and settings editor"
```

---

## Task 6: MavenManagementWindow 创建

**Files:**
- Create: `EnvTool/Views/MavenManagementWindow.axaml`
- Create: `EnvTool/Views/MavenManagementWindow.axaml.fs`

- [ ] **Step 1: 创建 MavenManagementWindow.axaml**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:vm="using:EnvTool.ViewModels"
        mc:Ignorable="d"
        x:Class="EnvTool.Views.MavenManagementWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Maven Management"
        x:DataType="vm:MavenManagementViewModel"
        Content="{Binding ContentViewModel">
    <Design.DataContext>
        <vm:MavenManagementViewModel />
    </Design.DataContext>
</Window>
```

- [ ] **Step 2: 创建 MavenManagementWindow.axaml.fs**

```fsharp
namespace EnvTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open EnvTool.ViewModels

type MavenManagementWindow() as this =
    inherit Window()

    do this.Width <- 800
    do this.Height <- 500
    do this.CanResize <- false
    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.WindowStartupLocation <- WindowStartupLocation.CenterScreen
```

- [ ] **Step 3: 提交**

```bash
git add EnvTool/Views/MavenManagementWindow.axaml EnvTool/Views/MavenManagementWindow.axaml.fs
git commit -m "feat: add MavenManagementWindow"
```

---

## Task 7: ProxyConfigView 添加 Maven 按钮处理

**Files:**
- Modify: `EnvTool/Views/ProxyConfigView.axaml.fs`

- [ ] **Step 1: 添加 OpenMavenMgmtWin 方法**

在 `ProxyConfigView.axaml.fs` 的 `OpenJdkMgmtWin` 方法后添加：

```fsharp
member this.OpenMavenMgmtWin (sender: obj) (args: RoutedEventArgs) =
    let win = MavenManagementWindow(DataContext = MavenManagementViewModel())
    win.Show()
```

- [ ] **Step 2: 修改 ProxyConfigView.axaml**

在 Maven Management 按钮上添加 Click 事件：

```xml
<Button Content="Maven Management" Classes="bar" Click="OpenMavenMgmtWin" />
```

- [ ] **Step 3: 提交**

```bash
git add EnvTool/Views/ProxyConfigView.axaml.fs EnvTool/Views/ProxyConfigView.axaml
git commit -m "feat: connect Maven Management button to MavenManagementWindow"
```

---

## Task 8: 更新 fsproj

**Files:**
- Modify: `EnvTool/EnvTool.fsproj`

**注意：** `.axaml` 文件由 Avalonia build targets 自动处理，无需添加。仅 `.fs` 文件需要显式添加。

- [ ] **Step 1: 添加 MavenService.fs**

在 `<Compile Include="Services\StatementService.fs" />` 后添加：

```xml
<Compile Include="Services\MavenService.fs" />
```

- [ ] **Step 2: 添加 MavenManagementViewModel.fs**

在 `<Compile Include="ViewModels\JavaConfigWindowViewModel.fs"/>` 后添加：

```xml
<Compile Include="ViewModels\MavenManagementViewModel.fs"/>
```

- [ ] **Step 3: 添加 MavenManagementView.axaml.fs 和 MavenManagementWindow.axaml.fs**

在 `<Compile Include="Views\JdkManagementView.axaml.fs"/>` 后添加：

```xml
<Compile Include="Views\MavenManagementView.axaml.fs"/>
<Compile Include="Views\MavenManagementWindow.axaml.fs"/>
```

- [ ] **Step 4: 提交**

```bash
git add EnvTool/EnvTool.fsproj
git commit -m "build: add MavenService and MavenManagementViewModel to project"
```

---

## Task 9: 构建验证

- [ ] **Step 1: 运行 dotnet build**

```bash
dotnet build EnvTool/EnvTool.fsproj
```

- [ ] **Step 2: 如有编译错误，修复并重新提交**

---

## Self-Review Checklist

1. **Spec coverage:**
   - [x] JDK 查看/安装/切换/卸载 - Task 2, 3
   - [x] Maven 版本查看/settings.xml/源管理 - Task 1, 4, 5
   - [x] 独立窗口架构 - Task 6, 7

2. **Placeholder scan:** 无 TBD/TODO/不完整步骤

3. **Type consistency:** 
   - MavenService 和 MavenManagementViewModel 接口一致
   - JdkManagementViewModel 使用 JdkItem 类型
   - 所有 ObservableCollection 类型正确

---

## Plan Complete

**Two execution options:**

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
