using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopApp.Services;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IUserService userService, ILogger<DashboardController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Home");
            }

            ViewBag.User = user;
            ViewBag.IsAdmin = user.Role == UserRole.Admin;
            
            return View();
        }

        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Home");
            }

            return View(user);
        }

        public IActionResult Calendar()
        {
            return View();
        }

        public IActionResult Spreadsheet()
        {
            return View();
        }

        public IActionResult Messages()
        {
            return View();
        }

        public IActionResult CodeEditor()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult UnitManagement()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Admin()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Home");
            }

            var isAdmin = await _userService.IsAdminAsync(userId.Value);
            if (!isAdmin)
            {
                return Forbid();
            }

            var users = await _userService.GetAllUsersAsync(userId.Value);
            return View(users);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}
