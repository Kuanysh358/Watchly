namespace Watchly.Web.Models.DataModels
{
    public class CommentLike
    {
        public int Id { get; set; }
        public int CommentId { get; set; }
        public string UserId { get; set; } = null!;

        public virtual MovieComment Comment { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
