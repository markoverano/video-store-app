using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using VideoStore.Backend.DTOs;
using VideoStore.Backend.Models;
using VideoStore.Backend.Repositories;

namespace VideoStore.Backend.Services
{
    public class VideoService : IVideoService
    {
        private readonly IVideoRepository _videoRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IThumbnailService _thumbnailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VideoService> _logger;

        public VideoService(
            IVideoRepository videoRepository,
            ICategoryRepository categoryRepository,
            IThumbnailService thumbnailService,
            IConfiguration configuration,
            ILogger<VideoService> logger)
        {
            _videoRepository = videoRepository;
            _categoryRepository = categoryRepository;
            _thumbnailService = thumbnailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IEnumerable<VideoDTO>> GetAllVideosAsync()
        {
            var videos = await _videoRepository.GetAllWithCategoriesAsync();
            return videos.Select(MapToDTO);
        }

        public async Task<VideoDTO?> GetVideoByIdAsync(int id)
        {
            var video = await _videoRepository.GetByIdWithCategoriesAsync(id);
            return video != null ? MapToDTO(video) : null;
        }

        public async Task<VideoUploadResponseDTO> UploadVideoAsync(VideoUploadDTO uploadDTO, IFormFile videoFile)
        {
            var uploadPath = _configuration.GetValue<string>("FileUpload:UploadPath") ?? "uploads/videos";
            var fullUploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);

            if (!Directory.Exists(fullUploadPath))
            {
                Directory.CreateDirectory(fullUploadPath);
            }

            var sanitizedFileName = SanitizeFileName(videoFile.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
            var filePath = Path.Combine(fullUploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream);
            }

            _logger.LogInformation("Video file saved: {FilePath}", filePath);

            var relativeVideoPath = Path.Combine(uploadPath, uniqueFileName);
            var thumbnailPath = await GenerateThumbnailSafelyAsync(relativeVideoPath);

            var video = new Video
            {
                Title = uploadDTO.Title,
                Description = uploadDTO.Description,
                FilePath = relativeVideoPath,
                ThumbnailPath = thumbnailPath,
                CreatedDate = DateTime.UtcNow
            };

            var createdVideo = await _videoRepository.CreateAsync(video);

            var categories = await GetOrCreateCategoriesAsync(uploadDTO.CategoryIds, uploadDTO.NewCategories);

            foreach (var category in categories)
            {
                createdVideo.VideoCategories.Add(new VideoCategory
                {
                    VideoId = createdVideo.Id,
                    CategoryId = category.Id
                });
            }

            await _videoRepository.UpdateAsync(createdVideo);

            _logger.LogInformation("Video record created with ID: {VideoId}", createdVideo.Id);

            return new VideoUploadResponseDTO
            {
                Id = createdVideo.Id,
                Title = createdVideo.Title,
                Message = "Video uploaded successfully",
                ThumbnailUrl = createdVideo.ThumbnailPath
            };
        }

        private async Task<string> GenerateThumbnailSafelyAsync(string videoFilePath)
        {
            try
            {
                var thumbnailPath = await _thumbnailService.GenerateThumbnailAsync(videoFilePath);
                _logger.LogInformation("Thumbnail generated successfully: {ThumbnailPath}", thumbnailPath);
                return thumbnailPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate thumbnail for video: {VideoPath}. Video will be saved without thumbnail.", videoFilePath);
                return string.Empty;
            }
        }

        private async Task<List<Category>> GetOrCreateCategoriesAsync(List<int> categoryIds, List<string> newCategoryNames)
        {
            var categories = new List<Category>();

            if (categoryIds.Any())
            {
                var existingCategories = await _categoryRepository.GetCategoriesByIdsAsync(categoryIds);
                categories.AddRange(existingCategories);
            }

            foreach (var categoryName in newCategoryNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var category = await _categoryRepository.GetOrCreateAsync(categoryName);
                if (!categories.Any(c => c.Id == category.Id))
                {
                    categories.Add(category);
                }
            }

            return categories;
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            sanitized = Regex.Replace(sanitized, @"\s+", "_");
            sanitized = Regex.Replace(sanitized, @"[^\w\.\-]", "");

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "video";
            }

            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);

            if (nameWithoutExtension.Length > 100)
            {
                nameWithoutExtension = nameWithoutExtension.Substring(0, 100);
            }

            return nameWithoutExtension + extension;
        }

        public async Task<(Stream? FileStream, string ContentType, string FileName)?> GetVideoStreamAsync(int id)
        {
            var video = await _videoRepository.GetByIdAsync(id);
            if (video == null)
            {
                return null;
            }

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), video.FilePath);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Video file not found: {FilePath}", fullPath);
                return null;
            }

            var contentType = GetContentType(video.FilePath);
            var fileName = Path.GetFileName(video.FilePath);
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return (fileStream, contentType, fileName);
        }

        private static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                _ => "application/octet-stream"
            };
        }

        private static VideoDTO MapToDTO(Video video)
        {
            return new VideoDTO
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                ThumbnailUrl = video.ThumbnailPath,
                CreatedDate = video.CreatedDate,
                Categories = video.VideoCategories
                    .Select(vc => new CategoryDTO
                    {
                        Id = vc.Category.Id,
                        Name = vc.Category.Name
                    })
                    .ToList()
            };
        }
    }
}
