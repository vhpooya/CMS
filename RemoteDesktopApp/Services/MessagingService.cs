using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public class MessagingService : IMessagingService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly ILogger<MessagingService> _logger;
        private readonly IWebHostEnvironment _environment;

        public MessagingService(
            RemoteDesktopDbContext context, 
            ILogger<MessagingService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        public async Task<ChatMessage> SendDirectMessageAsync(int senderId, int receiverId, string content, MessageType type = MessageType.Text, string? attachmentUrl = null)
        {
            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                Type = type,
                AttachmentUrl = attachmentUrl,
                SentAt = DateTime.UtcNow,
                Status = MessageStatus.Sent
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Load navigation properties
            await _context.Entry(message)
                .Reference(m => m.Sender)
                .LoadAsync();
            await _context.Entry(message)
                .Reference(m => m.Receiver)
                .LoadAsync();

            _logger.LogInformation("Direct message sent from user {SenderId} to user {ReceiverId}", senderId, receiverId);
            return message;
        }

        public async Task<ChatMessage> SendConversationMessageAsync(int senderId, int conversationId, string content, MessageType type = MessageType.Text, string? attachmentUrl = null)
        {
            // Verify user is participant in conversation
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == senderId && p.IsActive);

            if (participant == null)
                throw new UnauthorizedAccessException("User is not a participant in this conversation");

            var message = new ChatMessage
            {
                SenderId = senderId,
                ConversationId = conversationId,
                Content = content,
                Type = type,
                AttachmentUrl = attachmentUrl,
                SentAt = DateTime.UtcNow,
                Status = MessageStatus.Sent
            };

            _context.ChatMessages.Add(message);

            // Update conversation last message time
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.LastMessageAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Load navigation properties
            await _context.Entry(message)
                .Reference(m => m.Sender)
                .LoadAsync();
            await _context.Entry(message)
                .Reference(m => m.Conversation)
                .LoadAsync();

            _logger.LogInformation("Message sent to conversation {ConversationId} by user {SenderId}", conversationId, senderId);
            return message;
        }

        public async Task<List<ChatMessage>> GetDirectMessagesAsync(int userId1, int userId2, int page = 1, int pageSize = 50)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
                .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                           (m.SenderId == userId2 && m.ReceiverId == userId1))
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetConversationMessagesAsync(int conversationId, int userId, int page = 1, int pageSize = 50)
        {
            // Verify user is participant
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId && p.IsActive);

            if (!isParticipant)
                throw new UnauthorizedAccessException("User is not a participant in this conversation");

            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
                .Include(m => m.ReplyToMessage)
                .ThenInclude(rm => rm!.Sender)
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ConversationSummary>> GetUserConversationsAsync(int userId)
        {
            var conversations = await _context.ConversationParticipants
                .Include(p => p.Conversation)
                .ThenInclude(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .ThenInclude(m => m.Sender)
                .Include(p => p.Conversation)
                .ThenInclude(c => c.Participants)
                .ThenInclude(p => p.User)
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => p.Conversation)
                .ToListAsync();

            var summaries = new List<ConversationSummary>();

            foreach (var conversation in conversations)
            {
                var lastMessage = conversation.Messages.FirstOrDefault();
                var unreadCount = await GetUnreadConversationCountAsync(conversation.Id, userId);
                var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);

                var summary = new ConversationSummary
                {
                    Id = conversation.Id,
                    Name = conversation.Name,
                    Description = conversation.Description,
                    Type = conversation.Type,
                    AvatarUrl = conversation.AvatarUrl,
                    LastMessageAt = lastMessage?.SentAt,
                    LastMessageContent = lastMessage?.Content,
                    LastMessageSender = lastMessage?.Sender?.DisplayName,
                    UnreadCount = unreadCount,
                    IsMuted = participant?.IsMuted ?? false,
                    Participants = conversation.Participants.ToList()
                };

                // For direct conversations, set online status
                if (conversation.Type == ConversationType.Direct)
                {
                    var otherParticipant = conversation.Participants.FirstOrDefault(p => p.UserId != userId);
                    summary.IsOnline = otherParticipant?.User?.IsOnline ?? false;
                    
                    // Use other participant's name for direct conversations
                    if (otherParticipant != null)
                    {
                        summary.Name = otherParticipant.User.DisplayName;
                        summary.AvatarUrl = otherParticipant.User.ProfilePicture;
                    }
                }

                summaries.Add(summary);
            }

            return summaries.OrderByDescending(s => s.LastMessageAt).ToList();
        }

        public async Task<Conversation> CreateGroupConversationAsync(int creatorId, string name, string? description, List<int> participantIds)
        {
            var conversation = new Conversation
            {
                Name = name,
                Description = description,
                Type = ConversationType.Group,
                CreatedByUserId = creatorId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Add creator as owner
            var creatorParticipant = new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = creatorId,
                Role = ParticipantRole.Owner,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ConversationParticipants.Add(creatorParticipant);

            // Add other participants
            foreach (var participantId in participantIds.Where(id => id != creatorId))
            {
                var participant = new ConversationParticipant
                {
                    ConversationId = conversation.Id,
                    UserId = participantId,
                    Role = ParticipantRole.Member,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ConversationParticipants.Add(participant);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Group conversation {ConversationId} created by user {CreatorId}", conversation.Id, creatorId);
            return conversation;
        }

        public async Task<ConversationParticipant> AddParticipantAsync(int conversationId, int userId, int addedByUserId, ParticipantRole role = ParticipantRole.Member)
        {
            // Verify the user adding has permission
            var adderParticipant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == addedByUserId && p.IsActive);

            if (adderParticipant == null || adderParticipant.Role == ParticipantRole.Member)
                throw new UnauthorizedAccessException("Insufficient permissions to add participants");

            // Check if user is already a participant
            var existingParticipant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId);

            if (existingParticipant != null)
            {
                if (existingParticipant.IsActive)
                    throw new InvalidOperationException("User is already a participant");
                
                // Reactivate if previously removed
                existingParticipant.IsActive = true;
                existingParticipant.JoinedAt = DateTime.UtcNow;
                existingParticipant.Role = role;
                await _context.SaveChangesAsync();
                return existingParticipant;
            }

            var participant = new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ConversationParticipants.Add(participant);
            await _context.SaveChangesAsync();

            return participant;
        }

        public async Task<bool> RemoveParticipantAsync(int conversationId, int userId, int removedByUserId)
        {
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId && p.IsActive);

            if (participant == null)
                return false;

            // Check permissions
            if (userId != removedByUserId) // If not removing self
            {
                var removerParticipant = await _context.ConversationParticipants
                    .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == removedByUserId && p.IsActive);

                if (removerParticipant == null || removerParticipant.Role == ParticipantRole.Member)
                    throw new UnauthorizedAccessException("Insufficient permissions to remove participants");
            }

            participant.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateParticipantRoleAsync(int conversationId, int userId, int updatedByUserId, ParticipantRole newRole)
        {
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId && p.IsActive);

            if (participant == null)
                return false;

            // Check permissions
            var updaterParticipant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == updatedByUserId && p.IsActive);

            if (updaterParticipant == null || updaterParticipant.Role != ParticipantRole.Owner)
                throw new UnauthorizedAccessException("Only owners can update participant roles");

            participant.Role = newRole;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId, int userId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null)
                return false;

            // Check if already read
            var existingRead = await _context.MessageReads
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

            if (existingRead != null)
                return true;

            var messageRead = new MessageRead
            {
                MessageId = messageId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            };

            _context.MessageReads.Add(messageRead);

            // Update message read status if it's a direct message to this user
            if (message.ReceiverId == userId && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkConversationAsReadAsync(int conversationId, int userId)
        {
            var unreadMessages = await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId && 
                           m.SenderId != userId && 
                           !m.ReadReceipts.Any(r => r.UserId == userId))
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                var messageRead = new MessageRead
                {
                    MessageId = message.Id,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                };

                _context.MessageReads.Add(messageRead);
            }

            // Update last seen time
            await UpdateLastSeenAsync(conversationId, userId);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadMessageCountAsync(int userId)
        {
            // Count unread direct messages
            var directUnread = await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeleted)
                .CountAsync();

            // Count unread conversation messages
            var conversationUnread = await _context.ChatMessages
                .Where(m => m.ConversationId != null && 
                           m.SenderId != userId && 
                           !m.IsDeleted &&
                           m.Conversation!.Participants.Any(p => p.UserId == userId && p.IsActive) &&
                           !m.ReadReceipts.Any(r => r.UserId == userId))
                .CountAsync();

            return directUnread + conversationUnread;
        }

        public async Task<int> GetUnreadConversationCountAsync(int conversationId, int userId)
        {
            return await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId &&
                           m.SenderId != userId &&
                           !m.IsDeleted &&
                           !m.ReadReceipts.Any(r => r.UserId == userId))
                .CountAsync();
        }

        public async Task<ChatMessage?> EditMessageAsync(int messageId, int userId, string newContent)
        {
            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message == null)
                return null;

            message.Content = newContent;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<bool> DeleteMessageAsync(int messageId, int userId)
        {
            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message == null)
                return false;

            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<MessageReaction> AddReactionAsync(int messageId, int userId, string emoji)
        {
            // Check if reaction already exists
            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Emoji == emoji);

            if (existingReaction != null)
                return existingReaction;

            var reaction = new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji,
                CreatedAt = DateTime.UtcNow
            };

            _context.MessageReactions.Add(reaction);
            await _context.SaveChangesAsync();

            return reaction;
        }

        public async Task<bool> RemoveReactionAsync(int messageId, int userId, string emoji)
        {
            var reaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Emoji == emoji);

            if (reaction == null)
                return false;

            _context.MessageReactions.Remove(reaction);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ChatMessage>> SearchMessagesAsync(int userId, string query, int? conversationId = null, int page = 1, int pageSize = 20)
        {
            var messagesQuery = _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Conversation)
                .Where(m => !m.IsDeleted && m.Content.Contains(query));

            // Filter by conversation if specified
            if (conversationId.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.ConversationId == conversationId);
            }
            else
            {
                // Only show messages user has access to
                messagesQuery = messagesQuery.Where(m =>
                    m.SenderId == userId ||
                    m.ReceiverId == userId ||
                    (m.ConversationId != null && m.Conversation!.Participants.Any(p => p.UserId == userId && p.IsActive)));
            }

            return await messagesQuery
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<ChatMessage?> GetMessageByIdAsync(int messageId, int userId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Conversation)
                .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
                .Include(m => m.ReplyToMessage)
                .ThenInclude(rm => rm!.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId &&
                    (m.SenderId == userId ||
                     m.ReceiverId == userId ||
                     (m.ConversationId != null && m.Conversation!.Participants.Any(p => p.UserId == userId && p.IsActive))));
        }

        public async Task<Conversation?> GetConversationByIdAsync(int conversationId, int userId)
        {
            return await _context.Conversations
                .Include(c => c.Participants)
                .ThenInclude(p => p.User)
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == conversationId &&
                    c.Participants.Any(p => p.UserId == userId && p.IsActive));
        }

        public async Task<Conversation?> UpdateConversationAsync(int conversationId, int userId, string? name = null, string? description = null)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return null;

            // Check permissions
            var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId && p.IsActive);
            if (participant == null || participant.Role == ParticipantRole.Member)
                throw new UnauthorizedAccessException("Insufficient permissions to update conversation");

            if (!string.IsNullOrEmpty(name))
                conversation.Name = name;

            if (description != null)
                conversation.Description = description;

            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task<List<User>> GetOnlineUsersAsync(int currentUserId)
        {
            return await _context.Users
                .Where(u => u.Id != currentUserId && u.IsOnline && u.IsActive && !u.IsDeactivated)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        public async Task<List<User>> GetRecentContactsAsync(int userId, int limit = 10)
        {
            var recentContacts = await _context.ChatMessages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Select(m => m.SenderId == userId ? m.Receiver! : m.Sender)
                .Where(u => u.Id != userId)
                .Distinct()
                .Take(limit)
                .ToListAsync();

            return recentContacts;
        }

        public async Task<string> UploadAttachmentAsync(IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided");

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "messages");
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{userId}_{DateTime.UtcNow.Ticks}_{file.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/messages/{fileName}";
        }

        public async Task<List<ConversationParticipant>> GetConversationParticipantsAsync(int conversationId, int userId)
        {
            // Verify user is participant
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId && p.IsActive);

            if (!isParticipant)
                throw new UnauthorizedAccessException("User is not a participant in this conversation");

            return await _context.ConversationParticipants
                .Include(p => p.User)
                .Where(p => p.ConversationId == conversationId && p.IsActive)
                .OrderBy(p => p.Role)
                .ThenBy(p => p.User.DisplayName)
                .ToListAsync();
        }

        public async Task UpdateLastSeenAsync(int conversationId, int userId)
        {
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId && p.IsActive);

            if (participant != null)
            {
                participant.LastSeenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ToggleConversationMuteAsync(int conversationId, int userId)
        {
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId && p.IsActive);

            if (participant == null)
                return false;

            participant.IsMuted = !participant.IsMuted;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
