namespace Watchly.Web.Models.ViewModels
{
    public class ChatViewModel
    {
        public string FriendId { get; set; } = string.Empty;
        public string FriendName { get; set; } = string.Empty;
        public string? FriendAvatarUrl { get; set; }
        public IEnumerable<MovieOptionViewModel> AvailableMovies { get; set; } = new List<MovieOptionViewModel>();
        public IEnumerable<ChatMessageViewModel> Messages { get; set; } = new List<ChatMessageViewModel>();
    }

    public class ChatMessageViewModel
    {
        public bool IsOwn { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? Text { get; set; }
        public int? MovieId { get; set; }
        public string? MovieTitle { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
