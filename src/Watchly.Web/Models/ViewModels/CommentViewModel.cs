namespace Watchly.Web.Models.ViewModels
{
    public class CommentViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
