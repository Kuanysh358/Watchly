using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Watchly.Web.Models.DataModels;

namespace Watchly.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Genre> Genres => Set<Genre>();
        public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
        public DbSet<Watchlist> Watchlists => Set<Watchlist>();
        public DbSet<ViewHistory> ViewHistories => Set<ViewHistory>();
        public DbSet<MovieComment> MovieComments => Set<MovieComment>();
        public DbSet<MovieRating> MovieRatings => Set<MovieRating>();
        public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
        public DbSet<CommentDislike> CommentDislikes => Set<CommentDislike>();
        public DbSet<Friendship> Friendships => Set<Friendship>();
        public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
        public DbSet<DiscussionRoom> DiscussionRooms => Set<DiscussionRoom>();
        public DbSet<DiscussionRoomMessage> DiscussionRoomMessages => Set<DiscussionRoomMessage>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Movie>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Rating).HasColumnType("decimal(3,1)");
                entity.HasIndex(e => e.TmdbId).IsUnique().HasFilter("[TmdbId] IS NOT NULL");
            });

            builder.Entity<Genre>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });

            builder.Entity<MovieGenre>(entity =>
            {
                entity.HasKey(e => new { e.MovieId, e.GenreId });
                entity.HasOne(e => e.Movie).WithMany(m => m.MovieGenres).HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Genre).WithMany(g => g.MovieGenres).HasForeignKey(e => e.GenreId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Watchlist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User).WithMany(u => u.Watchlists).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Movie).WithMany(m => m.Watchlists).HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.UserId, e.MovieId }).IsUnique();
            });

            builder.Entity<ViewHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User).WithMany(u => u.ViewHistories).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Movie).WithMany(m => m.ViewHistories).HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.UserId, e.MovieId }).IsUnique();
            });

            builder.Entity<MovieComment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Movie).WithMany(m => m.Comments).HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User).WithMany(u => u.Comments).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<MovieRating>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Movie).WithMany(m => m.Ratings).HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User).WithMany(u => u.Ratings).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.UserId, e.MovieId }).IsUnique();
            });

            builder.Entity<CommentLike>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Comment).WithMany(c => c.Likes).HasForeignKey(e => e.CommentId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => new { e.CommentId, e.UserId }).IsUnique();
            });


            builder.Entity<CommentDislike>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Comment).WithMany(c => c.Dislikes).HasForeignKey(e => e.CommentId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => new { e.CommentId, e.UserId }).IsUnique();
            });

            builder.Entity<Friendship>(entity =>
            {
                entity.HasKey(e => new { e.UserId1, e.UserId2 });
                entity.HasOne(e => e.User1).WithMany(u => u.FriendshipsInitiated).HasForeignKey(e => e.UserId1).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.User2).WithMany(u => u.FriendshipsReceived).HasForeignKey(e => e.UserId2).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => new { e.UserId1, e.UserId2 }).IsUnique();
            });

            builder.Entity<DirectMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).HasMaxLength(2000);
                entity.HasOne(e => e.Sender).WithMany().HasForeignKey(e => e.SenderId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.Recipient).WithMany().HasForeignKey(e => e.RecipientId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.Movie).WithMany().HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<DiscussionRoom>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Movie).WithMany().HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.FriendUser).WithMany().HasForeignKey(e => e.FriendUserId).OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<DiscussionRoomMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).HasMaxLength(2000);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
                entity.HasOne(e => e.Room).WithMany(r => r.Messages).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Sender).WithMany().HasForeignKey(e => e.SenderId).OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<Genre>().HasData(
                new Genre { Id = 1, Name = "Боевик" },
                new Genre { Id = 2, Name = "Комедия" },
                new Genre { Id = 3, Name = "Драма" },
                new Genre { Id = 4, Name = "Ужасы" },
                new Genre { Id = 5, Name = "Фантастика" },
                new Genre { Id = 6, Name = "Триллер" },
                new Genre { Id = 7, Name = "Романтика" },
                new Genre { Id = 8, Name = "Анимация" },
                new Genre { Id = 9, Name = "Документальный" },
                new Genre { Id = 10, Name = "Мюзикл" }
            );
        }
    }
}
