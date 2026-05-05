namespace Watchly.Web.Models.DataModels
{
    public class Watchlist
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public int MovieId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationUser User { get; set; } = null!;

        public virtual Movie Movie { get; set; } = null!;
    }
}
