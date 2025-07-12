using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopApp.Models;
using RemoteDesktopApp.Services;

namespace RemoteDesktopApp.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarApiController : ControllerBase
    {
        private readonly ICalendarService _calendarService;
        private readonly ILogger<CalendarApiController> _logger;

        public CalendarApiController(ICalendarService calendarService, ILogger<CalendarApiController> logger)
        {
            _calendarService = calendarService;
            _logger = logger;
        }

        [HttpGet("events")]
        public async Task<ActionResult<List<CalendarEvent>>> GetEvents(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today.AddDays(30);

            var events = await _calendarService.GetEventsAsync(userId.Value, start, end);
            return Ok(events);
        }

        [HttpGet("events/{id}")]
        public async Task<ActionResult<CalendarEvent>> GetEvent(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var calendarEvent = await _calendarService.GetEventByIdAsync(id, userId.Value);
            if (calendarEvent == null)
                return NotFound();

            return Ok(calendarEvent);
        }

        [HttpPost("events")]
        public async Task<ActionResult<CalendarEvent>> CreateEvent([FromBody] CreateEventRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var calendarEvent = new CalendarEvent
                {
                    UserId = userId.Value,
                    Title = request.Title,
                    Description = request.Description,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsAllDay = request.IsAllDay,
                    Type = request.Type,
                    Priority = request.Priority,
                    Location = request.Location,
                    HasAlarm = request.HasAlarm,
                    AlarmMinutesBefore = request.AlarmMinutesBefore,
                    IsRecurring = request.IsRecurring,
                    RecurrenceType = request.RecurrenceType,
                    RecurrenceInterval = request.RecurrenceInterval,
                    RecurrenceEndDate = request.RecurrenceEndDate,
                    Color = request.Color,
                    Notes = request.Notes
                };

                var createdEvent = await _calendarService.CreateEventAsync(calendarEvent);
                return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar event for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while creating the event" });
            }
        }

        [HttpPut("events/{id}")]
        public async Task<ActionResult<CalendarEvent>> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var updatedEvent = new CalendarEvent
                {
                    Title = request.Title,
                    Description = request.Description,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsAllDay = request.IsAllDay,
                    Type = request.Type,
                    Priority = request.Priority,
                    Location = request.Location,
                    HasAlarm = request.HasAlarm,
                    AlarmMinutesBefore = request.AlarmMinutesBefore,
                    IsRecurring = request.IsRecurring,
                    RecurrenceType = request.RecurrenceType,
                    RecurrenceInterval = request.RecurrenceInterval,
                    RecurrenceEndDate = request.RecurrenceEndDate,
                    Color = request.Color,
                    Notes = request.Notes
                };

                var result = await _calendarService.UpdateEventAsync(id, userId.Value, updatedEvent);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating calendar event {EventId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while updating the event" });
            }
        }

        [HttpDelete("events/{id}")]
        public async Task<ActionResult> DeleteEvent(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _calendarService.DeleteEventAsync(id, userId.Value);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPost("events/{id}/complete")]
        public async Task<ActionResult> CompleteEvent(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _calendarService.CompleteEventAsync(id, userId.Value);
            if (!success)
                return NotFound();

            return Ok(new { message = "Event marked as completed" });
        }

        [HttpGet("today")]
        public async Task<ActionResult<List<CalendarEvent>>> GetTodayEvents()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var events = await _calendarService.GetTodayEventsAsync(userId.Value);
            return Ok(events);
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<List<CalendarEvent>>> GetUpcomingEvents([FromQuery] int days = 7)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var events = await _calendarService.GetUpcomingEventsAsync(userId.Value, days);
            return Ok(events);
        }

        [HttpGet("persian-date")]
        public ActionResult<PersianDate> GetPersianDate([FromQuery] DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;
            var persianDate = _calendarService.ConvertToPersianDate(targetDate);
            return Ok(persianDate);
        }

        [HttpGet("persian-holidays/{year}")]
        public async Task<ActionResult<List<PersianHoliday>>> GetPersianHolidays(int year)
        {
            var holidays = await _calendarService.GetPersianHolidaysAsync(year);
            return Ok(holidays);
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

    public class CreateEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsAllDay { get; set; }
        public EventType Type { get; set; }
        public EventPriority Priority { get; set; }
        public string? Location { get; set; }
        public bool HasAlarm { get; set; }
        public int? AlarmMinutesBefore { get; set; }
        public bool IsRecurring { get; set; }
        public RecurrenceType? RecurrenceType { get; set; }
        public int? RecurrenceInterval { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
        public string? Color { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateEventRequest : CreateEventRequest
    {
    }
}
