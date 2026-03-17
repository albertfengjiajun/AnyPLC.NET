using System;
using System.Threading;
using System.Threading.Tasks;
using AnyPLC.Core.S7;
using Moq;
using S7.Net;
using Xunit;

namespace AnyPLC.Tests;

public class S7ClientTests
{
    [Fact]
    public void Constructor_WithNullWrapper_ShouldThrowArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new S7Client((IS7PlcWrapper)null));
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_ShouldNotCallOpenAsync()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(true);
        var s7Client = new S7Client(mockWrapper.Object);

        // Act
        await s7Client.ConnectAsync();

        // Assert
        mockWrapper.Verify(w => w.OpenAsync(), Times.Never);
    }

    [Fact]
    public async Task ConnectAsync_WhenConnectionFails_ShouldThrowException()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        // First it checks if connected, returns false. Then after OpenAsync, it checks again.
        mockWrapper.SetupSequence(w => w.IsConnected)
            .Returns(false)
            .Returns(false);
        mockWrapper.Setup(w => w.IP).Returns("192.168.0.1");
        var s7Client = new S7Client(mockWrapper.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => s7Client.ConnectAsync());
        Assert.Contains("Failed to connect to S7 PLC", ex.Message);
        mockWrapper.Verify(w => w.OpenAsync(), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_WhenSuccessful_ShouldNotThrowException()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.SetupSequence(w => w.IsConnected)
            .Returns(false)
            .Returns(true);
        var s7Client = new S7Client(mockWrapper.Object);

        // Act
        var ex = await Record.ExceptionAsync(() => s7Client.ConnectAsync());

        // Assert
        Assert.Null(ex);
        mockWrapper.Verify(w => w.OpenAsync(), Times.Once);
    }

    [Fact]
    public void Disconnect_WhenConnected_ShouldCallClose()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(true);
        var s7Client = new S7Client(mockWrapper.Object);

        // Act
        s7Client.Disconnect();

        // Assert
        mockWrapper.Verify(w => w.Close(), Times.Once);
    }

    [Fact]
    public void Disconnect_WhenNotConnected_ShouldNotCallClose()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(false);
        var s7Client = new S7Client(mockWrapper.Object);

        // Act
        s7Client.Disconnect();

        // Assert
        mockWrapper.Verify(w => w.Close(), Times.Never);
    }

    [Fact]
    public async Task ReadAsync_WhenNotConnected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(false);
        var s7Client = new S7Client(mockWrapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => s7Client.ReadAsync<int>("DB1.DBD4"));
    }

    [Fact]
    public async Task ReadAsync_WhenReturnsNull_ShouldThrowException()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(true);
        mockWrapper.Setup(w => w.ReadAsync(It.IsAny<string>())).ReturnsAsync((object)null);
        var s7Client = new S7Client(mockWrapper.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => s7Client.ReadAsync<int>("DB1.DBD4"));
        Assert.Contains("Failed to read from S7 address", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_WithValidData_ShouldReturnConvertedValue()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(true);
        mockWrapper.Setup(w => w.ReadAsync("DB1.DBD4")).ReturnsAsync((object)42); // underlying int
        var s7Client = new S7Client(mockWrapper.Object);

        // Act
        var result = await s7Client.ReadAsync<int>("DB1.DBD4");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ReadAsync_WithIncompatibleType_ShouldThrowInvalidCastException()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(true);
        // Let's pretend it returns a complex object that cannot be converted to int
        mockWrapper.Setup(w => w.ReadAsync("DB1.DBD4")).ReturnsAsync(new object());
        var s7Client = new S7Client(mockWrapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCastException>(() => s7Client.ReadAsync<int>("DB1.DBD4"));
    }

    [Fact]
    public async Task WriteAsync_WhenNotConnected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(false);
        var s7Client = new S7Client(mockWrapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => s7Client.WriteAsync("DB1.DBD4", 42));
    }

    [Fact]
    public async Task WriteAsync_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(true);
        var s7Client = new S7Client(mockWrapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => s7Client.WriteAsync<object>("DB1.DBD4", null));
    }

    [Fact]
    public async Task WriteAsync_WithValidValue_ShouldCallWrapperWrite()
    {
        // Arrange
        var mockWrapper = new Mock<IS7PlcWrapper>();
        mockWrapper.Setup(w => w.IsConnected).Returns(true);
        var s7Client = new S7Client(mockWrapper.Object);
        var address = "DB1.DBD4";
        var valueToWrite = 42;

        // Act
        await s7Client.WriteAsync(address, valueToWrite);

        // Assert
        mockWrapper.Verify(w => w.Write(address, valueToWrite), Times.Once);
    }
}
