# Text Rewriter - Cross-Platform System Tray Application

## Context

De gebruiker wil een applicatie die **overal** waar hij iets schrijft (browser, editor, chat, email, etc.) tekst kan herschrijven via Claude AI. De app draait als system tray applicatie, wordt getriggerd door een instelbare sneltoets, en gebruikt de bestaande Claude Code OAuth-authenticatie. Meerdere herschrijfprofielen (bv. "Formeel Nederlands", "Casual Engels") moeten snel wisselbaar zijn.

## Tech Stack

| Component | Technologie | NuGet Package |
|-----------|-------------|---------------|
| Framework | .NET 8 | - |
| UI | Avalonia UI 11.1 | `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent` |
| Global Hotkeys | SharpHook 7.x | `SharpHook` |
| Clipboard | TextCopy 6.x | `TextCopy` |
| Claude API | Anthropic SDK | `Anthropic` |
| JSON Config | System.Text.Json | (built-in) |

## Projectstructuur

```
TextRewriter/
├── TextRewriter.sln
├── src/
│   └── TextRewriter/
│       ├── TextRewriter.csproj
│       ├── Program.cs                          # Entry point
│       ├── App.axaml                           # Avalonia app + TrayIcon definitie
│       ├── App.axaml.cs                        # App lifecycle, ShutdownMode.OnExplicitShutdown
│       ├── Assets/
│       │   ├── icon.ico                        # Tray icon
│       │   └── icon.png                        # Tray icon (Linux)
│       ├── Models/
│       │   ├── RewriteProfile.cs               # Profiel: naam, instructies, model
│       │   ├── AppSettings.cs                  # Shortcut, actief profiel, notificaties
│       │   └── OAuthToken.cs                   # Claude OAuth token model
│       ├── Services/
│       │   ├── HotkeyService.cs                # SharpHook global hotkey registratie
│       │   ├── ClipboardService.cs             # TextCopy wrapper + clipboard save/restore
│       │   ├── KeySimulationService.cs         # Ctrl+C / Ctrl+A / Ctrl+V simulatie
│       │   ├── RewriteService.cs               # Claude API aanroep met profiel-instructies
│       │   ├── AuthService.cs                  # OAuth token laden/refreshen
│       │   ├── ConfigService.cs                # JSON config laden/opslaan
│       │   └── RewriteOrchestrator.cs          # Hoofdflow: capture → rewrite → paste
│       ├── ViewModels/
│       │   ├── TrayViewModel.cs                # System tray menu commands
│       │   └── SettingsViewModel.cs            # Settings venster ViewModel
│       └── Views/
│           └── SettingsWindow.axaml(.cs)       # Profiel-editor + shortcut config
```

## Implementatieplan

### Stap 1: Project Setup
- `dotnet new sln` + `dotnet new avalonia.app`
- NuGet packages toevoegen: Avalonia, SharpHook, TextCopy, Anthropic
- `.gitignore` voor .NET
- Basisstructuur mappen aanmaken

### Stap 2: Models & Configuratie
**Bestanden:** `Models/RewriteProfile.cs`, `Models/AppSettings.cs`, `Models/OAuthToken.cs`, `Services/ConfigService.cs`

- `RewriteProfile`: `Name`, `SystemPrompt` (herschrijfinstructies), `Model` (claude model)
- `AppSettings`: `HotkeyModifiers`, `HotkeyKey`, `ActiveProfileName`, `ShowNotifications`, `Profiles[]`
- Config opslag: `~/.config/textrewriter/settings.json` (Linux/macOS), `%APPDATA%/TextRewriter/settings.json` (Windows)
- Default profielen meegeven: "Herschrijf (Nederlands)", "Rewrite (English)", "Formeel", "Casual"

### Stap 3: OAuth Authenticatie
**Bestand:** `Services/AuthService.cs`

- Lees token uit `~/.claude/.credentials.json` (Linux) met structuur:
  ```json
  { "claudeAiOauth": { "accessToken": "sk-ant-oat01-...", "refreshToken": "sk-ant-ort01-...", "expiresAt": 1748658860401 } }
  ```
- macOS: lees uit Keychain via `security find-generic-password -s "Claude Code-credentials" -w`
- Windows: lees uit Windows Credential Manager
- Check `expiresAt` → als verlopen, refresh via `POST https://console.anthropic.com/api/oauth/token`
- Fallback: ondersteun `ANTHROPIC_API_KEY` environment variable als alternatief

### Stap 4: Claude API Integratie
**Bestand:** `Services/RewriteService.cs`

- Gebruik Anthropic SDK met OAuth token als bearer auth
- System prompt = profiel-instructies (bv. "Herschrijf de volgende tekst in formeel Nederlands. Behoud de originele betekenis.")
- User message = de te herschrijven tekst
- Streaming response voor snellere feedback
- Error handling: 401 → token refresh → retry; rate limit → wacht + retry

### Stap 5: Keyboard & Clipboard Services
**Bestanden:** `Services/HotkeyService.cs`, `Services/ClipboardService.cs`, `Services/KeySimulationService.cs`

**HotkeyService:**
- SharpHook `TaskPoolGlobalHook` voor globale hotkey detectie
- Registreer configureerbare shortcut (default: `Ctrl+Shift+R`, macOS: `Cmd+Shift+R`)
- Fire event wanneer shortcut ingedrukt

