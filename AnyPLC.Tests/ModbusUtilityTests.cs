using Xunit;
using AnyPLC.Core.Utils;

namespace AnyPLC.Tests;

public class ModbusUtilityTests
{
    [Theory]
    [InlineData(0x1234, new byte[] { 0x12, 0x34 })]
    [InlineData(0xFFFF, new byte[] { 0xFF, 0xFF })]
    [InlineData(0x0000, new byte[] { 0x00, 0x00 })]
    public void GetBytesBigEndian_UShort_ShouldReturnCorrectBytes(ushort input, byte[] expected)
    {
        // Act
        var result = ModbusUtility.GetBytesBigEndian(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x1234, new byte[] { 0x12, 0x34 })]
    [InlineData(-1, new byte[] { 0xFF, 0xFF })] // -1 corresponds to 0xFFFF in 16-bit
    [InlineData(0, new byte[] { 0x00, 0x00 })]
    public void GetBytesBigEndian_Short_ShouldReturnCorrectBytes(short input, byte[] expected)
    {
        // Act
        var result = ModbusUtility.GetBytesBigEndian(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToUInt16BigEndian_ShouldReturnCorrectValue()
    {
        // Arrange
        byte[] buffer = new byte[] { 0x01, 0x12, 0x34, 0x02 };
        int startIndex = 1;

        // Act
        ushort result = ModbusUtility.ToUInt16BigEndian(buffer, startIndex);

        // Assert
        Assert.Equal(0x1234, result);
    }

    [Fact]
    public void ToInt16BigEndian_ShouldReturnCorrectValue()
    {
        // Arrange
        byte[] buffer = new byte[] { 0x01, 0xFF, 0xFF, 0x02 }; // 0xFFFF is -1
        int startIndex = 1;

        // Act
        short result = ModbusUtility.ToInt16BigEndian(buffer, startIndex);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void ToUInt16BigEndian_WithNullBuffer_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ModbusUtility.ToUInt16BigEndian(null, 0));
    }

    [Theory]
    [InlineData(new byte[] { 0x12, 0x34 }, -1)]
    [InlineData(new byte[] { 0x12, 0x34 }, 1)] // Not enough bytes left
    [InlineData(new byte[] { 0x12, 0x34 }, 2)]
    public void ToUInt16BigEndian_WithInvalidIndex_ShouldThrowArgumentOutOfRangeException(byte[] buffer, int startIndex)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ModbusUtility.ToUInt16BigEndian(buffer, startIndex));
    }
}
