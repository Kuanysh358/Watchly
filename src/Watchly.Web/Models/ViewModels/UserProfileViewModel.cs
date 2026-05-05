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

    public class MovieOptionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
