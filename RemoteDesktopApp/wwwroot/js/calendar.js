// Calendar & Events JavaScript

// Global variables
let currentDate = new Date();
let currentView = 'month';
let isPersianCalendar = false;
let events = [];
let persianHolidays = [];
let selectedDate = null;

// Persian calendar data
const persianMonths = [
    '', 'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
    'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
];

const persianDays = ['یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه'];

// Initialize calendar
$(document).ready(function() {
    initializeCalendar();
    setupEventHandlers();
    loadCalendarData();
});

function initializeCalendar() {
    updateCalendarDisplay();
    updateCurrentDateDisplay();
    
    // Set default start date for new events
    const now = new Date();
    const tomorrow = new Date(now.getTime() + 24 * 60 * 60 * 1000);
    document.getElementById('eventStartDate').value = formatDateTimeLocal(tomorrow);
}

function setupEventHandlers() {
    // View type change
    $('input[name="viewType"]').change(function() {
        currentView = this.id.replace('View', '');
        updateCalendarDisplay();
    });

    // Alarm checkbox
    $('#eventHasAlarm').change(function() {
        $('#alarmSettings').toggle(this.checked);
    });

    // Form validation
    $('#eventForm').on('submit', function(e) {
        e.preventDefault();
        saveEvent();
    });
}

function loadCalendarData() {
    loadEvents();
    loadTodayEvents();
    loadUpcomingEvents();
    loadPersianHolidays();
    updateEventCounts();
}

function loadEvents() {
    const startDate = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1);
    const endDate = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0);
    
    fetch(`/api/calendarapi/events?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`, {
        headers: {
            'Authorization': 'Bearer ' + localStorage.getItem('authToken')
        }
    })
    .then(response => response.json())
    .then(data => {
        events = data;
        updateCalendarDisplay();
        updateEventCounts();
    })
    .catch(error => {
        console.error('Error loading events:', error);
        dashboardUtils.showNotification('Failed to load events', 'error');
    });
}

function loadTodayEvents() {
    fetch('/api/calendarapi/today', {
        headers: {
            'Authorization': 'Bearer ' + localStorage.getItem('authToken')
        }
    })
    .then(response => response.json())
    .then(data => {
        displayTodayEvents(data);
    })
    .catch(error => {
        console.error('Error loading today events:', error);
    });
}

function loadUpcomingEvents() {
    fetch('/api/calendarapi/upcoming?days=7', {
        headers: {
            'Authorization': 'Bearer ' + localStorage.getItem('authToken')
        }
    })
    .then(response => response.json())
    .then(data => {
        displayUpcomingEvents(data);
    })
    .catch(error => {
        console.error('Error loading upcoming events:', error);
    });
}

function loadPersianHolidays() {
    const persianYear = isPersianCalendar ? getPersianYear(currentDate) : new Date().getFullYear() - 621;
    
    fetch(`/api/calendarapi/persian-holidays/${persianYear}`, {
        headers: {
            'Authorization': 'Bearer ' + localStorage.getItem('authToken')
        }
    })
    .then(response => response.json())
    .then(data => {
        persianHolidays = data;
        displayPersianHolidays(data);
        updateCalendarDisplay();
    })
    .catch(error => {
        console.error('Error loading Persian holidays:', error);
    });
}

function updateCalendarDisplay() {
    if (currentView === 'month') {
        renderMonthView();
    } else if (currentView === 'week') {
        renderWeekView();
    } else {
        renderDayView();
    }
    
    updateCurrentDateDisplay();
}

function renderMonthView() {
    const calendarBody = document.getElementById('calendar-body');
    calendarBody.innerHTML = '';
    
    const firstDay = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1);
    const lastDay = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0);
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - firstDay.getDay());
    
    for (let i = 0; i < 42; i++) {
        const cellDate = new Date(startDate);
        cellDate.setDate(startDate.getDate() + i);
        
        const dayElement = createDayElement(cellDate, firstDay.getMonth());
        calendarBody.appendChild(dayElement);
    }
}

