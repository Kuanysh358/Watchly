using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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
