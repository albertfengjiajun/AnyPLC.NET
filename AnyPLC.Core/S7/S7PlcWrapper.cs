using System;
using System.Threading.Tasks;
using S7.Net;

namespace AnyPLC.Core.S7;

/// <summary>
/// 对 S7netplus Plc 类的具体封装实现，用于真实的 S7 通信。
/// </summary>
public class S7PlcWrapper : IS7PlcWrapper
{
    private readonly Plc _plc;

    public S7PlcWrapper(CpuType cpu, string ip, short rack, short slot)
    {
        _plc = new Plc(cpu, ip, rack, slot);
    }

    public bool IsConnected => _plc.IsConnected;
    public string IP => _plc.IP;

    public Task OpenAsync() => _plc.OpenAsync();

    public void Close() => _plc.Close();

    public Task<object?> ReadAsync(string address) => _plc.ReadAsync(address);

    public void Write(string address, object value) => _plc.Write(address, value);
}
