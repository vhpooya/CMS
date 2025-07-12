using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public interface IConnectionRequestService
    {
        /// <summary>
        /// Sends a connection request to another user
        /// </summary>
        Task<ConnectionRequest> SendConnectionRequestAsync(int requesterId, string targetClientId, string? message = null);
        
        /// <summary>
        /// Responds to a connection request (accept/reject)
        /// </summary>
        Task<ConnectionRequest> RespondToRequestAsync(int requestId, int userId, bool accept, string? responseMessage = null);
        
        /// <summary>
        /// Gets pending connection requests for a user
        /// </summary>
        Task<List<ConnectionRequest>> GetPendingRequestsAsync(int userId);
        
        /// <summary>
        /// Gets sent connection requests for a user
        /// </summary>
        Task<List<ConnectionRequest>> GetSentRequestsAsync(int userId);
        
        /// <summary>
        /// Cancels a connection request
        /// </summary>
        Task<bool> CancelRequestAsync(int requestId, int userId);
        
        /// <summary>
        /// Gets a connection request by ID
        /// </summary>
        Task<ConnectionRequest?> GetRequestByIdAsync(int requestId);
        
        /// <summary>
        /// Expires old connection requests
        /// </summary>
        Task ExpireOldRequestsAsync();
        
        /// <summary>
        /// Checks if there's already a pending request between two users
        /// </summary>
        Task<bool> HasPendingRequestAsync(int requesterId, int targetUserId);
    }
}
