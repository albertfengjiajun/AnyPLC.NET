namespace AnyPLC.Core.Interfaces;

/// <summary>
/// 工业协议客户端的统一接口，提供网关层面对不同协议设备的统一抽象。
/// </summary>
public interface IProtocolClient : IDisposable
{
    /// <summary>
    /// 获取客户端连接状态
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 异步连接到设备
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开与设备的连接
    /// </summary>
    void Disconnect();

    /// <summary>
    /// 异步断开与设备的连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 根据地址/节点ID异步读取指定类型的数据
    /// </summary>
    /// <typeparam name="T">期望返回的数据类型 (如 bool, short, ushort, string等)</typeparam>
    /// <param name="address">变量地址或节点ID</param>
    /// <returns>读取到的数据</returns>
    Task<T> ReadAsync<T>(string address);

    /// <summary>
    /// 异步向指定的地址/节点ID写入数据
    /// </summary>
    /// <typeparam name="T">要写入的数据类型</typeparam>
    /// <param name="address">变量地址或节点ID</param>
    /// <param name="value">要写入的值</param>
    Task WriteAsync<T>(string address, T value);
}
