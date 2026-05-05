using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.DataModels
{
    public class DiscussionRoom
    {
        public int Id { get; set; }

        public int MovieId { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        [Required]
        public string FriendUserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Movie Movie { get; set; } = null!;
        public ApplicationUser CreatedByUser { get; set; } = null!;
        public ApplicationUser FriendUser { get; set; } = null!;
        public ICollection<DiscussionRoomMessage> Messages { get; set; } = new List<DiscussionRoomMessage>();
    }
}
