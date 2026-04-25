using Xunit;
using Microsoft.EntityFrameworkCore;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;
using Watchly.Web.Repositories;
using Watchly.Web.Services;

namespace Watchly.Tests;

public class MovieCrudTests
{
    private static ApplicationDbContext BuildContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_AssignsId()
    {
        using var ctx = BuildContext(nameof(CreateAsync_AssignsId));
        var repo = new MovieRepository(ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "A", ReleaseYear = 2020, Rating = 7 });
        Assert.True(movie.Id > 0);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMovie()
    {
        using var ctx = BuildContext(nameof(GetByIdAsync_ReturnsMovie));
        var repo = new MovieRepository(ctx);
        var created = await repo.CreateAsync(new Movie { Title = "B", ReleaseYear = 2021, Rating = 8 });
        var movie = await repo.GetByIdAsync(created.Id);
        Assert.NotNull(movie);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByTitle()
    {
        using var ctx = BuildContext(nameof(GetPagedAsync_FiltersByTitle));
        var repo = new MovieRepository(ctx);
        await repo.CreateAsync(new Movie { Title = "Matrix", ReleaseYear = 1999, Rating = 9 });
        await repo.CreateAsync(new Movie { Title = "Avatar", ReleaseYear = 2009, Rating = 8 });
        var result = await repo.GetPagedAsync("Matrix", null, null, null, null, null, "newest", 1, 10);
        Assert.Single(result.Movies);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByGenre()
    {
        using var ctx = BuildContext(nameof(GetPagedAsync_FiltersByGenre));
        ctx.Genres.Add(new Genre { Id = 99, Name = "Sci-Fi" });
        var movie = new Movie { Title = "Dune", ReleaseYear = 2021, Rating = 8 };
        movie.MovieGenres.Add(new MovieGenre { GenreId = 99 });
        ctx.Movies.Add(movie);
        await ctx.SaveChangesAsync();
        var repo = new MovieRepository(ctx);
        var result = await repo.GetPagedAsync(null, 99, null, null, null, null, "newest", 1, 10);
        Assert.Single(result.Movies);
    }

    [Fact]
    public async Task GetPagedAsync_SortByRating()
    {
        using var ctx = BuildContext(nameof(GetPagedAsync_SortByRating));
        var repo = new MovieRepository(ctx);
        await repo.CreateAsync(new Movie { Title = "Low", ReleaseYear = 2020, Rating = 6 });
        await repo.CreateAsync(new Movie { Title = "High", ReleaseYear = 2020, Rating = 9 });
        var result = await repo.GetPagedAsync(null, null, null, null, null, null, "rating", 1, 10);
        Assert.Equal("High", result.Movies.First().Title);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesMovie()
    {
        using var ctx = BuildContext(nameof(UpdateAsync_UpdatesMovie));
        var repo = new MovieRepository(ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "Old", ReleaseYear = 2020, Rating = 7 });
        movie.Title = "New";
        await repo.UpdateAsync(movie);
        var updated = await repo.GetByIdAsync(movie.Id);
        Assert.Equal("New", updated!.Title);
    }

    [Fact]
    public async Task DeleteAsync_RemovesMovie()
    {
        using var ctx = BuildContext(nameof(DeleteAsync_RemovesMovie));
        var repo = new MovieRepository(ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "X", ReleaseYear = 2020, Rating = 7 });
        await repo.DeleteAsync(movie.Id);
        Assert.False(await repo.ExistsAsync(movie.Id));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueForCreated()
    {
        using var ctx = BuildContext(nameof(ExistsAsync_ReturnsTrueForCreated));
        var repo = new MovieRepository(ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "Y", ReleaseYear = 2020, Rating = 7 });
        Assert.True(await repo.ExistsAsync(movie.Id));
    }

    [Fact]
    public async Task GetAllGenresAsync_ReturnsGenres()
    {
        using var ctx = BuildContext(nameof(GetAllGenresAsync_ReturnsGenres));
        ctx.Genres.Add(new Genre { Name = "Drama" });
        await ctx.SaveChangesAsync();
        var repo = new MovieRepository(ctx);
        var genres = await repo.GetAllGenresAsync();
        Assert.NotEmpty(genres);
    }

    [Fact]
    public async Task MovieService_CreateMovieAsync_Works()
    {
        using var ctx = BuildContext(nameof(MovieService_CreateMovieAsync_Works));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var id = await service.CreateMovieAsync(new MovieCreateEditViewModel { Title = "Z", ReleaseYear = 2020, Rating = 8 });
        Assert.True(id > 0);
    }

    [Fact]
    public async Task MovieService_UpdateMovieAsync_Works()
    {
        using var ctx = BuildContext(nameof(MovieService_UpdateMovieAsync_Works));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var id = await service.CreateMovieAsync(new MovieCreateEditViewModel { Title = "Before", ReleaseYear = 2020, Rating = 8 });
        await service.UpdateMovieAsync(new MovieCreateEditViewModel { Id = id, Title = "After", ReleaseYear = 2020, Rating = 8 });
        var updated = await repo.GetByIdAsync(id);
        Assert.Equal("After", updated!.Title);
    }

    [Fact]
    public async Task MovieService_DeleteMovieAsync_Works()
    {
        using var ctx = BuildContext(nameof(MovieService_DeleteMovieAsync_Works));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var id = await service.CreateMovieAsync(new MovieCreateEditViewModel { Title = "Del", ReleaseYear = 2020, Rating = 8 });
        await service.DeleteMovieAsync(id);
        Assert.False(await repo.ExistsAsync(id));
    }

    [Fact]
    public async Task MovieService_ToggleWatchlist_Adds()
    {
        using var ctx = BuildContext(nameof(MovieService_ToggleWatchlist_Adds));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "W", ReleaseYear = 2020, Rating = 7 });
        await service.ToggleWatchlistAsync(movie.Id, "u1");
        Assert.True(ctx.Watchlists.Any());
    }

    [Fact]
    public async Task MovieService_ToggleWatchlist_Removes()
    {
        using var ctx = BuildContext(nameof(MovieService_ToggleWatchlist_Removes));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "W", ReleaseYear = 2020, Rating = 7 });
        await service.ToggleWatchlistAsync(movie.Id, "u1");
        await service.ToggleWatchlistAsync(movie.Id, "u1");
        Assert.False(ctx.Watchlists.Any());
    }

    [Fact]
    public async Task MovieService_RecordView_CreatesHistory()
    {
        using var ctx = BuildContext(nameof(MovieService_RecordView_CreatesHistory));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "H", ReleaseYear = 2020, Rating = 7 });
        await service.RecordViewAsync(movie.Id, "u1");
        Assert.True(ctx.ViewHistories.Any());
    }

    [Fact]
    public async Task MovieService_RecordView_IncrementsCount()
    {
        using var ctx = BuildContext(nameof(MovieService_RecordView_IncrementsCount));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "H", ReleaseYear = 2020, Rating = 7 });
        await service.RecordViewAsync(movie.Id, "u1");
        await service.RecordViewAsync(movie.Id, "u1");
        Assert.Equal(2, ctx.ViewHistories.Single().ViewCount);
    }
}
