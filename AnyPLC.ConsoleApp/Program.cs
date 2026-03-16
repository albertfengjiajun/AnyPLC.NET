using System.Net.Sockets;
using System.Text;
using AnyPLC.Core;
using AnyPLC.Core.Exceptions;
using AnyPLC.Core.ModbusTcp;
using AnyPLC.Core.Omron;
using AnyPLC.Core.OpcUa;
using AnyPLC.Core.S7;
using AnyPLC.Core.Utils;
using S7.Net;

namespace AnyPLC.ConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.Title = "AnyPLC & OPC UA Industrial Gateway Demo";
        Console.WriteLine("工业协议网关测试应用程序 - .NET 10 版本");
        Console.WriteLine("======================================");

        // 初始化统一网关
        using var gateway = new ProtocolGateway();

        // 注册各种设备
        string modbusDeviceId = "PLC_Modbus_1";
        string opcuaDeviceId = "Server_OPCUA_1";
        string s7DeviceId = "PLC_S7_1";
        string omronDeviceId = "PLC_Omron_1";

        RegisterDevices(gateway, modbusDeviceId, opcuaDeviceId, s7DeviceId, omronDeviceId);

        Console.WriteLine("\n尝试连接所有设备...");

        try
        {
            await TryConnectAllAsync(gateway);

            Console.WriteLine("------------------------------------");

            await TestModbusDeviceAsync(gateway, modbusDeviceId);
            Console.WriteLine("------------------------------------");

            await TestOpcUaDeviceAsync(gateway, opcuaDeviceId);
            Console.WriteLine("------------------------------------");

            await TestS7DeviceAsync(gateway, s7DeviceId);
            Console.WriteLine("------------------------------------");

            await TestOmronDeviceAsync(gateway, omronDeviceId);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"发生意外错误: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
        }
        finally
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine("正在断开所有设备的连接...");
            gateway.DisconnectAll(); // 网关 Dispose 时也会调用
            Console.WriteLine("已完成断开。");
        }

        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }

    private static void RegisterDevices(ProtocolGateway gateway, string modbusDeviceId, string opcuaDeviceId, string s7DeviceId, string omronDeviceId)
    {
        // 1. 注册 Modbus TCP 设备
        string plcIpAddress = "127.0.0.1"; // 请替换为实际 IP
        int plcPort = 511;                 // 根据实际环境调整
        byte unitId = 1;                   // 根据实际环境调整

        var modbusClient = new ModbusTcpClient(plcIpAddress, plcPort, unitId);
        gateway.RegisterClient(modbusDeviceId, modbusClient);
        Console.WriteLine($"[网关] 已注册 Modbus TCP 设备: {modbusDeviceId} ({plcIpAddress}:{plcPort})");

        // 2. 注册 OPC UA 设备
        string opcuaUrl = "opc.tcp://localhost:4840"; // 请替换为实际 OPC UA 服务器地址

        var opcuaClient = new OpcUaClient(opcuaUrl);
        gateway.RegisterClient(opcuaDeviceId, opcuaClient);
        Console.WriteLine($"[网关] 已注册 OPC UA 设备: {opcuaDeviceId} ({opcuaUrl})");

        // 3. 注册 Siemens S7 设备
        string s7IpAddress = "127.0.0.1"; // 请替换为实际 S7 PLC IP 地址
        CpuType cpuType = CpuType.S71200; // 根据实际 PLC 类型调整
        short rack = 0;
        short slot = 1;

        var s7Client = new S7Client(cpuType, s7IpAddress, rack, slot);
        gateway.RegisterClient(s7DeviceId, s7Client);
        Console.WriteLine($"[网关] 已注册 Siemens S7 设备: {s7DeviceId} ({s7IpAddress}, CPU: {cpuType})");

        // 4. 注册 Omron (欧姆龙) CIP 设备
        string omronIpAddress = "127.0.0.1"; // 请替换为实际 Omron PLC IP 地址

        var omronClient = new OmronClient(omronIpAddress);
        gateway.RegisterClient(omronDeviceId, omronClient);
        Console.WriteLine($"[网关] 已注册 Omron 设备: {omronDeviceId} ({omronIpAddress})");
    }

    private static async Task TryConnectAllAsync(ProtocolGateway gateway)
    {
        // 在实际应用中可能需要分开处理每个设备的连接以防阻塞或某个失败导致整体失败
        // 这里为了演示网关功能，尝试统一连接
        try
        {
            // 注意：如果没有开启相应的 OPC UA 或 Modbus TCP 服务器，这里可能会超时或抛出异常。
            // 为了演示流畅，我们将跳过连接失败的异常退出。
            await gateway.ConnectAllAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[网关] 设备连接初始化完成！");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[警告] 部分设备连接失败，可能是因为未启动测试服务器: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task TestModbusDeviceAsync(ProtocolGateway gateway, string modbusDeviceId)
    {
        if (gateway.GetClient(modbusDeviceId).IsConnected)
        {
            Console.WriteLine($"[Modbus] 正在通过网关操作设备: {modbusDeviceId}");
            string modbusCoilAddress = "Coil:0"; // 读取地址为0的线圈

            try
            {
                bool coilState = await gateway.ReadAsync<bool>(modbusDeviceId, modbusCoilAddress);
                Console.WriteLine($"-> 读取 {modbusCoilAddress} 的状态: {coilState}");

                Console.WriteLine($"-> 尝试将 {modbusCoilAddress} 写入为 true...");
                await gateway.WriteAsync(modbusDeviceId, modbusCoilAddress, true);

                bool newCoilState = await gateway.ReadAsync<bool>(modbusDeviceId, modbusCoilAddress);
                Console.WriteLine($"-> 再次读取 {modbusCoilAddress} 的状态: {newCoilState}");
            }
            catch (Exception modEx)
            {
                Console.WriteLine($"-> Modbus 操作异常: {modEx.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[Modbus] 设备 {modbusDeviceId} 未连接，跳过读写测试。");
        }
    }

    private static async Task TestOpcUaDeviceAsync(ProtocolGateway gateway, string opcuaDeviceId)
    {
        if (gateway.GetClient(opcuaDeviceId).IsConnected)
        {
            Console.WriteLine($"[OPC UA] 正在通过网关操作设备: {opcuaDeviceId}");
            // 常见的仿真节点，请根据你的 OPC UA 服务器实际节点替换
            string opcuaNodeId = "ns=2;s=Demo.Static.Scalar.Int32";

            try
            {
                int nodeValue = await gateway.ReadAsync<int>(opcuaDeviceId, opcuaNodeId);
                Console.WriteLine($"-> 读取节点 {opcuaNodeId} 的值: {nodeValue}");

                int newValue = nodeValue + 1;
                Console.WriteLine($"-> 尝试将节点 {opcuaNodeId} 写入为 {newValue}...");
                await gateway.WriteAsync(opcuaDeviceId, opcuaNodeId, newValue);

                int finalNodeValue = await gateway.ReadAsync<int>(opcuaDeviceId, opcuaNodeId);
                Console.WriteLine($"-> 再次读取节点 {opcuaNodeId} 的值: {finalNodeValue}");
            }
            catch (Exception opcEx)
            {
                Console.WriteLine($"-> OPC UA 操作异常: {opcEx.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[OPC UA] 设备 {opcuaDeviceId} 未连接，跳过读写测试。");
        }
    }

    private static async Task TestS7DeviceAsync(ProtocolGateway gateway, string s7DeviceId)
    {
        if (gateway.GetClient(s7DeviceId).IsConnected)
        {
            Console.WriteLine($"[Siemens S7] 正在通过网关操作设备: {s7DeviceId}");
            // DB块地址，根据实际 PLC DB 配置进行修改 (例如读取 DB1 的第 0 个偏移的双字)
            string s7Address = "DB1.DBD0";

            try
            {
                int s7Value = await gateway.ReadAsync<int>(s7DeviceId, s7Address);
                Console.WriteLine($"-> 读取地址 {s7Address} 的值: {s7Value}");

                int s7NewValue = s7Value + 10;
                Console.WriteLine($"-> 尝试将地址 {s7Address} 写入为 {s7NewValue}...");
                await gateway.WriteAsync(s7DeviceId, s7Address, s7NewValue);

                int s7FinalValue = await gateway.ReadAsync<int>(s7DeviceId, s7Address);
                Console.WriteLine($"-> 再次读取地址 {s7Address} 的值: {s7FinalValue}");
            }
            catch (Exception s7Ex)
            {
                Console.WriteLine($"-> Siemens S7 操作异常: {s7Ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[Siemens S7] 设备 {s7DeviceId} 未连接，跳过读写测试。");
        }
    }

    private static async Task TestOmronDeviceAsync(ProtocolGateway gateway, string omronDeviceId)
    {
        if (gateway.GetClient(omronDeviceId).IsConnected)
        {
            Console.WriteLine($"[Omron] 正在通过网关操作设备: {omronDeviceId}");
            // 欧姆龙地址，在使用 libplctag 时通常是 CIP 变量名或数组
            string omronAddress = "myIntVariable";

            try
            {
                short omronValue = await gateway.ReadAsync<short>(omronDeviceId, omronAddress);
                Console.WriteLine($"-> 读取标签 {omronAddress} 的值: {omronValue}");

                short omronNewValue = (short)(omronValue + 5);
                Console.WriteLine($"-> 尝试将标签 {omronAddress} 写入为 {omronNewValue}...");
                await gateway.WriteAsync(omronDeviceId, omronAddress, omronNewValue);

                short omronFinalValue = await gateway.ReadAsync<short>(omronDeviceId, omronAddress);
                Console.WriteLine($"-> 再次读取标签 {omronAddress} 的值: {omronFinalValue}");
            }
            catch (Exception omronEx)
            {
                Console.WriteLine($"-> Omron 操作异常 (需真实PLC或仿真器支持): {omronEx.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[Omron] 设备 {omronDeviceId} 未连接，跳过读写测试。");
        }
    }
}
