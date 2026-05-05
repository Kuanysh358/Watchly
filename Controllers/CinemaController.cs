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

        public CinemaController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        public async Task<IActionResult> Index([FromQuery] MovieFilterViewModel filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return View(await _movieService.GetMoviesAsync(filter, userId));
        }

        public async Task<IActionResult> Detail(int id, string? commentSort = "newest")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var movie = await _movieService.GetMovieDetailAsync(id, userId, commentSort);
            if (movie == null) return NotFound();
            if (userId != null) await _movieService.RecordViewAsync(id, userId);
            ViewBag.CommentSort = commentSort ?? "newest";
            return View(movie);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int movieId, string text, int? parentCommentId)
        {
            if (!string.IsNullOrWhiteSpace(text))
                await _movieService.AddCommentAsync(movieId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, text, parentCommentId);
            return RedirectToAction(nameof(Detail), new { id = movieId });
        }

        [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId, int movieId)
        {
            await _movieService.DeleteCommentAsync(commentId);
            return RedirectToAction(nameof(Detail), new { id = movieId });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LikeComment(int commentId)
        {
            await _movieService.ToggleCommentLikeAsync(commentId, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok();
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DislikeComment(int commentId)
        {
            await _movieService.ToggleCommentDislikeAsync(commentId, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok();
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRating(int movieId, int score)
        {
            score = Math.Clamp(score, 1, 10);
            await _movieService.SetRatingAsync(movieId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, score);
            return RedirectToAction(nameof(Detail), new { id = movieId });
        }

        [Authorize, HttpPost]
        public async Task<IActionResult> SaveResume(int movieId, int positionSeconds)
        {
            await _movieService.SaveResumePositionAsync(movieId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, positionSeconds);
            return Ok();
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWatchlist(int movieId, string? returnUrl)
        {
            await _movieService.ToggleWatchlistAsync(movieId, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Detail), new { id = movieId });
        }

        [Authorize]
        public async Task<IActionResult> Watchlist() => View(await _movieService.GetWatchlistAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

        [Authorize]
        public async Task<IActionResult> ViewHistory() => View(await _movieService.GetViewHistoryAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
    }
}
