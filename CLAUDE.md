# CLAUDE.md - Text Rewriter Project

## Project Overview

Cross-platform system tray application (.NET 8 / Avalonia UI) that rewrites text anywhere using Claude AI. Triggered via a global keyboard shortcut, it captures selected text (or all text), sends it to Claude with configurable rewrite instructions, and pastes back the result.

## Architecture

```
src/
├── TextRewriter.Core/         # Models & interfaces (no external dependencies)
├── TextRewriter.Services/     # Business logic
│   ├── HotkeyService.cs       # SharpHook global keyboard hook
│   ├── InputSimulatorService.cs# Ctrl+C/A/V simulation (Cmd on macOS)
│   ├── ClipboardService.cs    # TextCopy clipboard wrapper
│   ├── RewriteService.cs      # Claude Messages API via HTTP
│   ├── AuthService.cs         # OAuth token from ~/.claude/.credentials.json
│   ├── SettingsService.cs     # JSON config persistence
│   ├── RewriteOrchestrator.cs # Core flow: capture → rewrite → paste
│   └── Platform/              # OS-specific services (Linux/Mac/Windows)
└── TextRewriter.App/          # Avalonia tray-only app
    ├── App.axaml.cs           # DI setup, tray icon, hotkey wiring
    ├── ViewModels/             # SettingsViewModel
    └── Views/                  # SettingsWindow.axaml
```

## Key Design Decisions

- **Tray-only app**: No main window. Uses `ShutdownMode.OnExplicitShutdown`.
- **NativeMenu for tray**: Built programmatically in `App.axaml.cs` because Avalonia NativeMenu has limited data binding.
- **Clipboard-based text capture**: Simulates Ctrl+C to copy selection, compares with previous clipboard to detect empty selection, falls back to Ctrl+A.
- **Direct HTTP for Claude API**: Uses `HttpClient` with raw Messages API instead of Anthropic NuGet SDK, to keep dependencies minimal and support both OAuth tokens (Bearer) and API keys (x-api-key header).
- **Platform detection**: `IPlatformService` with factory pattern. macOS uses Cmd instead of Ctrl for shortcuts.

## Build & Run

```bash
dotnet build                                          # Build all
dotnet run --project src/TextRewriter.App             # Run
dotnet publish src/TextRewriter.App -c Release -r win-x64 --self-contained   # Windows
dotnet publish src/TextRewriter.App -c Release -r osx-arm64 --self-contained # macOS Apple Silicon
dotnet publish src/TextRewriter.App -c Release -r linux-x64 --self-contained # Linux
```

## NuGet Packages

| Package | Version | Used in |
|---------|---------|---------|
| Avalonia | 11.1.5 | App |
| Avalonia.Desktop | 11.1.5 | App |
| Avalonia.Themes.Fluent | 11.1.5 | App |
| SharpHook | 5.3.7 | Services (hotkeys + key simulation) |
| TextCopy | 6.2.1 | Services (clipboard) |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | App |
| Microsoft.Extensions.Logging.Console | 8.0.1 | App |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | Services |

## Configuration

Settings stored at:
- Linux: `~/.config/textrewriter/settings.json`
- macOS: `~/Library/Application Support/TextRewriter/settings.json`
- Windows: `%APPDATA%\TextRewriter\settings.json`

Authentication priority:
1. `ANTHROPIC_API_KEY` env var
2. `CLAUDE_CODE_OAUTH_TOKEN` env var
3. `~/.claude/.credentials.json` (or macOS Keychain)

## Code Conventions

- C# 12, nullable enabled, implicit usings
- Interfaces in `TextRewriter.Core.Interfaces`
- Models in `TextRewriter.Core.Models`
- All services implement interfaces for testability
- Dutch UI strings (tray menu, notifications, default profiles)
- English code (variable names, comments)

## Important Notes

- **macOS**: Requires Accessibility permissions for SharpHook to work
- **Linux**: Requires `xsel` or `xclip` for clipboard, X11 for SharpHook (Wayland not supported)
- **Re-entrancy guard**: `RewriteOrchestrator._isProcessing` prevents double-triggers
- **Clipboard restore**: Original clipboard content is restored after paste (500ms delay)
- **Token caching**: AuthService caches tokens for 5 minutes, re-reads from disk on expiry
