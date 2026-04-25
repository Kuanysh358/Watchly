using Microsoft.EntityFrameworkCore;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;
using Watchly.Web.Repositories;

namespace Watchly.Web.Services
{
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly ApplicationDbContext _context;

        public MovieService(IMovieRepository movieRepository, ApplicationDbContext context)
        {
            _movieRepository = movieRepository;
            _context = context;
        }

        public async Task<MovieListViewModel> GetMoviesAsync(MovieFilterViewModel filter, string? userId)
        {
            var (movies, totalCount) = await _movieRepository.GetPagedAsync(
                filter.SearchQuery,
                filter.GenreId,
                filter.YearFrom,
                filter.YearTo,
                filter.RatingFrom,
                filter.RatingTo,
                filter.SortBy,
                filter.PageNumber,
                filter.PageSize);

            var watchlistIds = new HashSet<int>();
            if (userId != null)
            {
                watchlistIds = (await _context.Watchlists
                    .Where(w => w.UserId == userId)
                    .Select(w => w.MovieId)
                    .ToListAsync()).ToHashSet();
            }

            var movieCards = movies.Select(m => new MovieCardViewModel
            {
                Id = m.Id,
                Title = m.Title,
                PosterUrl = m.PosterUrl,
                ReleaseYear = m.ReleaseYear,
                Rating = m.Rating,
                Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                IsInWatchlist = watchlistIds.Contains(m.Id)
            });

            var genres = await _movieRepository.GetAllGenresAsync();

            return new MovieListViewModel
            {
                Movies = movieCards,
                Filter = filter,
                TotalCount = totalCount,
                AvailableGenres = genres.Select(g => new GenreDisplayViewModel { Id = g.Id, Name = g.Name })
            };
        }

        public async Task<MovieDetailViewModel?> GetMovieDetailAsync(int id, string? userId)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null) return null;

            var isInWatchlist = false;
            var hasBeenViewed = false;

            if (userId != null)
            {
                isInWatchlist = await _context.Watchlists
                    .AnyAsync(w => w.UserId == userId && w.MovieId == id);
                hasBeenViewed = await _context.ViewHistories
                    .AnyAsync(v => v.UserId == userId && v.MovieId == id);
            }

            return new MovieDetailViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                ReleaseYear = movie.ReleaseYear,
                Rating = movie.Rating,
                PosterUrl = movie.PosterUrl,
                TrailerUrl = movie.TrailerUrl,
                DurationMinutes = movie.DurationMinutes,
                Country = movie.Country,
                Director = movie.Director,
                Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                IsInWatchlist = isInWatchlist,
                HasBeenViewed = hasBeenViewed
            };
        }

        public async Task<MovieCreateEditViewModel> GetCreateEditViewModelAsync(int? id = null)
        {
            var genres = await _movieRepository.GetAllGenresAsync();
            var genreList = genres.Select(g => new GenreDisplayViewModel { Id = g.Id, Name = g.Name }).ToList();

            if (id == null)
            {
                return new MovieCreateEditViewModel { AvailableGenres = genreList };
            }

            var movie = await _movieRepository.GetByIdAsync(id.Value);
            if (movie == null) throw new KeyNotFoundException($"Movie {id} not found");

            return new MovieCreateEditViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                ReleaseYear = movie.ReleaseYear,
                Rating = movie.Rating,
                PosterUrl = movie.PosterUrl,
                TrailerUrl = movie.TrailerUrl,
                TmdbId = movie.TmdbId,
                DurationMinutes = movie.DurationMinutes,
                Country = movie.Country,
                Director = movie.Director,
                SelectedGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList(),
                AvailableGenres = genreList
            };
        }

        public async Task<int> CreateMovieAsync(MovieCreateEditViewModel model)
        {
            var movie = new Movie
            {
                Title = model.Title,
                Description = model.Description,
                ReleaseYear = model.ReleaseYear,
                Rating = model.Rating,
                PosterUrl = model.PosterUrl,
                TrailerUrl = model.TrailerUrl,
                TmdbId = model.TmdbId,
                DurationMinutes = model.DurationMinutes,
                Country = model.Country,
                Director = model.Director
            };

            foreach (var genreId in model.SelectedGenreIds)
            {
                movie.MovieGenres.Add(new MovieGenre { GenreId = genreId });
            }

            var created = await _movieRepository.CreateAsync(movie);
            return created.Id;
        }

        public async Task UpdateMovieAsync(MovieCreateEditViewModel model)
        {
            if (!model.Id.HasValue) throw new ArgumentException("Id is required for update");

            var movie = await _movieRepository.GetByIdAsync(model.Id.Value);
            if (movie == null) throw new KeyNotFoundException($"Movie {model.Id} not found");

            movie.Title = model.Title;
            movie.Description = model.Description;
            movie.ReleaseYear = model.ReleaseYear;
            movie.Rating = model.Rating;
            movie.PosterUrl = model.PosterUrl;
            movie.TrailerUrl = model.TrailerUrl;
            movie.TmdbId = model.TmdbId;
            movie.DurationMinutes = model.DurationMinutes;
            movie.Country = model.Country;
            movie.Director = model.Director;

            _context.MovieGenres.RemoveRange(movie.MovieGenres);
            movie.MovieGenres = model.SelectedGenreIds.Select(gid => new MovieGenre { MovieId = movie.Id, GenreId = gid }).ToList();

            await _movieRepository.UpdateAsync(movie);
        }

        public async Task DeleteMovieAsync(int id)
        {
            await _movieRepository.DeleteAsync(id);
        }

        public async Task ToggleWatchlistAsync(int movieId, string userId)
        {
            var entry = await _context.Watchlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId);

            if (entry != null)
            {
                _context.Watchlists.Remove(entry);
            }
            else
            {
                _context.Watchlists.Add(new Watchlist { UserId = userId, MovieId = movieId });
            }

            await _context.SaveChangesAsync();
        }

        public async Task RecordViewAsync(int movieId, string userId)
        {
            var entry = await _context.ViewHistories
                .FirstOrDefaultAsync(v => v.UserId == userId && v.MovieId == movieId);

            if (entry != null)
            {
                entry.ViewCount++;
                entry.LastViewedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ViewHistories.Add(new ViewHistory
                {
                    UserId = userId,
                    MovieId = movieId
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<MovieCardViewModel>> GetWatchlistAsync(string userId)
        {
            return await _context.Watchlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Movie)
                    .ThenInclude(m => m.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => new MovieCardViewModel
                {
                    Id = w.Movie.Id,
                    Title = w.Movie.Title,
                    PosterUrl = w.Movie.PosterUrl,
                    ReleaseYear = w.Movie.ReleaseYear,
                    Rating = w.Movie.Rating,
                    Genres = w.Movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    IsInWatchlist = true
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<MovieCardViewModel>> GetViewHistoryAsync(string userId)
        {
            return await _context.ViewHistories
                .Where(v => v.UserId == userId)
                .Include(v => v.Movie)
                    .ThenInclude(m => m.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                .OrderByDescending(v => v.LastViewedAt)
                .Select(v => new MovieCardViewModel
                {
                    Id = v.Movie.Id,
                    Title = v.Movie.Title,
                    PosterUrl = v.Movie.PosterUrl,
                    ReleaseYear = v.Movie.ReleaseYear,
                    Rating = v.Movie.Rating,
                    Genres = v.Movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    IsInWatchlist = false
                })
                .ToListAsync();
        }
    }
}
