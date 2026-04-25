using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.ViewModels
{
    public class ProfileEditViewModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Url]
        public string? AvatarUrl { get; set; }

        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }

        public int TotalViewedMovies { get; set; }
        public double TotalWatchedHours { get; set; }
        public List<string> TopGenres { get; set; } = new();
    }
}
