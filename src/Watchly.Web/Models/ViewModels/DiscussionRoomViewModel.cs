namespace Watchly.Web.Models.ViewModels
{
    public class DiscussionRoomViewModel
    {
        public int RoomId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? MoviePosterUrl { get; set; }
        public string FriendName { get; set; } = string.Empty;
        public string? FriendAvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsPending { get; set; }
        public bool IsClosed { get; set; }
        public bool IsInitiator { get; set; }
        public List<DiscussionRoomMessageViewModel> Messages { get; set; } = new();
    }

    public class DiscussionRoomMessageViewModel
    {
        public bool IsOwn { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatarUrl { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
