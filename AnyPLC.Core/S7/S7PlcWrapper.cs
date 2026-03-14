using System;
using System.Threading;
using System.Threading.Tasks;
using S7.Net;

namespace AnyPLC.Core.S7;

/// <summary>
/// 包装 S7.Net 的 Plc 类，使其实现 IS7Plc 接口，便于进行单元测试时的依赖注入。
/// </summary>
public class S7PlcWrapper : IS7Plc
{
    private readonly Plc _plc;

    /// <summary>
    /// 初始化 S7PlcWrapper。
    /// </summary>
    public S7PlcWrapper(CpuType cpu, string ip, short rack, short slot)
    {
        _plc = new Plc(cpu, ip, rack, slot);
    }

    /// <inheritdoc />
    public bool IsConnected => _plc.IsConnected;

    /// <inheritdoc />
    public string IP => _plc.IP;

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return _plc.OpenAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Close()
    {
        _plc.Close();
    }

    /// <inheritdoc />
    public Task<object?> ReadAsync(string variable, CancellationToken cancellationToken = default)
    {
        return _plc.ReadAsync(variable, cancellationToken);
    }

    /// <inheritdoc />
    public void Write(string variable, object value)
    {
        _plc.Write(variable, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // S7.Net.Plc does not expose a public Dispose method in 0.20.0, we just call Close.
        _plc.Close();
    }
}