function createDayElement(date, currentMonth) {
    const dayDiv = document.createElement('div');
    dayDiv.className = 'calendar-day';
    dayDiv.onclick = () => selectDate(date);
    
    // Add classes
    if (date.getMonth() !== currentMonth) {
        dayDiv.classList.add('other-month');
    }
    
    if (isToday(date)) {
        dayDiv.classList.add('today');
    }
    
    if (selectedDate && isSameDay(date, selectedDate)) {
        dayDiv.classList.add('selected');
    }
    
    // Check for holidays
    const persianDate = convertToPersianDate(date);
    const holiday = persianHolidays.find(h => h.month === persianDate.month && h.day === persianDate.day);
    if (holiday) {
        dayDiv.classList.add('holiday');
    }
    
    // Day number
    const dayNumber = document.createElement('div');
    dayNumber.className = 'calendar-day-number';
    dayNumber.textContent = date.getDate();
    dayDiv.appendChild(dayNumber);
    
    // Persian date
    if (isPersianCalendar) {
        const persianDiv = document.createElement('div');
        persianDiv.className = 'calendar-day-persian';
        persianDiv.textContent = `${persianDate.day} ${persianMonths[persianDate.month]}`;
        dayDiv.appendChild(persianDiv);
    }
    
    // Events
    const dayEvents = events.filter(event => isSameDay(new Date(event.startDate), date));
    if (dayEvents.length > 0) {
        dayDiv.classList.add('has-events');
        
        const eventsDiv = document.createElement('div');
        eventsDiv.className = 'calendar-events';
        
        dayEvents.slice(0, 3).forEach(event => {
            const eventDiv = document.createElement('div');
            eventDiv.className = `calendar-event priority-${getPriorityName(event.priority)} type-${getTypeName(event.type)}`;
            eventDiv.textContent = event.title;
            eventDiv.onclick = (e) => {
                e.stopPropagation();
                editEvent(event.id);
            };
            eventsDiv.appendChild(eventDiv);
        });
        
        if (dayEvents.length > 3) {
            const moreDiv = document.createElement('div');
            moreDiv.className = 'calendar-event';
            moreDiv.textContent = `+${dayEvents.length - 3} more`;
            eventsDiv.appendChild(moreDiv);
        }
        
        dayDiv.appendChild(eventsDiv);
    }
    
    return dayDiv;
}

function displayTodayEvents(todayEvents) {
    const container = document.getElementById('todayEvents');
    
    if (todayEvents.length === 0) {
        container.innerHTML = `
            <div class="text-center text-muted py-3">
                <i class="fas fa-calendar-day fa-2x mb-2"></i>
                <p class="mb-0">No events today</p>
            </div>
        `;
        return;
    }
    
    container.innerHTML = todayEvents.map(event => `
        <div class="event-item priority-${getPriorityName(event.priority)}" onclick="editEvent(${event.id})">
            <div class="event-time">${formatTime(event.startDate)}</div>
            <div class="event-content">
                <div class="event-title">${event.title}</div>
                <div class="event-details">
                    ${event.location ? `<i class="fas fa-map-marker-alt me-1"></i>${event.location}` : ''}
                </div>
            </div>
            <div class="event-priority ${getPriorityName(event.priority)}">${getPriorityName(event.priority)}</div>
        </div>
    `).join('');
}

function displayUpcomingEvents(upcomingEvents) {
    const container = document.getElementById('upcomingEvents');
    
    if (upcomingEvents.length === 0) {
        container.innerHTML = `
            <div class="text-center text-muted py-3">
                <i class="fas fa-calendar-plus fa-2x mb-2"></i>
                <p class="mb-0">No upcoming events</p>
            </div>
        `;
        return;
    }
    
    container.innerHTML = upcomingEvents.map(event => `
        <div class="event-item priority-${getPriorityName(event.priority)}" onclick="editEvent(${event.id})">
            <div class="event-time">${formatDate(event.startDate)}</div>
            <div class="event-content">
                <div class="event-title">${event.title}</div>
                <div class="event-details">
                    ${formatTime(event.startDate)}
                    ${event.location ? ` • ${event.location}` : ''}
                </div>
            </div>
        </div>
    `).join('');
}

function displayPersianHolidays(holidays) {
    const container = document.getElementById('persianHolidays');
    
    if (holidays.length === 0) {
        container.innerHTML = `
            <div class="text-center text-muted py-3">
                <i class="fas fa-calendar-star fa-2x mb-2"></i>
                <p class="mb-0">No holidays this year</p>
            </div>
        `;
        return;
    }
    
    // Show next few holidays
    const nextHolidays = holidays.slice(0, 5);
    container.innerHTML = nextHolidays.map(holiday => `
        <div class="holiday-item">
            <div class="holiday-date">${holiday.day}/${holiday.month}</div>
            <div class="holiday-name">${holiday.name}</div>
        </div>
    `).join('');
}

