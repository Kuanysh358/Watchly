using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;

namespace Watchly.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");
            var vm = await BuildProfileViewModelAsync(currentUser, currentUser.Id, true);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Public(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var profileUser = await _userManager.FindByIdAsync(id) ?? await _userManager.FindByNameAsync(id);
            if (profileUser == null) return NotFound();
            if (profileUser.Id == currentUser.Id) return RedirectToAction(nameof(Index));

            var vm = await BuildProfileViewModelAsync(profileUser, currentUser.Id, false);
            return View("Public", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ModelState.Remove(nameof(model.AvatarFile));
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Некорректные данные" });
            }

            var userName = model.UserName.Trim();
            var fullName = model.FullName.Trim();
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(fullName))
            {
                return BadRequest(new { message = "Заполните никнейм и имя" });
            }

            user.UserName = userName;
            user.FullName = fullName;

            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(model.AvatarFile.ContentType.ToLower()))
                {
                    return BadRequest(new { message = "Допустимы только изображения (jpg, png, gif, webp)" });
                }

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(model.AvatarFile.FileName);
                var fileName = $"{user.Id}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await model.AvatarFile.CopyToAsync(stream);
                user.AvatarUrl = $"/uploads/avatars/{fileName}";
            }
            else if (!string.IsNullOrWhiteSpace(model.AvatarUrl))
            {
                user.AvatarUrl = model.AvatarUrl.Trim();
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(new { message = string.Join("; ", updateResult.Errors.Select(e => e.Description)) });
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    return BadRequest(new { message = "Укажите текущий пароль" });
                }

                var passwordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    return BadRequest(new { message = string.Join("; ", passwordResult.Errors.Select(e => e.Description)) });
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            return Ok(new { avatarUrl = user.AvatarUrl, fullName = user.FullName, userName = user.UserName });
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            query = query?.Trim() ?? string.Empty;
            if (query.Length < 2) return Json(Array.Empty<FriendshipViewModel>());

            var users = await _context.Users
                .Where(u => u.Id != currentUser.Id && u.UserName != null && EF.Functions.Like(u.UserName, $"%{query}%"))
                .OrderBy(u => u.UserName)
                .Take(10)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();
            var relationships = await _context.Friendships
                .Where(f => (f.UserId1 == currentUser.Id && userIds.Contains(f.UserId2))
                         || (f.UserId2 == currentUser.Id && userIds.Contains(f.UserId1)))
                .ToListAsync();

            var results = users.Select(u =>
            {
                var relation = relationships.FirstOrDefault(f => (f.UserId1 == currentUser.Id && f.UserId2 == u.Id) || (f.UserId2 == currentUser.Id && f.UserId1 == u.Id));
                var isIncoming = relation?.Status == FriendshipStatus.Pending && relation.UserId2 == currentUser.Id;
                var isOutgoing = relation?.Status == FriendshipStatus.Pending && relation.UserId1 == currentUser.Id;
                return new FriendshipViewModel
                {
                    UserId = u.Id,
                    UserName = u.UserName ?? "User",
                    FullName = u.FullName,
                    AvatarUrl = u.AvatarUrl,
                    Status = relation?.Status ?? FriendshipStatus.None,
                    IsIncomingRequest = isIncoming,
                    IsOutgoingRequest = isOutgoing
                };
            }).ToList();

            return Json(results);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendFriendRequest(string userId, string? returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            if (string.IsNullOrWhiteSpace(userId) || userId == currentUser.Id) return BadRequest();

            var existing = await _context.Friendships.FirstOrDefaultAsync(f =>
                (f.UserId1 == currentUser.Id && f.UserId2 == userId) ||
                (f.UserId1 == userId && f.UserId2 == currentUser.Id));

            if (existing != null)
            {
                if (existing.Status != FriendshipStatus.Declined) return RedirectToLocal(returnUrl);
                _context.Friendships.Remove(existing);
            }

            _context.Friendships.Add(new Friendship
            {
                UserId1 = currentUser.Id,
                UserId2 = userId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return RedirectToLocal(returnUrl);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFriendRequest(string userId, string? returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var entry = await _context.Friendships.FirstOrDefaultAsync(f => f.UserId1 == userId && f.UserId2 == currentUser.Id && f.Status == FriendshipStatus.Pending);
            if (entry == null) return RedirectToLocal(returnUrl);

            entry.Status = FriendshipStatus.Accepted;
            await _context.SaveChangesAsync();
            return RedirectToLocal(returnUrl);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineFriendRequest(string userId, string? returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var entry = await _context.Friendships.FirstOrDefaultAsync(f => f.UserId1 == userId && f.UserId2 == currentUser.Id && f.Status == FriendshipStatus.Pending);
            if (entry == null) return RedirectToLocal(returnUrl);

            entry.Status = FriendshipStatus.Declined;
            await _context.SaveChangesAsync();
            return RedirectToLocal(returnUrl);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFriend(string userId, string? returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var entry = await _context.Friendships.FirstOrDefaultAsync(f =>
                (f.UserId1 == currentUser.Id && f.UserId2 == userId) ||
                (f.UserId1 == userId && f.UserId2 == currentUser.Id));

            if (entry != null)
            {
                _context.Friendships.Remove(entry);
                await _context.SaveChangesAsync();
            }

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Chat(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var friend = await _userManager.FindByIdAsync(id);
            if (friend == null) return NotFound();

            var isFriend = await _context.Friendships.AnyAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.UserId1 == currentUser.Id && f.UserId2 == friend.Id) || (f.UserId1 == friend.Id && f.UserId2 == currentUser.Id)));
            if (!isFriend) return RedirectToAction(nameof(Public), new { id = friend.Id });

            var messages = await _context.DirectMessages
                .Where(m => (m.SenderId == currentUser.Id && m.RecipientId == friend.Id)
                         || (m.SenderId == friend.Id && m.RecipientId == currentUser.Id))
                .Include(m => m.Movie)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var favoriteIds = await _context.Watchlists.Where(w => w.UserId == currentUser.Id).Select(w => w.MovieId).ToListAsync();
            var availableMovies = await _context.Movies.OrderBy(m => m.Title).Take(200)
                .Select(m => new MovieOptionViewModel { Id = m.Id, Title = m.Title, IsFavorite = favoriteIds.Contains(m.Id) })
                .ToListAsync();

            var vm = new ChatViewModel
            {
                FriendId = friend.Id,
                FriendName = friend.FullName ?? friend.UserName ?? "User",
                FriendAvatarUrl = friend.AvatarUrl,
                AvailableMovies = availableMovies,
                Messages = messages.Select(m => new ChatMessageViewModel
                {
                    IsOwn = m.SenderId == currentUser.Id,
                    SenderName = m.SenderId == currentUser.Id ? "Вы" : (friend.FullName ?? friend.UserName ?? "User"),
                    Text = m.Text,
                    MovieId = m.MovieId,
                    MovieTitle = m.Movie?.Title,
                    CreatedAt = m.CreatedAt
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string friendId, string? text, int? movieId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");
            if (string.IsNullOrWhiteSpace(friendId)) return RedirectToAction(nameof(Index));

            var isFriend = await _context.Friendships.AnyAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.UserId1 == currentUser.Id && f.UserId2 == friendId) || (f.UserId1 == friendId && f.UserId2 == currentUser.Id)));
            if (!isFriend) return RedirectToAction(nameof(Public), new { id = friendId });

            if (string.IsNullOrWhiteSpace(text) && !movieId.HasValue)
            {
                return RedirectToAction(nameof(Chat), new { id = friendId });
            }

            _context.DirectMessages.Add(new DirectMessage
            {
                SenderId = currentUser.Id,
                RecipientId = friendId,
                MovieId = movieId,
                Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Chat), new { id = friendId });
        }

        private async Task<UserProfileViewModel> BuildProfileViewModelAsync(ApplicationUser profileUser, string currentUserId, bool includePrivateData)
        {
            var history = await _context.ViewHistories
                .Where(v => v.UserId == profileUser.Id)
                .Include(v => v.Movie)
                .OrderByDescending(v => v.LastViewedAt)
                .ToListAsync();

            var ratings = await _context.MovieRatings
                .Where(r => r.UserId == profileUser.Id)
                .Include(r => r.Movie)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var friendsCount = await _context.Friendships.CountAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                (f.UserId1 == profileUser.Id || f.UserId2 == profileUser.Id));

            var relation = await _context.Friendships
                .Include(f => f.User1)
                .Include(f => f.User2)
                .FirstOrDefaultAsync(f => (f.UserId1 == currentUserId && f.UserId2 == profileUser.Id)
                                       || (f.UserId2 == currentUserId && f.UserId1 == profileUser.Id));

            var vm = new UserProfileViewModel
            {
                UserId = profileUser.Id,
                UserName = profileUser.UserName ?? "User",
                FullName = profileUser.FullName,
                AvatarUrl = profileUser.AvatarUrl,
                CreatedAt = profileUser.CreatedAt,
                IsCurrentUser = includePrivateData,
                ViewedMoviesCount = history.Select(h => h.MovieId).Distinct().Count(),
                FriendsCount = friendsCount,
                AverageRating = ratings.Count == 0 ? 0 : ratings.Average(r => r.Score),
                RecentViews = history.Take(6).Select(h => new RecentViewViewModel
                {
                    MovieId = h.MovieId,
                    Title = h.Movie.Title,
                    PosterUrl = h.Movie.PosterUrl,
                    ViewedAt = h.LastViewedAt
                }).ToList(),
                Ratings = ratings.Take(10).Select(r => new UserRatingViewModel
                {
                    MovieId = r.MovieId,
                    Title = r.Movie.Title,
                    PosterUrl = r.Movie.PosterUrl,
                    Score = r.Score
                }).ToList(),
                Relationship = relation == null ? null : new FriendshipViewModel
                {
                    UserId = profileUser.Id,
                    UserName = profileUser.UserName ?? "User",
                    FullName = profileUser.FullName,
                    AvatarUrl = profileUser.AvatarUrl,
                    Status = relation.Status,
                    IsIncomingRequest = relation.Status == FriendshipStatus.Pending && relation.UserId2 == currentUserId,
                    IsOutgoingRequest = relation.Status == FriendshipStatus.Pending && relation.UserId1 == currentUserId
                }
            };

            if (includePrivateData)
            {
                var friends = await _context.Friendships
                    .Where(f => f.Status == FriendshipStatus.Accepted && (f.UserId1 == profileUser.Id || f.UserId2 == profileUser.Id))
                    .Include(f => f.User1)
                    .Include(f => f.User2)
                    .ToListAsync();

                var incoming = await _context.Friendships
                    .Where(f => f.Status == FriendshipStatus.Pending && f.UserId2 == profileUser.Id)
                    .Include(f => f.User1)
                    .ToListAsync();

                var outgoing = await _context.Friendships
                    .Where(f => f.Status == FriendshipStatus.Pending && f.UserId1 == profileUser.Id)
                    .Include(f => f.User2)
                    .ToListAsync();

                vm.Friends = friends.Select(f =>
                {
                    var other = f.UserId1 == profileUser.Id ? f.User2 : f.User1;
                    return new FriendshipViewModel
                    {
                        UserId = other.Id,
                        UserName = other.UserName ?? "User",
                        FullName = other.FullName,
                        AvatarUrl = other.AvatarUrl,
                        Status = f.Status
                    };
                }).ToList();

                vm.IncomingRequests = incoming.Select(f => new FriendshipViewModel
                {
                    UserId = f.User1.Id,
                    UserName = f.User1.UserName ?? "User",
                    FullName = f.User1.FullName,
                    AvatarUrl = f.User1.AvatarUrl,
                    Status = f.Status,
                    IsIncomingRequest = true
                }).ToList();

                vm.OutgoingRequests = outgoing.Select(f => new FriendshipViewModel
                {
                    UserId = f.User2.Id,
                    UserName = f.User2.UserName ?? "User",
                    FullName = f.User2.FullName,
                    AvatarUrl = f.User2.AvatarUrl,
                    Status = f.Status,
                    IsOutgoingRequest = true
                }).ToList();

                vm.EditProfile = new ProfileEditViewModel
                {
                    UserName = profileUser.UserName ?? string.Empty,
                    FullName = profileUser.FullName ?? string.Empty,
                    AvatarUrl = profileUser.AvatarUrl
                };
            }
            else
            {
                vm.AvailableMovies = await _context.Movies.OrderBy(m => m.Title).Take(100)
                    .Select(m => new MovieOptionViewModel { Id = m.Id, Title = m.Title })
                    .ToListAsync();
            }

            return vm;
        }

        private IActionResult RedirectToLocal(string? returnUrl)
            => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }
}
