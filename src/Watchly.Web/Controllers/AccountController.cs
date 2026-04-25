using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User)) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            if (await _userManager.FindByEmailAsync(model.Email) != null || await _userManager.FindByNameAsync(model.Username) != null)
            {
                ModelState.AddModelError(string.Empty, "Аккаунт уже существует");
                return View(model);
            }

            var user = new ApplicationUser { UserName = model.Username, Email = model.Email, FullName = model.FullName };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            await _signInManager.SignInAsync(user, false);
            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User)) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var user = model.EmailOrUsername.Contains('@') ? await _userManager.FindByEmailAsync(model.EmailOrUsername) : await _userManager.FindByNameAsync(model.EmailOrUsername);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Неверные учётные данные");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Аккаунт заблокирован" : "Неверные учётные данные");
                return View(model);
            }

            return RedirectToLocal(returnUrl);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var history = await _db.ViewHistories.Where(v => v.UserId == user.Id).Include(v => v.Movie).ThenInclude(m => m.MovieGenres).ThenInclude(g => g.Genre).ToListAsync();
            var topGenres = history.SelectMany(v => v.Movie.MovieGenres.Select(g => g.Genre.Name)).GroupBy(x => x).OrderByDescending(g => g.Count()).Take(5).Select(g => g.Key).ToList();

            return View(new ProfileEditViewModel
            {
                FullName = user.FullName ?? user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                AvatarUrl = user.AvatarUrl,
                TotalViewedMovies = history.Select(h => h.MovieId).Distinct().Count(),
                TotalWatchedHours = history.Sum(h => h.WatchedMinutesTotal) / 60.0,
                TopGenres = topGenres
            });
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            if (!ModelState.IsValid) return View(model);

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.AvatarUrl = model.AvatarUrl;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError(string.Empty, "Укажите текущий пароль");
                    return View(model);
                }

                var passwordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    foreach (var e in passwordResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(model);
                }
            }

            TempData["SuccessMessage"] = "Профиль обновлён";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAdminCredentials(string email, string? currentPassword, string? newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            user.Email = email;
            user.UserName = email;
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join("; ", update.Errors.Select(e => e.Description));
                return RedirectToAction("Dashboard", "Admin");
            }

            if (!string.IsNullOrWhiteSpace(newPassword) && !string.IsNullOrWhiteSpace(currentPassword))
            {
                var pass = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!pass.Succeeded) TempData["ErrorMessage"] = string.Join("; ", pass.Errors.Select(e => e.Description));
                else TempData["SuccessMessage"] = "Учетные данные администратора обновлены";
            }

            return RedirectToAction("Dashboard", "Admin");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl) => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }
}
