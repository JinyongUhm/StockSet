Imports CandleServiceInterfacePrj
Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Runtime.Serialization
#If 0 Then
'2022.08.27: 아래 class 는 MemoryMappedFile 을 이용해 CybosLauncher 에서 제공하는 메모리를 빌려쓰는 방법으로 candle service 하는 것을 구현한 것이다.
'2022.08.27: 그러나 interfacing 하는데 시간이 걸리는 것인지, 너무 시간이 오래 걸려서 실패했다.
Public Class c06_CandleLink
    Public IndexInMMF As Integer
    Public CandleDataAccessor As IO.MemoryMappedFiles.MemoryMappedViewAccessor
    Public Shared CANDLE_STRUCTURE_SIZE As Integer = System.Runtime.InteropServices.Marshal.SizeOf(GetType(CandleStructure))
    Public ZeroPoint As Integer
    Public Length As Integer

    Public Sub New()
        CandleDataAccessor = CybosCandleStoreMMF.CreateViewAccessor(CANDLE_STRUCTURE_SIZE * 9601 * IndexInMMF, CANDLE_STRUCTURE_SIZE * 9601)
        ZeroPoint = 0
        Length = 0
    End Sub

    Public Sub Initialize(ByVal symbol_index As Integer)
        IndexInMMF = symbol_index
    End Sub

    Public Sub AddCandle(ByVal candle_to_add As CandleStructure)
        Dim physical_index As Integer

        physical_index = ZeroPoint + Length * CANDLE_STRUCTURE_SIZE Mod CANDLE_STRUCTURE_SIZE * 9601

        CandleDataAccessor.Write(Of CandleStructure)(physical_index, candle_to_add)
        Length += 1
        If Length > 9601 Then
            MsgBox("이게 아니야2")
        End If
    End Sub

    Public Sub RemoveCandle()
        ZeroPoint += CANDLE_STRUCTURE_SIZE
        If ZeroPoint = CANDLE_STRUCTURE_SIZE * 9601 Then
            ZeroPoint = 0
        ElseIf ZeroPoint > CANDLE_STRUCTURE_SIZE * 9601 Then
            MsgBox("이게 아니야1")
        End If
        Length -= 1
        If Length < 0 Then
            MsgBox("이게 아니야3")
        End If
    End Sub

    Public Function Candle(ByVal candle_index As Integer) As CandleStructure
        Dim physical_index As Integer
        Dim candle_to_return As CandleStructure

        If candle_index > Length - 1 Then
            MsgBox("이게 아니야4")
        End If

        physical_index = ZeroPoint + candle_index * CANDLE_STRUCTURE_SIZE Mod CANDLE_STRUCTURE_SIZE * 9601

        CandleDataAccessor.Read(Of CandleStructure)(physical_index, candle_to_return)

        Return candle_to_return
    End Function

    Public Function LastCandle() As CandleStructure
        Dim physical_index As Integer
        Dim candle_to_return As CandleStructure

        If Length = 0 Then
            MsgBox("이게 아니야5")
        End If

        physical_index = ZeroPoint + (Length - 1) * CANDLE_STRUCTURE_SIZE Mod CANDLE_STRUCTURE_SIZE * 9601

        CandleDataAccessor.Read(Of CandleStructure)(physical_index, candle_to_return)

        Return candle_to_return
    End Function

    Public Sub ClearCandle()
        ZeroPoint = 0
        Length = 0
    End Sub

    Public Function CandleCount() As Integer
        Return Length
    End Function
End Class
#End If

Public Class c06_CandleLink
    Public CandleFileSystem As c06a_CandleFileSystem
    Public SymbolIndex As Integer
    'Public CandleDataAccessor As IO.MemoryMappedFiles.MemoryMappedViewAccessor
    'Public Shared CANDLE_STRUCTURE_SIZE As Integer = System.Runtime.InteropServices.Marshal.SizeOf(GetType(CandleStructure))
    Public ZeroPoint As Integer
    Public Length As Integer

    Public Sub New(candle_file_system As c06a_CandleFileSystem)
        'CandleDataAccessor = CybosCandleStoreMMF.CreateViewAccessor(CANDLE_STRUCTURE_SIZE * 9601 * IndexInMMF, CANDLE_STRUCTURE_SIZE * 9601)
        CandleFileSystem = candle_file_system
    End Sub

    Public Sub Initialize(ByVal symbol_index As Integer)
        SymbolIndex = symbol_index
        ZeroPoint = 0
        Length = 0
    End Sub

    Public Sub AddCandle(ByVal candle_to_add As CandleStructure)
        Dim physical_index As Integer

        'physical_index = ZeroPoint + Length * CANDLE_STRUCTURE_SIZE Mod CANDLE_STRUCTURE_SIZE * 9601
        physical_index = ZeroPoint + Length Mod 9601

        'CandleDataAccessor.Write(Of CandleStructure)(physical_index, candle_to_add)
        CandleFileSystem.Write(SymbolIndex, physical_index, candle_to_add)
        Length += 1
        If Length > 9601 Then
            MsgBox("이게 아니야2")
        End If
    End Sub

    Public Sub RemoveCandle()
        ZeroPoint += 1
        If ZeroPoint = 9601 Then
            ZeroPoint = 0
        ElseIf ZeroPoint > 9601 Then
            MsgBox("이게 아니야1")
        End If
        Length -= 1
        If Length < 0 Then
            MsgBox("이게 아니야3")
        End If
    End Sub

    Public Function Candle(ByVal candle_index As Integer) As CandleStructure
        Dim physical_index As Integer
        Dim candle_to_return As CandleStructure

        If candle_index > Length - 1 Then
            MsgBox("이게 아니야4")
        End If

        physical_index = (ZeroPoint + candle_index) Mod 9601

        'CandleDataAccessor.Read(Of CandleStructure)(physical_index, candle_to_return)
        candle_to_return = CandleFileSystem.Read(SymbolIndex, physical_index)

        Return candle_to_return
    End Function

    Public Function LastCandle() As CandleStructure
        Dim physical_index As Integer
        Dim candle_to_return As CandleStructure

        If Length = 0 Then
            MsgBox("이게 아니야5")
        End If

        physical_index = (ZeroPoint + (Length - 1)) Mod 9601

        'CandleDataAccessor.Read(Of CandleStructure)(physical_index, candle_to_return)
        candle_to_return = CandleFileSystem.Read(SymbolIndex, physical_index)

        Return candle_to_return
    End Function

    Public Sub ClearCandle()
        ZeroPoint = 0
        Length = 0
    End Sub

    Public Function CandleCount() As Integer
        Return Length
    End Function
