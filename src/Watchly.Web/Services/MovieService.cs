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
            var (movies, totalCount) = await _movieRepository.GetPagedAsync(filter.SearchQuery, filter.GenreId, filter.YearFrom, filter.YearTo, filter.RatingFrom, filter.RatingTo, filter.SortBy, filter.PageNumber, filter.PageSize);
            var watchlistIds = userId == null
                ? new HashSet<int>()
                : (await _context.Watchlists.Where(w => w.UserId == userId).Select(w => w.MovieId).ToListAsync()).ToHashSet();

            var genres = await _movieRepository.GetAllGenresAsync();
            return new MovieListViewModel
            {
                Movies = movies.Select(MapCard).Select(c => { c.IsInWatchlist = watchlistIds.Contains(c.Id); return c; }).ToList(),
                Filter = filter,
                TotalCount = totalCount,
                AvailableGenres = genres.Select(g => new GenreDisplayViewModel { Id = g.Id, Name = g.Name }).ToList()
            };
        }

        public async Task<HomeIndexViewModel> GetHomeDataAsync(string? userId)
        {
            var all = await _movieRepository.GetAllAsync();
            return new HomeIndexViewModel
            {
                Popular = all.OrderByDescending(m => m.Rating).Take(8).Select(MapCard).ToList(),
                NewReleases = all.OrderByDescending(m => m.ReleaseYear).ThenByDescending(m => m.CreatedAt).Take(8).Select(MapCard).ToList(),
                ByGenres = all.SelectMany(m => m.MovieGenres.Select(g => new { m, g.Genre.Name }))
                    .GroupBy(x => x.Name)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .ToDictionary(g => g.Key, g => g.Select(x => MapCard(x.m)).Take(6).ToList())
            };
        }

        public async Task<MovieDetailViewModel?> GetMovieDetailAsync(int id, string? userId)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null) return null;

            var vm = new MovieDetailViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                ReleaseYear = movie.ReleaseYear,
                Rating = movie.Rating,
                PosterUrl = movie.PosterUrl,
                TrailerUrl = movie.TrailerUrl,
                VideoUrl = movie.VideoUrl,
                DurationMinutes = movie.DurationMinutes,
                Country = movie.Country,
                Director = movie.Director,
                Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                Comments = await _context.MovieComments.Where(c => c.MovieId == id).OrderByDescending(c => c.CreatedAt).Select(c => new CommentViewModel
                {
                    UserName = c.User.FullName ?? c.User.UserName ?? "User",
                    Text = c.Text,
                    CreatedAt = c.CreatedAt
                }).ToListAsync()
            };

            if (userId != null)
            {
                vm.IsInWatchlist = await _context.Watchlists.AnyAsync(w => w.UserId == userId && w.MovieId == id);
                var vh = await _context.ViewHistories.FirstOrDefaultAsync(v => v.UserId == userId && v.MovieId == id);
                vm.HasBeenViewed = vh != null;
                vm.ResumePositionSeconds = vh?.LastPositionSeconds ?? 0;
                vm.UserWatchedHours = (await _context.ViewHistories.Where(v => v.UserId == userId).SumAsync(v => v.WatchedMinutesTotal)) / 60.0;
                vm.UserRating = await _context.MovieRatings.Where(r => r.UserId == userId && r.MovieId == id).Select(r => r.Score).FirstOrDefaultAsync();
            }

            return vm;
        }

        public async Task<MovieCreateEditViewModel> GetCreateEditViewModelAsync(int? id = null)
        {
            var genreList = (await _movieRepository.GetAllGenresAsync()).Select(g => new GenreDisplayViewModel { Id = g.Id, Name = g.Name }).ToList();
            if (id == null) return new MovieCreateEditViewModel { AvailableGenres = genreList };

            var movie = await _movieRepository.GetByIdAsync(id.Value) ?? throw new KeyNotFoundException();
            return new MovieCreateEditViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                ReleaseYear = movie.ReleaseYear,
                Rating = movie.Rating,
                PosterUrl = movie.PosterUrl,
                TrailerUrl = movie.TrailerUrl,
                VideoUrl = movie.VideoUrl,
                TmdbId = movie.TmdbId,
                DurationMinutes = movie.DurationMinutes,
                Country = movie.Country,
                Director = movie.Director,
                SelectedGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList(),
                AvailableGenres = genreList
            };
        }

        public async Task<int?> CreateMovieAsync(MovieCreateEditViewModel model)
        {
            var duplicate = await _context.Movies.FirstOrDefaultAsync(m => (model.TmdbId.HasValue && m.TmdbId == model.TmdbId) || m.Title.ToLower() == model.Title.ToLower());
            if (duplicate != null) return null;

            var movie = new Movie
            {
                Title = model.Title,
                Description = model.Description,
                ReleaseYear = model.ReleaseYear,
                Rating = model.Rating,
                PosterUrl = model.PosterUrl,
                TrailerUrl = model.TrailerUrl,
                VideoUrl = model.VideoUrl,
                TmdbId = model.TmdbId,
                DurationMinutes = model.DurationMinutes,
                Country = model.Country,
                Director = model.Director
            };

            foreach (var genreId in model.SelectedGenreIds.Distinct()) movie.MovieGenres.Add(new MovieGenre { GenreId = genreId });
            var created = await _movieRepository.CreateAsync(movie);
            return created.Id;
        }

        public async Task UpdateMovieAsync(MovieCreateEditViewModel model)
        {
            if (!model.Id.HasValue) throw new ArgumentException("Id is required for update");
            var movie = await _movieRepository.GetByIdAsync(model.Id.Value) ?? throw new KeyNotFoundException();
            movie.Title = model.Title;
            movie.Description = model.Description;
            movie.ReleaseYear = model.ReleaseYear;
            movie.Rating = model.Rating;
            movie.PosterUrl = model.PosterUrl;
            movie.TrailerUrl = model.TrailerUrl;
            movie.VideoUrl = model.VideoUrl;
            movie.TmdbId = model.TmdbId;
            movie.DurationMinutes = model.DurationMinutes;
            movie.Country = model.Country;
            movie.Director = model.Director;

            _context.MovieGenres.RemoveRange(movie.MovieGenres);
            movie.MovieGenres = model.SelectedGenreIds.Distinct().Select(gid => new MovieGenre { MovieId = movie.Id, GenreId = gid }).ToList();
            await _movieRepository.UpdateAsync(movie);
        }

        public Task DeleteMovieAsync(int id) => _movieRepository.DeleteAsync(id);

        public async Task ToggleWatchlistAsync(int movieId, string userId)
        {
            var entry = await _context.Watchlists.FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId);
            if (entry != null) _context.Watchlists.Remove(entry);
            else _context.Watchlists.Add(new Watchlist { UserId = userId, MovieId = movieId });
            await _context.SaveChangesAsync();
        }

        public async Task RecordViewAsync(int movieId, string userId, int watchedSeconds = 0, int? resumePositionSeconds = null)
        {
            var entry = await _context.ViewHistories.FirstOrDefaultAsync(v => v.UserId == userId && v.MovieId == movieId);
            if (entry == null)
            {
                entry = new ViewHistory { UserId = userId, MovieId = movieId };
                _context.ViewHistories.Add(entry);
            }
            else entry.ViewCount++;

            entry.LastViewedAt = DateTime.UtcNow;
            entry.WatchedMinutesTotal += watchedSeconds / 60.0;
            if (resumePositionSeconds.HasValue) entry.LastPositionSeconds = Math.Max(0, resumePositionSeconds.Value);
            await _context.SaveChangesAsync();
        }

        public async Task SaveResumePositionAsync(int movieId, string userId, int positionSeconds)
            => await RecordViewAsync(movieId, userId, 0, positionSeconds);

        public async Task AddCommentAsync(int movieId, string userId, string text)
        {
            _context.MovieComments.Add(new MovieComment { MovieId = movieId, UserId = userId, Text = text.Trim() });
            await _context.SaveChangesAsync();
        }

        public async Task SetRatingAsync(int movieId, string userId, int score)
        {
            var entity = await _context.MovieRatings.FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);
            if (entity == null) _context.MovieRatings.Add(new MovieRating { MovieId = movieId, UserId = userId, Score = score });
            else entity.Score = score;

            await _context.SaveChangesAsync();
            var avg = await _context.MovieRatings.Where(r => r.MovieId == movieId).AverageAsync(r => (decimal?)r.Score) ?? 0m;
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie != null)
            {
                movie.Rating = Math.Round(avg, 1);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<MovieCardViewModel>> GetWatchlistAsync(string userId) => await _context.Watchlists.Where(w => w.UserId == userId)
            .Include(w => w.Movie).ThenInclude(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .OrderByDescending(w => w.AddedAt).Select(w => MapCard(w.Movie, true)).ToListAsync();

        public async Task<IEnumerable<MovieCardViewModel>> GetViewHistoryAsync(string userId) => await _context.ViewHistories.Where(v => v.UserId == userId)
            .Include(v => v.Movie).ThenInclude(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .OrderByDescending(v => v.LastViewedAt).Select(v => MapCard(v.Movie)).ToListAsync();

        private static MovieCardViewModel MapCard(Movie m, bool inWatchlist = false) => new()
        {
            Id = m.Id,
            Title = m.Title,
            PosterUrl = m.PosterUrl,
            ReleaseYear = m.ReleaseYear,
            Rating = m.Rating,
            Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
            IsInWatchlist = inWatchlist
        };
    }
}
