using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;
using Watchly.Web.Services;

namespace Watchly.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;
        private readonly IMovieService _movieService;
        private readonly IWebHostEnvironment _env;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext db, IMovieService movieService, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _movieService = movieService;
            _env = env;
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

            await _userManager.AddToRoleAsync(user, "User");
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
            var vm = await _movieService.GetProfileDataAsync(user.Id);
            return View(vm);
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            // Remove AvatarFile from model state validation since it's optional
            ModelState.Remove(nameof(model.AvatarFile));
            ModelState.Remove(nameof(model.AvatarUrl));
            if (!ModelState.IsValid)
            {
                var freshVm = await _movieService.GetProfileDataAsync(user.Id);
                freshVm.FullName = model.FullName;
                freshVm.Email = model.Email;
                return View(freshVm);
            }

            user.FullName = model.FullName;

            // Update email only if changed
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var e in setEmailResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    var freshVm = await _movieService.GetProfileDataAsync(user.Id);
                    freshVm.FullName = model.FullName;
                    freshVm.Email = model.Email;
                    return View(freshVm);
                }
            }

            // Handle avatar file upload
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(model.AvatarFile.ContentType.ToLower()))
                {
                    ModelState.AddModelError(nameof(model.AvatarFile), "Допустимы только изображения (jpg, png, gif, webp)");
                    var freshVm = await _movieService.GetProfileDataAsync(user.Id);
                    return View(freshVm);
                }

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(model.AvatarFile.FileName);
                var fileName = $"{user.Id}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await model.AvatarFile.CopyToAsync(stream);
                user.AvatarUrl = $"/uploads/avatars/{fileName}";
            }
            else if (!string.IsNullOrWhiteSpace(model.AvatarUrl))
            {
                user.AvatarUrl = model.AvatarUrl;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                var freshVm = await _movieService.GetProfileDataAsync(user.Id);
                return View(freshVm);
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError(string.Empty, "Укажите текущий пароль");
                    var freshVm = await _movieService.GetProfileDataAsync(user.Id);
                    return View(freshVm);
                }

                var passwordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    foreach (var e in passwordResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    var freshVm = await _movieService.GetProfileDataAsync(user.Id);
                    return View(freshVm);
                }

                await _signInManager.RefreshSignInAsync(user);
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

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmail = await _userManager.SetEmailAsync(user, email);
                if (!setEmail.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join("; ", setEmail.Errors.Select(e => e.Description));
                    return RedirectToAction("Dashboard", "Admin");
                }
            }

            if (!string.IsNullOrWhiteSpace(newPassword) && !string.IsNullOrWhiteSpace(currentPassword))
            {
                var pass = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!pass.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join("; ", pass.Errors.Select(e => e.Description));
                    return RedirectToAction("Dashboard", "Admin");
                }
                await _signInManager.RefreshSignInAsync(user);
            }

            TempData["SuccessMessage"] = "Учетные данные администратора обновлены";
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
