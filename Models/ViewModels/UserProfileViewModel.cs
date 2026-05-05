namespace Watchly.Web.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCurrentUser { get; set; }

        public int ViewedMoviesCount { get; set; }
        public int FriendsCount { get; set; }
        public double AverageRating { get; set; }

        public IEnumerable<RecentViewViewModel> RecentViews { get; set; } = new List<RecentViewViewModel>();
        public IEnumerable<UserRatingViewModel> Ratings { get; set; } = new List<UserRatingViewModel>();

        public IEnumerable<FriendshipViewModel> Friends { get; set; } = new List<FriendshipViewModel>();
        public IEnumerable<FriendshipViewModel> IncomingRequests { get; set; } = new List<FriendshipViewModel>();
        public IEnumerable<FriendshipViewModel> OutgoingRequests { get; set; } = new List<FriendshipViewModel>();
        public FriendshipViewModel? Relationship { get; set; }

        public IEnumerable<MovieOptionViewModel> AvailableMovies { get; set; } = new List<MovieOptionViewModel>();

        public IEnumerable<DiscussionRoomHistoryItemViewModel> DiscussionRoomHistory { get; set; } = new List<DiscussionRoomHistoryItemViewModel>();
        public IEnumerable<DirectChatHistoryItemViewModel> DirectChatHistory { get; set; } = new List<DirectChatHistoryItemViewModel>();

        public ProfileEditViewModel? EditProfile { get; set; }
    }

    public class RecentViewViewModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public DateTime ViewedAt { get; set; }
    }

    public class UserRatingViewModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public int Score { get; set; }
    }

    public class DiscussionRoomHistoryItemViewModel
    {
        public int RoomId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string FriendName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int MessagesCount { get; set; }
        public bool IsClosed { get; set; }
    }

    public class DirectChatHistoryItemViewModel
    {
        public string FriendId { get; set; } = string.Empty;
        public string FriendName { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public int MessagesCount { get; set; }
        public string? LastText { get; set; }
    }

    public class MovieOptionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
    }
}
