using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Controllers
{
    [Authorize]
    public class DiscussionRoomController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DiscussionRoomController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
            var room = await _context.DiscussionRooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (currentUserId == null || room == null) return RedirectToAction("Index", "Profile");
            if (room.FriendUserId != currentUserId) return Forbid();
            if (room.Status == DiscussionRoomStatus.Pending) room.Status = DiscussionRoomStatus.Active;
            await _context.SaveChangesAsync();
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
            var vm = new DiscussionRoomViewModel { RoomId = room.Id, MovieId = room.MovieId, MovieTitle = room.Movie.Title, MoviePosterUrl = room.Movie.PosterUrl, FriendName = string.IsNullOrWhiteSpace(friend.FullName) ? friend.UserName ?? "Друг" : friend.FullName, FriendAvatarUrl = friend.AvatarUrl, IsInitiator = room.CreatedByUserId == currentUserId, IsPending = room.Status == DiscussionRoomStatus.Pending, IsActive = room.Status == DiscussionRoomStatus.Active, IsClosed = room.Status == DiscussionRoomStatus.Closed, Messages = room.Messages.OrderBy(m => m.CreatedAt).Select(m => new DiscussionRoomMessageViewModel { IsOwn = m.SenderId == currentUserId, SenderName = string.IsNullOrWhiteSpace(m.Sender.FullName) ? m.Sender.UserName ?? "User" : m.Sender.FullName, SenderAvatarUrl = m.Sender.AvatarUrl, Text = m.Text, ImageUrl = m.ImageUrl, CreatedAt = m.CreatedAt }).ToList() };
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
            _context.DiscussionRoomMessages.Add(new DiscussionRoomMessage { RoomId = roomId, SenderId = currentUserId, Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim(), ImageUrl = imageUrl });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Room), new { id = roomId });
        }
    }
}
