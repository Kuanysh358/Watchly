using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Services
{
    public interface IMovieService
    {
        Task<MovieListViewModel> GetMoviesAsync(MovieFilterViewModel filter, string? userId);
        Task<MovieDetailViewModel?> GetMovieDetailAsync(int id, string? userId);
        Task<MovieCreateEditViewModel> GetCreateEditViewModelAsync(int? id = null);
        Task<int> CreateMovieAsync(MovieCreateEditViewModel model);
        Task UpdateMovieAsync(MovieCreateEditViewModel model);
        Task DeleteMovieAsync(int id);
        Task ToggleWatchlistAsync(int movieId, string userId);
        Task RecordViewAsync(int movieId, string userId);
        Task<IEnumerable<MovieCardViewModel>> GetWatchlistAsync(string userId);
        Task<IEnumerable<MovieCardViewModel>> GetViewHistoryAsync(string userId);
    }
}
