using System.Collections.Concurrent;
using Modbus.NET.Core.Interfaces;

namespace Modbus.NET.Core;

/// <summary>
/// 工业协议网关管理器，统一管理各种协议(Modbus, OPC UA等)的客户端。
/// 允许通过设备ID统一进行设备连接、数据读取和写入。
/// </summary>
public class ProtocolGateway : IDisposable
{
    private readonly ConcurrentDictionary<string, IProtocolClient> _clients = new();

    /// <summary>
    /// 注册一个设备客户端。
    /// </summary>
    /// <param name="deviceId">设备的唯一标识符</param>
    /// <param name="client">实现 IProtocolClient 的客户端实例</param>
    public void RegisterClient(string deviceId, IProtocolClient client)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty.", nameof(deviceId));

        if (client == null)
            throw new ArgumentNullException(nameof(client));

        _clients.AddOrUpdate(deviceId, client, (key, oldClient) =>
        {
            oldClient.Dispose();
            return client;
        });
    }

    /// <summary>
    /// 移除并释放指定设备的客户端。
    /// </summary>
    /// <param name="deviceId">设备的唯一标识符</param>
    public void RemoveClient(string deviceId)
    {
        if (_clients.TryRemove(deviceId, out var client))
        {
            client.Dispose();
        }
    }

    /// <summary>
    /// 异步连接所有已注册的设备。
    /// </summary>
    public async Task ConnectAllAsync(CancellationToken cancellationToken = default)
    {
        var connectTasks = _clients.Values.Select(client => client.ConnectAsync(cancellationToken));
        await Task.WhenAll(connectTasks);
    }

    /// <summary>
    /// 断开所有已注册设备的连接。
    /// </summary>
    public void DisconnectAll()
    {
        foreach (var client in _clients.Values)
        {
            client.Disconnect();
        }
    }

    /// <summary>
    /// 获取指定设备的客户端实例。
    /// </summary>
    public IProtocolClient GetClient(string deviceId)
    {
        if (_clients.TryGetValue(deviceId, out var client))
        {
            return client;
        }

        throw new KeyNotFoundException($"Device with ID '{deviceId}' not found.");
    }

    /// <summary>
    /// 异步读取指定设备的数据。
    /// </summary>
    public async Task<T> ReadAsync<T>(string deviceId, string address)
    {
        var client = GetClient(deviceId);
        return await client.ReadAsync<T>(address);
    }

    /// <summary>
    /// 异步写入数据到指定设备。
    /// </summary>
    public async Task WriteAsync<T>(string deviceId, string address, T value)
    {
        var client = GetClient(deviceId);
        await client.WriteAsync(address, value);
    }

    public void Dispose()
    {
        DisconnectAll();
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _clients.Clear();
    }
}
