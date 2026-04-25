using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.ViewModels
{
    public class MovieCreateEditViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 200 символов")]
        [Display(Name = "Название")]
        public string Title { get; set; } = null!;

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Год выпуска обязателен")]
        [Range(1900, 2100, ErrorMessage = "Год должен быть между 1900 и 2100")]
        [Display(Name = "Год выпуска")]
        public int ReleaseYear { get; set; }

        [Required(ErrorMessage = "Рейтинг обязателен")]
        [Range(0, 10, ErrorMessage = "Рейтинг должен быть между 0 и 10")]
        [Display(Name = "Рейтинг IMDB")]
        public decimal Rating { get; set; }

        [Url(ErrorMessage = "Некорректный URL")]
        [Display(Name = "URL постера")]
        public string? PosterUrl { get; set; }

        [Url(ErrorMessage = "Некорректный URL")]
        [Display(Name = "URL трейлера")]
        public string? TrailerUrl { get; set; }

        [Display(Name = "TMDB ID")]
        public int? TmdbId { get; set; }

        [Range(1, 1000, ErrorMessage = "Продолжительность должна быть между 1 и 1000 минут")]
        [Display(Name = "Продолжительность (минут)")]
        public int? DurationMinutes { get; set; }

        [StringLength(100, ErrorMessage = "Максимум 100 символов")]
        [Display(Name = "Страна")]
        public string? Country { get; set; }

        [StringLength(200, ErrorMessage = "Максимум 200 символов")]
        [Display(Name = "Режиссер")]
        public string? Director { get; set; }

        [Display(Name = "Жанры")]
        public List<int> SelectedGenreIds { get; set; } = new List<int>();

        public List<GenreDisplayViewModel> AvailableGenres { get; set; } = new List<GenreDisplayViewModel>();
    }
}
