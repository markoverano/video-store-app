namespace VideoStore.Backend.Services
{
    public interface IThumbnailService
    {
        Task<string> GenerateThumbnailAsync(string videoFilePath);
        string GetThumbnailDirectory();
    }
}
