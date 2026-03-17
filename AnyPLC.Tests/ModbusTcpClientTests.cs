using System;
using System.Threading.Tasks;
using Xunit;
using AnyPLC.Core.ModbusTcp;

namespace AnyPLC.Tests;

public class ModbusTcpClientTests
{
    [Fact]
    public async Task ReadMultipleCoilsAsync_NumberOfCoilsIsZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var client = new ModbusTcpClient("127.0.0.1");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.ReadMultipleCoilsAsync(0, 0));
        Assert.Equal("numberOfCoils", exception.ParamName);
        Assert.Contains("Number of coils must be between 1 and 2000.", exception.Message);
    }

    [Fact]
    public async Task ReadMultipleCoilsAsync_NumberOfCoilsIsGreaterThan2000_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var client = new ModbusTcpClient("127.0.0.1");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.ReadMultipleCoilsAsync(0, 2001));
        Assert.Equal("numberOfCoils", exception.ParamName);
        Assert.Contains("Number of coils must be between 1 and 2000.", exception.Message);
    }
}
