using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public class ConnectionRequestService : IConnectionRequestService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConnectionRequestService> _logger;

        public ConnectionRequestService(
            RemoteDesktopDbContext context,
            IUserService userService,
            IConfiguration configuration,
            ILogger<ConnectionRequestService> logger)
        {
            _context = context;
            _userService = userService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ConnectionRequest> SendConnectionRequestAsync(int requesterId, string targetClientId, string? message = null)
        {
            var targetUser = await _userService.GetUserByClientIdAsync(targetClientId);
            if (targetUser == null)
                throw new ArgumentException("Target user not found");

            if (requesterId == targetUser.Id)
                throw new ArgumentException("Cannot send request to yourself");

            // Check if there's already a pending request
            if (await HasPendingRequestAsync(requesterId, targetUser.Id))
                throw new InvalidOperationException("A pending request already exists");

            var timeoutMinutes = _configuration.GetValue<int>("RemoteDesktop:ConnectionRequestTimeoutMinutes", 5);
            
            var request = new ConnectionRequest
            {
                RequesterId = requesterId,
                TargetUserId = targetUser.Id,
                Message = message,
                Status = ConnectionRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes)
            };

            _context.ConnectionRequests.Add(request);
            await _context.SaveChangesAsync();

            // Load navigation properties
            await _context.Entry(request)
                .Reference(r => r.Requester)
                .LoadAsync();
            await _context.Entry(request)
                .Reference(r => r.TargetUser)
                .LoadAsync();

            _logger.LogInformation("Connection request sent from user {RequesterId} to user {TargetUserId}", 
                requesterId, targetUser.Id);

            return request;
        }

        public async Task<ConnectionRequest> RespondToRequestAsync(int requestId, int userId, bool accept, string? responseMessage = null)
        {
            var request = await _context.ConnectionRequests
                .Include(r => r.Requester)
                .Include(r => r.TargetUser)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                throw new ArgumentException("Connection request not found");

            if (request.TargetUserId != userId)
                throw new UnauthorizedAccessException("You can only respond to requests sent to you");

            if (request.Status != ConnectionRequestStatus.Pending)
                throw new InvalidOperationException("Request has already been responded to");

            if (request.ExpiresAt.HasValue && request.ExpiresAt < DateTime.UtcNow)
            {
                request.Status = ConnectionRequestStatus.Expired;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Request has expired");
            }

            request.Status = accept ? ConnectionRequestStatus.Accepted : ConnectionRequestStatus.Rejected;
            request.ResponseMessage = responseMessage;
            request.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Connection request {RequestId} {Status} by user {UserId}", 
                requestId, request.Status, userId);

            return request;
        }

        public async Task<List<ConnectionRequest>> GetPendingRequestsAsync(int userId)
        {
            return await _context.ConnectionRequests
                .Include(r => r.Requester)
                .Include(r => r.TargetUser)
                .Where(r => r.TargetUserId == userId && r.Status == ConnectionRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ConnectionRequest>> GetSentRequestsAsync(int userId)
        {
            return await _context.ConnectionRequests
                .Include(r => r.Requester)
                .Include(r => r.TargetUser)
                .Where(r => r.RequesterId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task<bool> CancelRequestAsync(int requestId, int userId)
        {
            var request = await _context.ConnectionRequests
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.RequesterId != userId)
                return false;

            if (request.Status != ConnectionRequestStatus.Pending)
                return false;

            request.Status = ConnectionRequestStatus.Cancelled;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Connection request {RequestId} cancelled by user {UserId}", requestId, userId);
            return true;
        }

        public async Task<ConnectionRequest?> GetRequestByIdAsync(int requestId)
        {
            return await _context.ConnectionRequests
                .Include(r => r.Requester)
                .Include(r => r.TargetUser)
                .FirstOrDefaultAsync(r => r.Id == requestId);
        }

        public async Task ExpireOldRequestsAsync()
        {
            var expiredRequests = await _context.ConnectionRequests
                .Where(r => r.Status == ConnectionRequestStatus.Pending && 
                           r.ExpiresAt.HasValue && r.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            foreach (var request in expiredRequests)
            {
                request.Status = ConnectionRequestStatus.Expired;
            }

            if (expiredRequests.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Expired {Count} connection requests", expiredRequests.Count);
            }
        }

        public async Task<bool> HasPendingRequestAsync(int requesterId, int targetUserId)
        {
            return await _context.ConnectionRequests
                .AnyAsync(r => r.RequesterId == requesterId && 
                              r.TargetUserId == targetUserId && 
                              r.Status == ConnectionRequestStatus.Pending);
        }
    }
}
