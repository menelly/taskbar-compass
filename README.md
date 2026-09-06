# Taskbar Compass

A customisable Windows taskbar with app groups, quick websites, and collectible pets.

**Gui Update 1.2 · Windows-only beta · x64**

## Dual-monitor recovery follow-up (2026-09-06)

The replacement still runs on the primary monitor only. Secondary monitors retain their Windows taskbars. The watchdog now restores Windows' taskbars after 15 seconds without a response from the taskbar UI, as well as after process exit. Local diagnostic logs in `%LOCALAPPDATA%\TaskbarCompass\diagnostics-*.log` record startup, display geometry, exit reasons, and exceptions; they are not uploaded automatically.

Live checks on the affected dual-monitor PC passed for all four edges, normal exit, abrupt exit, and restoration while the UI remained blocked. The original unexpected exit has not been reproduced or attributed to a specific cause. These source changes are not part of the published 1.2 download yet.

Before running any test that opens windows, moves or replaces a taskbar, or otherwise changes a person's display, explain the visual effects and wait for their explicit approval. The isolated menu fixture opens visible dialogs; live validation moves the taskbar through all four edges. A progress announcement or general debugging request is not approval for these actions.

## Gui Update 1.2

- Fixed the solid taskbar background covering app icons, including recovery after window-order changes.
- Pet Shop and the group editor now open independently, keeping the taskbar usable. Their close controls work without blocking the bar.
- The setup window stays open after applying the taskbar or changing its edge.
- Refreshed the group editor with a dark theme, app icons, clearer buttons, and a selected-app count.
- Groups can use a Folder, Star, Heart, or Grid icon with preset or custom colours and a live preview. The last-opened app icon remains an option.
- Custom icons persist across restarts and app switches. Existing group membership remains compatible with the previous save format.

The reported blanking and dialog problems were confirmed resolved by hands-on testing on the affected PC. Automated checks also cover window ordering, independent dialog lifecycle, group saving, and icon persistence. Broader Windows compatibility remains beta.

## Features

- Place the taskbar on any screen edge, with left/right or top/bottom app alignment.
- Change background transparency and colour while keeping app logos opaque.
- Pin apps, organise groups, and search installed apps and open windows.
- Add your own quick website buttons.
- Open Windows quick settings from the Wi-Fi/sound button or battery.
- Show a 12-hour clock and a live coin counter.
- Keep Bert the mouse, or unlock Sulverster the cat (100 coins) and Bark the dog (300 coins).
- Earn 1 coin per active minute and half a coin per idle minute after five minutes without input. Earnings require the replacement taskbar to be running; sleep and offline time do not earn coins.
- Move overlapping normal windows clear of the taskbar when a drag or resize finishes.

## Build and run

Requires Windows x64 with .NET Framework 4.x and its built-in C# compiler. Windows 11 is the intended platform; behaviour on other versions has not been fully tested.

Open PowerShell in the project folder and run:

```powershell
.\Source\BuildReplacement.ps1 -Destination .\dist
Copy-Item .\README.md .\dist\README.md
.\dist\TaskbarCompass.exe
```

Choose an edge and click **Continue**. Opening setup alone does not replace the Windows taskbar.

Optional installation for the current Windows account:

```powershell
.\InstallTaskbarCompass.ps1 -SourceDirectory .\dist
```

Restore and exit any existing Taskbar Compass session before installing an update. The installer creates desktop and Start-menu shortcuts. It does not enable automatic startup.

## Restore Windows' taskbar

Click **Restore** on Taskbar Compass or press **Ctrl+Alt+Shift+R**. You can also run **RestoreWindowsTaskbar.exe**, included in the build, for emergency restoration. A separate watchdog attempts restoration if the main process exits unexpectedly.

## Pets and saved data

Open **Windows icon → Pet Shop** or click the coin counter. Unlocked pets can be equipped repeatedly without paying again. Each has its own feeding reaction.

Preferences, coins, pet unlocks, and snack counts are stored locally in `%LOCALAPPDATA%\TaskbarCompass`. Coins save every five seconds, at shutdown/logoff notifications, and before normal taskbar restoration. Writes are flushed to disk; a previous valid pet save is retained as a backup. Abrupt power loss can still lose recent unsaved progress.

## Beta limitations

- Replaces the taskbar on the primary display; separate replacement bars on other monitors are not implemented.
- No native notification tray, notification badges, or complete Start-menu replacement.
- App matching can miss packaged apps or shortcuts that start an updater rather than the app itself.
- Some elevated or custom windows may reject activation or repositioning.
- Wi-Fi/sound drawings are shortcuts, not live connection or volume indicators; battery data is live.
- Recovery and window behaviour depend on Windows Explorer and should be tested carefully before everyday use.

## Development and validation

`Source/ReplacementBar.cs` contains the taskbar UI and Windows integration; `Source/PetShop.cs` contains pet progression and shop UI. The build script uses the Windows .NET Framework compiler and no downloaded dependencies.

Targeted development checks covered coin persistence and backup recovery, pet unlock prices, feed reactions, menu hit areas, and geometry on all four edges. These checks are not a guarantee of compatibility with every Windows setup. Private development logs and personal settings are intentionally excluded from this repository.

Please include your Windows version, taskbar edge, and steps to reproduce when reporting bugs. Do not attach personal settings or account information.

No licence has been selected yet; publication does not grant an additional open-source licence.

## Credits

Created by **Luka** — concept, design direction, feature ideas, and hands-on testing.

Built with **Nova (OpenAI Codex)** — AI-assisted implementation, debugging, and documentation.

Published with permission on Ren's GitHub account, **menelly**.


## 0.14.4 hotfix

Menus now open beside the taskbar instead of covering the app-icon column. Group editing is deferred until the menu closes, and the editor is centred on screen. Isolated checks cover menu placement on all four edges, Cancel, Escape, window close, and restoring taskbar interaction. Confirmation on affected PCs is still needed.

Run the isolated menu and dialog regression checks on Windows with: `.\Tests\RunMenuTests.ps1`. The fixture disables real taskbar initialization and uses temporary settings; it opens and dismisses test dialogs.

