using Microsoft.EntityFrameworkCore;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;

namespace Watchly.Web.Repositories
{
    public class MovieRepository : IMovieRepository
    {
        private readonly ApplicationDbContext _context;

        public MovieRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Movie>> GetAllAsync()
        {
            return await _context.Movies
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<Movie?> GetByIdAsync(int id)
        {
            return await _context.Movies
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<(IEnumerable<Movie> Movies, int TotalCount)> GetPagedAsync(
            string? searchQuery,
            int? genreId,
            int? yearFrom,
            int? yearTo,
            decimal? ratingFrom,
            decimal? ratingTo,
            string sortBy,
            int pageNumber,
            int pageSize)
        {
            var query = _context.Movies
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
                query = query.Where(m => m.Title.Contains(searchQuery));

            if (genreId.HasValue)
                query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));

            if (yearFrom.HasValue)
                query = query.Where(m => m.ReleaseYear >= yearFrom.Value);

            if (yearTo.HasValue)
                query = query.Where(m => m.ReleaseYear <= yearTo.Value);

            if (ratingFrom.HasValue)
                query = query.Where(m => m.Rating >= ratingFrom.Value);

            if (ratingTo.HasValue)
                query = query.Where(m => m.Rating <= ratingTo.Value);

            var totalCount = await query.CountAsync();

            query = sortBy switch
            {
                "rating" => query.OrderByDescending(m => m.Rating),
                "title" => query.OrderBy(m => m.Title),
                _ => query.OrderByDescending(m => m.CreatedAt)
            };

            var movies = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (movies, totalCount);
        }

        public async Task<Movie> CreateAsync(Movie movie)
        {
            movie.CreatedAt = DateTime.UtcNow;
            movie.UpdatedAt = DateTime.UtcNow;
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();
            return movie;
        }

        public async Task<Movie> UpdateAsync(Movie movie)
        {
            movie.UpdatedAt = DateTime.UtcNow;
            _context.Movies.Update(movie);
            await _context.SaveChangesAsync();
            return movie;
        }

        public async Task DeleteAsync(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Movies.AnyAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Genre>> GetAllGenresAsync()
        {
            return await _context.Genres.OrderBy(g => g.Name).ToListAsync();
        }
    }
}
