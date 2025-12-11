using Microsoft.AspNetCore.Mvc;
using VideoStore.Backend.DTOs;
using VideoStore.Backend.Services;

namespace VideoStore.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IVideoService _videoService;
        private readonly IFileValidationService _fileValidationService;
        private readonly ILogger<VideoController> _logger;

        public VideoController(
            IVideoService videoService,
            IFileValidationService fileValidationService,
            ILogger<VideoController> logger)
        {
            _videoService = videoService;
            _fileValidationService = fileValidationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VideoDTO>>> GetAllVideos()
        {
            var videos = await _videoService.GetAllVideosAsync();
            return Ok(videos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VideoDTO>> GetVideo(int id)
        {
            var video = await _videoService.GetVideoByIdAsync(id);

            if (video == null)
            {
                return NotFound();
            }

            return Ok(video);
        }

        [HttpPost]
        [RequestSizeLimit(104857600)]
        public async Task<ActionResult<VideoUploadResponseDTO>> UploadVideo(
            [FromForm] VideoUploadDTO uploadDTO,
            IFormFile videoFile)
        {
            if (videoFile == null)
            {
                _logger.LogWarning("Upload attempt with no file");
                return BadRequest(new { message = "No video file provided" });
            }

            if (Request.Form.Files.Count > 1)
            {
                _logger.LogWarning("Upload attempt with multiple files");
                return BadRequest(new { message = "Only one file can be uploaded at a time" });
            }

            if (!_fileValidationService.IsValidFileType(videoFile.FileName))
            {
                _logger.LogWarning("Upload attempt with invalid file type: {FileName}", videoFile.FileName);
                return StatusCode(415, new
                {
                    message = $"Invalid file type. Allowed types: {_fileValidationService.GetAllowedExtensions()}"
                });
            }

            if (!_fileValidationService.ValidateMimeType(videoFile.ContentType))
            {
                _logger.LogWarning("Upload attempt with invalid MIME type: {ContentType}", videoFile.ContentType);
                return StatusCode(415, new
                {
                    message = $"Invalid file type. Allowed types: {_fileValidationService.GetAllowedExtensions()}"
                });
            }

            if (!_fileValidationService.IsValidFileSize(videoFile.Length))
            {
                _logger.LogWarning("Upload attempt with file exceeding size limit: {FileSize} bytes", videoFile.Length);
                return StatusCode(413, new
                {
                    message = $"File size exceeds the maximum allowed size of {_fileValidationService.GetMaxFileSizeFormatted()}"
                });
            }

            try
            {
                var response = await _videoService.UploadVideoAsync(uploadDTO, videoFile);
                _logger.LogInformation("Video uploaded successfully: {VideoId}", response.Id);
                return CreatedAtAction(nameof(GetVideo), new { id = response.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video");
                return StatusCode(500, new { message = "An error occurred while uploading the video" });
            }
        }

        [HttpGet("{id}/stream")]
        public async Task<IActionResult> StreamVideo(int id)
        {
            var streamResult = await _videoService.GetVideoStreamAsync(id);

            if (streamResult == null)
            {
                return NotFound(new { message = "Video not found" });
            }

            var (fileStream, contentType, fileName) = streamResult.Value;

            if (fileStream == null)
            {
                return NotFound(new { message = "Video file not found" });
            }

            Response.Headers.Append("Accept-Ranges", "bytes");

            return File(fileStream, contentType, enableRangeProcessing: true);
        }
    }
}
