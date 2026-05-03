using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Watchly.Web.Data;
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
        private readonly ApplicationDbContext _db;

        public AdminController(IMovieService movieService, ILogger<AdminController> logger, ITmdbService tmdbService, IYouTubeService youtubeService, ApplicationDbContext db)
        {
            _movieService = movieService;
            _tmdbService = tmdbService;
            _youtubeService = youtubeService;
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Dashboard([FromQuery] MovieFilterViewModel filter)
            => View(await _movieService.GetMoviesAsync(filter, null));

        [HttpGet]
        public async Task<IActionResult> Create() => View(await _movieService.GetCreateEditViewModelAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieCreateEditViewModel model)
        {
            if (!ModelState.IsValid) return await RenderInvalidModel(model);
            var createdId = await _movieService.CreateMovieAsync(model);
            TempData["SuccessMessage"] = createdId.HasValue ? "Фильм успешно добавлен" : "Дубликат найден: фильм уже есть в базе.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try { return View("Create", await _movieService.GetCreateEditViewModelAsync(id)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieCreateEditViewModel model)
        {
            if (!ModelState.IsValid) return await RenderInvalidModel(model, true);
            await _movieService.UpdateMovieAsync(model);
            TempData["SuccessMessage"] = "Фильм обновлён";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _movieService.DeleteMovieAsync(id);
            TempData["SuccessMessage"] = "Фильм удалён";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> SearchTmdb(string query)
            => string.IsNullOrWhiteSpace(query) ? Json(new List<object>()) : Json((await _tmdbService.SearchMoviesAsync(query)).Take(10));

        [HttpGet]
        public async Task<IActionResult> GetTmdbMovie(int tmdbId)
        {
            var movie = await _tmdbService.GetMovieDetailAsync(tmdbId);
            if (movie == null) return NotFound();
            var videoId = await _youtubeService.SearchTrailerAsync($"{movie.Title} {movie.ReleaseYear} trailer official");
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
                trailerUrl = string.IsNullOrWhiteSpace(videoId) ? null : _youtubeService.GetEmbedUrl(videoId),
                videoId
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoImportTmdb([FromBody] AutoImportRequest? request)
        {
            var excludedTmdbIds = (request?.ExcludedTmdbIds ?? new()).ToHashSet();
            var batchSize = Math.Clamp(request?.BatchSize ?? 5, 1, 10);
            var popularMovies = (await _tmdbService.GetPopularMoviesAsync()).Where(m => !excludedTmdbIds.Contains(m.Id)).OrderBy(_ => Guid.NewGuid()).Take(batchSize).ToList();

            var dbGenres = await _db.Genres.ToListAsync();
            var imported = new List<object>();
            var importedTmdbIds = new List<int>();

            foreach (var popularMovie in popularMovies)
            {
                var detail = await _tmdbService.GetMovieDetailAsync(popularMovie.Id);
                if (detail == null) continue;

                var selectedGenreIds = new List<int>();
                foreach (var genreName in detail.Genres.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var genre = dbGenres.FirstOrDefault(g => g.Name.Equals(genreName, StringComparison.OrdinalIgnoreCase));
                    if (genre == null)
                    {
                        genre = new Models.DataModels.Genre { Name = genreName };
                        _db.Genres.Add(genre);
                        await _db.SaveChangesAsync();
                        dbGenres.Add(genre);
                    }
                    selectedGenreIds.Add(genre.Id);
                }

                var videoId = await _youtubeService.SearchTrailerAsync($"{detail.Title} {detail.ReleaseYear} trailer official");
                var createModel = new MovieCreateEditViewModel
                {
                    Title = detail.Title,
                    Description = detail.Overview,
                    ReleaseYear = detail.ReleaseYear ?? DateTime.UtcNow.Year,
                    Rating = detail.VoteAverage,
                    PosterUrl = string.IsNullOrWhiteSpace(detail.PosterPath) ? null : $"https://image.tmdb.org/t/p/original{detail.PosterPath}",
                    TrailerUrl = string.IsNullOrWhiteSpace(videoId) ? null : _youtubeService.GetEmbedUrl(videoId),
                    TmdbId = detail.Id,
                    DurationMinutes = detail.Runtime,
                    Country = detail.Country,
                    Director = detail.Director,
                    SelectedGenreIds = selectedGenreIds
                };

                var createdId = await _movieService.CreateMovieAsync(createModel);
                if (!createdId.HasValue) continue;

                importedTmdbIds.Add(detail.Id);
                imported.Add(new { title = detail.Title, releaseYear = detail.ReleaseYear, rating = detail.VoteAverage, trailerUrl = createModel.TrailerUrl, videoId });
            }

            return Json(new { importedCount = imported.Count, importedTmdbIds, movies = imported, message = imported.Count > 0 ? $"Импортировано фильмов: {imported.Count}" : "Нет новых фильмов (дубликаты исключены)." });
        }

        [HttpGet]
        public async Task<IActionResult> SearchYouTubeTrailer(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return NotFound();
            var videoId = await _youtubeService.SearchTrailerAsync(query);
            return string.IsNullOrWhiteSpace(videoId) ? NotFound() : Json(new { trailerUrl = _youtubeService.GetEmbedUrl(videoId) });
        }

        private async Task<IActionResult> RenderInvalidModel(MovieCreateEditViewModel model, bool edit = false)
        {
            model.AvailableGenres = (await _movieService.GetCreateEditViewModelAsync()).AvailableGenres;
            return View(edit ? "Create" : "Create", model);
        }

        public class AutoImportRequest
        {
            public List<int> ExcludedTmdbIds { get; set; } = new();
            public int BatchSize { get; set; } = 5;
        }
    }
}
