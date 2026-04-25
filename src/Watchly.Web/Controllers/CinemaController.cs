using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Watchly.Web.Models.ViewModels;
using Watchly.Web.Services;

namespace Watchly.Web.Controllers
{
    public class CinemaController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly ILogger<CinemaController> _logger;

        public CinemaController(IMovieService movieService, ILogger<CinemaController> logger)
        {
            _movieService = movieService;
            _logger = logger;
        }

        public async Task<IActionResult> Index([FromQuery] MovieFilterViewModel filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var viewModel = await _movieService.GetMoviesAsync(filter, userId);
            return View(viewModel);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var movie = await _movieService.GetMovieDetailAsync(id, userId);

            if (movie == null)
                return NotFound();

            if (userId != null)
            {
                await _movieService.RecordViewAsync(id, userId);
            }

            return View(movie);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWatchlist(int movieId, string? returnUrl)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _movieService.ToggleWatchlistAsync(movieId, userId);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Detail), new { id = movieId });
        }

        [Authorize]
        public async Task<IActionResult> Watchlist()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var movies = await _movieService.GetWatchlistAsync(userId);
            return View(movies);
        }

        [Authorize]
        public async Task<IActionResult> ViewHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var movies = await _movieService.GetViewHistoryAsync(userId);
            return View(movies);
        }
    }
}
