using FluentValidation;
using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Validators
{
    public class MovieCreateEditViewModelValidator : AbstractValidator<MovieCreateEditViewModel>
    {
        public MovieCreateEditViewModelValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Название обязательно")
                .MaximumLength(200).WithMessage("Название не должно превышать 200 символов");

            RuleFor(x => x.ReleaseYear)
                .InclusiveBetween(1900, 2100).WithMessage("Год должен быть между 1900 и 2100");

            RuleFor(x => x.Rating)
                .InclusiveBetween(0, 10).WithMessage("Рейтинг должен быть между 0 и 10");

            RuleFor(x => x.PosterUrl)
                .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Некорректный URL постера");

            RuleFor(x => x.TrailerUrl)
                .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Некорректный URL трейлера");

            RuleFor(x => x.DurationMinutes)
                .InclusiveBetween(1, 1000).When(x => x.DurationMinutes.HasValue)
                .WithMessage("Продолжительность должна быть между 1 и 1000 минут");

            RuleFor(x => x.Country)
                .MaximumLength(100).WithMessage("Страна не должна превышать 100 символов");

            RuleFor(x => x.Director)
                .MaximumLength(200).WithMessage("Режиссер не должен превышать 200 символов");
        }
    }
}
