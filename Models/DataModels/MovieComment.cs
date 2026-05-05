using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.DataModels
{
    public class MovieComment
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string UserId { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string Text { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? ParentCommentId { get; set; }

        public virtual Movie Movie { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
        public virtual ICollection<CommentDislike> Dislikes { get; set; } = new List<CommentDislike>();
    }
}
