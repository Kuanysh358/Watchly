using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.DataModels
{
    public class Movie
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public int ReleaseYear { get; set; }

        [Range(0, 10)]
        public decimal Rating { get; set; }

        public string? PosterUrl { get; set; }

        public string? TrailerUrl { get; set; }

        public string? VideoUrl { get; set; }

        public int? TmdbId { get; set; }

        public int? DurationMinutes { get; set; }

        public string? Country { get; set; }

        public string? Director { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();

        public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();

        public virtual ICollection<ViewHistory> ViewHistories { get; set; } = new List<ViewHistory>();

        public virtual ICollection<MovieComment> Comments { get; set; } = new List<MovieComment>();

        public virtual ICollection<MovieRating> Ratings { get; set; } = new List<MovieRating>();
    }
}
