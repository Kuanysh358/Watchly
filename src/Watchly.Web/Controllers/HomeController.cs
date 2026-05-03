using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using System.Security.Claims;
using Watchly.Web.Models.ViewModels;
using Watchly.Web.Services;

namespace Watchly.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMovieService _movieService;

        public HomeController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return View(await _movieService.GetHomeDataAsync(userId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string lang, string? returnUrl)
        {
            var selectedCulture = lang == "kz" ? "kk-KZ" : "ru-RU";
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selectedCulture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Index")! : returnUrl);
        }


        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            var filter = new MovieFilterViewModel { SearchQuery = query, PageSize = 10, PageNumber = 1 };
            var data = await _movieService.GetMoviesAsync(filter, null);
            return Json(data.Movies.Select(m => new { id = m.Id, title = m.Title, posterUrl = m.PosterUrl, rating = m.Rating, releaseYear = m.ReleaseYear }));
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
