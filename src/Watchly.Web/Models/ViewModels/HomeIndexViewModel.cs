namespace Watchly.Web.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public IEnumerable<MovieCardViewModel> Popular { get; set; } = Enumerable.Empty<MovieCardViewModel>();
        public IEnumerable<MovieCardViewModel> NewReleases { get; set; } = Enumerable.Empty<MovieCardViewModel>();
        public IEnumerable<MovieCardViewModel> Recommended { get; set; } = Enumerable.Empty<MovieCardViewModel>();
        public IDictionary<string, List<MovieCardViewModel>> ByGenres { get; set; } = new Dictionary<string, List<MovieCardViewModel>>();
    }
}
