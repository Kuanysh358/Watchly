using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;
using Watchly.Web.Services;

namespace Watchly.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly ILogger<AdminController> _logger;
        private readonly ITmdbService _tmdbService;
        private readonly IYouTubeService _youtubeService;

        public AdminController(IMovieService movieService, ILogger<AdminController> logger, ITmdbService tmdbService, IYouTubeService youtubeService)
        {
            _movieService = movieService;
            _tmdbService = tmdbService;
            _youtubeService = youtubeService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard([FromQuery] MovieFilterViewModel filter)
        {
            var viewModel = await _movieService.GetMoviesAsync(filter, null);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = await _movieService.GetCreateEditViewModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var genres = (await _movieService.GetCreateEditViewModelAsync()).AvailableGenres;
                model.AvailableGenres = genres;
                return View(model);
            }

            await _movieService.CreateMovieAsync(model);
            TempData["Success"] = "Фильм успешно добавлен";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var model = await _movieService.GetCreateEditViewModelAsync(id);
                return View("Create", model);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var genres = (await _movieService.GetCreateEditViewModelAsync()).AvailableGenres;
                model.AvailableGenres = genres;
                return View("Create", model);
            }

            try
            {
                await _movieService.UpdateMovieAsync(model);
                TempData["Success"] = "Фильм успешно обновлён";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _movieService.DeleteMovieAsync(id);
            TempData["Success"] = "Фильм успешно удалён";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> SearchTmdb(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return Json(new List<object>());

                var movies = await _tmdbService.SearchMoviesAsync(query);
                return Json(movies.Take(10));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching TMDB");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTmdbMovie(int tmdbId)
        {
            try
            {
                var movie = await _tmdbService.GetMovieDetailAsync(tmdbId);
                if (movie == null)
                    return NotFound();

                var trailerQuery = movie.ReleaseYear.HasValue
                    ? $"{movie.Title} {movie.ReleaseYear} trailer official"
                    : $"{movie.Title} trailer official";
                var videoId = await _youtubeService.SearchTrailerAsync(trailerQuery);
                var trailerUrl = string.IsNullOrWhiteSpace(videoId) ? null : _youtubeService.GetEmbedUrl(videoId);

                return Json(new
                {
                    id = movie.Id,
                    title = movie.Title,
                    posterPath = movie.PosterPath,
                    voteAverage = movie.VoteAverage,
                    releaseYear = movie.ReleaseYear,
                    overview = movie.Overview,
                    runtime = movie.Runtime,
                    country = movie.Country,
                    director = movie.Director,
                    genres = movie.Genres,
                    trailerUrl,
                    videoId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TMDB movie detail");
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoImportTmdb([FromBody] AutoImportRequest? request)
        {
            try
            {
                var excludedTmdbIds = (request?.ExcludedTmdbIds ?? new List<int>()).ToHashSet();
                var batchSize = Math.Clamp(request?.BatchSize ?? 5, 1, 10);

                var popularMovies = (await _tmdbService.GetPopularMoviesAsync())
                    .Where(m => !excludedTmdbIds.Contains(m.Id))
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(batchSize)
                    .ToList();

                if (!popularMovies.Any())
                {
                    return Json(new
                    {
                        importedCount = 0,
                        importedTmdbIds = new List<int>(),
                        movies = new List<object>(),
                        message = "Нет новых фильмов для импорта."
                    });
                }

                var imported = new List<object>();
                var importedTmdbIds = new List<int>();

                foreach (var popularMovie in popularMovies)
                {
                    var detail = await _tmdbService.GetMovieDetailAsync(popularMovie.Id);
                    if (detail == null)
                        continue;

                    var trailerQuery = detail.ReleaseYear.HasValue
                        ? $"{detail.Title} {detail.ReleaseYear} trailer official"
                        : $"{detail.Title} trailer official";

                    var videoId = await _youtubeService.SearchTrailerAsync(trailerQuery);
                    var trailerUrl = string.IsNullOrWhiteSpace(videoId) ? null : _youtubeService.GetEmbedUrl(videoId);

                    var createModel = new MovieCreateEditViewModel
                    {
                        Title = detail.Title,
                        Description = detail.Overview,
                        ReleaseYear = detail.ReleaseYear ?? DateTime.UtcNow.Year,
                        Rating = detail.VoteAverage,
                        PosterUrl = string.IsNullOrWhiteSpace(detail.PosterPath)
                            ? null
                            : $"https://image.tmdb.org/t/p/w500{detail.PosterPath}",
                        TrailerUrl = trailerUrl,
                        TmdbId = detail.Id,
                        DurationMinutes = detail.Runtime,
                        Country = detail.Country,
                        Director = detail.Director,
                        SelectedGenreIds = new List<int>()
                    };

                    await _movieService.CreateMovieAsync(createModel);

                    importedTmdbIds.Add(detail.Id);
                    imported.Add(new
                    {
                        title = detail.Title,
                        releaseYear = detail.ReleaseYear,
                        rating = detail.VoteAverage,
                        trailerUrl,
                        videoId
                    });
                }

                return Json(new
                {
                    importedCount = imported.Count,
                    importedTmdbIds,
                    movies = imported,
                    message = imported.Count > 0
                        ? $"Импортировано фильмов: {imported.Count}"
                        : "Не удалось импортировать фильмы."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto importing TMDB movies");
                return StatusCode(500, new { message = "Ошибка авто-импорта TMDB" });
            }
        }

        [HttpGet("import-genres")]
        public async Task<IActionResult> ImportGenres()
        {
            try
            {
                // ========== ПОЛУЧИТЬ ЖАНРЫ С TMDB ==========

                var tmdbGenres = await _tmdbService.GetGenresAsync();

                if (tmdbGenres.Count == 0)
                {
                    return BadRequest(new { error = "Не удалось получить жанры" });
                }

                int added = 0;
                int updated = 0;

                // ========== ДОБАВИТЬ ИЛИ ОБНОВИТЬ ЖАНРЫ ==========

                foreach (var tmdbGenre in tmdbGenres)
                {
                    // ========== ПРОВЕРИТЬ, УЖЕ ЛИ ЕСТЬ ==========

                    var existing = await dbContext.Genres
                        .FirstOrDefaultAsync(g => g.Id == tmdbGenre.Id);

                    if (existing == null)
                    {
                        // ========== НОВЫЙ ЖАНР ==========

                        var newGenre = new Genre
                        {
                            Id = tmdbGenre.Id,
                            Name = tmdbGenre.Name
                        };

                        _dbContext.Genres.Add(newGenre);
                        added++;
                    }
                    else if (existing.Name != tmdbGenre.Name)
                    {
                        // ========== ОБНОВИТЬ НАЗВАНИЕ ==========

                        existing.Name = tmdbGenre.Name;
                        _dbContext.Genres.Update(existing);
                        updated++;
                    }
                }

                // ========== СОХРАНИТЬ ==========

                await TmdbService.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Добавлено: {added}, Обновлено: {updated}",
                    added,
                    updated
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchYouTubeTrailer(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return NotFound();

                var videoId = await _youtubeService.SearchTrailerAsync(query);
                if (videoId == null)
                    return NotFound();

                var embedUrl = _youtubeService.GetEmbedUrl(videoId);
                return Json(new { trailerUrl = embedUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching YouTube trailer");
                return StatusCode(500);
            }
        }

        public class AutoImportRequest
        {
            public List<int> ExcludedTmdbIds { get; set; } = new();
            public int BatchSize { get; set; } = 5;
        }
    }
}