function updateCurrentDateDisplay() {
    const monthYear = document.getElementById('currentMonthYear');
    const persianDateEl = document.getElementById('persianDate');
    
    if (isPersianCalendar) {
        const persianDate = convertToPersianDate(currentDate);
        monthYear.textContent = `${persianMonths[persianDate.month]} ${persianDate.year}`;
        persianDateEl.textContent = `${currentDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' })}`;
    } else {
        monthYear.textContent = currentDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
        const persianDate = convertToPersianDate(currentDate);
        persianDateEl.textContent = `${persianMonths[persianDate.month]} ${persianDate.year}`;
    }
}

function updateEventCounts() {
    const today = new Date();
    const todayEvents = events.filter(event => isSameDay(new Date(event.startDate), today));
    
    const weekStart = new Date(today);
    weekStart.setDate(today.getDate() - today.getDay());
    const weekEnd = new Date(weekStart);
    weekEnd.setDate(weekStart.getDate() + 6);
    const weekEvents = events.filter(event => {
        const eventDate = new Date(event.startDate);
        return eventDate >= weekStart && eventDate <= weekEnd;
    });
    
    const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    const monthEnd = new Date(today.getFullYear(), today.getMonth() + 1, 0);
    const monthEvents = events.filter(event => {
        const eventDate = new Date(event.startDate);
        return eventDate >= monthStart && eventDate <= monthEnd;
    });
    
    document.getElementById('todayEventsCount').textContent = todayEvents.length;
    document.getElementById('weekEventsCount').textContent = weekEvents.length;
    document.getElementById('monthEventsCount').textContent = monthEvents.length;
}

// Navigation functions
function previousMonth() {
    currentDate.setMonth(currentDate.getMonth() - 1);
    loadCalendarData();
}

function nextMonth() {
    currentDate.setMonth(currentDate.getMonth() + 1);
    loadCalendarData();
}

function toggleCalendarType() {
    isPersianCalendar = !isPersianCalendar;
    const btn = document.getElementById('calendarTypeBtn');
    btn.textContent = isPersianCalendar ? 'Gregorian Calendar' : 'Persian Calendar';
    
    const calendarContainer = document.getElementById('calendar-container');
    calendarContainer.classList.toggle('persian-calendar', isPersianCalendar);
    
    updateCalendarDisplay();
}

function selectDate(date) {
    selectedDate = date;
    updateCalendarDisplay();
}

// Event management functions
function showCreateEventModal() {
    document.getElementById('eventModalTitle').textContent = 'Create New Event';
    document.getElementById('deleteEventBtn').style.display = 'none';
    document.getElementById('eventForm').reset();
    document.getElementById('eventId').value = '';
    
    // Set default date
    if (selectedDate) {
        document.getElementById('eventStartDate').value = formatDateTimeLocal(selectedDate);
    }
    
    const modal = new bootstrap.Modal(document.getElementById('eventModal'));
    modal.show();
}

function editEvent(eventId) {
    const event = events.find(e => e.id === eventId);
    if (!event) return;
    
    document.getElementById('eventModalTitle').textContent = 'Edit Event';
    document.getElementById('deleteEventBtn').style.display = 'inline-block';
    
    // Populate form
    document.getElementById('eventId').value = event.id;
    document.getElementById('eventTitle').value = event.title;
    document.getElementById('eventDescription').value = event.description || '';
    document.getElementById('eventStartDate').value = formatDateTimeLocal(new Date(event.startDate));
    document.getElementById('eventEndDate').value = event.endDate ? formatDateTimeLocal(new Date(event.endDate)) : '';
    document.getElementById('eventType').value = event.type;
    document.getElementById('eventPriority').value = event.priority;
    document.getElementById('eventLocation').value = event.location || '';
    document.getElementById('eventAllDay').checked = event.isAllDay;
    document.getElementById('eventHasAlarm').checked = event.hasAlarm;
    document.getElementById('alarmMinutes').value = event.alarmMinutesBefore || 15;
    document.getElementById('eventColor').value = event.color || '#3b82f6';
    document.getElementById('eventNotes').value = event.notes || '';
    
    $('#alarmSettings').toggle(event.hasAlarm);
    
    const modal = new bootstrap.Modal(document.getElementById('eventModal'));
    modal.show();
}

