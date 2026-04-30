using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Services
{
    public interface IMovieService
    {
        Task<MovieListViewModel> GetMoviesAsync(MovieFilterViewModel filter, string? userId);
        Task<MovieDetailViewModel?> GetMovieDetailAsync(int id, string? userId, string? commentSort = "newest");
        Task<MovieCreateEditViewModel> GetCreateEditViewModelAsync(int? id = null);
        Task<int?> CreateMovieAsync(MovieCreateEditViewModel model);
        Task UpdateMovieAsync(MovieCreateEditViewModel model);
        Task DeleteMovieAsync(int id);
        Task ToggleWatchlistAsync(int movieId, string userId);
        Task RecordViewAsync(int movieId, string userId, int watchedSeconds = 0, int? resumePositionSeconds = null);
        Task SaveResumePositionAsync(int movieId, string userId, int positionSeconds);
        Task<IEnumerable<MovieCardViewModel>> GetWatchlistAsync(string userId);
        Task<IEnumerable<MovieCardViewModel>> GetViewHistoryAsync(string userId);
        Task<HomeIndexViewModel> GetHomeDataAsync(string? userId);
        Task AddCommentAsync(int movieId, string userId, string text);
        Task DeleteCommentAsync(int commentId);
        Task ToggleCommentLikeAsync(int commentId, string userId);
        Task SetRatingAsync(int movieId, string userId, int score);
        Task<ProfileEditViewModel> GetProfileDataAsync(string userId);
    }
}
