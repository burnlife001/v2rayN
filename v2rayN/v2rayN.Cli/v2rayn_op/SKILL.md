---
name: v2rayn_op
description: |
  Control the local running v2rayN instance via CLI (node switch, sub update,
  core restart, app update). Use this skill when the user asks to switch proxy
  nodes, update subscriptions, restart the v2rayN core, or check for v2rayN
  updates from the terminal. Also triggers when the user says "切换节点",
  "更新订阅", "重启v2rayN", "检查v2rayN更新", or wants to list/manage nodes
  and subscriptions via CLI.
---

# v2rayn_op_local

Control the local v2rayN instance via its CLI (`v2rayn-cli`).

## Path Resolution (soft-coded)

Resolve the CLI path in this order:

1. **Environment variable** `V2RAYN_HOME` — if set, look for `<V2RAYN_HOME>/v2rayn-cli.exe`.
2. **Auto-detect** — scan these directories (newest first):
   - `E:/__work/__github/__network/v2rayN/v2rayN/v2rayN.Desktop/bin/Release/net10.0/v2rayn-cli.exe`
   - `E:/__work/__github/__network/v2rayN/v2rayN/v2rayN.Desktop/bin/Debug/net10.0/v2rayn-cli.exe`
3. **Fallback** — ask the user to provide the path.

On first run in a session, print the resolved path:

```
v2rayn-cli: <resolved-path>
```

If the file does not exist at the resolved path, report it and ask the user to build or correct the path.

**To persist a path**, set `V2RAYN_HOME` in the PowerShell profile:

```powershell
# PowerShell profile ($PROFILE)
$env:V2RAYN_HOME = "C:\path\to\v2rayN\v2rayN.Desktop\bin\Release\net10.0"
```

## Prerequisites

- v2rayN GUI must be running (CLI auto-launches it if not — uses `launch` command).
- Pipe communication timeout: 30s default.

## Available Commands

All commands use `--json` for structured output. Map user intent to CLI command:

### Node Operations (`node`)

| Intent | CLI Command | Notes |
|--------|-----------|-------|
| List all nodes | `v2rayn-cli node list --json` | Returns JSON array of nodes |
| Switch to node | `v2rayn-cli node switch <name> --json` | `<name>` is partial match (case-insensitive) against node Remarks |

### Subscription Operations (`sub`)

| Intent | CLI Command | Notes |
|--------|-----------|-------|
| List subscriptions | `v2rayn-cli sub list --json` | Returns JSON array of subs |
| Update all subscriptions | `v2rayn-cli sub update --json` | No `--id` = update all |
| Update specific sub | `v2rayn-cli sub update --id <subId> --json` | `<subId>` from `sub list` output |
| Update via proxy | `v2rayn-cli sub update --via-proxy --json` | Add `--via-proxy` flag |

### Core Operations (`core`)

| Intent | CLI Command | Notes |
|--------|-----------|-------|
| Restart core service | `v2rayn-cli core restart --json` | Calls `MainWindowViewModel.Reload()` |

### App Operations (`app`)

| Intent | CLI Command | Notes |
|--------|-----------|-------|
| Check for v2rayN update | `v2rayn-cli app update --json` | Downloads if newer version found |
| Check pre-release | `v2rayn-cli app update --pre-release --json` | Include pre-release versions |

### Launch

| Intent | CLI Command | Notes |
|--------|-----------|-------|
| Start v2rayN if not running | `v2rayn-cli launch` | Returns immediately if already running |

## Workflow Patterns

### Pattern A: Switch Node

```
1. If user doesn't specify node name → node list, show top nodes, ask which.
2. If user specifies name → node switch <name>.
3. Report switch result (node Name, Address, Port, Type).
```

### Pattern B: Update Subscription

```
1. sub update --json (update all, default).
2. If user asks for a specific sub → sub list first, then sub update --id <id>.
3. Report updated sub count.
```

### Pattern C: Restart Core

```
1. Confirm with user before restart (destructive — kills active connections).
2. core restart --json.
3. Wait a moment, verify v2rayN is still running.
```

### Pattern D: Check Updates

```
1. app update --json (standard release).
2. If user asks for pre-release → add --pre-release.
3. Report: "Update check triggered — result logged by UpdateService."
```

## Error Handling

| Exit Code | Meaning | Action |
|-----------|---------|--------|
| 0 | Success | Report result |
| 1 | Business error (e.g., name not found) | Show error message, suggest retry |
| 2 | Connection error (pipe unavailable) | Check if v2rayN is running, suggest `launch` |
| 3 | Usage error (wrong args) | Show correct usage |

## Under the Hood

CLI communicates with the running v2rayN GUI via Windows Named Pipe:

```
v2rayn-cli.exe
  → NamedPipeClientStream (pipe name derived from exe path)
    → CliPipeService (v2rayN GUI side)
      → CliCommandDispatcher → ICliCommand
        → CliCommandBridge → ViewModel methods
```

The bridge reuses the same entry points as GUI human operations:
- `node.switch` → `AppEvents.SetDefaultServerRequested` → `ProfilesViewModel.SetDefaultServer()`
- `sub.update` → `MainWindowViewModel.UpdateSubscriptionProcess()`
- `core.restart` → `MainWindowViewModel.Reload()`
- `app.update` → `UpdateService.CheckUpdateGuiN()`
