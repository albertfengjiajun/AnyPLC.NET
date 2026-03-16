using Xunit;
using AnyPLC.Core.OpcUa;

namespace AnyPLC.Tests;

public class OpcUaClientTests
{
    [Fact]
    public void CreateApplicationConfiguration_ShouldHaveSecureDefaults()
    {
        // Arrange
        var client = new OpcUaClient("opc.tcp://localhost:4840");

        // Act
        var config = client.CreateApplicationConfiguration();

        // Assert
        Assert.NotNull(config.SecurityConfiguration);

        // 核心安全配置验证
        Assert.False(config.SecurityConfiguration.AutoAcceptUntrustedCertificates, "AutoAcceptUntrustedCertificates should be false for security.");
        Assert.True(config.SecurityConfiguration.RejectSHA1SignedCertificates, "RejectSHA1SignedCertificates should be true for security.");
        Assert.Equal(2048, config.SecurityConfiguration.MinimumCertificateKeySize);
    }
}
