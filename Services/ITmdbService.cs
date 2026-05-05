namespace Watchly.Web.Services
{
    public interface ITmdbService
    {
        /// <summary>
        /// Поиск фильма в TMDB
        /// </summary>
        Task<IEnumerable<TmdbMovieDto>> SearchMoviesAsync(string query);

        /// <summary>
        /// Получить информацию о фильме по ID TMDB
        /// </summary>
        Task<TmdbMovieDetailDto?> GetMovieDetailAsync(int tmdbId);

        /// <summary>
        /// Получить популярные фильмы (page — номер страницы TMDB, 1–500)
        /// </summary>
        Task<IEnumerable<TmdbMovieDto>> GetPopularMoviesAsync(int page = 1);

        /// <summary>
        /// Найти YouTube trailer key из TMDB videos
        /// </summary>
        Task<string?> GetTrailerVideoIdAsync(int tmdbId);
    
}

public class TmdbMovieDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? PosterPath { get; set; }
        public decimal VoteAverage { get; set; }
        public int? ReleaseYear { get; set; }
        public string? Overview { get; set; }

    }

    public class TmdbMovieDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? PosterPath { get; set; }
        public decimal VoteAverage { get; set; }
        public int? ReleaseYear { get; set; }
        public string? Overview { get; set; }
        public int? Runtime { get; set; }
        public string? Country { get; set; }
        public string? Director { get; set; }
        public List<string> Genres { get; set; } = new();
    }


}
