using Watchly.Web.Models.DataModels;

namespace Watchly.Web.Models.ViewModels
{
    public class FriendshipViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public FriendshipStatus Status { get; set; }
        public bool IsIncomingRequest { get; set; }
        public bool IsOutgoingRequest { get; set; }
        public bool IsFriend => Status == FriendshipStatus.Accepted;
    }
}
