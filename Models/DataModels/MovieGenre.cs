namespace Watchly.Web.Models.DataModels
{
    public class MovieGenre
    {
        public int MovieId { get; set; }

        public int GenreId { get; set; }

        public virtual Movie Movie { get; set; } = null!;

        public virtual Genre Genre { get; set; } = null!;
    }
}
