using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Data
{
    public class RemoteDesktopDbContext : DbContext
    {
        public RemoteDesktopDbContext(DbContextOptions<RemoteDesktopDbContext> options) : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<ConnectionRequest> ConnectionRequests { get; set; }
        public DbSet<RemoteConnection> RemoteConnections { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<FileTransfer> FileTransfers { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<EventReminder> EventReminders { get; set; }
        public DbSet<Spreadsheet> Spreadsheets { get; set; }
        public DbSet<SpreadsheetShare> SpreadsheetShares { get; set; }
        public DbSet<SpreadsheetVersion> SpreadsheetVersions { get; set; }
        public DbSet<CodeProject> CodeProjects { get; set; }
        public DbSet<CodeExecution> CodeExecutions { get; set; }
        public DbSet<CodeVersion> CodeVersions { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserPerformanceMetric> UserPerformanceMetrics { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<MessageRead> MessageReads { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<UserGroupMember> UserGroupMembers { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<GroupPermission> GroupPermissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<CodeLibrary> CodeLibraries { get; set; }
        public DbSet<CodeLibraryRating> CodeLibraryRatings { get; set; }
        public DbSet<CodeExecutionHistory> CodeExecutionHistories { get; set; }
        public DbSet<PhoneCall> PhoneCalls { get; set; }
        public DbSet<SmsMessage> SmsMessages { get; set; }
        public DbSet<PhoneContact> PhoneContacts { get; set; }
        public DbSet<PhoneNotification> PhoneNotifications { get; set; }
        public DbSet<PhoneSettings> PhoneSettings { get; set; }

        // Unit management
        public DbSet<Unit> Units { get; set; }
        public DbSet<UnitLink> UnitLinks { get; set; }
        public DbSet<UnitCommunicationPermission> UnitCommunicationPermissions { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.ClientId).IsUnique();
                entity.Property(e => e.ClientId).HasMaxLength(12);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.DeactivatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.DeactivatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            // ConnectionRequest configuration
            modelBuilder.Entity<ConnectionRequest>(entity =>
            {
                entity.HasOne(e => e.Requester)
                    .WithMany(u => u.SentRequests)
                    .HasForeignKey(e => e.RequesterId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.TargetUser)
                    .WithMany(u => u.ReceivedRequests)
                    .HasForeignKey(e => e.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasIndex(e => new { e.RequesterId, e.TargetUserId, e.Status });
            });
            
            // RemoteConnection configuration
            modelBuilder.Entity<RemoteConnection>(entity =>
            {
                entity.HasOne(e => e.ControllerUser)
                    .WithMany(u => u.OutgoingConnections)
                    .HasForeignKey(e => e.ControllerUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.ControlledUser)
                    .WithMany(u => u.IncomingConnections)
                    .HasForeignKey(e => e.ControlledUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasIndex(e => e.SessionId).IsUnique();
                entity.HasIndex(e => e.ConnectionId);
                entity.HasIndex(e => new { e.ControllerUserId, e.ControlledUserId, e.Status });
            });
            
            // ChatMessage configuration
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedMessages)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Connection)
                    .WithMany(c => c.ChatMessages)
                    .HasForeignKey(e => e.ConnectionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ReplyToMessage)
                    .WithMany()
                    .HasForeignKey(e => e.ReplyToMessageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.SentAt });
                entity.HasIndex(e => new { e.ConversationId, e.SentAt });
                entity.HasIndex(e => e.ConnectionId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.IsRead);
            });
            
            // FileTransfer configuration
            modelBuilder.Entity<FileTransfer>(entity =>
            {
                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentFiles)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedFiles)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Connection)
                    .WithMany(c => c.FileTransfers)
                    .HasForeignKey(e => e.ConnectionId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.Status });
                entity.HasIndex(e => e.ConnectionId);
            });

            // CalendarEvent configuration
            modelBuilder.Entity<CalendarEvent>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.CalendarEvents)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.StartDate });
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Priority);
            });

            // EventReminder configuration
            modelBuilder.Entity<EventReminder>(entity =>
            {
                entity.HasOne(e => e.Event)
                    .WithMany(c => c.Reminders)
                    .HasForeignKey(e => e.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ReminderTime, e.IsSent });
            });

            // Spreadsheet configuration
            modelBuilder.Entity<Spreadsheet>(entity =>
            {
                entity.HasOne(e => e.Owner)
                    .WithMany(u => u.Spreadsheets)
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.OwnerId, e.CreatedAt });
                entity.HasIndex(e => e.IsPublic);
                entity.HasIndex(e => e.Category);
            });

            // SpreadsheetShare configuration
            modelBuilder.Entity<SpreadsheetShare>(entity =>
            {
                entity.HasOne(e => e.Spreadsheet)
                    .WithMany(s => s.Shares)
                    .HasForeignKey(e => e.SpreadsheetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SharedBy)
                    .WithMany()
                    .HasForeignKey(e => e.SharedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.SpreadsheetId, e.UserId }).IsUnique();
            });

            // SpreadsheetVersion configuration
            modelBuilder.Entity<SpreadsheetVersion>(entity =>
            {
                entity.HasOne(e => e.Spreadsheet)
                    .WithMany(s => s.Versions)
                    .HasForeignKey(e => e.SpreadsheetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.SpreadsheetId, e.Version }).IsUnique();
            });

            // CodeProject configuration
            modelBuilder.Entity<CodeProject>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.CodeProjects)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => e.Language);
                entity.HasIndex(e => e.IsPublic);
                entity.HasIndex(e => e.Category);
            });

            // CodeExecution configuration
            modelBuilder.Entity<CodeExecution>(entity =>
            {
                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Executions)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.ProjectId, e.ExecutedAt });
                entity.HasIndex(e => e.Status);
            });

            // CodeVersion configuration
            modelBuilder.Entity<CodeVersion>(entity =>
            {
                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Versions)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.ProjectId, e.Version }).IsUnique();
            });

            // UserActivity configuration
            modelBuilder.Entity<UserActivity>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Activities)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.Timestamp });
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Module);
                entity.HasIndex(e => e.Severity);
            });

            // UserSession configuration
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.SessionId).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.StartTime });
                entity.HasIndex(e => e.IsActive);
            });

            // UserPerformanceMetric configuration
            modelBuilder.Entity<UserPerformanceMetric>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
                entity.HasIndex(e => e.Date);
            });

            // Conversation configuration
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasOne(e => e.CreatedBy)
                    .WithMany(u => u.CreatedConversations)
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.LastMessageAt);
            });

            // ConversationParticipant configuration
            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Participants)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.ConversationParticipants)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.ConversationId, e.UserId }).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // MessageReaction configuration
            modelBuilder.Entity<MessageReaction>(entity =>
            {
                entity.HasOne(e => e.Message)
                    .WithMany(m => m.Reactions)
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.MessageReactions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.MessageId, e.UserId, e.Emoji }).IsUnique();
            });

            // MessageRead configuration
            modelBuilder.Entity<MessageRead>(entity =>
            {
                entity.HasOne(e => e.Message)
                    .WithMany(m => m.ReadReceipts)
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.MessageReads)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.MessageId, e.UserId }).IsUnique();
                entity.HasIndex(e => e.ReadAt);
            });

            // UserGroup configuration
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.HasOne(e => e.CreatedBy)
                    .WithMany(u => u.CreatedGroups)
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
            });

            // UserGroupMember configuration
            modelBuilder.Entity<UserGroupMember>(entity =>
            {
                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Members)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.GroupMemberships)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.AddedBy)
                    .WithMany()
                    .HasForeignKey(e => e.AddedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // Permission configuration
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasIndex(e => e.Key).IsUnique();
                entity.HasIndex(e => new { e.Module, e.Action });
                entity.HasIndex(e => e.Level);
            });

            // GroupPermission configuration
            modelBuilder.Entity<GroupPermission>(entity =>
            {
                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Permissions)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                    .WithMany(p => p.GroupPermissions)
                    .HasForeignKey(e => e.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.GrantedBy)
                    .WithMany()
                    .HasForeignKey(e => e.GrantedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.GroupId, e.PermissionId }).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // UserPermission configuration
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserPermissions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                    .WithMany(p => p.UserPermissions)
                    .HasForeignKey(e => e.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.GrantedBy)
                    .WithMany()
                    .HasForeignKey(e => e.GrantedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.UserId, e.PermissionId }).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Source);
            });

            // CodeLibrary configuration
            modelBuilder.Entity<CodeLibrary>(entity =>
            {
                entity.HasOne(e => e.CreatedBy)
                    .WithMany(u => u.CodeLibraries)
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Language);
                entity.HasIndex(e => e.IsPublic);
                entity.HasIndex(e => e.IsTemplate);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Rating);
            });

            // CodeLibraryRating configuration
            modelBuilder.Entity<CodeLibraryRating>(entity =>
            {
                entity.HasOne(e => e.CodeLibrary)
                    .WithMany(c => c.Ratings)
                    .HasForeignKey(e => e.CodeLibraryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.CodeLibraryRatings)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.CodeLibraryId, e.UserId }).IsUnique();
            });

            // CodeExecutionHistory configuration
            modelBuilder.Entity<CodeExecutionHistory>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExecutedAt);
                entity.HasIndex(e => e.Success);
            });

            // PhoneCall configuration
            modelBuilder.Entity<PhoneCall>(entity =>
            {
                entity.HasOne(e => e.Caller)
                    .WithMany(u => u.OutgoingCalls)
                    .HasForeignKey(e => e.CallerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.IncomingCalls)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.CallerId, e.ReceiverId, e.StartTime });
                entity.HasIndex(e => e.CallerPhoneNumber);
                entity.HasIndex(e => e.ReceiverPhoneNumber);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.StartTime);
            });

            // SmsMessage configuration
            modelBuilder.Entity<SmsMessage>(entity =>
            {
                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentSmsMessages)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedSmsMessages)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.SentAt });
                entity.HasIndex(e => e.SenderPhoneNumber);
                entity.HasIndex(e => e.ReceiverPhoneNumber);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.SentAt);
            });

            // PhoneContact configuration
            modelBuilder.Entity<PhoneContact>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.PhoneContacts)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ContactUser)
                    .WithMany(u => u.ContactedBy)
                    .HasForeignKey(e => e.ContactUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.UserId, e.ContactUserId }).IsUnique();
                entity.HasIndex(e => e.ContactPhoneNumber);
                entity.HasIndex(e => e.IsFavorite);
                entity.HasIndex(e => e.IsBlocked);
            });

            // PhoneNotification configuration
            modelBuilder.Entity<PhoneNotification>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.PhoneNotifications)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.IsRead });
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Priority);
            });

            // PhoneSettings configuration
            modelBuilder.Entity<PhoneSettings>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithOne(u => u.PhoneSettings)
                    .HasForeignKey<PhoneSettings>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId).IsUnique();
            });

            // Add unique constraint for phone numbers
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
            });

            // Unit configuration
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Name);

                // Self-referencing relationship for parent-child units
                entity.HasOne(e => e.ParentUnit)
                    .WithMany(e => e.SubUnits)
                    .HasForeignKey(e => e.ParentUnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Manager relationship
                entity.HasOne(e => e.Manager)
                    .WithMany()
                    .HasForeignKey(e => e.ManagerId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Created by relationship
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // User-Unit relationship
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasOne(e => e.Unit)
                    .WithMany(e => e.Users)
                    .HasForeignKey(e => e.UnitId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // UnitLink configuration
            modelBuilder.Entity<UnitLink>(entity =>
            {
                entity.HasOne(e => e.SourceUnit)
                    .WithMany(e => e.LinkedUnits)
                    .HasForeignKey(e => e.SourceUnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TargetUnit)
                    .WithMany(e => e.LinkedByUnits)
                    .HasForeignKey(e => e.TargetUnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Prevent self-linking
                entity.HasCheckConstraint("CK_UnitLink_NoSelfLink", "[SourceUnitId] <> [TargetUnitId]");
            });

            // UnitCommunicationPermission configuration
            modelBuilder.Entity<UnitCommunicationPermission>(entity =>
            {
                entity.HasOne(e => e.SourceUnit)
                    .WithMany(e => e.CommunicationPermissions)
                    .HasForeignKey(e => e.SourceUnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TargetUnit)
                    .WithMany(e => e.AllowedCommunications)
                    .HasForeignKey(e => e.TargetUnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Unique constraint for source-target-type combination
                entity.HasIndex(e => new { e.SourceUnitId, e.TargetUnitId, e.CommunicationType }).IsUnique();
            });
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This will be overridden by DI configuration
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RemoteDesktopApp;Trusted_Connection=true;MultipleActiveResultSets=true");
            }
        }
    }
}
