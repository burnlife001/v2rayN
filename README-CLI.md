# v2rayN CLI 使用与架构说明

v2rayN 附带一个独立的命令行工具 **v2rayn-cli**，用于在不打开 GUI 的情况下查询节点、切换服务器、更新订阅、重启核心、检查更新。它通过 **命名管道（Named Pipe）** 与正在运行的 v2rayN 主程序通信；如果主程序未运行，CLI 会自动拉起它。

## 快速上手

```bash
v2rayn node list                 # 列出全部节点（按订阅分组显示）
v2rayn node list --sub <id>      # 只列出某个订阅下的节点
v2rayn node switch <name>        # 切换活动服务器（按备注名模糊匹配，不区分大小写）
v2rayn sub list                  # 列出所有订阅
v2rayn sub update                # 更新全部订阅
v2rayn sub update --id <id>      # 只更新指定订阅
v2rayn sub update --via-proxy    # 通过代理更新
v2rayn core restart              # 重启代理核心（Xray/sing-box 等）
v2rayn app update                # 检查并更新 v2rayN 本体
v2rayn app update --pre-release  # 包含预发布版本
v2rayn launch                    # 启动 v2rayN（已在运行则直接返回成功）
```

所有命令都支持 `--json` 后缀，输出结构化 JSON，便于脚本处理：

```bash
v2rayn node list --json | jq '.[].Name'
```

无参数或 `-h` / `--help` / `help` 打印帮助。

## 退出码

| 码 | 常量 | 含义 |
|----|------|------|
| 0 | `Success` | 成功 |
| 1 | `BusinessError` | 服务端业务失败（如节点未找到、更新失败） |
| 2 | `UsageError` | 命令行用法错误 / 未知命令 |
| 3 | `ConnectionError` | 找不到 v2rayN 可执行文件，或无法启动/连接主程序 |
| 130 | `Cancelled` | 用户取消（Ctrl+C 约定码） |

## 架构

CLI 是**客户端-服务端**结构，两端都在本仓库内：

```
┌──────────────────┐   named pipe (JSON Lines)   ┌──────────────────────────┐
│  v2rayN.Cli      │ ──────────────────────────▶ │  v2rayN / v2rayN.Desktop │
│  (v2rayn-cli)    │ ◀────────────────────────── │  (GUI 主程序)             │
└──────────────────┘                             └──────────────────────────┘
```

### 客户端：`v2rayN/v2rayN.Cli/`

| 文件 | 职责 |
|------|------|
| `Program.cs` | 入口：解析命令/参数、探测管道、必要时拉起主程序、路由到各命令组 |
| `CliClient.cs` | 命名管道客户端：连接、发送请求行、读取响应行（30s 超时） |
| `ProcessLauncher.cs` | 定位 v2rayN 可执行文件（同目录 → 上级目录 → macOS .app bundle）、检测进程、启动并等待管道就绪（最长 30s） |
| `Commands/*.cs` | `node` / `sub` / `core` / `app` 四组命令的实现与响应处理 |
| `OutputFormatter.cs` | 人类可读文本 vs `--json` 输出；错误打印到 stderr（红色） |
| `ExitCodes.cs` | 退出码常量 |

自动拉起逻辑（`Program.RunAsync`）：先用 500ms 短超时探测管道；管道不存在且命令不是 `launch` 时，自动启动主程序并轮询等待管道就绪，再发送命令。

### 服务端：`v2rayN/ServiceLib/`

GUI 主程序（WPF 的 `App.xaml.cs` 与 Avalonia 的 `v2rayN.Desktop/Program.cs`）启动时通过 `CliPipeServiceFactory` 创建并启动管道服务：

| 组件 | 位置 | 职责 |
|------|------|------|
| `CliPipeService` | `Services/CliPipeService.cs` | 命名管道监听循环（最多 10 实例），按行读请求、写响应，单请求上限 1MB |
| `CliPipeServiceFactory` | `Services/CliPipeServiceFactory.cs` | 组装 dispatcher + 全部命令 + 单例 bridge |
| `CliCommandDispatcher` | `Handler/CliCommandDispatcher.cs` | 按 `request.Cmd` 路由到对应 `ICliCommand`，兜底异常 → `Fail` 响应 |
| `ICliCommand` 实现 | `Handler/CliCommands/*.cs` | `node.list` / `node.switch` / `sub.list` / `sub.update` / `core.restart` / `app.update` 六个命令的参数校验与执行 |
| `ICliCommandBridge` / `CliCommandBridge` | `Services/` | 命令与 GUI 之间的桥：读 `AppManager` 的节点/订阅数据；需要 UI 上下文的操作（切节点、更新订阅、重启核心）委托给 `MainWindowViewModel` / `ProfilesViewModel`（ViewModel 初始化时把自己注册到单例 bridge 上） |
| DTO | `Models/Dto/CliCommand/` | `CliRequest`、`CliResponse`、`NodeListItem`、`SubListItem` |

### 管道命名

管道名由主程序可执行文件路径决定，保证同一安装位置的 CLI 与 GUI 总能对上、不同安装互不串扰：

```
v2rayN-cli-v1-{md5(exePath)}
```

两端都用 `CliPipeService.GetPipeName(exePath)` 计算，CLI 侧用 `ProcessLauncher.GetV2RayNExePath()` 找到同一份 exe 路径。

### 线协议（JSON Lines）

一次连接一条请求、一条响应，均为 **UTF-8、单行 JSON、`\n` 结尾**：

```json
→ {"Cmd":"node.switch","Args":{"name":"香港"},"Version":"","Id":"<guid>"}
← {"Success":true,"Data":{"Id":"...","Name":"香港01","Address":"...","Port":443,"Type":"Vless","Delay":"","GroupId":"...","GroupName":""},"Error":null,"Id":"<guid>"}
```

失败响应：`{"Success":false,"Data":null,"Error":"Node not found: xxx","Id":"..."}`。

## 构建

CLI 是普通 .NET 控制台项目，Release 下发布为单文件：

```bash
dotnet publish v2rayN/v2rayN.Cli -c Release
```

产物名为 `v2rayn-cli`（`AssemblyName` 指定）。**部署要求**：CLI 必须与 `v2rayN.exe`（Windows）/ `v2rayN`（Linux/macOS）放在同一目录（或其下一级目录），否则 `GetV2RayNExePath` 会抛出 `FileNotFoundException`，退出码 3。

## 测试

服务端逻辑有完整单测，位于 `v2rayN/ServiceLib.Tests/CliCommand/`：

- `CliCommandHandlerTests.cs` — 六个命令的成功/失败路径（用 `FakeBridge` 隔离 UI）
- `CliCommandDispatcherTests.cs` — 路由、未知命令、缺命令
- `CliPipeServiceTests.cs` — 真实命名管道收发回环
- `CliCommandBridgeTests.cs`、`CliDtoTests.cs` — 桥与 DTO 序列化

```bash
dotnet test v2rayN/ServiceLib.Tests
```

## 扩展一个新命令

1. `ServiceLib/Handler/CliCommands/` 新增实现 `ICliCommand` 的类（`Name` 用 `域.动作` 格式，如 `node.ping`）。
2. 若需要新能力，在 `ICliCommandBridge` 加方法并在 `CliCommandBridge` 实现。
3. 在 `CliPipeServiceFactory.Create` 的列表里注册命令。
4. CLI 侧在对应 `Commands/*CliCommands.cs` 加方法，并在 `Program.cs` 的 switch 中接线（别忘了更新 `PrintHelp`）。
5. 在 `ServiceLib.Tests/CliCommand/` 补单测。
