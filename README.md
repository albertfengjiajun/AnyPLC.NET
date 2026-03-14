# AnyPLC

![LICENSE](https://img.shields.io/github/license/albertfengjiajun/AnyPLC)
![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Version](https://img.shields.io/badge/version-1.0.0-brightgreen)

AnyPLC 是一个轻量级且强大的 .NET 多工业协议网关通信库，专为与各种主流工业设备（如 PLC）进行通信而设计。

## 特性

- 支持最新的 .NET 10.0
- **多协议支持**：
  - Modbus TCP 协议
  - OPC UA (基于 `Opc.UaFx.Client`)
  - Siemens S7 (基于 `S7netplus`)
  - Omron (基于 `libplctag`)
- **统一的网关架构 (`ProtocolGateway`)**：允许使用一致的 API 注册、连接和操作不同协议的设备。
- 友好的异常处理与异步编程模型
- 简洁直观的 API 设计，详尽的中文注释

## 开始使用

### 安装

#### 1. 从源码构建

```bash
git clone https://github.com/albertfengjiajun/AnyPLC.git
cd AnyPLC
dotnet build
```

#### 2. 在你的项目中引用

在你的项目根目录下，添加对 AnyPLC.Core 项目的引用（请将 `<AnyPLC源码目录>` 替换为你实际克隆该仓库的本地路径）：

```bash
dotnet add reference <AnyPLC源码目录>/AnyPLC.Core/AnyPLC.Core.csproj
```

或将编译后的 `AnyPLC.Core.dll` 文件添加为程序集引用。

### 使用示例

#### 统一网关操作

AnyPLC 的核心是 `ProtocolGateway`，你可以通过它注册不同协议的设备并统一调用。

```csharp
using AnyPLC.Core;
using AnyPLC.Core.ModbusTcp;
using AnyPLC.Core.OpcUa;
using AnyPLC.Core.S7;
using S7.Net;

// 1. 初始化统一网关
using var gateway = new ProtocolGateway();

// 2. 注册不同协议的设备
gateway.RegisterClient("MyModbusDevice", new ModbusTcpClient("192.168.1.10", 502, 1));
gateway.RegisterClient("MyOpcUaServer", new OpcUaClient("opc.tcp://localhost:4840"));
gateway.RegisterClient("MyS7PLC", new S7Client(CpuType.S71200, "192.168.1.20", 0, 1));

// 3. 统一连接
await gateway.ConnectAllAsync();

// 4. 读取/写入 Modbus 数据
bool coilState = await gateway.ReadAsync<bool>("MyModbusDevice", "Coil:0");
await gateway.WriteAsync("MyModbusDevice", "HoldingRegister:100", (short)12345);

// 5. 读取/写入 OPC UA 节点
int opcuaValue = await gateway.ReadAsync<int>("MyOpcUaServer", "ns=2;s=Demo.Static.Scalar.Int32");

// 6. 读取/写入 Siemens S7 DB块
int s7Value = await gateway.ReadAsync<int>("MyS7PLC", "DB1.DBD0");

// 断开所有连接自动由 Gateway 的 Dispose 负责
```

## 注意事项

- Modbus 地址均为 0-based，并且遵循 `Type:Address` 格式（如 `Coil:0`, `HoldingRegister:100`）。
- 各个协议底层实现可能抛出其专属异常，建议在使用 `ProtocolGateway` 时配合良好的 Try-Catch 处理。

## 高级用法与测试

完整的高级用法示例请参考 `AnyPLC.ConsoleApp` 项目中的示例代码。项目同时提供了基于 xUnit 的测试套件 `AnyPLC.Tests` 以供参考。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

本项目采用 MIT 许可证 - 详情请查看 [LICENSE](LICENSE) 文件。

## 致谢

感谢所有对此项目做出贡献的开发者！
