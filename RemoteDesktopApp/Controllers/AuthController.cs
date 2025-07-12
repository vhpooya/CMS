using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RemoteDesktopApp.Helper;
using RemoteDesktopApp.Models;
using RemoteDesktopApp.Services;
using RemoteDesktopApp.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RemoteDesktopApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IUserService _userService;

        public object TempData { get; private set; }

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger, IUserService userService)
        {
            _configuration = configuration;
            _logger = logger;
            _userService = userService;
        }

      


        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _userService.AuthenticateAsync(request.Username, request.Password);
                if (user != null)
                {
                    var token = GenerateJwtToken(user);
                    var expiresAt = DateTime.UtcNow.AddHours(24);

                    var response = new LoginResponse
                    {
                        Success = true,
                        Token = token,
                        Message = "Login successful",
                        ExpiresAt = expiresAt,
                        User = new UserInfo
                        {
                            Username = user.Username,
                            DisplayName = user.DisplayName,
                            Role = "User",
                            Permissions = new List<string> { "RemoteDesktop", "ScreenCapture", "InputControl" }
                        }
                    };

                    _logger.LogInformation($"User {request.Username} logged in successfully");
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning($"Failed login attempt for user {request.Username}");
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }


        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _userService.CreateUserAsync(request.Username, request.Email, request.Password, request.DisplayName);
                var token = GenerateJwtToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(24);

                var response = new LoginResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Registration successful",
                    ExpiresAt = expiresAt,
                    User = new UserInfo
                    {
                        Username = user.Username,
                        DisplayName = user.DisplayName,
                        Role = "User",
                        Permissions = new List<string> { "RemoteDesktop", "ScreenCapture", "InputControl" }
                    }
                };

                _logger.LogInformation($"User {request.Username} registered successfully with client ID {user.ClientId}");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new LoginResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during registration"
                });
            }
        }

        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await _userService.UpdateUserOnlineStatusAsync(userId.Value, false);
            }

            _logger.LogInformation("User logged out");
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("validate")]
        public async Task<ActionResult> ValidateToken()
        {
            // This endpoint can be used to validate if the current token is still valid
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                return Ok(new { valid = true, username = username });
            }
            
            return Unauthorized(new { valid = false });
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<ActionResult<object>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user session" });
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new
                {
                    userId = user.Id,
                    username = user.Username,
                    displayName = user.DisplayName,
                    email = user.Email,
                    role = user.Role.ToString(),
                    clientId = user.ClientId,
                    phoneNumber = user.PhoneNumber,
                    isOnline = user.IsOnline,
                    isPhoneOnline = user.IsPhoneOnline
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "Error getting user information" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("userId", user.Id.ToString()),
                new Claim("clientId", user.ClientId),
                new Claim("displayName", user.DisplayName),
                new Claim("role", "User")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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

      


        [HttpGet]
        public IActionResult Register()
        {
            return Ok(new RegisterViewModels());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModels model)
        {
            if (!ModelState.IsValid)
            {
                return Ok(model);
            }

            var user = new User
            {
                Username = model.Username,
                PhoneNumber = Guid.NewGuid().ToString("N").Substring(1, 3),
                IsPhoneOnline = false,
                PhoneNotificationsEnabled = true,
                PhoneSoundEnabled = true,
                UnitId = model.UnitId,
                Email = model.Email,
                PasswordHash = _userService.HashPassword(model.PasswordHash),
                DisplayName = model.DisplayName,
                ClientId = await _userService.GenerateUniqueClientIdAsync(), // Generate a unique 12-character ID
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Role = model.Role
            };

          //  TempData["Success"] = "User registered successfully!";
            return RedirectToAction("Register");
        }

    }
}
