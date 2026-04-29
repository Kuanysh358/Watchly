namespace Watchly.Web.Models.ViewModels
{
    public class CommentViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int LikeCount { get; set; }
        public bool LikedByCurrentUser { get; set; }
    }
}