function saveEvent() {
    const form = document.getElementById('eventForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }
    
    const eventData = {
        title: document.getElementById('eventTitle').value,
        description: document.getElementById('eventDescription').value,
        startDate: new Date(document.getElementById('eventStartDate').value).toISOString(),
        endDate: document.getElementById('eventEndDate').value ? 
                 new Date(document.getElementById('eventEndDate').value).toISOString() : null,
        isAllDay: document.getElementById('eventAllDay').checked,
        type: parseInt(document.getElementById('eventType').value),
        priority: parseInt(document.getElementById('eventPriority').value),
        location: document.getElementById('eventLocation').value,
        hasAlarm: document.getElementById('eventHasAlarm').checked,
        alarmMinutesBefore: document.getElementById('eventHasAlarm').checked ? 
                           parseInt(document.getElementById('alarmMinutes').value) : null,
        color: document.getElementById('eventColor').value,
        notes: document.getElementById('eventNotes').value,
        isRecurring: false,
        recurrenceType: null,
        recurrenceInterval: null,
        recurrenceEndDate: null
    };
    
    const eventId = document.getElementById('eventId').value;
    const url = eventId ? `/api/calendarapi/events/${eventId}` : '/api/calendarapi/events';
    const method = eventId ? 'PUT' : 'POST';
    
    fetch(url, {
        method: method,
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + localStorage.getItem('authToken')
        },
        body: JSON.stringify(eventData)
    })
    .then(response => response.json())
    .then(data => {
        dashboardUtils.showNotification(
            eventId ? 'Event updated successfully!' : 'Event created successfully!', 
            'success'
        );
        
        const modal = bootstrap.Modal.getInstance(document.getElementById('eventModal'));
        modal.hide();
        
        loadCalendarData();
    })
    .catch(error => {
        console.error('Error saving event:', error);
        dashboardUtils.showNotification('Failed to save event', 'error');
    });
}

function deleteEvent() {
    const eventId = document.getElementById('eventId').value;
    if (!eventId) return;
    
    if (!confirm('Are you sure you want to delete this event?')) return;
    
    fetch(`/api/calendarapi/events/${eventId}`, {
        method: 'DELETE',
        headers: {
            'Authorization': 'Bearer ' + localStorage.getItem('authToken')
        }
    })
    .then(() => {
        dashboardUtils.showNotification('Event deleted successfully!', 'success');
        
        const modal = bootstrap.Modal.getInstance(document.getElementById('eventModal'));
        modal.hide();
        
        loadCalendarData();
    })
    .catch(error => {
        console.error('Error deleting event:', error);
        dashboardUtils.showNotification('Failed to delete event', 'error');
    });
}

// Utility functions
function isToday(date) {
    const today = new Date();
    return isSameDay(date, today);
}

function isSameDay(date1, date2) {
    return date1.getDate() === date2.getDate() &&
           date1.getMonth() === date2.getMonth() &&
           date1.getFullYear() === date2.getFullYear();
}

function formatDateTimeLocal(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    
    return `${year}-${month}-${day}T${hours}:${minutes}`;
}

function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric'
    });
}

function formatTime(dateString) {
    return new Date(dateString).toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit'
    });
}

function getPriorityName(priority) {
    const priorities = ['low', 'medium', 'high', 'critical'];
    return priorities[priority] || 'medium';
}

function getTypeName(type) {
    const types = ['personal', 'work', 'meeting', 'deadline', 'holiday', 'birthday', 'appointment', 'task'];
    return types[type] || 'personal';
}

function convertToPersianDate(gregorianDate) {
    // Simple Persian calendar conversion (approximation)
    const persianYear = gregorianDate.getFullYear() - 621;
    const persianMonth = gregorianDate.getMonth() + 1;
    const persianDay = gregorianDate.getDate();
    
    return {
        year: persianYear,
        month: persianMonth,
        day: persianDay
    };
}

function getPersianYear(date) {
    return date.getFullYear() - 621;
}

// Week and Day view functions (simplified for now)
function renderWeekView() {
    document.getElementById('calendar-body').innerHTML = '<div class="p-4 text-center text-muted">Week view coming soon...</div>';
}

function renderDayView() {
    document.getElementById('calendar-body').innerHTML = '<div class="p-4 text-center text-muted">Day view coming soon...</div>';
}
