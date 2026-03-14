using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using AnyPLC.Core.Interfaces;

namespace AnyPLC.Core.OpcUa;

/// <summary>
/// OPC UA 客户端实现，使用官方且完全开源免费的 OPCFoundation.NetStandard.Opc.Ua 库。
/// </summary>
public class OpcUaClient : IProtocolClient
{
    private readonly string _serverUrl;
    private Session? _session;

    public bool IsConnected => _session?.Connected == true;

    /// <summary>
    /// 初始化 OpcUaClient。
    /// </summary>
    /// <param name="serverUrl">OPC UA 服务器的 URL，例如 "opc.tcp://localhost:4840"</param>
    public OpcUaClient(string serverUrl)
    {
        _serverUrl = serverUrl;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        var hostName = System.Net.Dns.GetHostName();
        var config = new ApplicationConfiguration()
        {
            ApplicationName = "AnyPLC.Client",
            ApplicationUri = Opc.Ua.Utils.Format(@"urn:{0}:AnyPLC.Client", hostName),
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault", SubjectName = Opc.Ua.Utils.Format(@"CN={0}, DC={1}", "AnyPLC.Client", hostName) },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
                RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
                AutoAcceptUntrustedCertificates = true,
                RejectSHA1SignedCertificates = false,
                MinimumCertificateKeySize = 1024
            },
            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
            TraceConfiguration = new TraceConfiguration()
        };

        await config.ValidateAsync(ApplicationType.Client);

        if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
        {
            config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
        }

        var endpointDescription = await CoreClientUtils.SelectEndpointAsync(config, _serverUrl, false, 15000);
        var endpointConfiguration = EndpointConfiguration.Create(config);
        var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

        _session = await Opc.Ua.Client.Session.Create(config, endpoint, false, "AnyPLC.Session", 60000, null, null);
    }

    public void Disconnect()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }

    public async Task DisconnectAsync()
    {
        if (_session != null)
        {
            if (_session.Connected)
            {
                await _session.CloseAsync();
            }
            _session.Dispose();
            _session = null;
        }
    }

    public async Task<T> ReadAsync<T>(string address)
    {
        if (_session == null || !IsConnected)
            throw new InvalidOperationException("OPC UA Client is not connected.");

        // address 形如 "ns=2;s=Demo.Static.Scalar.Int32"
        NodeId nodeId = NodeId.Parse(address);

        var value = await _session.ReadValueAsync(nodeId);

        if (StatusCode.IsBad(value.StatusCode))
        {
            throw new Exception($"Failed to read node {address}. Status Code: {value.StatusCode}");
        }

        return (T)Convert.ChangeType(value.Value, typeof(T));
    }

    public async Task WriteAsync<T>(string address, T value)
    {
        if (_session == null || !IsConnected)
            throw new InvalidOperationException("OPC UA Client is not connected.");

        NodeId nodeId = NodeId.Parse(address);

        WriteValue valueToWrite = new WriteValue
        {
            NodeId = nodeId,
            AttributeId = Attributes.Value,
            Value = new DataValue(new Variant(value))
        };

        WriteValueCollection writeValues = new WriteValueCollection { valueToWrite };

        var response = await _session.WriteAsync(null, writeValues, CancellationToken.None);
        var results = response.Results;

        if (results != null && results.Count > 0 && StatusCode.IsBad(results[0]))
        {
            throw new Exception($"Failed to write to node {address}. Status Code: {results[0]}");
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
