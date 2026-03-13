using Modbus.NET.Core.Interfaces;
using HslCommunication;
using HslCommunication.Profinet.Omron;

namespace Modbus.NET.Core.Omron;

/// <summary>
/// 欧姆龙 PLC 客户端实现，使用 FINS/TCP 协议。
/// 依赖 HslCommunication 库。
/// </summary>
public class OmronFinsClient : IProtocolClient
{
    private readonly OmronFinsNet _omronFinsNet;
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    /// <summary>
    /// 初始化 OmronFinsClient。
    /// </summary>
    /// <param name="ipAddress">PLC 的 IP 地址</param>
    /// <param name="port">端口号，欧姆龙 FINS/TCP 默认通常为 9600</param>
    /// <param name="sa1">PC 节点号</param>
    /// <param name="da1">PLC 节点号 (如果为0，有些库会自动处理)</param>
    public OmronFinsClient(string ipAddress, int port = 9600, byte sa1 = 0, byte da1 = 0)
    {
        _omronFinsNet = new OmronFinsNet(ipAddress, port);
        if (sa1 != 0) _omronFinsNet.SA1 = sa1;
        if (da1 != 0) _omronFinsNet.DA1 = da1;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected) return;

        OperateResult connectResult = await _omronFinsNet.ConnectServerAsync();
        if (!connectResult.IsSuccess)
        {
            throw new Exception($"Failed to connect to Omron PLC at {_omronFinsNet.IpAddress}:{_omronFinsNet.Port}. Reason: {connectResult.Message}");
        }

        _isConnected = true;
    }

    public void Disconnect()
    {
        if (_isConnected)
        {
            _omronFinsNet.ConnectClose();
            _isConnected = false;
        }
    }

    /// <summary>
    /// 异步读取欧姆龙 PLC 数据。
    /// 地址格式示例: "D100" (数据寄存器), "C100" (计数器), "W100" (内部辅助继电器), 等等。
    /// </summary>
    public async Task<T> ReadAsync<T>(string address)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Omron Client is not connected.");

        Type t = typeof(T);

        if (t == typeof(bool))
        {
            var readResult = await _omronFinsNet.ReadBoolAsync(address);
            if (!readResult.IsSuccess) throw new Exception($"Failed to read Omron address: {address}. {readResult.Message}");
            return (T)(object)readResult.Content;
        }
        else if (t == typeof(short))
        {
            var readResult = await _omronFinsNet.ReadInt16Async(address);
            if (!readResult.IsSuccess) throw new Exception($"Failed to read Omron address: {address}. {readResult.Message}");
            return (T)(object)readResult.Content;
        }
        else if (t == typeof(ushort))
        {
            var readResult = await _omronFinsNet.ReadUInt16Async(address);
            if (!readResult.IsSuccess) throw new Exception($"Failed to read Omron address: {address}. {readResult.Message}");
            return (T)(object)readResult.Content;
        }
        else if (t == typeof(int))
        {
            var readResult = await _omronFinsNet.ReadInt32Async(address);
            if (!readResult.IsSuccess) throw new Exception($"Failed to read Omron address: {address}. {readResult.Message}");
            return (T)(object)readResult.Content;
        }
        else if (t == typeof(float))
        {
             var readResult = await _omronFinsNet.ReadFloatAsync(address);
             if (!readResult.IsSuccess) throw new Exception($"Failed to read Omron address: {address}. {readResult.Message}");
             return (T)(object)readResult.Content;
        }

        throw new NotSupportedException($"Reading type {t.Name} from Omron PLC is not currently wrapped in this basic gateway method.");
    }

    /// <summary>
    /// 异步写入欧姆龙 PLC 数据。
    /// </summary>
    public async Task WriteAsync<T>(string address, T value)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Omron Client is not connected.");

        if (value == null)
            throw new ArgumentNullException(nameof(value), "Omron Client Write requires a non-null value.");

        OperateResult result;

        if (value is bool boolVal)
        {
            result = await _omronFinsNet.WriteAsync(address, boolVal);
        }
        else if (value is short shortVal)
        {
            result = await _omronFinsNet.WriteAsync(address, shortVal);
        }
        else if (value is ushort ushortVal)
        {
            result = await _omronFinsNet.WriteAsync(address, ushortVal);
        }
        else if (value is int intVal)
        {
            result = await _omronFinsNet.WriteAsync(address, intVal);
        }
        else if (value is float floatVal)
        {
             result = await _omronFinsNet.WriteAsync(address, floatVal);
        }
        else
        {
             throw new NotSupportedException($"Writing type {typeof(T).Name} to Omron PLC is not currently wrapped.");
        }

        if (!result.IsSuccess)
        {
            throw new Exception($"Failed to write to Omron address {address}. {result.Message}");
        }
    }

    public void Dispose()
    {
        Disconnect();
        _omronFinsNet?.Dispose();
    }
}
