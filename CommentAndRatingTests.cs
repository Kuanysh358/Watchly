using Xunit;
using Microsoft.EntityFrameworkCore;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Repositories;
using Watchly.Web.Services;

namespace Watchly.Tests;

public class CommentAndRatingTests
{
    private static ApplicationDbContext BuildContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddCommentAsync_AddsComment()
    {
        using var ctx = BuildContext(nameof(AddCommentAsync_AddsComment));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "A", ReleaseYear = 2020, Rating = 7 });

        await service.AddCommentAsync(movie.Id, "user1", "Great film!");

        Assert.Single(ctx.MovieComments);
        Assert.Equal("Great film!", ctx.MovieComments.Single().Text);
    }

    [Fact]
    public async Task DeleteCommentAsync_RemovesComment()
    {
        using var ctx = BuildContext(nameof(DeleteCommentAsync_RemovesComment));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "B", ReleaseYear = 2020, Rating = 7 });

        await service.AddCommentAsync(movie.Id, "user1", "Test comment");
        var commentId = ctx.MovieComments.Single().Id;

        await service.DeleteCommentAsync(commentId);

        Assert.Empty(ctx.MovieComments);
    }

    [Fact]
    public async Task ToggleCommentLikeAsync_AddsLike()
    {
        using var ctx = BuildContext(nameof(ToggleCommentLikeAsync_AddsLike));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "C", ReleaseYear = 2020, Rating = 7 });

        await service.AddCommentAsync(movie.Id, "user1", "Good");
        var commentId = ctx.MovieComments.Single().Id;

        await service.ToggleCommentLikeAsync(commentId, "user2");

        Assert.Single(ctx.CommentLikes);
    }

    [Fact]
    public async Task ToggleCommentLikeAsync_RemovesExistingLike()
    {
        using var ctx = BuildContext(nameof(ToggleCommentLikeAsync_RemovesExistingLike));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "D", ReleaseYear = 2020, Rating = 7 });

        await service.AddCommentAsync(movie.Id, "user1", "Good");
        var commentId = ctx.MovieComments.Single().Id;

        await service.ToggleCommentLikeAsync(commentId, "user2");
        await service.ToggleCommentLikeAsync(commentId, "user2");

        Assert.Empty(ctx.CommentLikes);
    }

    [Fact]
    public async Task SetRatingAsync_UpdatesMovieRating()
    {
        using var ctx = BuildContext(nameof(SetRatingAsync_UpdatesMovieRating));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "E", ReleaseYear = 2020, Rating = 0 });

        await service.SetRatingAsync(movie.Id, "user1", 8);
        await service.SetRatingAsync(movie.Id, "user2", 6);

        var updated = await repo.GetByIdAsync(movie.Id);
        Assert.Equal(7.0m, updated!.Rating);
    }


    [Fact]
    public async Task SetRatingAsync_FirstUserRating_AveragesWithBaseMovieRating()
    {
        using var ctx = BuildContext(nameof(SetRatingAsync_FirstUserRating_AveragesWithBaseMovieRating));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "G", ReleaseYear = 2020, Rating = 6 });

        await service.SetRatingAsync(movie.Id, "user1", 8);

        var updated = await repo.GetByIdAsync(movie.Id);
        Assert.Equal(7.0m, updated!.Rating);
    }

    [Fact]
    public async Task SaveResumePositionAsync_StoresPosition()
    {
        using var ctx = BuildContext(nameof(SaveResumePositionAsync_StoresPosition));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "F", ReleaseYear = 2020, Rating = 7 });

        await service.SaveResumePositionAsync(movie.Id, "user1", 1234);

        var vh = ctx.ViewHistories.Single();
        Assert.Equal(1234, vh.LastPositionSeconds);
    }

    [Fact]
    public async Task ToggleCommentDislikeAsync_AddsDislike()
    {
        using var ctx = BuildContext(nameof(ToggleCommentDislikeAsync_AddsDislike));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "H", ReleaseYear = 2020, Rating = 7 });

        await service.AddCommentAsync(movie.Id, "user1", "Good");
        var commentId = ctx.MovieComments.Single().Id;

        await service.ToggleCommentDislikeAsync(commentId, "user2");

        Assert.Single(ctx.CommentDislikes);
    }

    [Fact]
    public async Task AddCommentAsync_WithParentComment_SetsParentId()
    {
        using var ctx = BuildContext(nameof(AddCommentAsync_WithParentComment_SetsParentId));
        var repo = new MovieRepository(ctx);
        var service = new MovieService(repo, ctx);
        var movie = await repo.CreateAsync(new Movie { Title = "I", ReleaseYear = 2020, Rating = 7 });

        await service.AddCommentAsync(movie.Id, "user1", "Root");
        var parent = ctx.MovieComments.Single();
        await service.AddCommentAsync(movie.Id, "user2", "Reply", parent.Id);

        var reply = ctx.MovieComments.OrderByDescending(x => x.Id).First();
        Assert.Equal(parent.Id, reply.ParentCommentId);
    }

}
