using System.ComponentModel.DataAnnotations;

namespace Watchly.Web.Models.DataModels
{
    public class Genre
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    }
}
