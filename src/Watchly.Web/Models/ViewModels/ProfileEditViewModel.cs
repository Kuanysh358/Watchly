using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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

        public IFormFile? AvatarFile { get; set; }

        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }

        public int TotalViewedMovies { get; set; }
        public double TotalWatchedHours { get; set; }
        public List<string> TopGenres { get; set; } = new();

        public IEnumerable<ViewHistoryItemViewModel> ViewHistoryItems { get; set; } = new List<ViewHistoryItemViewModel>();
        public IEnumerable<MovieCardViewModel> WatchlistItems { get; set; } = new List<MovieCardViewModel>();
        public IEnumerable<ResumeItemViewModel> ResumeItems { get; set; } = new List<ResumeItemViewModel>();
    }

    public class ViewHistoryItemViewModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public DateTime LastViewedAt { get; set; }
        public int ViewCount { get; set; }
    }

    public class ResumeItemViewModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public int LastPositionSeconds { get; set; }
        public string ResumeLabel => TimeSpan.FromSeconds(LastPositionSeconds).ToString(@"hh\:mm\:ss");
    }
}
