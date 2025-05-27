using Application.Interfaces.Communication.Publishers;
using Application.Models.DTOs;
using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class FirmwareTransferServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IOtaPublisher> _otaPublisherMock;
    private readonly Mock<ILogger<FirmwareTransferService>> _loggerMock;
    private readonly FirmwareTransferService _service;

    private FirmwareTransferServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _otaPublisherMock = new Mock<IOtaPublisher>();
        _loggerMock = new Mock<ILogger<FirmwareTransferService>>();
        
        _service = new FirmwareTransferService(
            _otaPublisherMock.Object,
            _loggerMock.Object);
    }

    public class SendFirmwareAsync(ITestOutputHelper outputHelper) : FirmwareTransferServiceTests(outputHelper)
    {
        [Fact]
        public async Task SendFirmwareAsync_SmallFirmware_SendsSingleChunk()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            
            var capturedCommands = new List<OtaChunkCommand>();
            _otaPublisherMock.Setup(x => x.PublishOtaChunkAsync(It.IsAny<OtaChunkCommand>()))
                .Callback<OtaChunkCommand>(cmd => capturedCommands.Add(cmd))
                .Returns(Task.CompletedTask);
            
            // Act
            await _service.SendFirmwareAsync(analyzerId, firmwareData);
            
            // Assert
            capturedCommands.Count.ShouldBe(1);
            
            var chunk = capturedCommands[0];
            chunk.AnalyzerId.ShouldBe(analyzerId.ToString());
            chunk.ChunkData.ShouldBe(firmwareData);
            chunk.ChunkIndex.ShouldBe(0u);
            chunk.ChunkSize.ShouldBe((uint)firmwareData.Length);
        }

        [Fact]
        public async Task SendFirmwareAsync_ExactlyChunkSize_SendsSingleChunk()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareData = new byte[4096]; // Præcis chunksize 
            Random.Shared.NextBytes(firmwareData);
            
            var capturedCommands = new List<OtaChunkCommand>();
            _otaPublisherMock.Setup(x => x.PublishOtaChunkAsync(It.IsAny<OtaChunkCommand>()))
                .Callback<OtaChunkCommand>(cmd => capturedCommands.Add(cmd))
                .Returns(Task.CompletedTask);
            
            // Act
            await _service.SendFirmwareAsync(analyzerId, firmwareData);
            
            // Assert
            capturedCommands.Count.ShouldBe(1);
            capturedCommands[0].ChunkSize.ShouldBe(4096u);
            capturedCommands[0].ChunkData.Length.ShouldBe(4096);
        }

        [Fact]
        public async Task SendFirmwareAsync_LargeFirmware_SendsMultipleChunks()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareData = new byte[10000]; // Skal kræve 3 chunks (4096 + 4096 + 1808)
            for (int i = 0; i < firmwareData.Length; i++)
            {
                firmwareData[i] = (byte)(i % 256);
            }
            
            var capturedCommands = new List<OtaChunkCommand>();
            _otaPublisherMock.Setup(x => x.PublishOtaChunkAsync(It.IsAny<OtaChunkCommand>()))
                .Callback<OtaChunkCommand>(cmd => capturedCommands.Add(cmd))
                .Returns(Task.CompletedTask);
            
            // Act
            await _service.SendFirmwareAsync(analyzerId, firmwareData);
            
            // Assert
            capturedCommands.Count.ShouldBe(3);
            
            // Første chunk 
            capturedCommands[0].ChunkIndex.ShouldBe(0u);
            capturedCommands[0].ChunkSize.ShouldBe(4096u);
            capturedCommands[0].ChunkData.Length.ShouldBe(4096);
            capturedCommands[0].ChunkData[0].ShouldBe((byte)0);
            
            // Andet chunk
            capturedCommands[1].ChunkIndex.ShouldBe(1u);
            capturedCommands[1].ChunkSize.ShouldBe(4096u);
            capturedCommands[1].ChunkData.Length.ShouldBe(4096);
            capturedCommands[1].ChunkData[0].ShouldBe((byte)0);
            
            // Trejde chunk
            capturedCommands[2].ChunkIndex.ShouldBe(2u);
            capturedCommands[2].ChunkSize.ShouldBe(1808u);
            capturedCommands[2].ChunkData.Length.ShouldBe(1808);
            capturedCommands[2].ChunkData[0].ShouldBe((byte)0);
        }

        [Fact]
        public async Task SendFirmwareAsync_EmptyFirmware_SendsNoChunks()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareData = Array.Empty<byte>();
            
            // Act
            await _service.SendFirmwareAsync(analyzerId, firmwareData);
            
            // Assert
            _otaPublisherMock.Verify(x => x.PublishOtaChunkAsync(It.IsAny<OtaChunkCommand>()), Times.Never);
        }

        [Fact]
        public async Task SendFirmwareAsync_VerifiesDataIntegrity()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareData = new byte[9000]; // Kræver 3 chunks 
            Random.Shared.NextBytes(firmwareData);
            
            var capturedChunks = new List<byte[]>();
            _otaPublisherMock.Setup(x => x.PublishOtaChunkAsync(It.IsAny<OtaChunkCommand>()))
                .Callback<OtaChunkCommand>(cmd => capturedChunks.Add(cmd.ChunkData))
                .Returns(Task.CompletedTask);
            
            // Act
            await _service.SendFirmwareAsync(analyzerId, firmwareData);
            
            // Assert - Lav firmware fra chunks 
            var reconstructed = capturedChunks.SelectMany(chunk => chunk).ToArray();
            reconstructed.Length.ShouldBe(firmwareData.Length);
            reconstructed.ShouldBe(firmwareData);
        }

        [Fact]
        public async Task SendFirmwareAsync_AppliesDelayBetweenChunks()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareData = new byte[8192]; // Præcis 2 chunks 
            
            var timestamps = new List<DateTime>();
            _otaPublisherMock.Setup(x => x.PublishOtaChunkAsync(It.IsAny<OtaChunkCommand>()))
                .Callback<OtaChunkCommand>(_ => timestamps.Add(DateTime.UtcNow))
                .Returns(Task.CompletedTask);
            
            // Act
            await _service.SendFirmwareAsync(analyzerId, firmwareData);
            
            // Assert
            timestamps.Count.ShouldBe(2);
            if (timestamps.Count >= 2)
            {
                var delay = timestamps[1] - timestamps[0];
                delay.TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(40); // Tolerance
            }
        }

        [Fact]
        public async Task SendFirmwareAsync_LargeFirmware_CalculatesCorrectTotalChunks()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var testCases = new[]
            {
                (size: 1, expectedChunks: 1),
                (size: 4096, expectedChunks: 1),
                (size: 4097, expectedChunks: 2),
                (size: 8192, expectedChunks: 2),
                (size: 12288, expectedChunks: 3),
                (size: 12289, expectedChunks: 4)
            };
            
            foreach (var testCase in testCases)
            {
                var capturedCommands = new List<OtaChunkCommand>();
                _otaPublisherMock.Setup(x => x.PublishOtaChunkAsync(It.IsAny<OtaChunkCommand>()))
                    .Callback<OtaChunkCommand>(cmd => capturedCommands.Add(cmd))
                    .Returns(Task.CompletedTask);
                
                var firmwareData = new byte[testCase.size];
                
                // Act
                await _service.SendFirmwareAsync(analyzerId, firmwareData);
                
                // Assert
                capturedCommands.Count.ShouldBe(testCase.expectedChunks);
                
                _otaPublisherMock.Reset();
            }
        }

        [Fact]
        public async Task SendFirmwareAsync_CorrectChunkIndexing()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareData = new byte[20000]; // 5 chunks
            
            var capturedCommands = new List<OtaChunkCommand>();
            _otaPublisherMock.Setup(x => x.PublishOtaChunkAsync(It.IsAny<OtaChunkCommand>()))
                .Callback<OtaChunkCommand>(cmd => capturedCommands.Add(cmd))
                .Returns(Task.CompletedTask);
            
            // Act
            await _service.SendFirmwareAsync(analyzerId, firmwareData);
            
            // Assert
            capturedCommands.Count.ShouldBe(5);
            for (uint i = 0; i < 5; i++)
            {
                capturedCommands[(int)i].ChunkIndex.ShouldBe(i);
            }
        }
    }
}