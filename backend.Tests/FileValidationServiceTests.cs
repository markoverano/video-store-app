using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VideoStore.Backend.Services;
using Xunit;

namespace VideoStore.Backend.Tests
{
    public class FileValidationServiceTests
    {
        private readonly FileValidationService _service;
        private readonly Mock<ILogger<FileValidationService>> _loggerMock;

        public FileValidationServiceTests()
        {
            _loggerMock = new Mock<ILogger<FileValidationService>>();
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c.GetSection("FileUpload:MaxFileSizeMB").Value).Returns("100");

            _service = new FileValidationService(_loggerMock.Object, configMock.Object);
        }

        [Theory]
        [InlineData("video.mp4", true)]
        [InlineData("video.MP4", true)]
        [InlineData("video.avi", true)]
        [InlineData("video.mov", true)]
        [InlineData("video.mkv", false)]
        [InlineData("video.wmv", false)]
        [InlineData("document.pdf", false)]
        [InlineData("", false)]
        [InlineData("noextension", false)]
        public void IsValidFileType_ValidatesCorrectly(string fileName, bool expectedResult)
        {
            var result = _service.IsValidFileType(fileName);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(1024, true)]
        [InlineData(50 * 1024 * 1024, true)]
        [InlineData(100 * 1024 * 1024, true)]
        [InlineData(101 * 1024 * 1024, false)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        public void IsValidFileSize_ValidatesCorrectly(long fileSize, bool expectedResult)
        {
            var result = _service.IsValidFileSize(fileSize);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("video/mp4", true)]
        [InlineData("video/x-msvideo", true)]
        [InlineData("video/quicktime", true)]
        [InlineData("video/webm", false)]
        [InlineData("application/pdf", false)]
        [InlineData("", false)]
        public void ValidateMimeType_ValidatesCorrectly(string contentType, bool expectedResult)
        {
            var result = _service.ValidateMimeType(contentType);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void GetAllowedExtensions_ReturnsExpectedFormattedString()
        {
            var result = _service.GetAllowedExtensions();
            Assert.Contains("MP4", result);
            Assert.Contains("AVI", result);
            Assert.Contains("MOV", result);
        }

        [Fact]
        public void GetMaxFileSizeInBytes_Returns100MB()
        {
            var result = _service.GetMaxFileSizeInBytes();
            Assert.Equal(100L * 1024 * 1024, result);
        }

        [Fact]
        public void GetMaxFileSizeFormatted_ReturnsReadableFormat()
        {
            var result = _service.GetMaxFileSizeFormatted();
            Assert.Equal("100 MB", result);
        }
    }
}
