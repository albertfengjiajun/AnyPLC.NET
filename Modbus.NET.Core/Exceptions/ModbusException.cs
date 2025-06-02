namespace Modbus.NET.Core.Exceptions;

/// <summary>
/// 自定义 Modbus 异常类。
/// </summary>
public class ModbusException : Exception
{
    public byte? ExceptionCode { get; }

    public ModbusException(string message) : base(message) { }
    public ModbusException(string message, Exception innerException) : base(message, innerException) { }
    public ModbusException(string message, byte exceptionCode) : base($"{message} (Modbus Exception Code: 0x{exceptionCode:X2})")
    {
        ExceptionCode = exceptionCode;
    }
}