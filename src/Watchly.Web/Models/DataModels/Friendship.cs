using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.DataModels
{
    public enum FriendshipStatus
    {
        None = -1,
        Pending = 0,
        Accepted = 1,
        Declined = 2
    }

    public class Friendship
    {
        [Required]
        public string UserId1 { get; set; } = string.Empty;

        [Required]
        public string UserId2 { get; set; } = string.Empty;

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User1 { get; set; } = null!;

        public ApplicationUser User2 { get; set; } = null!;
    }
}
