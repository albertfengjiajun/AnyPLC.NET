namespace Modbus.NET.Core.Utils;

/// <summary>
/// 辅助类，用于 Modbus 协议相关的数据转换，特别是处理大端字节序。
/// Modbus 协议规定网络字节顺序为大端 (Big-Endian)。
/// </summary>
public class ModbusUtility
{
    /// <summary>
    /// 将 ushort (16位无符号整数) 转换为大端字节序的 byte 数组。
    /// </summary>
    public static byte[] GetBytesBigEndian(ushort value)
    {
        return new byte[] { (byte)(value >> 8), (byte)value };
    }

    /// <summary>
    /// 将 short (16位有符号整数) 转换为大端字节序的 byte 数组。
    /// </summary>
    public static byte[] GetBytesBigEndian(short value)
    {
        return new byte[] { (byte)(value >> 8), (byte)value };
    }

    /// <summary>
    /// 从大端字节序的 byte 数组中指定位置开始转换回 ushort。
    /// </summary>
    public static ushort ToUInt16BigEndian(byte[] buffer, int startIndex)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (startIndex < 0 || startIndex + 1 >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(startIndex));
        return (ushort)((buffer[startIndex] << 8) | buffer[startIndex + 1]);
    }

    /// <summary>
    /// 从大端字节序的 byte 数组中指定位置开始转换回 short。
    /// </summary>
    public static short ToInt16BigEndian(byte[] buffer, int startIndex)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (startIndex < 0 || startIndex + 1 >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(startIndex));
        return (short)((buffer[startIndex] << 8) | buffer[startIndex + 1]);
    }
}