using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class CalendarEvent
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public bool IsAllDay { get; set; } = false;
        
        [Required]
        public EventType Type { get; set; } = EventType.Personal;
        
        [Required]
        public EventPriority Priority { get; set; } = EventPriority.Medium;
        
        [StringLength(50)]
        public string? Location { get; set; }
        
        public bool HasAlarm { get; set; } = false;
        
        public int? AlarmMinutesBefore { get; set; }
        
        public bool IsRecurring { get; set; } = false;
        
        public RecurrenceType? RecurrenceType { get; set; }
        
        public int? RecurrenceInterval { get; set; }
        
        public DateTime? RecurrenceEndDate { get; set; }
        
        [StringLength(7)] // #RRGGBB format
        public string? Color { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsCompleted { get; set; } = false;
        
        public DateTime? CompletedAt { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        public virtual ICollection<EventReminder> Reminders { get; set; } = new List<EventReminder>();
    }
    
    public class EventReminder
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int EventId { get; set; }
        
        [Required]
        public DateTime ReminderTime { get; set; }
        
        public bool IsSent { get; set; } = false;
        
        public DateTime? SentAt { get; set; }
        
        [Required]
        public ReminderType Type { get; set; } = ReminderType.Notification;
        
        [StringLength(500)]
        public string? Message { get; set; }
        
        // Navigation properties
        [ForeignKey("EventId")]
        public virtual CalendarEvent Event { get; set; } = null!;
    }
    
    public enum EventType
    {
        Personal = 0,
        Work = 1,
        Meeting = 2,
        Deadline = 3,
        Holiday = 4,
        Birthday = 5,
        Appointment = 6,
        Task = 7
    }
    
    public enum EventPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }
    
    public enum RecurrenceType
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        Yearly = 3
    }
    
    public enum ReminderType
    {
        Notification = 0,
        Email = 1,
        SMS = 2,
        Popup = 3
    }
}
