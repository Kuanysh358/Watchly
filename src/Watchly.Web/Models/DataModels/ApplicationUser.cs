using Microsoft.AspNetCore.Identity;

namespace Watchly.Web.Models.DataModels
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? AvatarUrl { get; set; }

        public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();

        public virtual ICollection<ViewHistory> ViewHistories { get; set; } = new List<ViewHistory>();

        public virtual ICollection<MovieComment> Comments { get; set; } = new List<MovieComment>();

        public virtual ICollection<MovieRating> Ratings { get; set; } = new List<MovieRating>();
    }
}
