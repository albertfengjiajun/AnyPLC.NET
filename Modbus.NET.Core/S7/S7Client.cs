using Modbus.NET.Core.Interfaces;
using S7.Net;

namespace Modbus.NET.Core.S7;

/// <summary>
/// 西门子 S7 客户端实现，支持通过 S7netplus 与西门子 PLC (如 S7-1200, S7-1500, S7-300 等) 通信。
/// </summary>
public class S7Client : IProtocolClient
{
    private readonly Plc _plc;

    public bool IsConnected => _plc?.IsConnected == true;

    /// <summary>
    /// 初始化 S7Client。
    /// </summary>
    /// <param name="cpu">CPU 类型 (如 CpuType.S71200)</param>
    /// <param name="ip">PLC 的 IP 地址</param>
    /// <param name="rack">机架号 (Rack)，通常为 0</param>
    /// <param name="slot">槽号 (Slot)，S7-1200/1500 通常为 1 或 0，S7-300 通常为 2</param>
    public S7Client(CpuType cpu, string ip, short rack, short slot)
    {
        _plc = new Plc(cpu, ip, rack, slot);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        await _plc.OpenAsync();
        if (!IsConnected)
        {
            throw new Exception($"Failed to connect to S7 PLC at {_plc.IP}");
        }
    }

    public void Disconnect()
    {
        if (_plc != null && _plc.IsConnected)
        {
            _plc.Close();
        }
    }

    /// <summary>
    /// 根据地址异步读取数据。
    /// S7netplus 支持字符串解析，例如 "DB1.DBD4" (读取DB1块中偏移量4的DWord), "M10.0" (读取位)
    /// </summary>
    public async Task<T> ReadAsync<T>(string address)
    {
        if (_plc == null || !IsConnected)
            throw new InvalidOperationException("S7 Client is not connected.");

        // S7netplus 的 Read 方法可以接受字符串地址并返回对象，需要转换为目标类型
        var result = await _plc.ReadAsync(address);

        if (result == null)
        {
            throw new Exception($"Failed to read from S7 address: {address}");
        }

        try
        {
            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch (InvalidCastException)
        {
            throw new InvalidCastException($"Cannot convert the read value '{result}' (type: {result.GetType()}) to the requested type '{typeof(T)}'.");
        }
    }

    /// <summary>
    /// 异步向指定的地址写入数据。
    /// </summary>
    public async Task WriteAsync<T>(string address, T value)
    {
        if (_plc == null || !IsConnected)
            throw new InvalidOperationException("S7 Client is not connected.");

        if (value == null)
            throw new ArgumentNullException(nameof(value), "S7 Client Write requires a non-null value.");

        // S7netplus 的 Write 方法可以接受字符串地址和要写入的值对象
        await Task.Run(() => _plc.Write(address, value));
    }

    public void Dispose()
    {
        Disconnect();
    }
}
