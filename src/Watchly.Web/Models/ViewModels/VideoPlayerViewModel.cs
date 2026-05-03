namespace Watchly.Web.Models.ViewModels
{
    public class VideoPlayerViewModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;
        public bool IsIframe { get; set; }
        public int ResumePositionSeconds { get; set; }
    }
}
