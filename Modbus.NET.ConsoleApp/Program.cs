using System.Net.Sockets;
using System.Text;
using Modbus.NET.Core.Exceptions;
using Modbus.NET.Core.ModbusTcp;
using Modbus.NET.Core.Utils;

namespace Modbus.NET.ConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        // !!! 重要: 将 "YOUR_PLC_IP_ADDRESS" 替换为你的PLC的实际IP地址 !!!
        string plcIpAddress = "127.0.0.1"; // 例如 "192.168.1.10" 或 "127.0.0.1" (如果使用本地模拟器)
        int plcPort = 511;
        byte unitId = 255; // 通常PLC的单元ID为1

        // 提醒：所有地址都是0-based。
        // 例如，线圈1对应地址0，保持寄存器40001对应地址0。

        Console.Title = "Modbus.NET Demo Application";
        Console.WriteLine("Modbus.NET 测试应用程序 - .NET 9 版本");
        Console.WriteLine("======================================");

        using (var client = new ModbusTcpClient(plcIpAddress, plcPort, unitId))
        {
            try
            {
                Console.WriteLine($"正在尝试连接到PLC: {plcIpAddress}:{plcPort} Unit ID: {unitId}...");
                await client.ConnectAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("成功连接到PLC！");
                Console.ResetColor();
                Console.WriteLine("------------------------------------");

                // --- 示例：线圈操作 ---
                ushort coilAddr = 0; // 第1个线圈 (00001)
                Console.WriteLine($"读取地址为 {coilAddr} 的线圈状态...");
                bool initialCoilState = await client.ReadSingleCoilAsync(coilAddr);
                Console.WriteLine($"线圈 {coilAddr} 的初始状态: {initialCoilState}");

                Console.WriteLine($"正在将线圈 {coilAddr} 置为 ON (true)...");
                await client.WriteSingleCoilAsync(coilAddr, true);
                bool coilStateAfterWriteOn = await client.ReadSingleCoilAsync(coilAddr);
                Console.WriteLine($"线圈 {coilAddr} 写入ON后的状态: {coilStateAfterWriteOn}");

                Console.WriteLine($"正在将线圈 {coilAddr} 置为 OFF (false)...");
                await client.WriteSingleCoilAsync(coilAddr, false);
                bool coilStateAfterWriteOff = await client.ReadSingleCoilAsync(coilAddr);
                Console.WriteLine($"线圈 {coilAddr} 写入OFF后的状态: {coilStateAfterWriteOff}");
                Console.WriteLine("------------------------------------");

                // --- 示例：多个线圈操作 ---
                ushort startCoilAddrMulti = 10; // 从第11个线圈开始 (00011)
                ushort numCoils = 5;
                Console.WriteLine($"读取从地址 {startCoilAddrMulti} 开始的 {numCoils} 个线圈...");
                bool[] multiCoils = await client.ReadMultipleCoilsAsync(startCoilAddrMulti, numCoils);
                for (int i = 0; i < multiCoils.Length; i++)
                {
                    Console.WriteLine($"线圈 {startCoilAddrMulti + i}: {multiCoils[i]}");
                }

                bool[] coilsToWrite = new bool[] { true, false, true, true, false };
                Console.WriteLine($"正在写入从地址 {startCoilAddrMulti} 开始的多个线圈，值为: {string.Join(", ", coilsToWrite)}");
                await client.WriteMultipleCoilsAsync(startCoilAddrMulti, coilsToWrite);
                
                Console.WriteLine($"读取写入后从地址 {startCoilAddrMulti} 开始的 {numCoils} 个线圈...");
                multiCoils = await client.ReadMultipleCoilsAsync(startCoilAddrMulti, numCoils);
                for (int i = 0; i < multiCoils.Length; i++)
                {
                    Console.WriteLine($"线圈 {startCoilAddrMulti + i} (写入后): {multiCoils[i]}");
                }
                Console.WriteLine("------------------------------------");

                // --- 示例：保持寄存器操作 (short/ushort) ---
                ushort regAddrShort = 100; // 第101个保持寄存器 (40101)
                short shortToWrite = -12345;
                Console.WriteLine($"正在写入short值 {shortToWrite} 到寄存器 {regAddrShort}...");
                await client.WriteShortAsync(regAddrShort, shortToWrite);
                short readShortValue = await client.ReadShortAsync(regAddrShort);
                Console.WriteLine($"从寄存器 {regAddrShort} 读取的short值: {readShortValue}");

                ushort regAddrUShort = 101; // 第102个保持寄存器 (40102)
                ushort ushortToWrite = 54321;
                Console.WriteLine($"正在写入ushort值 {ushortToWrite} 到寄存器 {regAddrUShort}...");
                await client.WriteUShortAsync(regAddrUShort, ushortToWrite);
                ushort readUShortValue = await client.ReadUShortAsync(regAddrUShort);
                Console.WriteLine($"从寄存器 {regAddrUShort} 读取的ushort值: {readUShortValue}");
                Console.WriteLine("------------------------------------");

                // --- 示例：保持寄存器操作 (string) ---
                ushort strStartAddr = 200; // 字符串起始寄存器地址 (40201)
                ushort strNumRegs = 10;    // 字符串占用10个寄存器 (20字节)
                string? stringToWrite = "HelloModbusTCP!";

                // 写入字符串 (正常顺序)
                Console.WriteLine($"正在写入字符串 \"{stringToWrite}\" 到从地址 {strStartAddr} 开始的 {strNumRegs} 个寄存器 (正常字节顺序)...");
                await client.WriteStringAsync(strStartAddr, stringToWrite, strNumRegs, false, Encoding.ASCII);
                string readStringNormal = await client.ReadStringAsync(strStartAddr, strNumRegs, false, Encoding.ASCII);
                Console.WriteLine($"读取的字符串 (正常顺序): \"{readStringNormal}\"");

                // 写入字符串 (颠倒字节对顺序)
                // 注意：颠倒字节对意味着寄存器中的字节顺序是 BA DC FE... 而不是 AB CD EF...
                Console.WriteLine($"正在写入字符串 \"{stringToWrite}\" 到从地址 {strStartAddr} 开始的 {strNumRegs} 个寄存器 (颠倒字节对顺序)...");
                await client.WriteStringAsync(strStartAddr, stringToWrite, strNumRegs, true, Encoding.ASCII);
                string readStringReversed = await client.ReadStringAsync(strStartAddr, strNumRegs, true, Encoding.ASCII);
                Console.WriteLine($"读取的字符串 (颠倒字节对): \"{readStringReversed}\"");

                // 清空字符串区域 (写入空字符串)
                await client.WriteStringAsync(strStartAddr, "", strNumRegs, false, Encoding.ASCII);
                Console.WriteLine("------------------------------------");

                // --- 示例：保持寄存器位操作 (bool) ---
                ushort regForBitOp = 300; // 用于位操作的寄存器地址 (40301)
                byte bitOffset = 5;      // 操作寄存器中的第5位 (0-15)

                // 先确保寄存器有个已知值，例如0
                await client.WriteUShortAsync(regForBitOp, 0);
                Console.WriteLine($"初始化寄存器 {regForBitOp} 为0，准备进行位操作。");

                Console.WriteLine($"读取寄存器 {regForBitOp} 的第 {bitOffset} 位...");
                bool initialBitState = await client.ReadBoolFromRegisterBitAsync(regForBitOp, bitOffset);
                Console.WriteLine($"寄存器 {regForBitOp} 中第 {bitOffset} 位的初始状态: {initialBitState}");

                Console.WriteLine($"正在将寄存器 {regForBitOp} 的第 {bitOffset} 位写入为true...");
                await client.WriteBoolToRegisterBitAsync(regForBitOp, bitOffset, true);
                bool bitStateAfterWriteTrue = await client.ReadBoolFromRegisterBitAsync(regForBitOp, bitOffset);
                ushort regValueAfterTrue = await client.ReadUShortAsync(regForBitOp);
                Console.WriteLine($"写入true后第 {bitOffset} 位的状态: {bitStateAfterWriteTrue} (寄存器值: {regValueAfterTrue} / 0x{regValueAfterTrue:X4})");

                Console.WriteLine($"正在将寄存器 {regForBitOp} 的第 {bitOffset} 位写入为false...");
                await client.WriteBoolToRegisterBitAsync(regForBitOp, bitOffset, false);
                bool bitStateAfterWriteFalse = await client.ReadBoolFromRegisterBitAsync(regForBitOp, bitOffset);
                ushort regValueAfterFalse = await client.ReadUShortAsync(regForBitOp);
                Console.WriteLine($"写入false后第 {bitOffset} 位的状态: {bitStateAfterWriteFalse} (寄存器值: {regValueAfterFalse} / 0x{regValueAfterFalse:X4})");
                Console.WriteLine("------------------------------------");

                // 演示读取多个保持寄存器并解析 (例如，读取之前写入的short和ushort)
                ushort multiRegReadStart = 100; // 从地址100开始
                ushort numRegsToRead = 2;       // 读取2个寄存器
                Console.WriteLine($"从地址 {multiRegReadStart} 开始读取 {numRegsToRead} 个寄存器的原始数据...");
                byte[] rawRegisters = await client.ReadHoldingRegistersRawAsync(multiRegReadStart, numRegsToRead);
                if (rawRegisters.Length == numRegsToRead * 2)
                {
                    short val1 = ModbusUtility.ToInt16BigEndian(rawRegisters, 0); // 第一个寄存器 (地址100)
                    ushort val2 = ModbusUtility.ToUInt16BigEndian(rawRegisters, 2); // 第二个寄存器 (地址101)
                    Console.WriteLine($"原始读取 - 寄存器 {multiRegReadStart} (short): {val1}");
                    Console.WriteLine($"原始读取 - 寄存器 {multiRegReadStart + 1} (ushort): {val2}");
                }
                Console.WriteLine("------------------------------------");
            }
            catch (ModbusException modbusEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Modbus错误: {modbusEx.Message}");
                if (modbusEx.ExceptionCode.HasValue)
                {
                    Console.WriteLine($"Modbus异常代码: 0x{modbusEx.ExceptionCode.Value:X2}");
                    // 在这里可以根据具体的 Modbus Exception Code 给出更详细的错误信息
                    // 例如: 0x01 = ILLEGAL FUNCTION, 0x02 = ILLEGAL DATA ADDRESS, etc.
                }
                Console.ResetColor();
            }
            catch (SocketException sockEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Socket错误: {sockEx.Message} (错误代码: {sockEx.SocketErrorCode})");
                Console.ResetColor();
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
                Console.WriteLine("正在断开与PLC的连接...");
                client.Disconnect();
                Console.WriteLine("已断开连接。");
            }
        }

        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}
