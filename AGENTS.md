# Agent notes — pass-through-extensions

- Windows app for HCI Nerdz *pass-through extensions* pattern.
- Solution: `PassThroughExtensions.sln` — Core (peel/settings/registry), Broker (open verb), App (WPF UI), tests.
- User-level registry only (`HKCU\Software\Classes`) via *Apply to Explorer*.
- Settings: `%LOCALAPPDATA%\HCI-Nerdz\pass-through-extensions\settings.json` (`customSuffixes`, `disabledDefaults`).
- Built-in list includes `.old`. Users add more in the app.
- `legacy/` — original PowerShell installer (superseded).
- Docs: https://hci-nerdz.github.io/docs/hci-nerdz/pass-through-extensions.html
- Demo: https://hci-nerdz.github.io/demos/pass-through-extensions/
- Machine/env facts: `$CODE_ROOT/MEMORIES.md` only.
