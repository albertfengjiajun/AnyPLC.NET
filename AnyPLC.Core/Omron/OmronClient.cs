using System.Collections.Concurrent;
using libplctag;
using libplctag.DataTypes;
using AnyPLC.Core.Interfaces;

namespace AnyPLC.Core.Omron;

/// <summary>
/// 欧姆龙 PLC 客户端实现，使用开源免费的 libplctag 库。
/// </summary>
public class OmronClient : IProtocolClient
{
    private readonly string _ipAddress;
    private readonly string _path;
    private bool _isConnected;

    // 缓存已创建的 Tag 以提升性能
    private readonly ConcurrentDictionary<string, ITag> _tags = new();

    public bool IsConnected => _isConnected;

    /// <summary>
    /// 初始化 OmronClient
    /// </summary>
    /// <param name="ipAddress">欧姆龙 PLC 的 IP 地址</param>
    /// <param name="plcType">欧姆龙 PLC 系列标识 (如 "omron-njnx", "omron-cj2")</param>
    public OmronClient(string ipAddress, string plcType = "omron-njnx")
    {
        _ipAddress = ipAddress;

        // 构建 libplctag 需要的路由路径
        // 例如：protocol=omron-njnx&gateway=192.168.1.10&path=1,0
        // 注：具体 path 取决于欧姆龙的网络配置，这里以常见直连为例
        _path = $"protocol={plcType}&gateway={ipAddress}&path=1,0";
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        // libplctag 采用按需创建 Tag 的方式，因此 Connect 阶段可以只做标记
        // 实际的连接会在读/写第一次操作 Tag 时发生。
        _isConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        Disconnect();
        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        _isConnected = false;
        foreach (var tag in _tags.Values)
        {
            tag.Dispose();
        }
        _tags.Clear();
    }

    /// <summary>
    /// 辅助方法：获取或创建指定的 Tag
    /// address 可以是 CIP/FINS 支持的格式，通常是变量名（对于 NJ/NX 系列）或者 FINS 内存地址
    /// </summary>
    private Tag<TPlcMapper, TNetType> GetOrCreateTag<TPlcMapper, TNetType>(string address)
        where TPlcMapper : IPlcMapper<TNetType>, new()
    {
        var tag = _tags.GetOrAdd(address, key =>
        {
            var newTag = new Tag<TPlcMapper, TNetType>()
            {
                Gateway = _ipAddress,
                Path = "1,0",         // Default routing
                PlcType = PlcType.Omron, // libplctag enum for Omron
                Protocol = Protocol.ab_eip, // Omron NJ/NX often use EtherNet/IP (CIP)
                Name = key,
                Timeout = TimeSpan.FromSeconds(5)
            };

            // 初始化，建立与 PLC 的实际关联
            newTag.Initialize();
            return newTag;
        });

        return (Tag<TPlcMapper, TNetType>)tag;
    }

    public async Task<T> ReadAsync<T>(string address)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Omron Client is not connected.");

        Type t = typeof(T);

        // 使用 Task.Run 将 libplctag 的同步操作包装为异步
        return await Task.Run(() =>
        {
            if (t == typeof(bool))
            {
                var tag = GetOrCreateTag<BoolPlcMapper, bool>(address);
                tag.Read();
                return (T)(object)tag.Value;
            }
            if (t == typeof(short))
            {
                var tag = GetOrCreateTag<IntPlcMapper, short>(address);
                tag.Read();
                return (T)(object)tag.Value;
            }
            if (t == typeof(ushort))
            {
                // libplctag C# wrapper might use Dint for ushort mapping or we manually cast from Int
                var tag = GetOrCreateTag<IntPlcMapper, short>(address);
                tag.Read();
                return (T)(object)(ushort)tag.Value;
            }
            if (t == typeof(int))
            {
                var tag = GetOrCreateTag<DintPlcMapper, int>(address);
                tag.Read();
                return (T)(object)tag.Value;
            }
            if (t == typeof(float))
            {
                var tag = GetOrCreateTag<RealPlcMapper, float>(address);
                tag.Read();
                return (T)(object)tag.Value;
            }

            throw new NotSupportedException($"Reading type {t.Name} from Omron PLC is not currently wrapped.");
        });
    }

    public async Task WriteAsync<T>(string address, T value)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Omron Client is not connected.");

        if (value == null)
            throw new ArgumentNullException(nameof(value), "Omron Client Write requires a non-null value.");

        Type t = typeof(T);

        await Task.Run(() =>
        {
            if (t == typeof(bool))
            {
                var tag = GetOrCreateTag<BoolPlcMapper, bool>(address);
                tag.Value = (bool)(object)value;
                tag.Write();
            }
            else if (t == typeof(short))
            {
                var tag = GetOrCreateTag<IntPlcMapper, short>(address);
                tag.Value = (short)(object)value;
                tag.Write();
            }
            else if (t == typeof(ushort))
            {
                var tag = GetOrCreateTag<IntPlcMapper, short>(address);
                tag.Value = (short)(ushort)(object)value;
                tag.Write();
            }
            else if (t == typeof(int))
            {
                var tag = GetOrCreateTag<DintPlcMapper, int>(address);
                tag.Value = (int)(object)value;
                tag.Write();
            }
            else if (t == typeof(float))
            {
                var tag = GetOrCreateTag<RealPlcMapper, float>(address);
                tag.Value = (float)(object)value;
                tag.Write();
            }
            else
            {
                throw new NotSupportedException($"Writing type {t.Name} to Omron PLC is not currently wrapped.");
            }
        });
    }

    public void Dispose()
    {
        Disconnect();
    }
}
