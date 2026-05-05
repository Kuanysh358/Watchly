using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Watchly.Web.Models.ViewModels
{
    public class ProfileEditViewModel
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Url]
        public string? AvatarUrl { get; set; }

        public IFormFile? AvatarFile { get; set; }

        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }

    }
}
