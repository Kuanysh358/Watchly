using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.ViewModels
{
    public class MovieFilterViewModel
    {
        [Display(Name = "Поиск")]
        public string? SearchQuery { get; set; }

        [Display(Name = "Жанр")]
        public int? GenreId { get; set; }

        [Display(Name = "От года")]
        public int? YearFrom { get; set; }

        [Display(Name = "До года")]
        public int? YearTo { get; set; }

        [Display(Name = "Минимальный рейтинг")]
        [Range(0, 10, ErrorMessage = "Рейтинг должен быть между 0 и 10")]
        public decimal? RatingFrom { get; set; }

        [Display(Name = "Максимальный рейтинг")]
        [Range(0, 10, ErrorMessage = "Рейтинг должен быть между 0 и 10")]
        public decimal? RatingTo { get; set; }

        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 12;

        [Display(Name = "Сортировка")]
        public string SortBy { get; set; } = "newest";
    }
}
