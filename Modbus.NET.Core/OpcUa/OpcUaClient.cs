using Modbus.NET.Core.Interfaces;
using Opc.UaFx.Client;

namespace Modbus.NET.Core.OpcUa;

/// <summary>
/// OPC UA 客户端实现，用于与 OPC UA 服务器通信。
/// </summary>
public class OpcUaClient : IProtocolClient
{
    private readonly string _serverUrl;
    private OpcClient? _client;

    public bool IsConnected => _client?.State == OpcClientState.Connected;

    /// <summary>
    /// 初始化 OpcUaClient。
    /// </summary>
    /// <param name="serverUrl">OPC UA 服务器的 URL，例如 "opc.tcp://localhost:4840"</param>
    public OpcUaClient(string serverUrl)
    {
        _serverUrl = serverUrl;
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return Task.CompletedTask;

        _client = new OpcClient(_serverUrl);
        // Opc.UaFx.Client 的 Connect 方法是同步的，为了接口统一包装为 Task
        return Task.Run(() =>
        {
            _client.Connect();
        }, cancellationToken);
    }

    public void Disconnect()
    {
        if (_client != null)
        {
            if (_client.State == OpcClientState.Connected)
            {
                _client.Disconnect();
            }
            _client.Dispose();
            _client = null;
        }
    }

    public Task<T> ReadAsync<T>(string address)
    {
        if (_client == null || !IsConnected)
            throw new InvalidOperationException("OPC UA Client is not connected.");

        // Opc.UaFx.Client 提供直接的泛型 ReadNode 方法
        return Task.Run(() =>
        {
            return _client.ReadNode(address).As<T>();
        });
    }

    public Task WriteAsync<T>(string address, T value)
    {
        if (_client == null || !IsConnected)
            throw new InvalidOperationException("OPC UA Client is not connected.");

        return Task.Run(() =>
        {
            var opcStatus = _client.WriteNode(address, value);
            if (!opcStatus.IsGood)
            {
                throw new Exception($"Failed to write to node {address}. Status: {opcStatus.Description}");
            }
        });
    }

    public void Dispose()
    {
        Disconnect();
    }
}