**ClipboardService:**
- `GetTextAsync()` / `SetTextAsync()` via TextCopy
- `SaveClipboard()` / `RestoreClipboard()` om originele clipboard inhoud te bewaren

**KeySimulationService:**
- SharpHook `EventSimulator` voor key simulation
- `SimulateCopy()`: Ctrl+C (Cmd+C op macOS)
- `SimulateSelectAll()`: Ctrl+A (Cmd+A op macOS)
- `SimulatePaste()`: Ctrl+V (Cmd+V op macOS)
- Platform detection via `RuntimeInformation.IsOSPlatform()`

### Stap 6: Rewrite Orchestrator (Kernlogica)
**Bestand:** `Services/RewriteOrchestrator.cs`

Flow wanneer hotkey ingedrukt:
1. Bewaar huidige clipboard inhoud
2. Simuleer Ctrl+C → wacht 100ms
3. Lees clipboard
4. Vergelijk met opgeslagen clipboard:
   - Als **gelijk** (niets was geselecteerd) → Simuleer Ctrl+A + Ctrl+C → lees clipboard opnieuw
   - Als **verschillend** → tekst was geselecteerd, gebruik deze
5. Stuur tekst + actief profiel instructies naar Claude API
6. Ontvang herschreven tekst
7. Kopieer herschreven tekst naar clipboard
8. Simuleer Ctrl+V → tekst wordt geplakt
9. Herstel originele clipboard inhoud (optioneel, na delay)

### Stap 7: System Tray & UI
**Bestanden:** `App.axaml`, `App.axaml.cs`, `ViewModels/TrayViewModel.cs`

**Tray-only applicatie** (geen hoofdvenster):
- `ShutdownMode.OnExplicitShutdown` in `App.axaml.cs`
- TrayIcon met NativeMenu in `App.axaml`:
  - **Actief profiel** (submenu met alle profielen, radiobutton-stijl)
  - Separator
  - **Instellingen...** → opent SettingsWindow
  - **Shortcut:** `Ctrl+Shift+R` (toont huidige shortcut)
  - Separator
  - **Afsluiten**

### Stap 8: Settings Window
**Bestanden:** `Views/SettingsWindow.axaml(.cs)`, `ViewModels/SettingsViewModel.cs`

Avalonia venster met tabs:
- **Profielen tab**: Lijst van profielen, naam bewerken, instructies (multiline TextBox), model selectie, toevoegen/verwijderen
- **Sneltoets tab**: Huidige shortcut tonen, "Nieuwe shortcut opnemen" knop
- **Authenticatie tab**: Status tonen (verbonden/niet verbonden), token info, "Vernieuw authenticatie" knop

### Stap 9: Notificaties & Feedback
- Tray icon tooltip wijzigt naar "Herschrijven..." tijdens verwerking
- Desktop notificatie bij succes/fout (optioneel, via Avalonia notification API of native toast)
- Bij fout: notificatie met foutmelding

### Stap 10: Packaging & Distributie
- Self-contained publish voor elk platform:
  - `dotnet publish -r win-x64 --self-contained`
  - `dotnet publish -r osx-arm64 --self-contained` (+ `.app` bundle)
  - `dotnet publish -r linux-x64 --self-contained`
- macOS: Info.plist met Accessibility permission request
- Linux: `.desktop` file voor autostart

## Platform-specifieke aandachtspunten

| Platform | Modifier Key | Clipboard | Hotkey | Extra |
|----------|-------------|-----------|--------|-------|
| Windows | Ctrl | TextCopy (native) | SharpHook | Geen extra permissions |
| macOS | Cmd (⌘) | TextCopy (pbcopy/pbpaste) | SharpHook | **Accessibility permission vereist** |
| Linux | Ctrl | TextCopy (xsel/xclip) | SharpHook (X11 only) | `xsel` moet geïnstalleerd zijn |

macOS Accessibility check via P/Invoke:
```csharp
[DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
static extern bool AXIsProcessTrustedWithOptions(IntPtr options);
```

## Default Profielen

1. **Herschrijf (NL)**: "Herschrijf de volgende tekst in helder, professioneel Nederlands. Behoud de originele betekenis en toon."
2. **Rewrite (EN)**: "Rewrite the following text in clear, professional English. Maintain the original meaning and tone."
3. **Formeel**: "Herschrijf in een formele, zakelijke stijl. Gebruik u-vorm."
4. **Casual**: "Herschrijf in een informele, vriendelijke toon. Gebruik je-vorm."
5. **Beknopt**: "Maak de tekst korter en bondiger zonder informatie te verliezen."

## Verificatie & Testen

1. **Build**: `dotnet build` moet slagen zonder fouten
2. **Unit tests**: Services testen met mocks (ConfigService, RewriteService)
3. **Handmatige test flow**:
   - Start applicatie → tray icon verschijnt
   - Open Notepad/TextEdit → typ tekst → selecteer → druk shortcut → tekst wordt herschreven
   - Zonder selectie → druk shortcut → alle tekst wordt herschreven
   - Rechtsklik tray icon → wissel profiel → herschrijf opnieuw → andere stijl
   - Open instellingen → wijzig profiel instructies → sla op → test opnieuw
4. **Auth test**: Verwijder token → app toont melding → herstel token → werkt weer
