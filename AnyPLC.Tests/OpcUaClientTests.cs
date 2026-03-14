using System;
using System.Threading.Tasks;
using AnyPLC.Core.OpcUa;
using Xunit;

namespace AnyPLC.Tests;

public class OpcUaClientTests
{
    private const string TestServerUrl = "opc.tcp://localhost:4840";

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var client = new OpcUaClient(TestServerUrl);

        // Assert
        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task ReadAsync_WhenNotConnected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var client = new OpcUaClient(TestServerUrl);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ReadAsync<int>("ns=2;s=Demo"));
        Assert.Equal("OPC UA Client is not connected.", exception.Message);
    }

    [Fact]
    public async Task WriteAsync_WhenNotConnected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var client = new OpcUaClient(TestServerUrl);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.WriteAsync("ns=2;s=Demo", 42));
        Assert.Equal("OPC UA Client is not connected.", exception.Message);
    }

    [Fact]
    public void Disconnect_WhenNotConnected_ShouldNotThrow()
    {
        // Arrange
        var client = new OpcUaClient(TestServerUrl);

        // Act
        var exception = Record.Exception(() => client.Disconnect());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_WhenNotConnected_ShouldNotThrow()
    {
        // Arrange
        var client = new OpcUaClient(TestServerUrl);

        // Act
        var exception = Record.Exception(() => client.Dispose());

        // Assert
        Assert.Null(exception);
    }
}
