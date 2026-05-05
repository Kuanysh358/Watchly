using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.DataModels
{
    public class DiscussionRoomMessage
    {
        public int Id { get; set; }
        public int RoomId { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Text { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsSystemMessage { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DiscussionRoom Room { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;
    }
}
