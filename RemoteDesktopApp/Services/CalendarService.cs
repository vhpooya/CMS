using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;
using System.Globalization;

namespace RemoteDesktopApp.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly ILogger<CalendarService> _logger;
        private readonly PersianCalendar _persianCalendar;

        public CalendarService(RemoteDesktopDbContext context, ILogger<CalendarService> logger)
        {
            _context = context;
            _logger = logger;
            _persianCalendar = new PersianCalendar();
        }

        public async Task<List<CalendarEvent>> GetEventsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.CalendarEvents
                .Where(e => e.UserId == userId && 
                           e.StartDate >= startDate && 
                           e.StartDate <= endDate)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<CalendarEvent?> GetEventByIdAsync(int eventId, int userId)
        {
            return await _context.CalendarEvents
                .Include(e => e.Reminders)
                .FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId);
        }

        public async Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent)
        {
            calendarEvent.CreatedAt = DateTime.UtcNow;
            
            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();

            // Create reminders if alarm is set
            if (calendarEvent.HasAlarm)
            {
                await CreateEventRemindersAsync(calendarEvent);
            }

            _logger.LogInformation("Created calendar event {EventId} for user {UserId}", 
                calendarEvent.Id, calendarEvent.UserId);

            return calendarEvent;
        }

        public async Task<CalendarEvent?> UpdateEventAsync(int eventId, int userId, CalendarEvent updatedEvent)
        {
            var existingEvent = await _context.CalendarEvents
                .Include(e => e.Reminders)
                .FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId);

            if (existingEvent == null)
                return null;

            // Update properties
            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartDate = updatedEvent.StartDate;
            existingEvent.EndDate = updatedEvent.EndDate;
            existingEvent.IsAllDay = updatedEvent.IsAllDay;
            existingEvent.Type = updatedEvent.Type;
            existingEvent.Priority = updatedEvent.Priority;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.HasAlarm = updatedEvent.HasAlarm;
            existingEvent.AlarmMinutesBefore = updatedEvent.AlarmMinutesBefore;
            existingEvent.Color = updatedEvent.Color;
            existingEvent.Notes = updatedEvent.Notes;
            existingEvent.UpdatedAt = DateTime.UtcNow;

            // Remove old reminders and create new ones if alarm is set
            _context.EventReminders.RemoveRange(existingEvent.Reminders);
            if (updatedEvent.HasAlarm)
            {
                await CreateEventRemindersAsync(existingEvent);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated calendar event {EventId} for user {UserId}", 
                eventId, userId);

            return existingEvent;
        }

        public async Task<bool> DeleteEventAsync(int eventId, int userId)
        {
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId);

            if (calendarEvent == null)
                return false;

            _context.CalendarEvents.Remove(calendarEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted calendar event {EventId} for user {UserId}", 
                eventId, userId);

            return true;
        }

        public async Task<List<CalendarEvent>> GetTodayEventsAsync(int userId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _context.CalendarEvents
                .Where(e => e.UserId == userId && 
                           e.StartDate >= today && 
                           e.StartDate < tomorrow)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<List<CalendarEvent>> GetUpcomingEventsAsync(int userId, int days = 7)
        {
            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(days);

            return await _context.CalendarEvents
                .Where(e => e.UserId == userId && 
                           e.StartDate >= startDate && 
                           e.StartDate <= endDate)
                .OrderBy(e => e.StartDate)
                .Take(10)
                .ToListAsync();
        }

        public async Task<bool> CompleteEventAsync(int eventId, int userId)
        {
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId);

            if (calendarEvent == null)
                return false;

            calendarEvent.IsCompleted = true;
            calendarEvent.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<PersianHoliday>> GetPersianHolidaysAsync(int persianYear)
        {
            // Persian holidays (static list - in a real app, this could be from database)
            var holidays = new List<PersianHoliday>
            {
                new() { Month = 1, Day = 1, Name = "نوروز", Description = "سال نو فارسی", IsOfficial = true },
                new() { Month = 1, Day = 2, Name = "عید نوروز", Description = "جشن سال نو", IsOfficial = true },
                new() { Month = 1, Day = 3, Name = "عید نوروز", Description = "جشن سال نو", IsOfficial = true },
                new() { Month = 1, Day = 4, Name = "عید نوروز", Description = "جشن سال نو", IsOfficial = true },
                new() { Month = 1, Day = 12, Name = "روز جمهوری اسلامی", Description = "روز ملی", IsOfficial = true },
                new() { Month = 1, Day = 13, Name = "سیزده بدر", Description = "روز طبیعت", IsOfficial = true },
                new() { Month = 3, Day = 14, Name = "رحلت امام خمینی", Description = "روز ملی", IsOfficial = true },
                new() { Month = 3, Day = 15, Name = "قیام 15 خرداد", Description = "روز ملی", IsOfficial = true },
                new() { Month = 11, Day = 22, Name = "پیروزی انقلاب اسلامی", Description = "روز ملی", IsOfficial = true }
            };

            return await Task.FromResult(holidays);
        }

        public PersianDate ConvertToPersianDate(DateTime gregorianDate)
        {
            var persianYear = _persianCalendar.GetYear(gregorianDate);
            var persianMonth = _persianCalendar.GetMonth(gregorianDate);
            var persianDay = _persianCalendar.GetDayOfMonth(gregorianDate);
            var dayOfWeek = _persianCalendar.GetDayOfWeek(gregorianDate);

            var monthNames = new[] { "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", 
                                   "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
            
            var dayNames = new[] { "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنج‌شنبه", "جمعه", "شنبه" };

            return new PersianDate
            {
                Year = persianYear,
                Month = persianMonth,
                Day = persianDay,
                MonthName = monthNames[persianMonth],
                DayName = dayNames[(int)dayOfWeek]
            };
        }

        public DateTime ConvertToGregorianDate(int year, int month, int day)
        {
            return _persianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);
        }

        public async Task<List<EventReminder>> GetPendingRemindersAsync()
        {
            var now = DateTime.UtcNow;
            
            return await _context.EventReminders
                .Include(r => r.Event)
                .ThenInclude(e => e.User)
                .Where(r => !r.IsSent && r.ReminderTime <= now)
                .OrderBy(r => r.ReminderTime)
                .ToListAsync();
        }

        public async Task MarkReminderAsSentAsync(int reminderId)
        {
            var reminder = await _context.EventReminders.FindAsync(reminderId);
            if (reminder != null)
            {
                reminder.IsSent = true;
                reminder.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateEventRemindersAsync(CalendarEvent calendarEvent)
        {
            if (!calendarEvent.HasAlarm || !calendarEvent.AlarmMinutesBefore.HasValue)
                return;

            var reminderTime = calendarEvent.StartDate.AddMinutes(-calendarEvent.AlarmMinutesBefore.Value);
            
            var reminder = new EventReminder
            {
                EventId = calendarEvent.Id,
                ReminderTime = reminderTime,
                Type = ReminderType.Notification,
                Message = $"Reminder: {calendarEvent.Title} starts in {calendarEvent.AlarmMinutesBefore} minutes"
            };

            _context.EventReminders.Add(reminder);
            await _context.SaveChangesAsync();
        }
    }
}
