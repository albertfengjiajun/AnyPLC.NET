using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnyPLC.Core.S7;

/// <summary>
/// 定义 S7 PLC 客户端的接口，用于解耦底层 S7.Net 实现并支持单元测试。
/// </summary>
public interface IS7Plc : IDisposable
{
    /// <summary>
    /// 获取是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 获取 PLC IP 地址
    /// </summary>
    string IP { get; }

    /// <summary>
    /// 异步打开连接
    /// </summary>
    Task OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 关闭连接
    /// </summary>
    void Close();

    /// <summary>
    /// 异步读取指定地址的值
    /// </summary>
    Task<object?> ReadAsync(string variable, CancellationToken cancellationToken = default);

    /// <summary>
    /// 向指定地址写入值
    /// </summary>
    void Write(string variable, object value);
}
