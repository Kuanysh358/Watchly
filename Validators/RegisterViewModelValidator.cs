using FluentValidation;
using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Validators
{
    public class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
    {
        public RegisterViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обязателен")
                .EmailAddress().WithMessage("Некорректный email");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Имя пользователя обязательно")
                .MinimumLength(3).WithMessage("Имя должно содержать минимум 3 символа")
                .MaximumLength(30).WithMessage("Имя не должно превышать 30 символов")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Имя может содержать только буквы, цифры и символ подчеркивания");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен")
                .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов")
                .MaximumLength(100).WithMessage("Пароль не должен превышать 100 символов")
                .Matches(@"[A-Z]").WithMessage("Пароль должен содержать хотя бы одну заглавную букву")
                .Matches(@"[0-9]").WithMessage("Пароль должен содержать хотя бы одну цифру");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Подтверждение пароля обязательно")
                .Equal(x => x.Password).WithMessage("Пароли не совпадают");
        }
    }
}
