# Maven 管理功能优化设计

## 概述

优化现有 Maven 管理功能，修复已知 bug 并新增添加源功能。

## 问题修复

### 1. SetDefaultSource BUG

**文件**: `EnvTool/Services/MavenService.fs`

**问题**: `SetDefaultSource` 函数在遍历所有 mirror 时错误地使用 `idElem.Remove()` 删除整个 `<id>` 元素，导致 XML 结构被破坏，settings.xml 内容被清空。

**当前错误代码**:
```fsharp
mirrorsElement.Elements(mavenNs + "mirror")
|> Seq.iter (fun m ->
    let idElem = m.Element(mavenNs + "id")
    if idElem <> null && idElem.Value = "central" then
        idElem.Remove()  // BUG: 删除整个 <id> 元素
)
```

**修复方案**: 使用 `SetValue()` 替换值而不是删除元素
```fsharp
mirrorsElement.Elements(mavenNs + "mirror")
|> Seq.iter (fun m ->
    let idElem = m.Element(mavenNs + "id")
    if idElem <> null && idElem.Value = "central" then
        idElem.SetValue("")  // 清除默认值标记
)
```

## 新增功能

### 2. 添加 Maven 镜像源

**文件**: `EnvTool/ViewModels/MavenManagementViewModel.fs`
**文件**: `EnvTool/Views/MavenManagementView.axaml`
**文件**: `EnvTool/Views/MavenManagementView.axaml.fs`

**功能描述**: 在源管理 Tab 添加"新增"按钮，点击弹出对话框输入 id、name、url，调用 `AddSourceCommand` 执行添加。

**UI 变更**:
- 源管理 Tab 添加"新增"按钮（位于删除/设为默认按钮右侧）
- 添加对话框窗口包含三个输入框：Id、名称、地址
- "确定"按钮调用添加逻辑
- "取消"按钮关闭对话框

**ViewModel 变更**:
- 添加 `ShowAddSourceDialogCommand`
- 添加 `IsAddDialogOpen` 属性控制对话框显示
- 添加 `NewSourceId`、`NewSourceName`、`NewSourceUrl` 属性绑定输入框

**View 变更**:
- 添加对话框 UI（Window 或 Popup）
- 绑定 ShowAddSourceDialogCommand 到新增按钮
- 对话框关闭时重置输入字段
