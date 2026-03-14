using System;
using System.Threading;
using System.Threading.Tasks;
using AnyPLC.Core.S7;
using Moq;
using Xunit;

namespace AnyPLC.Tests;

/// <summary>
/// 测试西门子 S7 客户端 (S7Client) 的核心逻辑。
/// </summary>
public class S7ClientTests
{
    private readonly Mock<IS7Plc> _mockPlc;
    private readonly S7Client _client;

    public S7ClientTests()
    {
        _mockPlc = new Mock<IS7Plc>();
        _client = new S7Client(_mockPlc.Object);
    }

    [Fact]
    public void Constructor_NullPlc_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new S7Client(null!));
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_DoesNothing()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(true);

        // Act
        await _client.ConnectAsync();

        // Assert
        _mockPlc.Verify(p => p.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConnectAsync_WhenNotConnected_CallsOpenAsync()
    {
        // Arrange
        // 第一次调用 IsConnected (ConnectAsync 开头) 返回 false，
        // 第二次调用 IsConnected (OpenAsync 之后验证) 返回 true
        _mockPlc.SetupSequence(p => p.IsConnected)
            .Returns(false)
            .Returns(true);

        // Act
        await _client.ConnectAsync();

        // Assert
        _mockPlc.Verify(p => p.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_WhenOpenFails_ThrowsException()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(false);
        _mockPlc.Setup(p => p.IP).Returns("127.0.0.1");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _client.ConnectAsync());
        Assert.Contains("Failed to connect to S7 PLC at 127.0.0.1", ex.Message);
    }

    [Fact]
    public void Disconnect_WhenConnected_CallsClose()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(true);

        // Act
        _client.Disconnect();

        // Assert
        _mockPlc.Verify(p => p.Close(), Times.Once);
    }

    [Fact]
    public void Disconnect_WhenNotConnected_DoesNotCallClose()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(false);

        // Act
        _client.Disconnect();

        // Assert
        _mockPlc.Verify(p => p.Close(), Times.Never);
    }

    [Fact]
    public async Task ReadAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _client.ReadAsync<short>("DB1.DBW0"));
    }

    [Fact]
    public async Task ReadAsync_WhenResultIsNull_ThrowsException()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(true);
        _mockPlc.Setup(p => p.ReadAsync("DB1.DBW0", It.IsAny<CancellationToken>()))
            .ReturnsAsync((object?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _client.ReadAsync<short>("DB1.DBW0"));
        Assert.Contains("Failed to read from S7 address: DB1.DBW0", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_WithValidValue_ReturnsConvertedValue()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(true);
        // 模拟读取返回了 int, 但期望转换为 short
        _mockPlc.Setup(p => p.ReadAsync("DB1.DBW0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1234);

        // Act
        var result = await _client.ReadAsync<short>("DB1.DBW0");

        // Assert
        Assert.Equal(1234, result);
    }

    [Fact]
    public async Task ReadAsync_WithIncompatibleType_ThrowsInvalidCastException()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(true);
        // 返回一个无法转换为 short 的类型
        _mockPlc.Setup(p => p.ReadAsync("DB1.DBW0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCastException>(() => _client.ReadAsync<short>("DB1.DBW0"));
    }

    [Fact]
    public async Task WriteAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _client.WriteAsync("DB1.DBW0", (short)1234));
    }

    [Fact]
    public async Task WriteAsync_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.WriteAsync<string>("DB1.DBD0", null!));
    }

    [Fact]
    public async Task WriteAsync_WithValidValue_CallsWrite()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(true);
        short testValue = 1234;

        // Act
        await _client.WriteAsync("DB1.DBW0", testValue);

        // Assert
        _mockPlc.Verify(p => p.Write("DB1.DBW0", testValue), Times.Once);
    }

    [Fact]
    public void Dispose_CallsDisconnectAndDispose()
    {
        // Arrange
        _mockPlc.Setup(p => p.IsConnected).Returns(true);

        // Act
        _client.Dispose();

        // Assert
        _mockPlc.Verify(p => p.Close(), Times.Once);
        _mockPlc.Verify(p => p.Dispose(), Times.Once);
    }
}
