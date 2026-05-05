using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;

namespace Watchly.Web.Hubs
{
    [Authorize]
    public class DiscussionRoomHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public DiscussionRoomHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinRoom(int roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(roomId));
        }

        public async Task LeaveRoom(int roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(roomId));
        }

        public async Task SendMessage(int roomId, string? text, string? imageUrl)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return;

            var room = await _context.DiscussionRooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null || room.Status != DiscussionRoomStatus.Active) return;
            if (room.CreatedByUserId != userId && room.FriendUserId != userId) return;

            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(imageUrl)) return;

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var senderName = (sender?.FullName ?? sender?.UserName) ?? "User";
            var senderAvatar = sender?.AvatarUrl;

            var message = new DiscussionRoomMessage
            {
                RoomId = roomId,
                SenderId = userId,
                Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl,
                IsSystemMessage = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.DiscussionRoomMessages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.Group(RoomGroup(roomId)).SendAsync("ReceiveMessage", new
            {
                isOwn = false,
                senderName,
                senderAvatar,
                text = message.Text,
                imageUrl = message.ImageUrl,
                createdAtUtc = message.CreatedAt.ToString("o"),
                isSystemMessage = false,
                senderId = userId
            });
        }

        public async Task NotifyJoined(int roomId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return;

            var room = await _context.DiscussionRooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null || room.FriendUserId != userId) return;

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var senderName = (sender?.FullName ?? sender?.UserName) ?? "User";

            var joinText = $"{senderName} қосылды / присоединился";
            var message = new DiscussionRoomMessage
            {
                RoomId = roomId,
                SenderId = userId,
                Text = joinText,
                IsSystemMessage = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.DiscussionRoomMessages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.Group(RoomGroup(roomId)).SendAsync("ReceiveMessage", new
            {
                isOwn = false,
                senderName = "system",
                senderAvatar = (string?)null,
                text = joinText,
                imageUrl = (string?)null,
                createdAtUtc = message.CreatedAt.ToString("o"),
                isSystemMessage = true,
                senderId = userId
            });
        }

        private static string RoomGroup(int roomId) => $"room_{roomId}";
    }
}
