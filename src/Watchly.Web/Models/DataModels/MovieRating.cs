using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.DataModels
{
    public class MovieRating
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string UserId { get; set; } = null!;

        [Range(1, 10)]
        public int Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Movie Movie { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
