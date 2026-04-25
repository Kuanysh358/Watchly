namespace Watchly.Web.Models.ViewModels
{
    public class MovieListViewModel
    {
        public IEnumerable<MovieCardViewModel> Movies { get; set; } = new List<MovieCardViewModel>();

        public MovieFilterViewModel Filter { get; set; } = new MovieFilterViewModel();

        public int TotalCount { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Filter.PageSize);

        public int CurrentPage => Filter.PageNumber;

        public IEnumerable<GenreDisplayViewModel> AvailableGenres { get; set; } = new List<GenreDisplayViewModel>();
    }

    public class MovieCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? PosterUrl { get; set; }
        public int ReleaseYear { get; set; }
        public decimal Rating { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public bool IsInWatchlist { get; set; }
    }

    public class GenreDisplayViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
