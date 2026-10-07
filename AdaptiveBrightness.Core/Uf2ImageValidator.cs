using System.Buffers.Binary;

namespace AdaptiveBrightness.Core;

public sealed record Uf2ImageInfo(uint FamilyId, int BlockCount, int PayloadBytes, long LengthBytes);

public static class Uf2ImageValidator
{
    public const uint Rp2040FamilyId = 0xE48BFF56;
    private const uint MagicStart0 = 0x0A324655;
    private const uint MagicStart1 = 0x9E5D5157;
    private const uint MagicEnd = 0x0AB16F30;
    private const uint FamilyIdFlag = 0x00002000;
    private const int BlockLength = 512;
    private const int Rp2040PayloadLength = 256;
    private const int MaximumBlockCount = 8192;

    public static bool TryValidateFile(string path, out Uf2ImageInfo? image, out string error)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return TryValidate(stream, out image, out error);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            image = null;
            error = $"无法读取 UF2 固件：{ex.Message}";
            return false;
        }
    }

    public static bool TryValidate(Stream stream, out Uf2ImageInfo? image, out string error)
    {
        image = null;
        if (!stream.CanRead || !stream.CanSeek)
        {
            error = "UF2 数据源必须可读取并支持定位。";
            return false;
        }

        var length = stream.Length;
        if (length < BlockLength || length % BlockLength != 0 || length / BlockLength > MaximumBlockCount)
        {
            error = "UF2 文件长度无效或超出安全校验范围。";
            return false;
        }

        var blockCount = checked((int)(length / BlockLength));
        var block = new byte[BlockLength];
        uint expectedFamily = 0;
        var expectedPayloadLength = 0;
        for (var index = 0; index < blockCount; index++)
        {
            stream.Position = (long)index * BlockLength;
            if (stream.Read(block, 0, block.Length) != BlockLength)
            {
                error = "UF2 文件包含不完整的数据块。";
                return false;
            }

            var span = block.AsSpan();
            var magic0 = BinaryPrimitives.ReadUInt32LittleEndian(span[0..4]);
            var magic1 = BinaryPrimitives.ReadUInt32LittleEndian(span[4..8]);
            var flags = BinaryPrimitives.ReadUInt32LittleEndian(span[8..12]);
            var payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(span[16..20]);
            var blockNumber = BinaryPrimitives.ReadUInt32LittleEndian(span[20..24]);
            var declaredBlockCount = BinaryPrimitives.ReadUInt32LittleEndian(span[24..28]);
            var family = BinaryPrimitives.ReadUInt32LittleEndian(span[28..32]);
            var endMagic = BinaryPrimitives.ReadUInt32LittleEndian(span[508..512]);

            if (magic0 != MagicStart0 || magic1 != MagicStart1 || endMagic != MagicEnd)
            {
                error = $"UF2 第 {index} 块的标识签名无效。";
                return false;
            }
            if ((flags & FamilyIdFlag) == 0 || family != Rp2040FamilyId)
            {
                error = "UF2 固件不是带 RP2040 family ID 的映像。";
                return false;
            }
            if (blockNumber != index || declaredBlockCount != blockCount)
            {
                error = "UF2 块编号或声明的总块数不一致。";
                return false;
            }
            if (payloadLength != Rp2040PayloadLength)
            {
                error = "RP2040 UF2 数据块的有效载荷长度不是 256 字节。";
                return false;
            }
            if (index == 0)
            {
                expectedFamily = family;
                expectedPayloadLength = checked((int)payloadLength);
            }
            else if (family != expectedFamily || payloadLength != expectedPayloadLength)
            {
                error = "UF2 各数据块的 family ID 或有效载荷长度不一致。";
                return false;
            }
        }

        image = new Uf2ImageInfo(expectedFamily, blockCount, expectedPayloadLength, length);
        error = "";
        return true;
    }
}

public static class Rp2040BootloaderIdentity
{
    public static bool IsExpectedDrive(string? volumeLabel, bool isRemovable, string? bootInfo, bool hasIndexFile)
    {
        if (!string.Equals(volumeLabel?.Trim(), "RPI-RP2", StringComparison.OrdinalIgnoreCase) || !isRemovable || !hasIndexFile || string.IsNullOrWhiteSpace(bootInfo))
            return false;

        return bootInfo.Contains("RP2040", StringComparison.OrdinalIgnoreCase) ||
               bootInfo.Contains("RP2", StringComparison.OrdinalIgnoreCase) ||
               bootInfo.Contains("RPI-RP2", StringComparison.OrdinalIgnoreCase);
    }
}
