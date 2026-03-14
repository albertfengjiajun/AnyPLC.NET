# AI Agents 指南 (AI Agents Guidelines)

欢迎，AI 助手！这个文件包含了协助开发和维护 **AnyPLC.NET** 项目的特定约定和指令。当处理与此代码库相关的任务时，请务必遵守以下规则：

## 核心约束：语言 (Core Constraint: Language)
- **始终保持中文对话**。无论是回复用户的提问、提供计划、编写代码注释还是提交 PR 描述，都必须使用流利且专业的简体中文。
- **Git 提交信息必须为中文**。在使用 `submit` 工具进行提交时，`commit_message`（包括标题和正文）、`title` 和 `description` 都必须使用中文编写，严禁使用英文作为主标题。
- (Always keep the conversation language in Chinese. Replies, plans, code comments, PR descriptions, and git commit messages must be in fluent, professional Simplified Chinese.)

## 项目概述
AnyPLC.NET 是一个基于 **.NET 10.0** 构建的多工业协议网关。它通过统一的接口和管理器，抽象出底层协议细节，使得在一个应用中能够同时和多种异构工业设备（如 PLC、传感器）通信。

## 架构原则
- **接口驱动**：所有新的协议实现都必须继承自 `AnyPLC.Core.Interfaces.IProtocolClient`。
- **网关统一管理**：所有的设备读写生命周期应能通过 `AnyPLC.Core.ProtocolGateway` 管理器进行调度，使用唯一的 `deviceId` 进行映射。
- **异步优先**：I/O 操作（读写）必须实现为基于 `Task` 的异步方法（`ReadAsync<T>` 和 `WriteAsync<T>`）。若底层库不支持原生异步，需使用 `Task.Run()` 进行安全包装，确保不阻塞主线程。

## 第三方依赖与选型 (开源优先)
- **严禁引入商业/收费库**。例如，明确**禁止使用** `HslCommunication` 和 `Opc.UaFx.Client`。
- 在增加新协议支持时，**必须**寻找和使用开源、免费、热度高且稳定的社区库。
  - OPC UA：使用 `OPCFoundation.NetStandard.Opc.Ua`。
  - Siemens S7：使用 `S7netplus`。
  - Omron / AB / Modbus (可选)：使用 `libplctag` 或我们自研的纯 C# ModbusTcpClient。

## 测试规范
- 本项目包含一个基于 `xUnit` 的自动化测试套件项目：`AnyPLC.Tests`。
- 添加新功能或修改核心逻辑后，必须编写或更新相应的单元测试。
- 在不依赖物理硬件设备进行测试时，请使用 `Moq` 库来模拟 (Mock) `IProtocolClient` 的接口行为。
- 执行测试的命令为：`dotnet test AnyPLC.NET.sln`。

## 其他开发惯例
- 遇到任何编译、依赖或测试失败的情况，请优先仔细阅读错误日志，诊断根本原因，而不是盲目安装或卸载包。
- 在修改代码后，必须自行进行编译构建测试 (`dotnet build AnyPLC.NET.sln`)，确保没有引入新的警告或错误。

---
*此文件将帮助你更好地理解项目的上下文并避免走弯路。祝编码愉快！*
