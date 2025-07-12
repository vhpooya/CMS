using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public class MobilePhoneService : IMobilePhoneService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly ILogger<MobilePhoneService> _logger;
        // private readonly IUnitService _unitService;

        public MobilePhoneService(RemoteDesktopDbContext context, ILogger<MobilePhoneService> logger)
        {
            _context = context;
            _logger = logger;
            // _unitService = unitService;
        }

        public async Task<string> AssignPhoneNumberAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            // If user already has a phone number, return it
            if (!string.IsNullOrEmpty(user.PhoneNumber))
                return user.PhoneNumber;

            // Generate a unique 3-digit phone number
            var phoneNumber = await GenerateUniquePhoneNumberAsync();
            
            user.PhoneNumber = phoneNumber;
            user.IsPhoneOnline = true;
            user.LastPhoneActivity = DateTime.UtcNow;

            // Create default phone settings
            var phoneSettings = new PhoneSettings
            {
                UserId = userId,
                NotificationsEnabled = true,
                SoundEnabled = true,
                VibrationEnabled = true,
                ShowOnlineStatus = true,
                RingtoneVolume = 80,
                NotificationVolume = 60
            };

            _context.PhoneSettings.Add(phoneSettings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned phone number {PhoneNumber} to user {UserId}", phoneNumber, userId);
            return phoneNumber;
        }

        public async Task<User?> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.Users
                .Include(u => u.PhoneSettings)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<List<OnlineContact>> GetOnlineContactsAsync(int currentUserId)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return new List<OnlineContact>();

            // Get all online users except current user
            var onlineUsers = await _context.Users
                .Where(u => u.Id != currentUserId &&
                           u.IsActive &&
                           !u.IsDeactivated &&
                           u.IsPhoneOnline &&
                           !string.IsNullOrEmpty(u.PhoneNumber))
                .ToListAsync();

            var contacts = new List<OnlineContact>();

            // Check if contacts are favorites or blocked
            var userContacts = await _context.PhoneContacts
                .Where(pc => pc.UserId == currentUserId)
                .ToListAsync();

            foreach (var user in onlineUsers)
            {
                // Check unit communication permissions
                // TODO: Implement unit communication permissions
                // var canCommunicate = await _unitService.CanUsersCommunicateAsync(currentUserId, user.Id, CommunicationType.PhoneCall);
                // if (!canCommunicate)
                //     continue;

                var phoneContact = userContacts.FirstOrDefault(pc => pc.ContactPhoneNumber == user.PhoneNumber);

                var contact = new OnlineContact
                {
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePicture = user.ProfilePicture,
                    IsOnline = user.IsPhoneOnline,
                    LastActivity = user.LastPhoneActivity,
                    IsFavorite = phoneContact?.IsFavorite ?? false,
                    IsBlocked = phoneContact?.IsBlocked ?? false
                };

                if (!contact.IsBlocked)
                {
                    contacts.Add(contact);
                }
            }

            return contacts.OrderBy(c => c.DisplayName).ToList();
        }

        public async Task<PhoneCall> InitiateCallAsync(int callerId, string receiverPhoneNumber, bool isVideoCall = false)
        {
            var caller = await _context.Users.FindAsync(callerId);
            var receiver = await GetUserByPhoneNumberAsync(receiverPhoneNumber);

            if (caller == null || receiver == null)
                throw new ArgumentException("Caller or receiver not found");

            if (!receiver.IsPhoneOnline)
                throw new InvalidOperationException("Receiver is not online");

            var call = new PhoneCall
            {
                CallerId = callerId,
                ReceiverId = receiver.Id,
                CallerPhoneNumber = caller.PhoneNumber,
                ReceiverPhoneNumber = receiverPhoneNumber,
                Status = CallStatus.Initiated,
                StartTime = DateTime.UtcNow,
                IsVideoCall = isVideoCall
            };

            _context.PhoneCalls.Add(call);
            await _context.SaveChangesAsync();

            // Create notification for receiver
            await CreateNotificationAsync(receiver.Id, NotificationType.IncomingCall, 
                $"Incoming call from {caller.DisplayName}", 
                $"Call from {caller.PhoneNumber}", 
                NotificationPriority.High);

            _logger.LogInformation("Call initiated from {CallerPhone} to {ReceiverPhone}", 
                caller.PhoneNumber, receiverPhoneNumber);

            return call;
        }

        public async Task<PhoneCall?> AnswerCallAsync(int callId, int userId)
        {
            var call = await _context.PhoneCalls
                .Include(c => c.Caller)
                .Include(c => c.Receiver)
                .FirstOrDefaultAsync(c => c.Id == callId && c.ReceiverId == userId);

            if (call == null || call.Status != CallStatus.Initiated)
                return null;

            call.Status = CallStatus.Answered;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Call {CallId} answered by user {UserId}", callId, userId);
            return call;
        }

        public async Task<PhoneCall?> DeclineCallAsync(int callId, int userId, CallEndReason reason = CallEndReason.Declined)
        {
            var call = await _context.PhoneCalls
                .FirstOrDefaultAsync(c => c.Id == callId && c.ReceiverId == userId);

            if (call == null)
                return null;

            call.Status = CallStatus.Declined;
            call.EndTime = DateTime.UtcNow;
            call.EndReason = reason;
            call.Duration = call.EndTime - call.StartTime;

            await _context.SaveChangesAsync();

            // Create missed call notification for caller
            await CreateNotificationAsync(call.CallerId, NotificationType.MissedCall,
                "Call declined", 
                $"Call to {call.ReceiverPhoneNumber} was declined",
                NotificationPriority.Normal);

            return call;
        }

        public async Task<PhoneCall?> EndCallAsync(int callId, int userId, CallEndReason reason = CallEndReason.Normal)
        {
            var call = await _context.PhoneCalls
                .FirstOrDefaultAsync(c => c.Id == callId && 
                    (c.CallerId == userId || c.ReceiverId == userId));

            if (call == null)
                return null;

            call.Status = CallStatus.Ended;
            call.EndTime = DateTime.UtcNow;
            call.EndReason = reason;
            call.Duration = call.EndTime - call.StartTime;

            await _context.SaveChangesAsync();

            // Create call ended notification
            var otherUserId = call.CallerId == userId ? call.ReceiverId : call.CallerId;
            await CreateNotificationAsync(otherUserId, NotificationType.CallEnded,
                "Call ended", 
                $"Call duration: {call.Duration?.ToString(@"mm\:ss")}",
                NotificationPriority.Low);

            return call;
        }

        public async Task<List<PhoneCall>> GetCallHistoryAsync(int userId, int page = 1, int pageSize = 50)
        {
            return await _context.PhoneCalls
                .Include(c => c.Caller)
                .Include(c => c.Receiver)
                .Where(c => c.CallerId == userId || c.ReceiverId == userId)
                .OrderByDescending(c => c.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<SmsMessage> SendSmsAsync(int senderId, string receiverPhoneNumber, string content, SmsPriority priority = SmsPriority.Normal)
        {
            var sender = await _context.Users.FindAsync(senderId);
            var receiver = await GetUserByPhoneNumberAsync(receiverPhoneNumber);

            if (sender == null || receiver == null)
                throw new ArgumentException("Sender or receiver not found");

            var sms = new SmsMessage
            {
                SenderId = senderId,
                ReceiverId = receiver.Id,
                SenderPhoneNumber = sender.PhoneNumber,
                ReceiverPhoneNumber = receiverPhoneNumber,
                Content = content,
                Priority = priority,
                Status = SmsStatus.Sent,
                SentAt = DateTime.UtcNow,
                IsDelivered = receiver.IsPhoneOnline
            };

            if (sms.IsDelivered)
            {
                sms.DeliveredAt = DateTime.UtcNow;
                sms.Status = SmsStatus.Delivered;
            }

            _context.SmsMessages.Add(sms);
            await _context.SaveChangesAsync();

            // Create notification for receiver
            await CreateNotificationAsync(receiver.Id, NotificationType.NewSms,
                $"New message from {sender.DisplayName}",
                content.Length > 50 ? content.Substring(0, 50) + "..." : content,
                priority == SmsPriority.Urgent ? NotificationPriority.High : NotificationPriority.Normal);

            _logger.LogInformation("SMS sent from {SenderPhone} to {ReceiverPhone}", 
                sender.PhoneNumber, receiverPhoneNumber);

            return sms;
        }

        public async Task<List<SmsMessage>> GetSmsConversationAsync(int userId, string otherPhoneNumber, int page = 1, int pageSize = 50)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new List<SmsMessage>();

            return await _context.SmsMessages
                .Include(s => s.Sender)
                .Include(s => s.Receiver)
                .Where(s => (s.SenderPhoneNumber == user.PhoneNumber && s.ReceiverPhoneNumber == otherPhoneNumber) ||
                           (s.SenderPhoneNumber == otherPhoneNumber && s.ReceiverPhoneNumber == user.PhoneNumber))
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<SmsConversation>> GetSmsConversationsAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new List<SmsConversation>();

            var conversations = await _context.SmsMessages
                .Include(s => s.Sender)
                .Include(s => s.Receiver)
                .Where(s => s.SenderId == userId || s.ReceiverId == userId)
                .Where(s => !s.IsDeleted)
                .GroupBy(s => s.SenderId == userId ? s.ReceiverPhoneNumber : s.SenderPhoneNumber)
                .Select(g => new
                {
                    PhoneNumber = g.Key,
                    LastMessage = g.OrderByDescending(s => s.SentAt).First(),
                    UnreadCount = g.Count(s => s.ReceiverId == userId && !s.IsRead)
                })
                .ToListAsync();

            var result = new List<SmsConversation>();

            foreach (var conv in conversations)
            {
                var otherUser = await GetUserByPhoneNumberAsync(conv.PhoneNumber);
                if (otherUser != null)
                {
                    var phoneContact = await _context.PhoneContacts
                        .FirstOrDefaultAsync(pc => pc.UserId == userId && pc.ContactPhoneNumber == conv.PhoneNumber);

                    result.Add(new SmsConversation
                    {
                        PhoneNumber = conv.PhoneNumber,
                        ContactName = phoneContact?.ContactName ?? otherUser.DisplayName,
                        ProfilePicture = otherUser.ProfilePicture,
                        LastMessage = conv.LastMessage.Content,
                        LastMessageTime = conv.LastMessage.SentAt,
                        UnreadCount = conv.UnreadCount,
                        IsOnline = otherUser.IsPhoneOnline,
                        IsFavorite = phoneContact?.IsFavorite ?? false,
                        IsBlocked = phoneContact?.IsBlocked ?? false
                    });
                }
            }

            return result.OrderByDescending(c => c.LastMessageTime).ToList();
        }

        public async Task<bool> MarkSmsAsReadAsync(int messageId, int userId)
        {
            var message = await _context.SmsMessages
                .FirstOrDefaultAsync(s => s.Id == messageId && s.ReceiverId == userId);

            if (message == null || message.IsRead)
                return false;

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            message.Status = SmsStatus.Read;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkConversationAsReadAsync(int userId, string otherPhoneNumber)
        {
            var unreadMessages = await _context.SmsMessages
                .Where(s => s.ReceiverId == userId &&
                           s.SenderPhoneNumber == otherPhoneNumber &&
                           !s.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                message.Status = SmsStatus.Read;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadSmsCountAsync(int userId)
        {
            return await _context.SmsMessages
                .CountAsync(s => s.ReceiverId == userId && !s.IsRead && !s.IsDeleted);
        }

        public async Task<PhoneContact> AddContactAsync(int userId, string phoneNumber, string? customName = null)
        {
            var contactUser = await GetUserByPhoneNumberAsync(phoneNumber);
            if (contactUser == null)
                throw new ArgumentException("User with this phone number not found");

            var existingContact = await _context.PhoneContacts
                .FirstOrDefaultAsync(pc => pc.UserId == userId && pc.ContactPhoneNumber == phoneNumber);

            if (existingContact != null)
                return existingContact;

            var contact = new PhoneContact
            {
                UserId = userId,
                ContactUserId = contactUser.Id,
                ContactPhoneNumber = phoneNumber,
                ContactName = customName ?? contactUser.DisplayName,
                AddedAt = DateTime.UtcNow
            };

            _context.PhoneContacts.Add(contact);
            await _context.SaveChangesAsync();

            return contact;
        }

        public async Task<bool> RemoveContactAsync(int userId, string phoneNumber)
        {
            var contact = await _context.PhoneContacts
                .FirstOrDefaultAsync(pc => pc.UserId == userId && pc.ContactPhoneNumber == phoneNumber);

            if (contact == null)
                return false;

            _context.PhoneContacts.Remove(contact);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<PhoneContact>> GetContactsAsync(int userId, bool favoritesOnly = false)
        {
            var query = _context.PhoneContacts
                .Include(pc => pc.ContactUser)
                .Where(pc => pc.UserId == userId);

            if (favoritesOnly)
                query = query.Where(pc => pc.IsFavorite);

            return await query
                .OrderBy(pc => pc.ContactName)
                .ToListAsync();
        }

        public async Task<bool> ToggleFavoriteContactAsync(int userId, string phoneNumber)
        {
            var contact = await _context.PhoneContacts
                .FirstOrDefaultAsync(pc => pc.UserId == userId && pc.ContactPhoneNumber == phoneNumber);

            if (contact == null)
                return false;

            contact.IsFavorite = !contact.IsFavorite;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleBlockContactAsync(int userId, string phoneNumber)
        {
            var contact = await _context.PhoneContacts
                .FirstOrDefaultAsync(pc => pc.UserId == userId && pc.ContactPhoneNumber == phoneNumber);

            if (contact == null)
            {
                // Create contact if it doesn't exist
                await AddContactAsync(userId, phoneNumber);
                contact = await _context.PhoneContacts
                    .FirstOrDefaultAsync(pc => pc.UserId == userId && pc.ContactPhoneNumber == phoneNumber);
            }

            if (contact != null)
            {
                contact.IsBlocked = !contact.IsBlocked;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<PhoneNotification> CreateNotificationAsync(int userId, NotificationType type, string title, string message, NotificationPriority priority = NotificationPriority.Normal)
        {
            var notification = new PhoneNotification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Priority = priority,
                CreatedAt = DateTime.UtcNow,
                Icon = GetNotificationIcon(type),
                Color = GetNotificationColor(type)
            };

            _context.PhoneNotifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<List<PhoneNotification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.PhoneNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> MarkNotificationAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.PhoneNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PhoneSettings> GetPhoneSettingsAsync(int userId)
        {
            var settings = await _context.PhoneSettings
                .FirstOrDefaultAsync(ps => ps.UserId == userId);

            if (settings == null)
            {
                settings = new PhoneSettings
                {
                    UserId = userId,
                    NotificationsEnabled = true,
                    SoundEnabled = true,
                    VibrationEnabled = true,
                    ShowOnlineStatus = true,
                    RingtoneVolume = 80,
                    NotificationVolume = 60
                };

                _context.PhoneSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<PhoneSettings> UpdatePhoneSettingsAsync(int userId, PhoneSettings settings)
        {
            var existingSettings = await _context.PhoneSettings
                .FirstOrDefaultAsync(ps => ps.UserId == userId);

            if (existingSettings == null)
            {
                settings.UserId = userId;
                _context.PhoneSettings.Add(settings);
            }
            else
            {
                existingSettings.NotificationsEnabled = settings.NotificationsEnabled;
                existingSettings.SoundEnabled = settings.SoundEnabled;
                existingSettings.VibrationEnabled = settings.VibrationEnabled;
                existingSettings.ShowOnlineStatus = settings.ShowOnlineStatus;
                existingSettings.AutoAnswerEnabled = settings.AutoAnswerEnabled;
                existingSettings.AutoAnswerDelay = settings.AutoAnswerDelay;
                existingSettings.RingtoneUrl = settings.RingtoneUrl;
                existingSettings.NotificationSoundUrl = settings.NotificationSoundUrl;
                existingSettings.RingtoneVolume = settings.RingtoneVolume;
                existingSettings.NotificationVolume = settings.NotificationVolume;
                existingSettings.DoNotDisturbEnabled = settings.DoNotDisturbEnabled;
                existingSettings.DoNotDisturbStart = settings.DoNotDisturbStart;
                existingSettings.DoNotDisturbEnd = settings.DoNotDisturbEnd;
                existingSettings.CallForwardingEnabled = settings.CallForwardingEnabled;
                existingSettings.ForwardToPhoneNumber = settings.ForwardToPhoneNumber;
                existingSettings.ShowTypingIndicator = settings.ShowTypingIndicator;
                existingSettings.ReadReceiptsEnabled = settings.ReadReceiptsEnabled;
                existingSettings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return settings;
        }

        public async Task UpdatePhoneOnlineStatusAsync(int userId, bool isOnline)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsPhoneOnline = isOnline;
                user.LastPhoneActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetMissedCallsCountAsync(int userId)
        {
            return await _context.PhoneCalls
                .CountAsync(c => c.ReceiverId == userId && c.Status == CallStatus.Missed);
        }

        public async Task<List<PhoneContact>> SearchContactsAsync(int userId, string query)
        {
            return await _context.PhoneContacts
                .Include(pc => pc.ContactUser)
                .Where(pc => pc.UserId == userId &&
                    (pc.ContactName.Contains(query) ||
                     pc.ContactPhoneNumber.Contains(query) ||
                     pc.ContactUser.DisplayName.Contains(query)))
                .OrderBy(pc => pc.ContactName)
                .ToListAsync();
        }

        public async Task<List<RecentContact>> GetRecentContactsAsync(int userId, int limit = 10)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new List<RecentContact>();

            var recentCalls = await _context.PhoneCalls
                .Include(c => c.Caller)
                .Include(c => c.Receiver)
                .Where(c => c.CallerId == userId || c.ReceiverId == userId)
                .OrderByDescending(c => c.StartTime)
                .Take(limit)
                .Select(c => new RecentContact
                {
                    PhoneNumber = c.CallerId == userId ? c.ReceiverPhoneNumber : c.CallerPhoneNumber,
                    ContactName = c.CallerId == userId ? c.Receiver.DisplayName : c.Caller.DisplayName,
                    ProfilePicture = c.CallerId == userId ? c.Receiver.ProfilePicture : c.Caller.ProfilePicture,
                    LastContactTime = c.StartTime,
                    ContactType = "call",
                    IsOnline = c.CallerId == userId ? c.Receiver.IsPhoneOnline : c.Caller.IsPhoneOnline
                })
                .ToListAsync();

            var recentSms = await _context.SmsMessages
                .Include(s => s.Sender)
                .Include(s => s.Receiver)
                .Where(s => s.SenderId == userId || s.ReceiverId == userId)
                .OrderByDescending(s => s.SentAt)
                .Take(limit)
                .Select(s => new RecentContact
                {
                    PhoneNumber = s.SenderId == userId ? s.ReceiverPhoneNumber : s.SenderPhoneNumber,
                    ContactName = s.SenderId == userId ? s.Receiver.DisplayName : s.Sender.DisplayName,
                    ProfilePicture = s.SenderId == userId ? s.Receiver.ProfilePicture : s.Sender.ProfilePicture,
                    LastContactTime = s.SentAt,
                    ContactType = "sms",
                    IsOnline = s.SenderId == userId ? s.Receiver.IsPhoneOnline : s.Sender.IsPhoneOnline
                })
                .ToListAsync();

            return recentCalls.Concat(recentSms)
                .GroupBy(c => c.PhoneNumber)
                .Select(g => g.OrderByDescending(c => c.LastContactTime).First())
                .OrderByDescending(c => c.LastContactTime)
                .Take(limit)
                .ToList();
        }

        public async Task<bool> IsPhoneNumberAvailableAsync(string phoneNumber)
        {
            return !await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<CallStatistics> GetCallStatisticsAsync(int userId)
        {
            var calls = await _context.PhoneCalls
                .Where(c => c.CallerId == userId || c.ReceiverId == userId)
                .ToListAsync();

            var totalCalls = calls.Count;
            var incomingCalls = calls.Count(c => c.ReceiverId == userId);
            var outgoingCalls = calls.Count(c => c.CallerId == userId);
            var missedCalls = calls.Count(c => c.ReceiverId == userId && c.Status == CallStatus.Missed);

            var completedCalls = calls.Where(c => c.Duration.HasValue).ToList();
            var totalDuration = completedCalls.Sum(c => c.Duration?.TotalSeconds ?? 0);
            var averageDuration = completedCalls.Any() ? totalDuration / completedCalls.Count : 0;

            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            return new CallStatistics
            {
                TotalCalls = totalCalls,
                IncomingCalls = incomingCalls,
                OutgoingCalls = outgoingCalls,
                MissedCalls = missedCalls,
                TotalCallDuration = TimeSpan.FromSeconds(totalDuration),
                AverageCallDuration = TimeSpan.FromSeconds(averageDuration),
                CallsToday = calls.Count(c => c.StartTime.Date == today),
                CallsThisWeek = calls.Count(c => c.StartTime >= thisWeek),
                CallsThisMonth = calls.Count(c => c.StartTime >= thisMonth)
            };
        }

        public async Task<SmsStatistics> GetSmsStatisticsAsync(int userId)
        {
            var messages = await _context.SmsMessages
                .Where(s => s.SenderId == userId || s.ReceiverId == userId)
                .Where(s => !s.IsDeleted)
                .ToListAsync();

            var totalMessages = messages.Count;
            var sentMessages = messages.Count(s => s.SenderId == userId);
            var receivedMessages = messages.Count(s => s.ReceiverId == userId);
            var unreadMessages = messages.Count(s => s.ReceiverId == userId && !s.IsRead);

            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var activeConversations = messages
                .GroupBy(s => s.SenderId == userId ? s.ReceiverPhoneNumber : s.SenderPhoneNumber)
                .Count();

            return new SmsStatistics
            {
                TotalMessages = totalMessages,
                SentMessages = sentMessages,
                ReceivedMessages = receivedMessages,
                UnreadMessages = unreadMessages,
                MessagesToday = messages.Count(s => s.SentAt.Date == today),
                MessagesThisWeek = messages.Count(s => s.SentAt >= thisWeek),
                MessagesThisMonth = messages.Count(s => s.SentAt >= thisMonth),
                ActiveConversations = activeConversations
            };
        }

        public async Task CleanupOldDataAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

            // Delete old call history
            var oldCalls = await _context.PhoneCalls
                .Where(c => c.StartTime < cutoffDate)
                .ToListAsync();

            _context.PhoneCalls.RemoveRange(oldCalls);

            // Delete old SMS messages
            var oldMessages = await _context.SmsMessages
                .Where(s => s.SentAt < cutoffDate)
                .ToListAsync();

            _context.SmsMessages.RemoveRange(oldMessages);

            // Delete old notifications
            var oldNotifications = await _context.PhoneNotifications
                .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                .ToListAsync();

            _context.PhoneNotifications.RemoveRange(oldNotifications);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up old phone data older than {Days} days", daysToKeep);
        }

        private async Task<string> GenerateUniquePhoneNumberAsync()
        {
            var random = new Random();
            string phoneNumber;

            do
            {
                // Generate 3-digit number (100-999)
                phoneNumber = random.Next(100, 1000).ToString();
            }
            while (!await IsPhoneNumberAvailableAsync(phoneNumber));

            return phoneNumber;
        }

        private string GetNotificationIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.IncomingCall => "fas fa-phone-alt",
                NotificationType.MissedCall => "fas fa-phone-slash",
                NotificationType.NewSms => "fas fa-sms",
                NotificationType.CallEnded => "fas fa-phone",
                NotificationType.ContactOnline => "fas fa-circle",
                NotificationType.ContactOffline => "far fa-circle",
                _ => "fas fa-bell"
            };
        }

        private string GetNotificationColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.IncomingCall => "#28a745",
                NotificationType.MissedCall => "#dc3545",
                NotificationType.NewSms => "#007bff",
                NotificationType.CallEnded => "#6c757d",
                NotificationType.ContactOnline => "#28a745",
                NotificationType.ContactOffline => "#6c757d",
                _ => "#17a2b8"
            };
        }
    }
}
