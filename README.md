# Modbus.NET

![LICENSE](https://img.shields.io/github/license/albertfengjiajun/Modbus.NET)
![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![Version](https://img.shields.io/badge/version-0.1.0-brightgreen)

Modbus.NET 是一个轻量级的 .NET Modbus 通信库，专为与支持 Modbus TCP 协议的设备（如 PLC）进行通信设计。

## 特性

- 纯 C# 实现，支持 .NET 9.0
- 支持 Modbus TCP 协议
- 完整支持线圈（Coils）操作：读取、写入单个线圈，批量读写多个线圈
- 完整支持保持寄存器（Holding Registers）操作：读取、写入单个寄存器，批量读写多个寄存器
- 支持对寄存器中特定位的读写操作
- 内置类型转换：短整型（short/ushort）和字符串与寄存器的转换
- 友好的异常处理
- 简洁直观的 API 设计
- 详细的代码注释和示例

## 开始使用

### 安装

#### 1. 从源码构建

```bash
git clone https://github.com/albertfengjiajun/Modbus.NET.git
cd Modbus.NET
dotnet build
```

#### 2. 在你的项目中引用

在项目中添加对 Modbus.NET.Core 的引用：

```bash
dotnet add reference path/to/Modbus.NET.Core/Modbus.NET.Core.csproj
```

或将编译后的 DLL 添加为引用。

### 使用示例

#### 基本连接与操作

```csharp
using Modbus.NET.Core.ModbusTcp;

// 创建 Modbus TCP 客户端
using var client = new ModbusTcpClient("192.168.1.10", 502, 1);

// 连接到设备
await client.ConnectAsync();

// 读取线圈
bool coilState = await client.ReadSingleCoilAsync(0); // 读取地址为0的线圈 (00001)
Console.WriteLine($"线圈状态: {coilState}");

// 写入线圈
await client.WriteSingleCoilAsync(0, true); // 将地址为0的线圈设置为ON

// 读取保持寄存器
short value = await client.ReadShortAsync(100); // 读取地址为100的寄存器 (40101)
Console.WriteLine($"寄存器值: {value}");

// 写入保持寄存器
await client.WriteShortAsync(100, 12345); // 写入短整型值

// 读取字符串
string text = await client.ReadStringAsync(200, 10); // 从地址200开始读取10个寄存器的字符串
Console.WriteLine($"字符串: {text}");

// 断开连接
client.Disconnect();
```

#### 批量操作

```csharp
// 读取多个线圈
bool[] coils = await client.ReadMultipleCoilsAsync(10, 5); // 从地址10开始读取5个线圈

// 写入多个线圈
await client.WriteMultipleCoilsAsync(10, new bool[] { true, false, true, false, true });

// 写入多个寄存器
await client.WriteMultipleRegistersAsync(100, new ushort[] { 1, 2, 3, 4, 5 });
```

#### 寄存器位操作

```csharp
// 读取寄存器中的特定位
bool bitValue = await client.ReadBoolFromRegisterBitAsync(300, 5); // 读取地址300的寄存器的第5位

// 写入寄存器中的特定位
await client.WriteBoolToRegisterBitAsync(300, 5, true); // 将地址300的寄存器的第5位设置为true
```

#### 字符串操作

```csharp
// 写入字符串
await client.WriteStringAsync(200, "Hello Modbus", 10);

// 读取字符串（支持字节对顺序颠倒）
string text = await client.ReadStringAsync(200, 10, true); // 第三个参数为true表示颠倒字节对顺序
```

## 注意事项

- 所有地址均为 0-based。例如，标准 Modbus 中的线圈 1 对应地址 0，保持寄存器 40001 对应地址 0。
- 默认连接超时为 5 秒。
- 异常处理请参考示例代码中的 try-catch 块。

## 高级用法

完整的高级用法示例请参考 `Modbus.NET.ConsoleApp` 项目中的示例代码。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

本项目采用 MIT 许可证 - 详情请查看 [LICENSE](LICENSE) 文件。

## 致谢

感谢所有对此项目做出贡献的开发者！
