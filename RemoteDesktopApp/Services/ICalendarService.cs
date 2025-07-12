using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public interface ICalendarService
    {
        /// <summary>
        /// Gets events for a specific user within a date range
        /// </summary>
        Task<List<CalendarEvent>> GetEventsAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets a specific event by ID
        /// </summary>
        Task<CalendarEvent?> GetEventByIdAsync(int eventId, int userId);
        
        /// <summary>
        /// Creates a new calendar event
        /// </summary>
        Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent);
        
        /// <summary>
        /// Updates an existing calendar event
        /// </summary>
        Task<CalendarEvent?> UpdateEventAsync(int eventId, int userId, CalendarEvent updatedEvent);
        
        /// <summary>
        /// Deletes a calendar event
        /// </summary>
        Task<bool> DeleteEventAsync(int eventId, int userId);
        
        /// <summary>
        /// Gets events for today for a specific user
        /// </summary>
        Task<List<CalendarEvent>> GetTodayEventsAsync(int userId);
        
        /// <summary>
        /// Gets upcoming events for a specific user
        /// </summary>
        Task<List<CalendarEvent>> GetUpcomingEventsAsync(int userId, int days = 7);
        
        /// <summary>
        /// Marks an event as completed
        /// </summary>
        Task<bool> CompleteEventAsync(int eventId, int userId);
        
        /// <summary>
        /// Gets Persian/Solar calendar holidays for a specific year
        /// </summary>
        Task<List<PersianHoliday>> GetPersianHolidaysAsync(int persianYear);
        
        /// <summary>
        /// Converts Gregorian date to Persian date
        /// </summary>
        PersianDate ConvertToPersianDate(DateTime gregorianDate);
        
        /// <summary>
        /// Converts Persian date to Gregorian date
        /// </summary>
        DateTime ConvertToGregorianDate(int year, int month, int day);
        
        /// <summary>
        /// Gets events that need reminders to be sent
        /// </summary>
        Task<List<EventReminder>> GetPendingRemindersAsync();
        
        /// <summary>
        /// Marks a reminder as sent
        /// </summary>
        Task MarkReminderAsSentAsync(int reminderId);
        
        /// <summary>
        /// Creates reminders for an event
        /// </summary>
        Task CreateEventRemindersAsync(CalendarEvent calendarEvent);
    }
    
    public class PersianDate
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public string DayName { get; set; } = string.Empty;
        public bool IsHoliday { get; set; }
        public string? HolidayName { get; set; }
    }
    
    public class PersianHoliday
    {
        public int Month { get; set; }
        public int Day { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsOfficial { get; set; } = true;
    }
}
