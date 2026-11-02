# 明日香 × Codex 启动动画

[English](README.md) | **简体中文** | [日本語](README.ja.md)

一个 Windows 同人启动器：在 Codex 窗口上播放约 5 秒的明日香动画，然后进入 Codex。

![动画最终画面](preview.png)

## 运行要求

- 64 位 Windows，并已安装 Codex 桌面应用。
- 将 `AsukaCodex.exe` 和 `assets` 文件夹放在一起。启动器使用已安装 Codex 应用的 Windows 激活标识。

## 开始使用

从 GitHub 将仓库下载为 ZIP，解压后双击 `AsukaCodex.exe`。如果 Codex 尚未打开，启动器会先启动它并等待窗口出现。动画跟随窗口位置和大小，并针对窄窗口和宽窗口采用不同布局。

- **Esc**：跳过动画并进入 Codex。
- **Alt+F4**：关闭动画。冷启动时，如果 Codex 已被启动，关闭动画不会撤销此次启动。
- 每个 Windows 登录会话只显示一个动画。画面素材加载失败时直接打开 Codex；音效加载失败时继续播放无声音画面。

在 PowerShell 中运行 `install-shortcuts.ps1` 可创建桌面快捷方式。移动项目后，请先删除本项目创建的两个旧快捷方式，再重新运行脚本。脚本不会覆盖同名的其他快捷方式。

## 预览与构建

打开 Codex 后运行：

```powershell
.\AsukaCodex.exe --preview
.\AsukaCodex.exe --preview --hold
```

第一条完整播放动画但不激活 Codex；第二条静音停在最终画面，按 Esc 或 Alt+F4 退出。

使用 Windows 自带的 .NET Framework 编译器构建并检查素材：

```powershell
.\build.ps1
.\AsukaCodex.exe --frames
.\tests\verify-assets.ps1
.\build\WindowTargetTests.exe
```

`build.ps1` 还会运行应用激活测试，可能唤起 Codex，并可能需要正常桌面权限；无需安装 .NET SDK。`AsukaCodex.exe --frames` 将关键帧输出到 `previews/`；`--preview --measure` 在该目录记录 WPF 渲染回调间隔和窗口边界。回调间隔不等同于 GPU 实际呈现帧率。

## 说明

这是非官方个人同人项目，与 OpenAI 无关联。睁眼效果由三个眼部状态和过渡构成，并非完整逐帧角色动画。启动器不会修改已安装的 Codex 应用。现有验收记录未实测完全退出后的冷启动，也未实测跨不同 DPI 显示器移动。

## 免责声明

本项目为非官方同人作品，未获得作品中所涉及角色、名称或标志的权利人认可，也与其无隶属关系；相关权利归各自权利人所有。本项目按现状提供，不作任何担保；使用者应自行确认使用、修改或再分发其中内容所需的授权。本仓库尚未指定开源许可证。
