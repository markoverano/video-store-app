using Microsoft.AspNetCore.Http;
using VideoStore.Backend.DTOs;

namespace VideoStore.Backend.Services
{
    public interface IVideoService
    {
        Task<IEnumerable<VideoDTO>> GetAllVideosAsync();
        Task<VideoDTO?> GetVideoByIdAsync(int id);
        Task<VideoUploadResponseDTO> UploadVideoAsync(VideoUploadDTO uploadDTO, IFormFile videoFile);
        Task<(Stream? FileStream, string ContentType, string FileName)?> GetVideoStreamAsync(int id);
    }
}