End Class

Public Class c06a_CandleFileSystem
    Public CandleServiceFile As IO.FileStream
    'Public CandleFormatter As New BinaryFormatter
    Public TotalSymbolNumber As Integer
    Public Shared CANDLE_STRUCTURE_SIZE As Integer = System.Runtime.InteropServices.Marshal.SizeOf(GetType(CandleStructure))
    Private _CandleFileKey As TracingKey

    Public Sub New(ByVal total_symbol_number As Integer)
        TotalSymbolNumber = total_symbol_number

        CandleServiceFile = File.Create("candle_service.hex")
        CandleServiceFile.SetLength(CANDLE_STRUCTURE_SIZE * 9601 * total_symbol_number)
        'CandleFormatter = New IO.MemoryStream(CandleServiceBytesArray)
    End Sub

    Private Function Convert2PhysicalPoint(ByVal symbol_index As Integer, ByVal candle_index As UInt64) As UInt64
        Return (symbol_index * 9601 + candle_index) * CANDLE_STRUCTURE_SIZE
    End Function

    Public Function Read(ByVal symbol_index As Integer, ByVal candle_index As UInt64) As CandleStructure
        Dim physical_point As UInt64 = Convert2PhysicalPoint(symbol_index, candle_index)
        SafeEnterTrace(_CandleFileKey, 1)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        CandleServiceFile.Seek(physical_point, SeekOrigin.Begin)
        Dim binary_reader As New BinaryReader(CandleServiceFile)
        Dim read_candle As CandleStructure
        'read_candle.Average35Minutes = binary_reader.ReadSingle
        'read_candle.Average70Minutes = binary_reader.ReadSingle
        'read_candle.Average140Minutes = binary_reader.ReadSingle
        'read_candle.Average280Minutes = binary_reader.ReadSingle
        'read_candle.Average560Minutes = binary_reader.ReadSingle
        'read_candle.Average1200Minutes = binary_reader.ReadSingle
        read_candle.Average2400Minutes = binary_reader.ReadSingle
        read_candle.Average4800Minutes = binary_reader.ReadSingle
        read_candle.Average9600Minutes = binary_reader.ReadSingle
        read_candle.CandleTime = DateTime.FromBinary(binary_reader.ReadInt64)
        read_candle.Open = binary_reader.ReadUInt32
        read_candle.Close = binary_reader.ReadUInt32
        read_candle.High = binary_reader.ReadUInt32
        read_candle.Low = binary_reader.ReadUInt32
        read_candle.Amount = binary_reader.ReadUInt32
        read_candle.AccumAmount = binary_reader.ReadUInt64

        SafeLeaveTrace(_CandleFileKey, 101)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        Return read_candle
    End Function

    Public Sub Write(ByVal symbol_index As Integer, ByVal candle_index As UInt64, ByVal candle_to_write As CandleStructure)
        Dim physical_point As UInt64 = Convert2PhysicalPoint(symbol_index, candle_index)
        SafeEnterTrace(_CandleFileKey, 2)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        CandleServiceFile.Seek(physical_point, SeekOrigin.Begin)
        Dim binary_writer As New BinaryWriter(CandleServiceFile)
        'binary_writer.Write(candle_to_write.Average35Minutes)
        'binary_writer.Write(candle_to_write.Average70Minutes)
        'binary_writer.Write(candle_to_write.Average140Minutes)
        'binary_writer.Write(candle_to_write.Average280Minutes)
        'binary_writer.Write(candle_to_write.Average560Minutes)
        'binary_writer.Write(candle_to_write.Average1200Minutes)
        binary_writer.Write(candle_to_write.Average2400Minutes)
        binary_writer.Write(candle_to_write.Average4800Minutes)
        binary_writer.Write(candle_to_write.Average9600Minutes)
        binary_writer.Write(candle_to_write.CandleTime.ToBinary)
        binary_writer.Write(candle_to_write.Open)
        binary_writer.Write(candle_to_write.Close)
        binary_writer.Write(candle_to_write.High)
        binary_writer.Write(candle_to_write.Low)
        binary_writer.Write(candle_to_write.Amount)
        binary_writer.Write(candle_to_write.AccumAmount)
        SafeLeaveTrace(_CandleFileKey, 201)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
End Class