using System;
using System.Threading.Tasks;

namespace AnyPLC.Core.S7;

/// <summary>
/// S7netplus Plc 类的抽象接口，用于支持依赖注入和单元测试。
/// </summary>
public interface IS7PlcWrapper
{
    bool IsConnected { get; }
    string IP { get; }
    Task OpenAsync();
    void Close();
    Task<object?> ReadAsync(string address);
    void Write(string address, object value);
}
