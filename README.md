# Text Rewriter

Een cross-platform system tray applicatie die tekst herschrijft met Claude AI, overal waar je schrijft.

## Features

- **Systeembreed**: Werkt in elke applicatie (browser, editor, chat, email, etc.)
- **Selectie of alles**: Herschrijft geselecteerde tekst, of alle tekst als niets geselecteerd is
- **Instelbare sneltoets**: Standaard `Ctrl+Shift+R` (macOS: `Cmd+Shift+R`)
- **Profielen**: Meerdere herschrijfprofielen (Formeel, Casual, NL, EN, Beknopt)
- **OAuth**: Hergebruikt je bestaande Claude Code authenticatie
- **Cross-platform**: Windows, macOS en Linux

## Vereisten

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Claude Code geïnstalleerd en ingelogd (`claude` in terminal)
- Linux: `xsel` of `xclip` (`sudo apt install xsel`)
- macOS: Accessibility permissions (System Settings → Privacy → Accessibility)

## Installatie & Gebruik

```bash
# Clone en build
git clone <repo-url>
cd rewriter
dotnet build

# Start de applicatie
dotnet run --project src/TextRewriter.App
```

De applicatie start als system tray icon. Gebruik:
1. Selecteer tekst in een applicatie
2. Druk `Ctrl+Shift+R`
3. De tekst wordt herschreven en automatisch geplakt

## Configuratie

Instellingen worden opgeslagen in:
- **Linux**: `~/.config/textrewriter/settings.json`
- **macOS**: `~/Library/Application Support/TextRewriter/settings.json`
- **Windows**: `%APPDATA%\TextRewriter\settings.json`

### Authenticatie

De applicatie leest automatisch je Claude Code OAuth tokens uit:
- **Linux**: `~/.claude/.credentials.json`
- **macOS**: Keychain of `~/.claude/.credentials.json`
- **Windows**: `~/.claude/.credentials.json`

Alternatief: stel de `ANTHROPIC_API_KEY` environment variable in.

## Profielen

Standaard profielen:
| Profiel | Beschrijving |
|---------|-------------|
| Herschrijf (NL) | Helder, professioneel Nederlands |
| Rewrite (EN) | Clear, professional English |
| Formeel | Zakelijke stijl, u-vorm |
| Casual | Informele toon, je-vorm |
| Beknopt | Korter en bondiger |

Eigen profielen toevoegen via **Rechtsklik tray icon → Instellingen**.

## Projectstructuur

```
src/
├── TextRewriter.Core/       # Models en interfaces
├── TextRewriter.Services/   # Business logic (hotkeys, clipboard, API, orchestrator)
└── TextRewriter.App/        # Avalonia UI (system tray, settings venster)
```

## Publiceren

```bash
# Self-contained build per platform
dotnet publish src/TextRewriter.App -c Release -r win-x64 --self-contained
dotnet publish src/TextRewriter.App -c Release -r osx-arm64 --self-contained
dotnet publish src/TextRewriter.App -c Release -r linux-x64 --self-contained
```
