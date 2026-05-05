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
                Movies = movies.Select(m => MapCard(m)).Select(c => { c.IsInWatchlist = watchlistIds.Contains(c.Id); return c; }).ToList(),
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
                Popular = all.OrderByDescending(m => m.Rating).ThenByDescending(m => m.CreatedAt).Take(8).Select(m => MapCard(m)).ToList(),
                NewReleases = all.OrderByDescending(m => m.ReleaseYear).ThenByDescending(m => m.CreatedAt).Where(m => m.ReleaseYear >= DateTime.UtcNow.Year - 1).Take(8).Select(m => MapCard(m)).ToList(),
                Recommended = all.OrderByDescending(m => m.Rating).ThenByDescending(m => m.ReleaseYear).Take(8).Select(m => MapCard(m)).ToList(),
                ByGenres = all.SelectMany(m => m.MovieGenres.Select(g => new { m, g.Genre.Name }))
                    .GroupBy(x => x.Name)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .ToDictionary(g => g.Key, g => g.Select(x => MapCard(x.m)).Take(6).ToList())
            };
        }

        public async Task<MovieDetailViewModel?> GetMovieDetailAsync(int id, string? userId, string? commentSort = "newest")
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null) return null;

            var commentsQuery = _context.MovieComments
                .Where(c => c.MovieId == id)
                .Include(c => c.User)
                .Include(c => c.Likes)
                .Include(c => c.Dislikes);

            IQueryable<MovieComment> orderedComments = commentSort == "popular"
                ? commentsQuery.OrderByDescending(c => c.Likes.Count)
                : commentsQuery.OrderByDescending(c => c.CreatedAt);

            var commentList = await orderedComments.Select(c => new CommentViewModel
            {
                Id = c.Id,
                UserId = c.UserId,
                UserName = c.User.FullName ?? c.User.UserName ?? "User",
                Text = c.Text,
                CreatedAt = c.CreatedAt,
                LikeCount = c.Likes.Count,
                DislikeCount = c.Dislikes.Count,
                LikedByCurrentUser = userId != null && c.Likes.Any(l => l.UserId == userId),
                DislikedByCurrentUser = userId != null && c.Dislikes.Any(l => l.UserId == userId),
                ParentCommentId = c.ParentCommentId
            }).ToListAsync();

            var vm = new MovieDetailViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                ReleaseYear = movie.ReleaseYear,
                Rating = movie.Rating,
                PosterUrl = movie.PosterUrl,
                BackdropUrl = movie.PosterUrl,
                TrailerUrl = movie.TrailerUrl,
                VideoUrl = movie.VideoUrl,
                TmdbId = movie.TmdbId,
                DurationMinutes = movie.DurationMinutes,
                Country = movie.Country,
                Director = movie.Director,
                Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                Comments = commentList
            };

            if (userId != null)
            {
                vm.IsInWatchlist = await _context.Watchlists.AnyAsync(w => w.UserId == userId && w.MovieId == id);
                var vh = await _context.ViewHistories.FirstOrDefaultAsync(v => v.UserId == userId && v.MovieId == id);
                vm.HasBeenViewed = vh != null;
                vm.ResumePositionSeconds = vh?.LastPositionSeconds ?? 0;
                vm.UserWatchedHours = (await _context.ViewHistories.Where(v => v.UserId == userId).SumAsync(v => v.WatchedMinutesTotal)) / 60.0;
                vm.UserRating = await _context.MovieRatings.Where(r => r.UserId == userId && r.MovieId == id).Select(r => r.Score).FirstOrDefaultAsync();

                vm.ShareFriends = await _context.Friendships
                    .Where(f => f.Status == FriendshipStatus.Accepted && (f.UserId1 == userId || f.UserId2 == userId))
                    .Join(_context.Users,
                        f => f.UserId1 == userId ? f.UserId2 : f.UserId1,
                        u => u.Id,
                        (f, u) => new FriendshipViewModel
                        {
                            UserId = u.Id,
                            UserName = u.UserName ?? "User",
                            FullName = u.FullName,
                            AvatarUrl = u.AvatarUrl,
                            Status = FriendshipStatus.Accepted
                        })
                    .OrderBy(f => f.UserName)
                    .ToListAsync();
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


        private static string? NormalizeVideoUrl(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            if (Uri.TryCreate(input, UriKind.Absolute, out _)) return input;
            if (input.StartsWith('/') && !input.StartsWith("//")) return input;

            var normalized = input.Replace('\\', '/');
            var marker = "wwwroot/videos/";
            var idx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var tail = normalized[(idx + marker.Length)..].TrimStart('/');
                if (!string.IsNullOrWhiteSpace(tail)) return $"/videos/{tail}";
            }

            return input;
        }

        public async Task<int?> CreateMovieAsync(MovieCreateEditViewModel model)
        {
            var duplicate = model.TmdbId.HasValue
                ? await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == model.TmdbId)
                : await _context.Movies.FirstOrDefaultAsync(m => m.Title.ToLower() == model.Title.ToLower() && m.ReleaseYear == model.ReleaseYear);
            if (duplicate != null) return null;

            var movie = new Movie
            {
                Title = model.Title,
                Description = model.Description,
                ReleaseYear = model.ReleaseYear,
                Rating = model.Rating,
                PosterUrl = model.PosterUrl,
                TrailerUrl = model.TrailerUrl,
                VideoUrl = NormalizeVideoUrl(model.VideoUrl),
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
            movie.VideoUrl = NormalizeVideoUrl(model.VideoUrl);
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

        public async Task AddCommentAsync(int movieId, string userId, string text, int? parentCommentId = null)
        {
            _context.MovieComments.Add(new MovieComment { MovieId = movieId, UserId = userId, Text = text.Trim(), ParentCommentId = parentCommentId });
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCommentAsync(int commentId)
        {
            var comment = await _context.MovieComments.FindAsync(commentId);
            if (comment != null)
            {
                _context.MovieComments.Remove(comment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ToggleCommentLikeAsync(int commentId, string userId)
        {
            var existing = await _context.CommentLikes.FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);
            if (existing != null) _context.CommentLikes.Remove(existing);
            else
            {
                _context.CommentLikes.Add(new CommentLike { CommentId = commentId, UserId = userId });
                var dislike = await _context.CommentDislikes.FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);
                if (dislike != null) _context.CommentDislikes.Remove(dislike);
            }
            await _context.SaveChangesAsync();
        }

        public async Task ToggleCommentDislikeAsync(int commentId, string userId)
        {
            var existing = await _context.CommentDislikes.FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);
            if (existing != null) _context.CommentDislikes.Remove(existing);
            else
            {
                _context.CommentDislikes.Add(new CommentDislike { CommentId = commentId, UserId = userId });
                var like = await _context.CommentLikes.FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);
                if (like != null) _context.CommentLikes.Remove(like);
            }
            await _context.SaveChangesAsync();
        }

        public async Task SetRatingAsync(int movieId, string userId, int score)
        {
            var entity = await _context.MovieRatings.FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);
            if (entity == null) _context.MovieRatings.Add(new MovieRating { MovieId = movieId, UserId = userId, Score = score });
            else entity.Score = score;

            await _context.SaveChangesAsync();
            var ratingQuery = _context.MovieRatings.Where(r => r.MovieId == movieId);
            var avg = await ratingQuery.AverageAsync(r => (decimal?)r.Score) ?? 0m;
            var ratingCount = await ratingQuery.CountAsync();
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie != null)
            {
                if (ratingCount == 1)
                {
                    movie.Rating = Math.Round((movie.Rating + avg) / 2m, 1);
                }
                else
                {
                    movie.Rating = Math.Round(avg, 1);
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<MovieCardViewModel>> GetWatchlistAsync(string userId) => await _context.Watchlists.Where(w => w.UserId == userId)
            .Include(w => w.Movie).ThenInclude(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .OrderByDescending(w => w.AddedAt).Select(w => MapCard(w.Movie, true)).ToListAsync();

        public async Task<IEnumerable<MovieCardViewModel>> GetViewHistoryAsync(string userId) => await _context.ViewHistories.Where(v => v.UserId == userId)
            .Include(v => v.Movie).ThenInclude(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .OrderByDescending(v => v.LastViewedAt).Select(v => MapCard(v.Movie)).ToListAsync();

        private static string? ToHighResTmdbImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return imageUrl;
            return imageUrl.Replace("/t/p/w500", "/t/p/original", StringComparison.OrdinalIgnoreCase);
        }

        private static MovieCardViewModel MapCard(Movie m, bool inWatchlist = false) => new()
        {
            Id = m.Id,
            Title = m.Title,
            PosterUrl = ToHighResTmdbImage(m.PosterUrl),
            ReleaseYear = m.ReleaseYear,
            Rating = m.Rating,
            Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
            IsInWatchlist = inWatchlist
        };
    }
}
