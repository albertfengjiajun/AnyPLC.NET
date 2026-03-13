using Moq;
using Xunit;
using AnyPLC.Core;
using AnyPLC.Core.Interfaces;

namespace AnyPLC.Tests;

public class ProtocolGatewayTests
{
    [Fact]
    public void RegisterClient_ShouldAddClientSuccessfully()
    {
        // Arrange
        using var gateway = new ProtocolGateway();
        var mockClient = new Mock<IProtocolClient>();

        // Act
        gateway.RegisterClient("TestDevice1", mockClient.Object);

        // Assert
        var retrievedClient = gateway.GetClient("TestDevice1");
        Assert.NotNull(retrievedClient);
        Assert.Equal(mockClient.Object, retrievedClient);
    }

    [Fact]
    public void RegisterClient_WithDuplicateId_ShouldReplaceAndDisposeOldClient()
    {
        // Arrange
        using var gateway = new ProtocolGateway();
        var mockClient1 = new Mock<IProtocolClient>();
        var mockClient2 = new Mock<IProtocolClient>();

        // Act
        gateway.RegisterClient("TestDevice", mockClient1.Object);
        gateway.RegisterClient("TestDevice", mockClient2.Object);

        // Assert
        var retrievedClient = gateway.GetClient("TestDevice");
        Assert.Equal(mockClient2.Object, retrievedClient);
        mockClient1.Verify(c => c.Dispose(), Times.Once); // 确保旧客户端被释放
    }

    [Fact]
    public void GetClient_WithInvalidId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        using var gateway = new ProtocolGateway();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => gateway.GetClient("NonExistentDevice"));
    }

    [Fact]
    public void RemoveClient_ShouldRemoveAndDisposeClient()
    {
        // Arrange
        using var gateway = new ProtocolGateway();
        var mockClient = new Mock<IProtocolClient>();
        gateway.RegisterClient("TestDevice", mockClient.Object);

        // Act
        gateway.RemoveClient("TestDevice");

        // Assert
        Assert.Throws<KeyNotFoundException>(() => gateway.GetClient("TestDevice"));
        mockClient.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ConnectAllAsync_ShouldCallConnectOnAllClients()
    {
        // Arrange
        using var gateway = new ProtocolGateway();
        var mockClient1 = new Mock<IProtocolClient>();
        var mockClient2 = new Mock<IProtocolClient>();

        mockClient1.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockClient2.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        gateway.RegisterClient("Device1", mockClient1.Object);
        gateway.RegisterClient("Device2", mockClient2.Object);

        // Act
        await gateway.ConnectAllAsync();

        // Assert
        mockClient1.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockClient2.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DisconnectAll_ShouldCallDisconnectOnAllClients()
    {
        // Arrange
        using var gateway = new ProtocolGateway();
        var mockClient1 = new Mock<IProtocolClient>();
        var mockClient2 = new Mock<IProtocolClient>();

        gateway.RegisterClient("Device1", mockClient1.Object);
        gateway.RegisterClient("Device2", mockClient2.Object);

        // Act
        gateway.DisconnectAll();

        // Assert
        mockClient1.Verify(c => c.Disconnect(), Times.Once);
        mockClient2.Verify(c => c.Disconnect(), Times.Once);
    }

    [Fact]
    public async Task ReadAsync_ShouldRouteToCorrectClient()
    {
        // Arrange
        using var gateway = new ProtocolGateway();
        var mockClient = new Mock<IProtocolClient>();

        string deviceId = "SensorA";
        string address = "AddressX";
        int expectedValue = 42;

        mockClient.Setup(c => c.ReadAsync<int>(address)).ReturnsAsync(expectedValue);
        gateway.RegisterClient(deviceId, mockClient.Object);

        // Act
        var result = await gateway.ReadAsync<int>(deviceId, address);

        // Assert
        Assert.Equal(expectedValue, result);
        mockClient.Verify(c => c.ReadAsync<int>(address), Times.Once);
    }

    [Fact]
    public async Task WriteAsync_ShouldRouteToCorrectClient()
    {
        // Arrange
        using var gateway = new ProtocolGateway();
        var mockClient = new Mock<IProtocolClient>();

        string deviceId = "ActuatorB";
        string address = "AddressY";
        bool valueToWrite = true;

        mockClient.Setup(c => c.WriteAsync(address, valueToWrite)).Returns(Task.CompletedTask);
        gateway.RegisterClient(deviceId, mockClient.Object);

        // Act
        await gateway.WriteAsync(deviceId, address, valueToWrite);

        // Assert
        mockClient.Verify(c => c.WriteAsync(address, valueToWrite), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldDisposeAllClients()
    {
        // Arrange
        var gateway = new ProtocolGateway();
        var mockClient1 = new Mock<IProtocolClient>();
        var mockClient2 = new Mock<IProtocolClient>();

        gateway.RegisterClient("Device1", mockClient1.Object);
        gateway.RegisterClient("Device2", mockClient2.Object);

        // Act
        gateway.Dispose();

        // Assert
        mockClient1.Verify(c => c.Disconnect(), Times.Once);
        mockClient1.Verify(c => c.Dispose(), Times.Once);
        mockClient2.Verify(c => c.Disconnect(), Times.Once);
        mockClient2.Verify(c => c.Dispose(), Times.Once);
    }
}
