# AnyPLC.NET

![LICENSE](https://img.shields.io/github/license/albertfengjiajun/AnyPLC)
![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Version](https://img.shields.io/badge/version-1.0.0-brightgreen)

**AnyPLC.NET** (原 Modbus.NET) 是一个轻量级、跨平台且极其强大的 **.NET 多工业协议网关框架**。它旨在为开发者提供一个统一、简洁的 API，以消除不同工业设备（如 PLC、传感器、服务器）底层通信协议的差异。

通过 AnyPLC.NET，你可以轻松地在一个应用程序中同时管理和连接西门子、欧姆龙、Modbus 设备以及 OPC UA 服务器。

## ✨ 核心特性

- **🚀 最新框架**：完全基于现代的 .NET 10.0 构建，支持跨平台 (Windows, Linux, macOS)。
- **🌐 统一的网关架构**：通过核心类 `ProtocolGateway`，以唯一的设备 ID (`DeviceId`) 注册、连接和管理所有异构设备。
- **🔌 多协议支持 (全部基于纯开源免费组件)**：
  - **Modbus TCP**: 内部原生纯 C# 高性能实现。
  - **OPC UA**: 基于官方 `OPCFoundation.NetStandard.Opc.Ua`。
  - **Siemens S7**: 基于广受好评的 `S7netplus`，支持 S7-1200/1500/300/400 等。
  - **Omron**: 基于 `libplctag` (FINS/CIP)，支持 NJ/NX 等标签读写。
- **⚡ 异步优先**：全面采用 `async/await` 异步编程模型，不会阻塞主线程，适用于高并发网关场景。
- **🧩 极易扩展**：只需实现 `IProtocolClient` 接口，即可轻松接入你自己的私有协议或物联网 (IoT) 协议。

---

## 📦 开始使用

### 1. 从源码构建

```bash
git clone https://github.com/albertfengjiajun/AnyPLC.git
cd AnyPLC
dotnet build AnyPLC.NET.sln
```

### 2. 在你的项目中引用

在你的项目根目录下，添加对核心项目的引用（请将 `<AnyPLC源码目录>` 替换为你实际克隆该仓库的本地路径）：

```bash
dotnet add reference <AnyPLC源码目录>/AnyPLC.Core/AnyPLC.Core.csproj
```

或者构建整个解决方案后，将生成的 `AnyPLC.Core.dll` 文件添加为程序集引用。

---

## 🛠️ 快速入门：构建你的第一个工业网关

AnyPLC 的核心理念是**注册 -> 连接 -> 统一读写**。以下是一个典型的使用场景：

```csharp
using AnyPLC.Core;
using AnyPLC.Core.ModbusTcp;
using AnyPLC.Core.OpcUa;
using AnyPLC.Core.S7;
using AnyPLC.Core.Omron;
using S7.Net;

// 1. 初始化统一网关 (支持 IDisposable)
using var gateway = new ProtocolGateway();

// 2. 注册不同协议的设备并赋予全局唯一的 DeviceID
gateway.RegisterClient("Line1_Modbus", new ModbusTcpClient("192.168.1.10", 502, 1));
gateway.RegisterClient("Line2_S7", new S7Client(CpuType.S71200, "192.168.1.20", 0, 1));
gateway.RegisterClient("Plant_OPCUA", new OpcUaClient("opc.tcp://192.168.1.30:4840"));
gateway.RegisterClient("Line3_Omron", new OmronClient("192.168.1.40"));

// 3. 统一发起异步连接
// 网关将并行连接所有已注册的客户端
await gateway.ConnectAllAsync();

// 4. 使用统一的泛型 API 读取数据
try
{
    // 读取 Modbus 线圈
    bool isRunning = await gateway.ReadAsync<bool>("Line1_Modbus", "Coil:0");

    // 读取 S7 数据块
    int speed = await gateway.ReadAsync<int>("Line2_S7", "DB1.DBD0");

    // 读取 OPC UA 节点
    int temperature = await gateway.ReadAsync<int>("Plant_OPCUA", "ns=2;s=Demo.Static.Scalar.Int32");

    // 读取 Omron 标签
    short pressure = await gateway.ReadAsync<short>("Line3_Omron", "myPressureTag");

    Console.WriteLine($"状态: {isRunning}, 速度: {speed}, 温度: {temperature}, 压力: {pressure}");

    // 5. 写入数据
    await gateway.WriteAsync("Line1_Modbus", "HoldingRegister:100", (short)12345);
    await gateway.WriteAsync("Line2_S7", "DB1.DBD4", (int)8888);
}
catch(Exception ex)
{
    Console.WriteLine($"通信异常: {ex.Message}");
}
// 离开 using 作用域时，网关会自动断开所有连接并释放资源
```

---

## 📋 协议与地址映射指南

在使用 `gateway.ReadAsync<T>(deviceId, address)` 时，不同的底层客户端要求不同的 `address` 字符串格式：

| 协议名称 | 底层实现库 | 地址字符串 (`address`) 格式要求 | 示例 |
| :--- | :--- | :--- | :--- |
| **Modbus TCP** | *(内置)* | `<类型>:<0基地址>`。支持的类型：`Coil`, `HoldingRegister` | `Coil:0`<br>`HoldingRegister:100` |
| **Siemens S7** | `S7netplus` | 标准西门子绝对地址格式 | `DB1.DBD0` (读取双字)<br>`M10.0` (读取位) |
| **OPC UA** | `OPCFoundation` | 标准的 OPC UA 节点 ID (NodeId) 字符串 | `ns=2;s=MyVariable`<br>`i=2258` |
| **Omron (CIP)** | `libplctag` | 欧姆龙 PLC 中定义的标签变量名 | `MyIntVariable`<br>`D100` (取决于配置) |

---

## 🧱 高级：如何扩展自定义协议？

AnyPLC.NET 设计得非常灵活。如果你需要接入一个特殊的串口设备或 REST API，只需要创建一个类实现 `IProtocolClient` 即可：

```csharp
using AnyPLC.Core.Interfaces;

public class MyCustomClient : IProtocolClient
{
    public bool IsConnected { get; private set; }

    public Task ConnectAsync(CancellationToken cancellationToken = default) { /* 连接逻辑 */ }
    public void Disconnect() { /* 断开逻辑 */ }

    public Task<T> ReadAsync<T>(string address) { /* 读取逻辑 */ }
    public Task WriteAsync<T>(string address, T value) { /* 写入逻辑 */ }

    public void Dispose() => Disconnect();
}

// 然后在你的应用中直接注册给网关：
gateway.RegisterClient("MyCustomDevice", new MyCustomClient());
```

---

## 🧪 测试与演示应用

本项目包含完整的自动化测试以及一个详尽的演示控制台程序：

- **自动化测试**: 运行 `dotnet test AnyPLC.NET.sln` 即可执行 `AnyPLC.Tests` 中的 xUnit 测试用例。
- **演示应用**: 导航到 `AnyPLC.ConsoleApp` 目录并运行 `dotnet run`，它将演示如何并行连接 Modbus, OPC UA, S7 和 Omron 客户端。**(提示：请在运行前修改 `Program.cs` 中的 IP 地址为你网络中的真实设备或模拟器地址)**。

## 🤝 贡献

如果你希望增加对新协议（如 MQTT, EtherNet/IP, BACnet）的支持，或者发现了 Bug，我们非常欢迎你提交 Issue 和 Pull Request！

## 📄 许可证

本项目采用 MIT 许可证 - 详情请查看 [LICENSE](LICENSE) 文件。
