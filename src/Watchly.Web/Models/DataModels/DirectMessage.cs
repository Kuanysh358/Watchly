using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.DataModels
{
    public class DirectMessage
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string RecipientId { get; set; } = string.Empty;

        public int? MovieId { get; set; }

        [MaxLength(2000)]
        public string? Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser Sender { get; set; } = null!;

        public ApplicationUser Recipient { get; set; } = null!;

        public Movie? Movie { get; set; }
    }
}
