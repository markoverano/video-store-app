using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VideoStore.Backend.Services
{
    public class ThumbnailService : IThumbnailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ThumbnailService> _logger;
        private readonly string _thumbnailDirectory;
        private readonly string _relativeThumbnailPath;
        private readonly int _thumbnailWidth;
        private readonly int _thumbnailHeight;
        private readonly string _ffmpegPath;

        public ThumbnailService(IConfiguration configuration, ILogger<ThumbnailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _relativeThumbnailPath = _configuration.GetValue<string>("Thumbnail:UploadPath") ?? "uploads/thumbnails";
            _thumbnailDirectory = Path.Combine(Directory.GetCurrentDirectory(), _relativeThumbnailPath);
            _thumbnailWidth = _configuration.GetValue<int>("Thumbnail:Width", 256);
            _thumbnailHeight = _configuration.GetValue<int>("Thumbnail:Height", 256);
            _ffmpegPath = _configuration.GetValue<string>("Thumbnail:FFmpegPath") ?? "ffmpeg";

            EnsureThumbnailDirectoryExists();
        }

        public async Task<string> GenerateThumbnailAsync(string videoFilePath)
        {
            var absoluteVideoPath = GetAbsolutePath(videoFilePath);

            if (!File.Exists(absoluteVideoPath))
            {
                _logger.LogError("Video file not found: {VideoPath}", absoluteVideoPath);
                throw new FileNotFoundException("Video file not found", absoluteVideoPath);
            }

            var thumbnailFileName = $"{Guid.NewGuid()}.jpg";
            var thumbnailFullPath = Path.Combine(_thumbnailDirectory, thumbnailFileName);
            var relativeThumbnailPath = Path.Combine(_relativeThumbnailPath, thumbnailFileName);

            try
            {
                await ExtractThumbnailWithFFmpegAsync(absoluteVideoPath, thumbnailFullPath);

                if (!File.Exists(thumbnailFullPath))
                {
                    _logger.LogWarning("FFmpeg did not generate thumbnail, creating placeholder");
                    await CreatePlaceholderThumbnailAsync(thumbnailFullPath);
                }

                _logger.LogInformation("Thumbnail generated: {ThumbnailPath}", relativeThumbnailPath);
                return relativeThumbnailPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate thumbnail for video: {VideoPath}", videoFilePath);

                try
                {
                    await CreatePlaceholderThumbnailAsync(thumbnailFullPath);
                    _logger.LogInformation("Placeholder thumbnail created: {ThumbnailPath}", relativeThumbnailPath);
                    return relativeThumbnailPath;
                }
                catch (Exception placeholderEx)
                {
                    _logger.LogError(placeholderEx, "Failed to create placeholder thumbnail");
                    throw new InvalidOperationException("Failed to generate thumbnail and placeholder creation failed", ex);
                }
            }
        }

        public string GetThumbnailDirectory()
        {
            return _thumbnailDirectory;
        }

        private async Task ExtractThumbnailWithFFmpegAsync(string videoPath, string outputPath)
        {
            var arguments = BuildFFmpegArguments(videoPath, outputPath);

            _logger.LogDebug("Executing FFmpeg: {FFmpegPath} {Arguments}", _ffmpegPath, arguments);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            var errorBuilder = new System.Text.StringBuilder();

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();

                var timeoutMilliseconds = _configuration.GetValue<int>("Thumbnail:TimeoutSeconds", 30) * 1000;
                var completed = await Task.Run(() => process.WaitForExit(timeoutMilliseconds));

                if (!completed)
                {
                    process.Kill(true);
                    throw new TimeoutException($"FFmpeg process timed out after {timeoutMilliseconds / 1000} seconds");
                }

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("FFmpeg exited with code {ExitCode}. Error: {Error}",
                        process.ExitCode, errorBuilder.ToString());
                }
            }
            catch (Exception ex) when (ex is not TimeoutException)
            {
                _logger.LogError(ex, "Error executing FFmpeg");
                throw;
            }
        }

        private string BuildFFmpegArguments(string videoPath, string outputPath)
        {
            return $"-i \"{videoPath}\" " +
                   $"-ss 00:00:01 " +
                   $"-vframes 1 " +
                   $"-vf \"scale={_thumbnailWidth}:{_thumbnailHeight}:force_original_aspect_ratio=decrease,pad={_thumbnailWidth}:{_thumbnailHeight}:(ow-iw)/2:(oh-ih)/2:black\" " +
                   $"-y \"{outputPath}\"";
        }

        private async Task CreatePlaceholderThumbnailAsync(string outputPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await CreatePlaceholderWithSystemDrawingAsync(outputPath);
            }
            else
            {
                await CreatePlaceholderWithFFmpegAsync(outputPath);
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private async Task CreatePlaceholderWithSystemDrawingAsync(string outputPath)
        {
            var placeholderBytes = GeneratePlaceholderImageBytes();
            await File.WriteAllBytesAsync(outputPath, placeholderBytes);
        }

        private async Task CreatePlaceholderWithFFmpegAsync(string outputPath)
        {
            var arguments = $"-f lavfi -i color=c=404040:s={_thumbnailWidth}x{_thumbnailHeight}:d=1 " +
                           $"-vframes 1 -y \"{outputPath}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            try
            {
                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 || !File.Exists(outputPath))
                {
                    throw new InvalidOperationException("FFmpeg failed to create placeholder image");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create placeholder with FFmpeg");
                throw;
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private byte[] GeneratePlaceholderImageBytes()
        {
            var width = _thumbnailWidth;
            var height = _thumbnailHeight;

            using var bitmap = new System.Drawing.Bitmap(width, height);
            using var graphics = System.Drawing.Graphics.FromImage(bitmap);

            graphics.Clear(System.Drawing.Color.FromArgb(64, 64, 64));

            using var font = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold);
            using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);

            var text = "No Preview";
            var textSize = graphics.MeasureString(text, font);
            var textX = (width - textSize.Width) / 2;
            var textY = (height - textSize.Height) / 2;

            graphics.DrawString(text, font, brush, textX, textY);

            using var playIconBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(128, 255, 255, 255));
            var trianglePoints = new System.Drawing.PointF[]
            {
                new(width / 2 - 15, height / 2 - 40),
                new(width / 2 - 15, height / 2 - 10),
                new(width / 2 + 15, height / 2 - 25)
            };
            graphics.FillPolygon(playIconBrush, trianglePoints);

            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            return memoryStream.ToArray();
        }

        private string GetAbsolutePath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }
            return Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        }

        private void EnsureThumbnailDirectoryExists()
        {
            if (!Directory.Exists(_thumbnailDirectory))
            {
                Directory.CreateDirectory(_thumbnailDirectory);
                _logger.LogInformation("Created thumbnail directory: {Directory}", _thumbnailDirectory);
            }
        }
    }
}
