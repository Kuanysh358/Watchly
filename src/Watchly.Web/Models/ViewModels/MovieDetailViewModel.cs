namespace Watchly.Web.Models.ViewModels
{
    public class MovieDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int ReleaseYear { get; set; }
        public decimal Rating { get; set; }
        public string? PosterUrl { get; set; }
        public string? TrailerUrl { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Country { get; set; }
        public string? Director { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public bool IsInWatchlist { get; set; }
        public bool HasBeenViewed { get; set; }
    }
}
