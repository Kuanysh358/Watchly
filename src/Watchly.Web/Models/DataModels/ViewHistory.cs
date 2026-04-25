namespace Watchly.Web.Models.DataModels
{
    public class ViewHistory
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public int MovieId { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        public int ViewCount { get; set; } = 1;

        public DateTime LastViewedAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationUser User { get; set; } = null!;

        public virtual Movie Movie { get; set; } = null!;
    }
}
