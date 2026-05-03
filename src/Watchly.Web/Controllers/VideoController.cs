using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Watchly.Web.Services;
using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Controllers
{
    public class VideoController : Controller
    {
        private readonly IMovieService _movieService;
        public VideoController(IMovieService movieService) => _movieService = movieService;

        [Authorize]
        public async Task<IActionResult> Play(int movieId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var movie = await _movieService.GetMovieDetailAsync(movieId, userId);
            if (movie == null) return NotFound();

            if (string.IsNullOrWhiteSpace(movie.VideoUrl) && !movie.TmdbId.HasValue)
            {
                return BadRequest("Для фильма не задан VideoUrl или TMDB ID.");
            }

            var url = !string.IsNullOrWhiteSpace(movie.VideoUrl)
                ? movie.VideoUrl!
                : $"https://vidsrc.to/embed/movie/{movie.TmdbId!.Value}";

            var isIframe = IsEmbedUrl(url);

            return View(new VideoPlayerViewModel
            {
                MovieId = movie.Id,
                Title = movie.Title,
                StreamUrl = url,
                IsIframe = isIframe,
                ResumePositionSeconds = movie.ResumePositionSeconds
            });
        }

        private static bool IsEmbedUrl(string url) =>
            url.Contains("vidsrc", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("embed", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("youtube.com/embed", StringComparison.OrdinalIgnoreCase);
    }
}
