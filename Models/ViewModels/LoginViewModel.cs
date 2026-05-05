using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email или имя пользователя обязательны")]
        [Display(Name = "Email или имя пользователя")]
        public string EmailOrUsername { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = null!;

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }
}
