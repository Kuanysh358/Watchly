using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Watchly.Web.Data;
using Watchly.Web.Hubs;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Controllers
{
    [Authorize]
    public class DiscussionRoomController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<DiscussionRoomHub> _hub;

        public DiscussionRoomController(ApplicationDbContext context, IWebHostEnvironment env, IHubContext<DiscussionRoomHub> hub)
        {
            _context = context;
            _env = env;
            _hub = hub;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int movieId, string friendId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null || string.IsNullOrWhiteSpace(friendId)) return RedirectToAction("Detail", "Cinema", new { id = movieId });

            var isFriend = await _context.Friendships.AnyAsync(f => ((f.UserId1 == currentUserId && f.UserId2 == friendId) || (f.UserId1 == friendId && f.UserId2 == currentUserId)) && f.Status == FriendshipStatus.Accepted);
            if (!isFriend) return Forbid();

            var room = new DiscussionRoom { MovieId = movieId, CreatedByUserId = currentUserId, FriendUserId = friendId, Status = DiscussionRoomStatus.Pending };
            _context.DiscussionRooms.Add(room);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Room), new { id = room.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int roomId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var room = await _context.DiscussionRooms.Include(r => r.FriendUser).FirstOrDefaultAsync(r => r.Id == roomId);
            if (currentUserId == null || room == null) return RedirectToAction("Index", "Profile");
            if (room.FriendUserId != currentUserId) return Forbid();
            if (room.Status == DiscussionRoomStatus.Pending)
            {
                room.Status = DiscussionRoomStatus.Active;

                var joiner = room.FriendUser;
                var joinerName = (joiner?.FullName ?? joiner?.UserName) ?? "User";
                var joinText = $"{joinerName} қосылды / присоединился";

                _context.DiscussionRoomMessages.Add(new DiscussionRoomMessage
                {
                    RoomId = roomId,
                    SenderId = currentUserId,
                    Text = joinText,
                    IsSystemMessage = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                await _hub.Clients.Group($"room_{roomId}").SendAsync("ReceiveMessage", new
                {
                    isOwn = false,
                    senderName = "system",
                    senderAvatar = (string?)null,
                    text = joinText,
                    imageUrl = (string?)null,
                    createdAt = DateTime.UtcNow.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    isSystemMessage = true,
                    senderId = currentUserId
                });
            }
            return RedirectToAction(nameof(Room), new { id = room.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int roomId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var room = await _context.DiscussionRooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (currentUserId == null || room == null) return RedirectToAction("Index", "Profile");
            if (room.CreatedByUserId != currentUserId && room.FriendUserId != currentUserId) return Forbid();
            room.Status = DiscussionRoomStatus.Closed;
            room.ClosedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"room_{roomId}").SendAsync("RoomClosed");

            return RedirectToAction(nameof(Room), new { id = room.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Room(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Unauthorized();
            var room = await _context.DiscussionRooms.Include(r => r.Movie).Include(r => r.CreatedByUser).Include(r => r.FriendUser).Include(r => r.Messages).ThenInclude(m => m.Sender).FirstOrDefaultAsync(r => r.Id == id);
            if (room == null) return NotFound();
            if (room.CreatedByUserId != currentUserId && room.FriendUserId != currentUserId) return Forbid();
            var friend = room.CreatedByUserId == currentUserId ? room.FriendUser : room.CreatedByUser;

            var movie = room.Movie;
            string? videoUrl = null;
            bool isIframe = false;
            if (!string.IsNullOrWhiteSpace(movie.VideoUrl))
            {
                videoUrl = movie.VideoUrl;
                isIframe = IsEmbedUrl(movie.VideoUrl);
            }
            else if (movie.TmdbId.HasValue)
            {
                videoUrl = $"https://vidsrc.to/embed/movie/{movie.TmdbId.Value}";
                isIframe = true;
            }
            else if (!string.IsNullOrWhiteSpace(movie.TrailerUrl))
            {
                videoUrl = movie.TrailerUrl;
                isIframe = IsEmbedUrl(movie.TrailerUrl);
            }

            var vm = new DiscussionRoomViewModel
            {
                RoomId = room.Id,
                MovieId = room.MovieId,
                MovieTitle = movie.Title,
                MoviePosterUrl = movie.PosterUrl,
                MovieVideoUrl = videoUrl,
                MovieVideoIsIframe = isIframe,
                FriendName = string.IsNullOrWhiteSpace(friend.FullName) ? friend.UserName ?? "Друг" : friend.FullName,
                FriendAvatarUrl = friend.AvatarUrl,
                IsInitiator = room.CreatedByUserId == currentUserId,
                IsPending = room.Status == DiscussionRoomStatus.Pending,
                IsActive = room.Status == DiscussionRoomStatus.Active,
                IsClosed = room.Status == DiscussionRoomStatus.Closed,
                Messages = room.Messages.OrderBy(m => m.CreatedAt).Select(m => new DiscussionRoomMessageViewModel
                {
                    IsOwn = m.SenderId == currentUserId && !m.IsSystemMessage,
                    SenderName = m.IsSystemMessage ? "system" : (string.IsNullOrWhiteSpace(m.Sender.FullName) ? m.Sender.UserName ?? "User" : m.Sender.FullName),
                    SenderAvatarUrl = m.IsSystemMessage ? null : m.Sender.AvatarUrl,
                    Text = m.Text,
                    ImageUrl = m.ImageUrl,
                    IsSystemMessage = m.IsSystemMessage,
                    CreatedAt = m.CreatedAt
                }).ToList()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int roomId, string? text, IFormFile? image)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Unauthorized();
            var room = await _context.DiscussionRooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null) return NotFound();
            if (room.CreatedByUserId != currentUserId && room.FriendUserId != currentUserId) return Forbid();
            if (room.Status != DiscussionRoomStatus.Active) return RedirectToAction(nameof(Room), new { id = roomId });
            string? imageUrl = null;
            if (image != null && image.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "room-images");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(image.FileName);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);
                imageUrl = $"/uploads/room-images/{fileName}";
            }
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(imageUrl)) return RedirectToAction(nameof(Room), new { id = roomId });

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            var senderName = (sender?.FullName ?? sender?.UserName) ?? "User";

            var message = new DiscussionRoomMessage
            {
                RoomId = roomId,
                SenderId = currentUserId,
                Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
                ImageUrl = imageUrl,
                IsSystemMessage = false
            };
            _context.DiscussionRoomMessages.Add(message);
            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"room_{roomId}").SendAsync("ReceiveMessage", new
            {
                isOwn = false,
                senderName,
                senderAvatar = sender?.AvatarUrl,
                text = message.Text,
                imageUrl = message.ImageUrl,
                createdAt = message.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                isSystemMessage = false,
                senderId = currentUserId
            });

            return RedirectToAction(nameof(Room), new { id = roomId });
        }

        private static bool IsEmbedUrl(string url) =>
            url.Contains("vidsrc", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("embed", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("youtube.com/embed", StringComparison.OrdinalIgnoreCase);
    }
}
