namespace Watchly.Web.Services
{
    public interface IYouTubeService
    {
        /// <summary>
        /// Поиск видео трейлера на YouTube
        /// </summary>
        Task<string?> SearchTrailerAsync(string movieTitle);

        /// <summary>
        /// Получить URL встраиваемого видео
        /// </summary>
        string GetEmbedUrl(string videoId);

        /// <summary>
        /// Получить превью видео
        /// </summary>
        string GetThumbnailUrl(string videoId);
    }
}