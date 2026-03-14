using System.Net.Sockets;
using System.Text;
using AnyPLC.Core.Exceptions;
using AnyPLC.Core.Interfaces;
using AnyPLC.Core.Utils;

namespace AnyPLC.Core.ModbusTcp;

/// <summary>
/// Modbus TCP 客户端实现，用于与 Modbus TCP 设备（如 PLC）通信。
/// </summary>
public class ModbusTcpClient : IProtocolClient
{
    private TcpClient _tcpClient;
    private NetworkStream _networkStream;
    private readonly string _ipAddress;
    private readonly int _port;
    private ushort _transactionId = 0; // 事务标识符，每次通信递增
    private readonly byte _unitId;       // 单元标识符 (从站地址)
    private readonly object _lock = new object(); // 用于同步访问共享资源 (事务ID和网络流)
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5); // 默认超时时间

    /// <summary>
    /// Modbus 功能码常量
    /// </summary>
    private static class FunctionCodes
    {
        public const byte ReadCoils = 0x01;
        public const byte ReadDiscreteInputs = 0x02; // 未在本示例中实现，但列出以供参考
        public const byte ReadHoldingRegisters = 0x03;
        public const byte ReadInputRegisters = 0x04; // 未在本示例中实现
        public const byte WriteSingleCoil = 0x05;
        public const byte WriteSingleRegister = 0x06;
        public const byte WriteMultipleCoils = 0x0F;
        public const byte WriteMultipleRegisters = 0x10;
    }

    public bool IsConnected => _tcpClient?.Connected == true;

    /// <summary>
    /// 初始化 ModbusTcpClient。
    /// </summary>
    /// <param name="ipAddress">PLC 或 Modbus TCP 服务器的 IP 地址。</param>
    /// <param name="port">端口号，默认为 502。</param>
    /// <param name="unitId">单元标识符 (Slave ID)，通常为 1。</param>
    public ModbusTcpClient(string ipAddress, int port = 502, byte unitId = 1)
    {
        _ipAddress = ipAddress;
        _port = port;
        _unitId = unitId;
    }

    /// <summary>
    /// 异步连接到 Modbus TCP 服务器。
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_tcpClient?.Connected == true) return;

        _tcpClient = new TcpClient();
        try
        {
            await _tcpClient.ConnectAsync(_ipAddress, _port, cancellationToken).ConfigureAwait(false);
            _networkStream = _tcpClient.GetStream();
            _networkStream.ReadTimeout = (int)_defaultTimeout.TotalMilliseconds;
            _networkStream.WriteTimeout = (int)_defaultTimeout.TotalMilliseconds;
        }
        catch (Exception)
        {
            _tcpClient?.Dispose();
            _tcpClient = null;
            _networkStream?.Dispose();
            _networkStream = null;
            throw;
        }
    }

    /// <summary>
    /// 断开与服务器的连接。
    /// </summary>
    public Task DisconnectAsync()
    {
        Disconnect();
        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        _networkStream?.Close(); // Close会处理Dispose
        _tcpClient?.Close();     // Close会处理Dispose
        _networkStream = null;
        _tcpClient = null;
    }

    /// <summary>
    /// 获取下一个事务标识符。
    /// </summary>
    private ushort GetNextTransactionId()
    {
        lock (_lock)
        {
            _transactionId++;
            if (_transactionId == 0) _transactionId = 1; // 避免 Transaction ID 为 0 (虽然协议未禁止)
            return _transactionId;
        }
    }

    /// <summary>
    /// 核心方法：执行 Modbus 功能，发送请求并接收响应。
    /// </summary>
    /// <param name="functionCode">Modbus 功能码。</param>
    /// <param name="pduData">协议数据单元 (PDU) 的数据部分。</param>
    /// <returns>从服务器返回的 PDU 数据部分。</returns>
    private async Task<byte[]> ExecuteModbusRequestAsync(byte functionCode, byte[] pduData)
    {
        if (_tcpClient == null || !_tcpClient.Connected || _networkStream == null)
        {
            throw new InvalidOperationException("Client not connected.");
        }

        ushort currentTransactionId;
        byte[] requestMbap;
        byte[] fullRequest;

        lock (_lock) // 确保TransactionId 和 Stream写入的原子性
        {
            currentTransactionId = GetNextTransactionId();

            // 1. 构建 MBAP (Modbus Application Protocol) Header (7 bytes)
            //    - Transaction Identifier (2 bytes): currentTransactionId
            //    - Protocol Identifier (2 bytes): 0x0000 (for Modbus)
            //    - Length (2 bytes): Number of following bytes (Unit ID + PDU)
            //    - Unit Identifier (1 byte): _unitId
            ushort pduLength = (ushort)(1 + pduData.Length); // UnitID (1) + FunctionCode (implicitly in pduData[0] or pduData itself if FC is separate)
                                                             // Corrected: pduLength is actually just FC + FC_Data. MBAP Length field is UnitID + FC + FC_Data
            ushort mbapLengthField = (ushort)(1 + 1 + pduData.Length); // UnitID + FunctionCode + PDU_Data

            requestMbap = new byte[7];
            Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(currentTransactionId), 0, requestMbap, 0, 2); // Transaction ID
            requestMbap[2] = 0x00; // Protocol ID Hi
            requestMbap[3] = 0x00; // Protocol ID Lo
            Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(mbapLengthField), 0, requestMbap, 4, 2); // Length
            requestMbap[6] = _unitId; // Unit ID

            // 2. 构建完整的请求报文 (MBAP + Function Code + PDU Data)
            fullRequest = new byte[7 + 1 + pduData.Length];
            Buffer.BlockCopy(requestMbap, 0, fullRequest, 0, 7);
            fullRequest[7] = functionCode; // Function Code
            Buffer.BlockCopy(pduData, 0, fullRequest, 8, pduData.Length); // PDU Data
        }

        // 3. 发送请求
        await _networkStream.WriteAsync(fullRequest, 0, fullRequest.Length).ConfigureAwait(false);
        await _networkStream.FlushAsync().ConfigureAwait(false);


        // 4. 接收响应 MBAP Header (前7字节)
        byte[] responseMbap = new byte[7];
        int bytesRead = await _networkStream.ReadAsync(responseMbap, 0, 7).ConfigureAwait(false);
        if (bytesRead < 7)
        {
            throw new ModbusException("Incomplete MBAP header received.");
        }

        // 5. 解析响应 MBAP Header
        ushort responseTransactionId = ModbusUtility.ToUInt16BigEndian(responseMbap, 0);
        ushort responseProtocolId = ModbusUtility.ToUInt16BigEndian(responseMbap, 2);
        ushort responseMbapLengthField = ModbusUtility.ToUInt16BigEndian(responseMbap, 4);
        byte responseUnitId = responseMbap[6];

        // 校验响应 MBAP Header
        if (responseTransactionId != currentTransactionId)
        {
            throw new ModbusException($"Transaction ID mismatch. Expected: {currentTransactionId}, Got: {responseTransactionId}");
        }
        if (responseProtocolId != 0x0000)
        {
            throw new ModbusException($"Protocol ID mismatch. Expected: 0x0000, Got: {responseProtocolId}");
        }
        if (responseUnitId != _unitId && _unitId != 0 && _unitId != 255) // UnitID 0 for broadcast (no response), 255 for some gateways. Be flexible.
        {
            // Some PLCs might respond with their actual UnitID even if request was for 0 or 255, or if unit ID is misconfigured.
            // For strict checking, this should be an exact match.
            // Console.WriteLine($"Warning: Unit ID mismatch. Expected: {_unitId}, Got: {responseUnitId}");
        }

        // 6. 接收响应 PDU (Function Code + Data or Exception)
        // responseMbapLengthField = UnitID (1 byte) + PDU (FC + Data). So PDU length is responseMbapLengthField - 1
        int pduResponseLength = responseMbapLengthField - 1;
        if (pduResponseLength <= 0) {
             throw new ModbusException("Invalid PDU length in response MBAP header.");
        }
        byte[] responsePdu = new byte[pduResponseLength];
        bytesRead = await _networkStream.ReadAsync(responsePdu, 0, pduResponseLength).ConfigureAwait(false);

        if (bytesRead < pduResponseLength)
        {
            throw new ModbusException("Incomplete PDU received.");
        }

        // 7. 检查 Modbus 异常
        // 如果功能码的最高位为1 (即 > 0x80)，则表示服务器返回了异常
        byte responseFunctionCode = responsePdu[0];
        if (responseFunctionCode > 0x80 && responseFunctionCode == (functionCode | 0x80))
        {
            byte exceptionCode = responsePdu[1]; // 异常码在PDU的第二个字节
            throw new ModbusException($"Modbus exception received. Function Code: {functionCode}, Exception Code: {exceptionCode}", exceptionCode);
        }
        if (responseFunctionCode != functionCode)
        {
             throw new ModbusException($"Function code mismatch. Expected: {functionCode}, Got: {responseFunctionCode}");
        }

        // 8. 返回 PDU 数据部分 (不含功能码)
        byte[] responseData = new byte[responsePdu.Length - 1];
        Buffer.BlockCopy(responsePdu, 1, responseData, 0, responseData.Length);
        return responseData;
    }


    #region 线圈操作 (Coils)

    /// <summary>
    /// 读取单个线圈 (FC01)。
    /// </summary>
    /// <param name="coilAddress">线圈的0-based地址。</param>
    /// <returns>线圈状态 (true for ON, false for OFF)。</returns>
    public async Task<bool> ReadSingleCoilAsync(ushort coilAddress)
    {
        bool[] coils = await ReadMultipleCoilsAsync(coilAddress, 1);
        return coils[0];
    }

    /// <summary>
    /// 读取多个线圈 (FC01)。
    /// </summary>
    /// <param name="startAddress">起始线圈的0-based地址。</param>
    /// <param name="numberOfCoils">要读取的线圈数量。</param>
    /// <returns>一个布尔数组，表示线圈的状态。</returns>
    public async Task<bool[]> ReadMultipleCoilsAsync(ushort startAddress, ushort numberOfCoils)
    {
        if (numberOfCoils == 0 || numberOfCoils > 2000) // Modbus规范限制
            throw new ArgumentOutOfRangeException(nameof(numberOfCoils), "Number of coils must be between 1 and 2000.");

        // PDU Data for FC01: Start Address (2 bytes), Quantity of Coils (2 bytes)
        byte[] pduData = new byte[4];
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(startAddress), 0, pduData, 0, 2);
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(numberOfCoils), 0, pduData, 2, 2);

        byte[] responseData = await ExecuteModbusRequestAsync(FunctionCodes.ReadCoils, pduData);

        // Response PDU Data for FC01: Byte Count (1 byte), Coil Status (N bytes)
        byte byteCount = responseData[0];
        bool[] coils = new bool[numberOfCoils];
        for (int i = 0; i < numberOfCoils; i++)
        {
            int byteIndex = i / 8;
            int bitIndex = i % 8;
            if (byteIndex < byteCount) // 确保不越界访问 responseData
            {
                coils[i] = (responseData[1 + byteIndex] & (1 << bitIndex)) != 0;
            }
            else
            {
                // 如果 numberOfCoils 不是8的倍数，服务器可能返回比实际请求的coil少的byte，但我们只关心请求的数量
                // 或者数据有问题，这里可以选择抛异常或默认false
                coils[i] = false; // Default to false if data is short, or handle error
            }
        }
        return coils;
    }

    /// <summary>
    /// 写入单个线圈 (FC05)。
    /// </summary>
    /// <param name="coilAddress">线圈的0-based地址。</param>
    /// <param name="value">要写入的值 (true for ON, false for OFF)。</param>
    public async Task WriteSingleCoilAsync(ushort coilAddress, bool value)
    {
        // PDU Data for FC05: Output Address (2 bytes), Output Value (2 bytes: 0xFF00 for ON, 0x0000 for OFF)
        byte[] pduData = new byte[4];
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(coilAddress), 0, pduData, 0, 2);
        ushort outputValue = value ? (ushort)0xFF00 : (ushort)0x0000;
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(outputValue), 0, pduData, 2, 2);

        byte[] responseData = await ExecuteModbusRequestAsync(FunctionCodes.WriteSingleCoil, pduData);

        // 校验响应: FC05响应会回显请求
        ushort respAddr = ModbusUtility.ToUInt16BigEndian(responseData, 0);
        ushort respVal = ModbusUtility.ToUInt16BigEndian(responseData, 2);
        if (respAddr != coilAddress || respVal != outputValue)
        {
            throw new ModbusException("WriteSingleCoil response mismatch.");
        }
    }

    /// <summary>
    /// 写入多个线圈 (FC15)。
    /// </summary>
    /// <param name="startAddress">起始线圈的0-based地址。</param>
    /// <param name="values">要写入的布尔值数组。</param>
    public async Task WriteMultipleCoilsAsync(ushort startAddress, bool[] values)
    {
        if (values == null || values.Length == 0 || values.Length > 1968) // Modbus规范限制
            throw new ArgumentOutOfRangeException(nameof(values), "Number of coils to write must be between 1 and 1968.");

        ushort numberOfCoils = (ushort)values.Length;
        byte byteCount = (byte)((numberOfCoils + 7) / 8); // 计算存储这些位所需的字节数

        // PDU Data for FC15: Start Address (2 bytes), Quantity of Outputs (2 bytes), Byte Count (1 byte), Output Values (N bytes)
        byte[] pduData = new byte[5 + byteCount];
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(startAddress), 0, pduData, 0, 2);
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(numberOfCoils), 0, pduData, 2, 2);
        pduData[4] = byteCount;

        for (int i = 0; i < numberOfCoils; i++)
        {
            if (values[i])
            {
                pduData[5 + (i / 8)] |= (byte)(1 << (i % 8));
            }
        }

        byte[] responseData = await ExecuteModbusRequestAsync(FunctionCodes.WriteMultipleCoils, pduData);

        // 校验响应: FC15响应包含起始地址和数量
        ushort respAddr = ModbusUtility.ToUInt16BigEndian(responseData, 0);
        ushort respQuant = ModbusUtility.ToUInt16BigEndian(responseData, 2);
        if (respAddr != startAddress || respQuant != numberOfCoils)
        {
            throw new ModbusException("WriteMultipleCoils response mismatch.");
        }
    }

    #endregion

    #region 保持寄存器操作 (Holding Registers)

    /// <summary>
    /// 读取单个或多个保持寄存器 (FC03)。
    /// </summary>
    /// <param name="startAddress">起始寄存器的0-based地址。</param>
    /// <param name="numberOfRegisters">要读取的寄存器数量。</param>
    /// <returns>包含原始寄存器数据的字节数组 (每个寄存器2字节)。</returns>
    public async Task<byte[]> ReadHoldingRegistersRawAsync(ushort startAddress, ushort numberOfRegisters)
    {
        if (numberOfRegisters == 0 || numberOfRegisters > 125) // Modbus规范限制
            throw new ArgumentOutOfRangeException(nameof(numberOfRegisters), "Number of registers must be between 1 and 125.");

        // PDU Data for FC03: Start Address (2 bytes), Quantity of Registers (2 bytes)
        byte[] pduData = new byte[4];
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(startAddress), 0, pduData, 0, 2);
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(numberOfRegisters), 0, pduData, 2, 2);

        byte[] responseData = await ExecuteModbusRequestAsync(FunctionCodes.ReadHoldingRegisters, pduData);

        // Response PDU Data for FC03: Byte Count (1 byte), Register Values (N*2 bytes)
        byte byteCount = responseData[0];
        if (byteCount != numberOfRegisters * 2)
        {
            throw new ModbusException("ReadHoldingRegisters response byte count mismatch.");
        }

        byte[] registerValues = new byte[byteCount];
        Buffer.BlockCopy(responseData, 1, registerValues, 0, byteCount);
        return registerValues;
    }

    /// <summary>
    /// 写入单个保持寄存器 (FC06)。
    /// </summary>
    /// <param name="registerAddress">寄存器的0-based地址。</param>
    /// <param name="value">要写入的16位值 (ushort)。</param>
    public async Task WriteSingleRegisterAsync(ushort registerAddress, ushort value)
    {
        // PDU Data for FC06: Register Address (2 bytes), Register Value (2 bytes)
        byte[] pduData = new byte[4];
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(registerAddress), 0, pduData, 0, 2);
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(value), 0, pduData, 2, 2);

        byte[] responseData = await ExecuteModbusRequestAsync(FunctionCodes.WriteSingleRegister, pduData);

        // 校验响应: FC06响应会回显请求
        ushort respAddr = ModbusUtility.ToUInt16BigEndian(responseData, 0);
        ushort respVal = ModbusUtility.ToUInt16BigEndian(responseData, 2);
        if (respAddr != registerAddress || respVal != value)
        {
            throw new ModbusException("WriteSingleRegister response mismatch.");
        }
    }
     /// <summary>
    /// 写入单个保持寄存器 (FC06) - short overload.
    /// </summary>
    public async Task WriteSingleRegisterAsync(ushort registerAddress, short value)
    {
        await WriteSingleRegisterAsync(registerAddress, (ushort)value);
    }


    /// <summary>
    /// 写入多个保持寄存器 (FC16)。
    /// </summary>
    /// <param name="startAddress">起始寄存器的0-based地址。</param>
    /// <param name="values">要写入的16位值数组 (ushort[])。每个元素是一个寄存器的值。</param>
    public async Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values)
    {
        if (values == null || values.Length == 0 || values.Length > 123) // Modbus规范限制
            throw new ArgumentOutOfRangeException(nameof(values), "Number of registers to write must be between 1 and 123.");

        ushort numberOfRegisters = (ushort)values.Length;
        byte byteCount = (byte)(numberOfRegisters * 2);

        // PDU Data for FC16: Start Address (2 bytes), Quantity of Registers (2 bytes), Byte Count (1 byte), Register Values (N*2 bytes)
        byte[] pduData = new byte[5 + byteCount];
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(startAddress), 0, pduData, 0, 2);
        Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(numberOfRegisters), 0, pduData, 2, 2);
        pduData[4] = byteCount;

        for (int i = 0; i < numberOfRegisters; i++)
        {
            Buffer.BlockCopy(ModbusUtility.GetBytesBigEndian(values[i]), 0, pduData, 5 + (i * 2), 2);
        }

        byte[] responseData = await ExecuteModbusRequestAsync(FunctionCodes.WriteMultipleRegisters, pduData);

        // 校验响应: FC16响应包含起始地址和数量
        ushort respAddr = ModbusUtility.ToUInt16BigEndian(responseData, 0);
        ushort respQuant = ModbusUtility.ToUInt16BigEndian(responseData, 2);
        if (respAddr != startAddress || respQuant != numberOfRegisters)
        {
            throw new ModbusException("WriteMultipleRegisters response mismatch.");
        }
    }
    /// <summary>
    /// 写入多个保持寄存器 (FC16) - short[] overload.
    /// </summary>
    public async Task WriteMultipleRegistersAsync(ushort startAddress, short[] values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        ushort[] ushortValues = Array.ConvertAll(values, v => (ushort)v);
        await WriteMultipleRegistersAsync(startAddress, ushortValues);
    }


    // --- Typed Data Access for Holding Registers ---

    public async Task<short> ReadShortAsync(ushort registerAddress)
    {
        byte[] rawData = await ReadHoldingRegistersRawAsync(registerAddress, 1);
        return ModbusUtility.ToInt16BigEndian(rawData, 0);
    }

    public async Task WriteShortAsync(ushort registerAddress, short value)
    {
        await WriteSingleRegisterAsync(registerAddress, value);
    }

    public async Task<ushort> ReadUShortAsync(ushort registerAddress)
    {
        byte[] rawData = await ReadHoldingRegistersRawAsync(registerAddress, 1);
        return ModbusUtility.ToUInt16BigEndian(rawData, 0);
    }

    public async Task WriteUShortAsync(ushort registerAddress, ushort value)
    {
        await WriteSingleRegisterAsync(registerAddress, value);
    }

    /// <summary>
    /// 从多个连续的保持寄存器读取字符串。
    /// </summary>
    /// <param name="startAddress">起始寄存器的0-based地址。</param>
    /// <param name="numberOfRegisters">组成字符串的寄存器数量 (每个寄存器2字节)。</param>
    /// <param name="reverseBytePairsInWord">是否颠倒每个16位寄存器内两个字节的顺序。
    /// 例如，如果寄存器存储 'B' 'A' (0x4241)，设置为true则按 'A' 'B' (0x4142) 解析。</param>
    /// <param name="encoding">字符串编码，默认为 ASCII。</param>
    /// <returns>读取到的字符串。</returns>
    public async Task<string> ReadStringAsync(ushort startAddress, ushort numberOfRegisters, bool reverseBytePairsInWord = false, Encoding? encoding = null)
    {
        if (encoding == null) encoding = Encoding.ASCII;

        byte[] rawData = await ReadHoldingRegistersRawAsync(startAddress, numberOfRegisters);
        byte[] stringBytes = new byte[rawData.Length];

        if (reverseBytePairsInWord)
        {
            for (int i = 0; i < numberOfRegisters; i++)
            {
                stringBytes[i * 2] = rawData[i * 2 + 1];
                stringBytes[i * 2 + 1] = rawData[i * 2];
            }
        }
        else
        {
            Buffer.BlockCopy(rawData, 0, stringBytes, 0, rawData.Length);
        }
        
        // 移除末尾的NUL字符 (0x00)
        int actualLength = stringBytes.Length;
        while(actualLength > 0 && stringBytes[actualLength-1] == 0x00)
        {
            actualLength--;
        }

        return encoding.GetString(stringBytes, 0, actualLength);
    }

    /// <summary>
    /// 将字符串写入多个连续的保持寄存器。
    /// </summary>
    /// <param name="startAddress">起始寄存器的0-based地址。</param>
    /// <param name="text">要写入的字符串。</param>
    /// <param name="fixedNumberOfRegisters">字符串将占用的固定寄存器数量。如果字符串转换后的字节数不足，将用0填充；如果超出，将被截断。</param>
    /// <param name="reverseBytePairsInWord">写入时是否颠倒每个16位字内两个字节的顺序。
    /// 例如，字符串 "AB" (0x4142)，设置为true则作为 'B' 'A' (0x4241) 写入寄存器。</param>
    /// <param name="encoding">字符串编码，默认为 ASCII。</param>
    public async Task WriteStringAsync(ushort startAddress, string? text, ushort fixedNumberOfRegisters, bool reverseBytePairsInWord = false, Encoding? encoding = null)
    {
        if (encoding == null) encoding = Encoding.ASCII;
        if (text == null) text = "";

        byte[] textBytes = encoding.GetBytes(text);
        int totalBytesToWrite = fixedNumberOfRegisters * 2;
        byte[] registerDataBytes = new byte[totalBytesToWrite]; // 用0初始化

        int bytesToCopy = Math.Min(textBytes.Length, totalBytesToWrite);
        
        if (reverseBytePairsInWord)
        {
            for (int i = 0; i < fixedNumberOfRegisters; i++)
            {
                int bytePos1 = i * 2;
                int bytePos2 = i * 2 + 1;

                if (bytePos2 < bytesToCopy) // 有两个字符可以组成一对
                {
                    registerDataBytes[bytePos1] = textBytes[bytePos2]; // 颠倒
                    registerDataBytes[bytePos2] = textBytes[bytePos1];
                }
                else if (bytePos1 < bytesToCopy) // 只有一个字符剩下，放在颠倒后的第一个位置 (高位)
                {
                    registerDataBytes[bytePos1] = 0x00; // 低位补0
                    registerDataBytes[bytePos2] = textBytes[bytePos1]; // 字符在高位
                }
                else // 超出textBytes范围，已由0填充
                {
                    // registerDataBytes is already 0-padded
                }
            }
        }
        else
        {
            Buffer.BlockCopy(textBytes, 0, registerDataBytes, 0, bytesToCopy);
        }


        ushort[] registers = new ushort[fixedNumberOfRegisters];
        for (int i = 0; i < fixedNumberOfRegisters; i++)
        {
            registers[i] = ModbusUtility.ToUInt16BigEndian(registerDataBytes, i * 2);
        }

        await WriteMultipleRegistersAsync(startAddress, registers);
    }

    /// <summary>
    /// 从保持寄存器的特定位读取布尔值。
    /// </summary>
    /// <param name="registerAddress">寄存器的0-based地址。</param>
    /// <param name="bitOffset">要读取的位偏移 (0-15)。</param>
    /// <returns>该位的状态 (true/false)。</returns>
    public async Task<bool> ReadBoolFromRegisterBitAsync(ushort registerAddress, byte bitOffset)
    {
        if (bitOffset > 15)
            throw new ArgumentOutOfRangeException(nameof(bitOffset), "Bit offset must be between 0 and 15.");

        ushort registerValue = await ReadUShortAsync(registerAddress);
        return (registerValue & (1 << bitOffset)) != 0;
    }

    /// <summary>
    /// 向保持寄存器的特定位写入布尔值。
    /// 注意：这是一个读-修改-写操作，不是原子的。如果需要原子性，PLC应支持FC22 (Mask Write Register)。
    /// </summary>
    /// <param name="registerAddress">寄存器的0-based地址。</param>
    /// <param name="bitOffset">要写入的位偏移 (0-15)。</param>
    /// <param name="value">要写入的布尔值。</param>
    public async Task WriteBoolToRegisterBitAsync(ushort registerAddress, byte bitOffset, bool value)
    {
        if (bitOffset > 15)
            throw new ArgumentOutOfRangeException(nameof(bitOffset), "Bit offset must be between 0 and 15.");

        ushort currentValue;
        lock (_lock) // 简单锁定，防止并发对此客户端实例的相同地址进行读改写冲突
        {
            // 需要先读取当前值
            // 注意：此处简化了，实际应用中，如果多个任务同时操作一个寄存器的不同位，
            // 这个读-改-写不是原子的，可能导致数据不一致。
            // 解决方案：使用信号量控制对特定寄存器的访问，或者PLC支持FC22 (Mask Write Register)。
        }
        currentValue = await ReadUShortAsync(registerAddress); // 实际读取在锁外

        ushort newValue;
        if (value)
        {
            newValue = (ushort)(currentValue | (1 << bitOffset)); // 设置位
        }
        else
        {
            newValue = (ushort)(currentValue & ~(1 << bitOffset)); // 清除位
        }

        if (newValue != currentValue) // 仅当值改变时才写入
        {
            await WriteUShortAsync(registerAddress, newValue);
        }
    }


    #endregion

    public async Task<T> ReadAsync<T>(string address)
    {
        // 简单地址解析，例如 "Coil:1", "HoldingRegister:100"
        var parts = address.Split(':');
        if (parts.Length != 2 || !ushort.TryParse(parts[1], out ushort addr))
        {
            throw new ArgumentException("Invalid Modbus address format. Use 'Coil:<address>' or 'HoldingRegister:<address>'.", nameof(address));
        }

        string type = parts[0];

        if (typeof(T) == typeof(bool))
        {
            if (type.Equals("Coil", StringComparison.OrdinalIgnoreCase))
            {
                var result = await ReadSingleCoilAsync(addr);
                return (T)(object)result;
            }
        }
        else if (typeof(T) == typeof(short))
        {
            if (type.Equals("HoldingRegister", StringComparison.OrdinalIgnoreCase))
            {
                var result = await ReadShortAsync(addr);
                return (T)(object)result;
            }
        }
        else if (typeof(T) == typeof(ushort))
        {
            if (type.Equals("HoldingRegister", StringComparison.OrdinalIgnoreCase))
            {
                var result = await ReadUShortAsync(addr);
                return (T)(object)result;
            }
        }

        throw new NotSupportedException($"Reading type {typeof(T).Name} from {type} is not supported or not implemented.");
    }

    public async Task WriteAsync<T>(string address, T value)
    {
        var parts = address.Split(':');
        if (parts.Length != 2 || !ushort.TryParse(parts[1], out ushort addr))
        {
            throw new ArgumentException("Invalid Modbus address format. Use 'Coil:<address>' or 'HoldingRegister:<address>'.", nameof(address));
        }

        string type = parts[0];

        if (value is bool boolValue && type.Equals("Coil", StringComparison.OrdinalIgnoreCase))
        {
            await WriteSingleCoilAsync(addr, boolValue);
            return;
        }
        else if (value is short shortValue && type.Equals("HoldingRegister", StringComparison.OrdinalIgnoreCase))
        {
            await WriteShortAsync(addr, shortValue);
            return;
        }
        else if (value is ushort ushortValue && type.Equals("HoldingRegister", StringComparison.OrdinalIgnoreCase))
        {
            await WriteUShortAsync(addr, ushortValue);
            return;
        }

        throw new NotSupportedException($"Writing type {typeof(T).Name} to {type} is not supported or not implemented.");
    }

    public void Dispose()
    {
        Disconnect();
    }
}