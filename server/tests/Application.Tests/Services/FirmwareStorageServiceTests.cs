using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

#pragma warning disable S3881 // SonarAnalyzer vil have andet dispose pattern, men dette  er en test 
public class FirmwareStorageServiceTests : IDisposable
#pragma warning restore S3881
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<ILogger<FirmwareStorageService>> _loggerMock;
    private readonly FirmwareStorageService _service;
    private readonly string _testFirmwarePath;

    public FirmwareStorageServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _loggerMock = new Mock<ILogger<FirmwareStorageService>>();
        
        _testFirmwarePath = Path.Combine(Path.GetTempPath(), $"firmware_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFirmwarePath);
        
        _service = new FirmwareStorageService(_loggerMock.Object);
        var field = typeof(FirmwareStorageService).GetField("_firmwarePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_service, _testFirmwarePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFirmwarePath))
        {
            Directory.Delete(_testFirmwarePath, true);
        }
        GC.SuppressFinalize(this);
    }

    public class SaveFirmwareAsync(ITestOutputHelper outputHelper) : FirmwareStorageServiceTests(outputHelper)
    {
        [Fact]
        public async Task SaveFirmwareAsync_WithValidData_SavesFile()
        {
            // Arrange
            var firmwareId = Guid.NewGuid();
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            
            // Act
            await _service.SaveFirmwareAsync(firmwareId, data);
            
            // Assert
            var expectedPath = Path.Combine(_testFirmwarePath, $"firmware_{firmwareId}.bin");
            File.Exists(expectedPath).ShouldBeTrue();
            
            var savedData = await File.ReadAllBytesAsync(expectedPath);
            savedData.ShouldBe(data);
        }

        [Fact]
        public async Task SaveFirmwareAsync_Overwrites_ExistingFile()
        {
            // Arrange
            var firmwareId = Guid.NewGuid();
            var originalData = new byte[] { 0x01, 0x02 };
            var newData = new byte[] { 0x03, 0x04, 0x05 };
            
            await _service.SaveFirmwareAsync(firmwareId, originalData);
            
            // Act
            await _service.SaveFirmwareAsync(firmwareId, newData);
            
            // Assert
            var expectedPath = Path.Combine(_testFirmwarePath, $"firmware_{firmwareId}.bin");
            var savedData = await File.ReadAllBytesAsync(expectedPath);
            savedData.ShouldBe(newData);
            savedData.Length.ShouldBe(3);
        }

        [Fact]
        public async Task SaveFirmwareAsync_WithEmptyData_SavesEmptyFile()
        {
            // Arrange
            var firmwareId = Guid.NewGuid();
            var data = Array.Empty<byte>();
            
            // Act
            await _service.SaveFirmwareAsync(firmwareId, data);
            
            // Assert
            var expectedPath = Path.Combine(_testFirmwarePath, $"firmware_{firmwareId}.bin");
            File.Exists(expectedPath).ShouldBeTrue();
            
            var fileInfo = new FileInfo(expectedPath);
            fileInfo.Length.ShouldBe(0);
        }
    }

    public class ReadFirmwareAsync(ITestOutputHelper outputHelper) : FirmwareStorageServiceTests(outputHelper)
    {
        [Fact]
        public async Task ReadFirmwareAsync_ExistingFile_ReturnsData()
        {
            // Arrange
            var firmwareId = Guid.NewGuid();
            var data = new byte[] { 0xFF, 0xFE, 0xFD };
            
            await _service.SaveFirmwareAsync(firmwareId, data);
            
            // Act
            var result = await _service.ReadFirmwareAsync(firmwareId);
            
            // Assert
            result.ShouldBe(data);
        }

        [Fact]
        public async Task ReadFirmwareAsync_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var firmwareId = Guid.NewGuid();
            
            // Act & Assert
            await Should.ThrowAsync<FileNotFoundException>(() => _service.ReadFirmwareAsync(firmwareId));
        }

        [Fact]
        public async Task ReadFirmwareAsync_LargeFile_ReadsCorrectly()
        {
            // Arrange
            var firmwareId = Guid.NewGuid();
            var data = new byte[5 * 1024 * 1024]; // 5MB
            Random.Shared.NextBytes(data);
            
            await _service.SaveFirmwareAsync(firmwareId, data);
            
            // Act
            var result = await _service.ReadFirmwareAsync(firmwareId);
            
            // Assert
            result.Length.ShouldBe(data.Length);
            result.ShouldBe(data);
        }
    }

    public class FirmwareExists(ITestOutputHelper outputHelper) : FirmwareStorageServiceTests(outputHelper)
    {
        [Fact]
        public async Task FirmwareExists_FileExists_ReturnsTrue()
        {
            // Arrange
            var firmwareId = Guid.NewGuid();
            await _service.SaveFirmwareAsync(firmwareId, [0x01]);
            
            // Act
            var result = _service.FirmwareExists(firmwareId);
            
            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public void FirmwareExists_FileDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var firmwareId = Guid.NewGuid();
            
            // Act
            var result = _service.FirmwareExists(firmwareId);
            
            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task FirmwareExists_AfterDeletion_ReturnsFalse()
        {
            // Arrange
            var firmwareId = Guid.NewGuid();
            await _service.SaveFirmwareAsync(firmwareId, [0x01]);
            
            var filePath = Path.Combine(_testFirmwarePath, $"firmware_{firmwareId}.bin");
            File.Delete(filePath);
            
            // Act
            var result = _service.FirmwareExists(firmwareId);
            
            // Assert
            result.ShouldBeFalse();
        }
    }

    public class CalculateCrc32(ITestOutputHelper outputHelper) : FirmwareStorageServiceTests(outputHelper)
    {
        private static readonly byte[] EmptyData = [];
        private static readonly byte[] SingleByteData = { 0x00 };
        private static readonly byte[] TestStringData = "123456789"u8.ToArray();

        [Fact]
        public void CalculateCrc32_EmptyData_ReturnsZero()
        {
            // Act
            var result = _service.CalculateCrc32(EmptyData);
            
            // Assert
            result.ShouldBe(0x00000000u);
        }
        
        [Fact]
        public void CalculateCrc32_SingleByte_ReturnsExpectedCrc()
        {
            // Act
            var result = _service.CalculateCrc32(SingleByteData);
            
            // Assert
            result.ShouldBe(0xD202EF8Du);
        }
        
        [Fact]
        public void CalculateCrc32_TestString_ReturnsExpectedCrc()
        {
            // Act
            var result = _service.CalculateCrc32(TestStringData);
            
            // Assert
            result.ShouldBe(0xCBF43926u);
        }

        [Fact]
        public void CalculateCrc32_SameData_ReturnsSameCrc()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            
            // Act
            var crc1 = _service.CalculateCrc32(data);
            var crc2 = _service.CalculateCrc32(data);
            
            // Assert
            crc1.ShouldBe(crc2);
        }

        [Fact]
        public void CalculateCrc32_DifferentData_ReturnsDifferentCrc()
        {
            // Arrange
            var data1 = new byte[] { 0x01, 0x02, 0x03 };
            var data2 = new byte[] { 0x01, 0x02, 0x04 }; // Sidste byte er anderledes 
            
            // Act
            var crc1 = _service.CalculateCrc32(data1);
            var crc2 = _service.CalculateCrc32(data2);
            
            // Assert
            crc1.ShouldNotBe(crc2);
        }

        [Fact]
        public void CalculateCrc32_LargeData_CalculatesCorrectly()
        {
            // Arrange
            var data = new byte[1024 * 1024]; // 1MB
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }
            
            // Act
            var result = _service.CalculateCrc32(data);
            
            // Assert
            result.ShouldNotBe(0u);
            result.ShouldNotBe(0xFFFFFFFFu);
            
            // Tjek igen for konsistens 
            var result2 = _service.CalculateCrc32(data);
            result2.ShouldBe(result);
        }
    }
}