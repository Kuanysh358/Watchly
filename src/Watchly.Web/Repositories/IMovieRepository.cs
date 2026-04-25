using Watchly.Web.Models.DataModels;

namespace Watchly.Web.Repositories
{
    public interface IMovieRepository
    {
        Task<IEnumerable<Movie>> GetAllAsync();
        Task<Movie?> GetByIdAsync(int id);
        Task<(IEnumerable<Movie> Movies, int TotalCount)> GetPagedAsync(
            string? searchQuery,
            int? genreId,
            int? yearFrom,
            int? yearTo,
            decimal? ratingFrom,
            decimal? ratingTo,
            string sortBy,
            int pageNumber,
            int pageSize);
        Task<Movie> CreateAsync(Movie movie);
        Task<Movie> UpdateAsync(Movie movie);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<IEnumerable<Genre>> GetAllGenresAsync();
    }
}
