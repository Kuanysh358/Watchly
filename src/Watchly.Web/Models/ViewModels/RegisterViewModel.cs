using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [Display(Name = "Имя пользователя")]
        [StringLength(30, MinimumLength = 1, ErrorMessage = "Имя должно быть от 1 до 30 символов")]
        public string Username { get; set; } = null!;

        [Display(Name = "Полное имя")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 100 символов")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
