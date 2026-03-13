using System.Net.Sockets;
using System.Text;
using Modbus.NET.Core;
using Modbus.NET.Core.Exceptions;
using Modbus.NET.Core.ModbusTcp;
using Modbus.NET.Core.OpcUa;
using Modbus.NET.Core.Utils;

namespace Modbus.NET.ConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.Title = "Modbus.NET & OPC UA Industrial Gateway Demo";
        Console.WriteLine("工业协议网关测试应用程序 - .NET 9 版本");
        Console.WriteLine("======================================");

        // 初始化统一网关
        using var gateway = new ProtocolGateway();

        // 1. 注册 Modbus TCP 设备
        string modbusDeviceId = "PLC_Modbus_1";
        string plcIpAddress = "127.0.0.1"; // 请替换为实际 IP
        int plcPort = 511;                 // 根据实际环境调整
        byte unitId = 1;                   // 根据实际环境调整

        var modbusClient = new ModbusTcpClient(plcIpAddress, plcPort, unitId);
        gateway.RegisterClient(modbusDeviceId, modbusClient);
        Console.WriteLine($"[网关] 已注册 Modbus TCP 设备: {modbusDeviceId} ({plcIpAddress}:{plcPort})");

        // 2. 注册 OPC UA 设备
        string opcuaDeviceId = "Server_OPCUA_1";
        string opcuaUrl = "opc.tcp://localhost:4840"; // 请替换为实际 OPC UA 服务器地址

        var opcuaClient = new OpcUaClient(opcuaUrl);
        gateway.RegisterClient(opcuaDeviceId, opcuaClient);
        Console.WriteLine($"[网关] 已注册 OPC UA 设备: {opcuaDeviceId} ({opcuaUrl})");

        Console.WriteLine("\n尝试连接所有设备...");

        try
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

            Console.WriteLine("------------------------------------");

            // --- 示例：使用统一网关接口操作 Modbus ---
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
            Console.WriteLine("------------------------------------");

            // --- 示例：使用统一网关接口操作 OPC UA ---
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
}
