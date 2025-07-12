// Business Workspace Pro - Dashboard JavaScript

// Global variables
let currentUser = null;
let notificationConnection = null;

// Initialize dashboard
$(document).ready(function() {
    initializeDashboard();
});

// Get current user from server (cookie-based auth)
async function getCurrentUser() {
    try {
        const response = await fetch('/api/auth/current-user', {
            method: 'GET',
            credentials: 'include'
        });

        if (response.ok) {
            return await response.json();
        } else {
            throw new Error('User not authenticated');
        }
    } catch (error) {
        console.error('Error getting current user:', error);
        throw error;
    }
}

// Initialize dashboard functionality
async function initializeDashboard() {
    try {
        // Get current user from server
        currentUser = await getCurrentUser();

        // Setup sidebar toggle for mobile
        setupSidebarToggle();

        // Setup tooltips
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });

        // Initialize notification hub
        setupNotifications();

        // Load user data
        loadUserData();

        // Setup auto-refresh for time-sensitive data
        setInterval(refreshDashboardData, 30000); // Refresh every 30 seconds

    } catch (error) {
        console.error('Error initializing dashboard:', error);
        // Redirect to login if user not authenticated
        window.location.href = '/Home/Login';
    }
}

// Setup sidebar toggle for mobile
function setupSidebarToggle() {
    // Add mobile menu button if not exists
    if (window.innerWidth <= 768 && !document.getElementById('sidebarToggle')) {
        const navbar = document.querySelector('.navbar .container-fluid');
        const toggleBtn = document.createElement('button');
        toggleBtn.id = 'sidebarToggle';
        toggleBtn.className = 'btn btn-outline-light d-md-none me-2';
        toggleBtn.innerHTML = '<i class="fas fa-bars"></i>';
        toggleBtn.onclick = toggleSidebar;
        navbar.insertBefore(toggleBtn, navbar.firstChild);
    }
}

// Toggle sidebar visibility on mobile
function toggleSidebar() {
    const sidebar = document.querySelector('.sidebar');
    sidebar.classList.toggle('show');
}

// Setup real-time notifications
function setupNotifications() {
    // Initialize SignalR connection for notifications
    notificationConnection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationhub")
        .build();

    notificationConnection.start().then(function () {
        console.log("Notification hub connected");
        
        // Join user's notification group
        if (currentUser) {
            notificationConnection.invoke("JoinUserGroup", currentUser.userId);
        }
    }).catch(function (err) {
        console.error("Notification hub connection error:", err);
    });

    // Handle incoming notifications
    notificationConnection.on("ReceiveNotification", function (notification) {
        showNotification(notification.message, notification.type);
        updateNotificationBadge();
    });
}

// Load user-specific data
function loadUserData() {
    if (!currentUser) return;

    // Load dashboard statistics
    loadDashboardStats();
    
    // Load recent activity
    loadRecentActivity();
    
    // Load upcoming events
    loadUpcomingEvents();
}

// Load dashboard statistics
function loadDashboardStats() {
    // TODO: Replace with actual API calls
    fetch('/api/dashboard/stats', {
        headers: {
            'Authorization': 'Bearer ' + localStorage.getItem('authToken')
        }
    })
    .then(response => response.json())
    .then(data => {
        updateStatsCards(data);
    })
    .catch(error => {
        console.error('Error loading stats:', error);
        // Use mock data for now
        updateStatsCards({
            todayEvents: 3,
            totalSpreadsheets: 12,
            codeProjects: 8,
            unreadMessages: 5
        });
    });
}

// Update statistics cards
function updateStatsCards(stats) {
    const elements = {
        'todayEvents': stats.todayEvents || 0,
        'totalSpreadsheets': stats.totalSpreadsheets || 0,
        'codeProjects': stats.codeProjects || 0,
        'unreadMessages': stats.unreadMessages || 0
    };

    Object.keys(elements).forEach(id => {
        const element = document.getElementById(id);
        if (element) {
            animateNumber(element, elements[id]);
        }
    });
}

// Animate number counting
function animateNumber(element, targetValue) {
    const startValue = parseInt(element.textContent) || 0;
    const duration = 1000;
    const startTime = performance.now();

    function updateNumber(currentTime) {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);
        
        const currentValue = Math.floor(startValue + (targetValue - startValue) * progress);
        element.textContent = currentValue;

        if (progress < 1) {
            requestAnimationFrame(updateNumber);
        }
    }

    requestAnimationFrame(updateNumber);
}

// Load recent activity
function loadRecentActivity() {
    // TODO: Replace with actual API call
    console.log('Loading recent activity...');
}

// Load upcoming events
function loadUpcomingEvents() {
    // TODO: Replace with actual API call
    console.log('Loading upcoming events...');
}

// Refresh dashboard data
function refreshDashboardData() {
    if (document.visibilityState === 'visible') {
        loadDashboardStats();
        updateNotificationBadge();
    }
}

// Show notification
function showNotification(message, type = 'info', duration = 5000) {
    const alertClass = {
        'success': 'alert-success',
        'error': 'alert-danger',
        'warning': 'alert-warning',
        'info': 'alert-info'
    }[type] || 'alert-info';

    const notification = document.createElement('div');
    notification.className = `alert ${alertClass} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 80px; right: 20px; z-index: 1050; min-width: 300px; max-width: 400px;';
    notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="fas fa-${getNotificationIcon(type)} me-2"></i>
            <span>${message}</span>
            <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert"></button>
        </div>
    `;

    document.body.appendChild(notification);

    // Auto-remove after duration
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, duration);
}

// Get notification icon based on type
function getNotificationIcon(type) {
    const icons = {
        'success': 'check-circle',
        'error': 'exclamation-triangle',
        'warning': 'exclamation-circle',
        'info': 'info-circle'
    };
    return icons[type] || 'info-circle';
}

// Update notification badge
function updateNotificationBadge() {
    // TODO: Get actual unread count from API
    const badge = document.getElementById('notificationCount');
    if (badge) {
        // Mock count for now
        badge.textContent = '3';
    }
}

// Utility functions
function formatDate(date) {
    return new Date(date).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

function formatTime(date) {
    return new Date(date).toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit'
    });
}

function formatDateTime(date) {
    return formatDate(date) + ' ' + formatTime(date);
}

// Handle window resize
window.addEventListener('resize', function() {
    setupSidebarToggle();
    
    // Hide sidebar on mobile when resizing to desktop
    if (window.innerWidth > 768) {
        const sidebar = document.querySelector('.sidebar');
        sidebar.classList.remove('show');
    }
});

// Handle page visibility change
document.addEventListener('visibilitychange', function() {
    if (document.visibilityState === 'visible') {
        refreshDashboardData();
    }
});

// Export functions for global use
window.dashboardUtils = {
    showNotification,
    formatDate,
    formatTime,
    formatDateTime,
    refreshDashboardData
};
