# Asuka × Codex Launch Animation

**English** | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

A Windows fan-made launcher that plays an approximately five-second Asuka animation over the Codex window before bringing Codex to the foreground.

![Final animation frame](preview.png)

## Requirements

- 64-bit Windows with the Codex desktop app installed.
- Keep `AsukaCodex.exe` and the `assets` folder together. The launcher uses the installed Codex app's Windows activation identifier.

## Getting started

Download the repository as a ZIP from GitHub, extract it, and double-click `AsukaCodex.exe`. When Codex is not already open, the launcher starts it first and waits for its window. The animation follows the window's position and size, with layouts for narrow and wide windows.

- **Esc:** Skip the animation and enter Codex.
- **Alt+F4:** Close the animation. If Codex was already started during a cold launch, this does not undo that launch.
- Only one animation is shown per Windows login session. If visual assets cannot be loaded, the launcher opens Codex directly; if sound fails, the animation continues silently.

To create desktop shortcuts, run `install-shortcuts.ps1` in PowerShell. If you move the project, remove the two shortcuts created by this project before running the script again. The script refuses to overwrite an unrelated shortcut with the same name.

## Preview and build

With Codex open, run:

```powershell
.\AsukaCodex.exe --preview
.\AsukaCodex.exe --preview --hold
```

The first plays the animation without activating Codex. The second holds the final frame silently; press Esc or Alt+F4 to exit.

To build with the Windows .NET Framework compiler and check the assets:

```powershell
.\build.ps1
.\AsukaCodex.exe --frames
.\tests\verify-assets.ps1
.\build\WindowTargetTests.exe
```

`build.ps1` also runs an application activation test, which can activate Codex and may require normal desktop permissions. No .NET SDK is required. `AsukaCodex.exe --frames` exports frames to `previews/`; `--preview --measure` records WPF rendering callback intervals and window bounds there. Callback intervals are not the actual GPU frame rate.

## Notes

This is an unofficial personal fan project and is not affiliated with OpenAI. The eye animation uses three eye states with transitions, rather than a full frame-by-frame character animation. The launcher does not modify the installed Codex app. Cold launch and movement between monitors with different DPI settings have not been verified in the recorded acceptance test.

## Disclaimer

This project is an unofficial fan work and is not endorsed by or affiliated with the owners of the characters, names, or marks depicted. Those rights remain with their respective owners. The project is provided as is, without warranty; users are responsible for determining whether they have the necessary permissions to use, modify, or redistribute its contents. No open-source license has been specified for this repository.
