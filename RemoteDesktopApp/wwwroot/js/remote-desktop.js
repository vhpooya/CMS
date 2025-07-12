// Remote Desktop Client JavaScript
class RemoteDesktopClient {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.screenUpdateInterval = null;
        this.currentQuality = 85;
        this.currentMonitor = -1;
        this.frameRate = 30;
        this.isMouseDown = false;
        this.lastMousePosition = { x: 0, y: 0 };
        
        this.initializeElements();
        this.setupEventListeners();
        this.checkAuthentication();
    }
    
    initializeElements() {
        this.elements = {
            connectBtn: document.getElementById('connectBtn'),
            disconnectBtn: document.getElementById('disconnectBtn'),
            connectionStatus: document.getElementById('connectionStatus'),
            connectionInfo: document.getElementById('connectionInfo'),
            loadingIndicator: document.getElementById('loadingIndicator'),
            desktopScreen: document.getElementById('desktopScreen'),
            desktopViewer: document.getElementById('desktopViewer'),
            qualitySelect: document.getElementById('qualitySelect'),
            monitorSelect: document.getElementById('monitorSelect'),
            fpsSlider: document.getElementById('fpsSlider'),
            fpsValue: document.getElementById('fpsValue'),
            fullscreenBtn: document.getElementById('fullscreenBtn'),
            screenshotBtn: document.getElementById('screenshotBtn'),
            keyboardBtn: document.getElementById('keyboardBtn')
        };
    }
    
    setupEventListeners() {
        // Connection buttons
        this.elements.connectBtn.addEventListener('click', () => this.connect());
        this.elements.disconnectBtn.addEventListener('click', () => this.disconnect());
        
        // Control changes
        this.elements.qualitySelect.addEventListener('change', (e) => {
            this.currentQuality = parseInt(e.target.value);
        });
        
        this.elements.monitorSelect.addEventListener('change', (e) => {
            this.currentMonitor = parseInt(e.target.value);
        });
        
        this.elements.fpsSlider.addEventListener('input', (e) => {
            this.frameRate = parseInt(e.target.value);
            this.elements.fpsValue.textContent = this.frameRate;
            this.updateScreenCaptureInterval();
        });
        
        // Desktop viewer interactions
        this.elements.desktopViewer.addEventListener('mousedown', (e) => this.handleMouseDown(e));
        this.elements.desktopViewer.addEventListener('mouseup', (e) => this.handleMouseUp(e));
        this.elements.desktopViewer.addEventListener('mousemove', (e) => this.handleMouseMove(e));
        this.elements.desktopViewer.addEventListener('wheel', (e) => this.handleMouseWheel(e));
        this.elements.desktopViewer.addEventListener('contextmenu', (e) => e.preventDefault());
        
        // Keyboard events
        document.addEventListener('keydown', (e) => this.handleKeyDown(e));
        document.addEventListener('keyup', (e) => this.handleKeyUp(e));
        
        // Other buttons
        this.elements.fullscreenBtn.addEventListener('click', () => this.toggleFullscreen());
        this.elements.screenshotBtn.addEventListener('click', () => this.takeScreenshot());
        this.elements.keyboardBtn.addEventListener('click', () => this.showVirtualKeyboard());
    }
    
    checkAuthentication() {
        const token = localStorage.getItem('authToken');
        if (!token) {
            window.location.href = '/Home/Login';
            return;
        }
        
        // Validate token
        fetch('/api/auth/validate', {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        })
        .then(response => response.json())
        .then(data => {
            if (!data.valid) {
                localStorage.removeItem('authToken');
                localStorage.removeItem('user');
                window.location.href = '/Home/Login';
            }
        })
        .catch(error => {
            console.error('Token validation error:', error);
            window.location.href = '/Home/Login';
        });
    }
    
    async connect() {
        try {
            this.updateConnectionStatus('connecting', 'Connecting...');
            this.elements.connectBtn.style.display = 'none';
            
            const token = localStorage.getItem('authToken');
            
            // Create SignalR connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/remotehub', {
                    accessTokenFactory: () => token
                })
                .withAutomaticReconnect()
                .build();
            
            // Setup connection event handlers
            this.setupConnectionHandlers();
            
            // Start connection
            await this.connection.start();
            
            this.isConnected = true;
            this.updateConnectionStatus('online', 'Connected');
            this.elements.disconnectBtn.style.display = 'inline-block';
            
            // Start screen capture
            this.startScreenCapture();
            
        } catch (error) {
            console.error('Connection error:', error);
            this.updateConnectionStatus('offline', 'Connection failed');
            this.elements.connectBtn.style.display = 'inline-block';
        }
    }
    
    setupConnectionHandlers() {
        this.connection.on('ScreenInfo', (data) => {
            console.log('Screen info received:', data);
            this.updateMonitorList(data.Monitors);
        });
        
        this.connection.on('ScreenCapture', (imageData) => {
            this.updateScreen(imageData);
        });
        
        this.connection.on('Error', (message) => {
            console.error('Server error:', message);
            this.showNotification('Error: ' + message, 'danger');
        });
        
        this.connection.on('InputAck', (action) => {
            console.log('Input acknowledged:', action);
        });
        
        this.connection.onclose(() => {
            this.isConnected = false;
            this.updateConnectionStatus('offline', 'Disconnected');
            this.elements.connectBtn.style.display = 'inline-block';
            this.elements.disconnectBtn.style.display = 'none';
            this.stopScreenCapture();
        });
        
        this.connection.onreconnecting(() => {
            this.updateConnectionStatus('connecting', 'Reconnecting...');
        });
        
        this.connection.onreconnected(() => {
            this.updateConnectionStatus('online', 'Reconnected');
            this.startScreenCapture();
        });
    }
    
    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
        }
        this.stopScreenCapture();
    }
    
    startScreenCapture() {
        this.elements.loadingIndicator.style.display = 'none';
        this.elements.desktopScreen.style.display = 'block';
        
        this.updateScreenCaptureInterval();
    }
    
    updateScreenCaptureInterval() {
        if (this.screenUpdateInterval) {
            clearInterval(this.screenUpdateInterval);
        }
        
        const interval = 1000 / this.frameRate;
        this.screenUpdateInterval = setInterval(() => {
            this.requestScreenCapture();
        }, interval);
    }
    
    stopScreenCapture() {
        if (this.screenUpdateInterval) {
            clearInterval(this.screenUpdateInterval);
            this.screenUpdateInterval = null;
        }
        
        this.elements.loadingIndicator.style.display = 'block';
        this.elements.desktopScreen.style.display = 'none';
    }
    
    async requestScreenCapture() {
        if (!this.isConnected || !this.connection) return;
        
        try {
            if (this.currentMonitor >= 0) {
                await this.connection.invoke('RequestMonitorCapture', this.currentMonitor, this.currentQuality);
            } else {
                await this.connection.invoke('RequestScreenCapture', this.currentQuality);
            }
        } catch (error) {
            console.error('Screen capture request error:', error);
        }
    }
    
    updateScreen(imageData) {
        this.elements.desktopScreen.src = 'data:image/jpeg;base64,' + imageData;
    }
    
    updateMonitorList(monitors) {
        const select = this.elements.monitorSelect;
        select.innerHTML = '<option value="-1">All Monitors</option>';
        
        monitors.forEach(monitor => {
            const option = document.createElement('option');
            option.value = monitor.Index;
            option.textContent = `${monitor.Name} (${monitor.Bounds.Width}x${monitor.Bounds.Height})`;
            if (monitor.IsPrimary) {
                option.textContent += ' - Primary';
            }
            select.appendChild(option);
        });
    }
    
    updateConnectionStatus(status, message) {
        const statusElement = this.elements.connectionStatus;
        const infoElement = this.elements.connectionInfo;
        
        statusElement.className = `status-indicator status-${status}`;
        statusElement.innerHTML = `<span class="status-dot"></span>${this.getStatusText(status)}`;
        infoElement.textContent = message;
    }
    
    getStatusText(status) {
        switch (status) {
            case 'online': return 'Connected';
            case 'connecting': return 'Connecting';
            case 'offline': return 'Disconnected';
            default: return 'Unknown';
        }
    }
    
    // Mouse event handlers
    handleMouseDown(e) {
        if (!this.isConnected) return;
        
        e.preventDefault();
        this.isMouseDown = true;
        
        const coords = this.getRelativeCoordinates(e);
        const button = this.getMouseButton(e.button);
        
        this.connection.invoke('MouseClick', coords.x, coords.y, button, false);
    }
    
    handleMouseUp(e) {
        if (!this.isConnected) return;
        
        e.preventDefault();
        this.isMouseDown = false;
    }
    
    handleMouseMove(e) {
        if (!this.isConnected) return;
        
        const coords = this.getRelativeCoordinates(e);
        
        // Throttle mouse move events
        if (Math.abs(coords.x - this.lastMousePosition.x) > 2 || 
            Math.abs(coords.y - this.lastMousePosition.y) > 2) {
            this.connection.invoke('MouseMove', coords.x, coords.y);
            this.lastMousePosition = coords;
        }
    }
    
    handleMouseWheel(e) {
        if (!this.isConnected) return;
        
        e.preventDefault();
        const coords = this.getRelativeCoordinates(e);
        const delta = e.deltaY > 0 ? -120 : 120;
        
        this.connection.invoke('MouseWheel', coords.x, coords.y, delta);
    }
    
    // Keyboard event handlers
    handleKeyDown(e) {
        if (!this.isConnected) return;
        
        // Don't capture certain key combinations
        if (e.ctrlKey && (e.key === 'r' || e.key === 'R' || e.key === 'F5')) return;
        if (e.key === 'F12') return;
        
        e.preventDefault();
        this.connection.invoke('KeyPress', e.keyCode, true);
    }
    
    handleKeyUp(e) {
        if (!this.isConnected) return;
        
        if (e.ctrlKey && (e.key === 'r' || e.key === 'R' || e.key === 'F5')) return;
        if (e.key === 'F12') return;
        
        e.preventDefault();
        this.connection.invoke('KeyPress', e.keyCode, false);
    }
    
    // Utility methods
    getRelativeCoordinates(e) {
        const rect = this.elements.desktopScreen.getBoundingClientRect();
        const scaleX = this.elements.desktopScreen.naturalWidth / rect.width;
        const scaleY = this.elements.desktopScreen.naturalHeight / rect.height;
        
        return {
            x: Math.round((e.clientX - rect.left) * scaleX),
            y: Math.round((e.clientY - rect.top) * scaleY)
        };
    }
    
    getMouseButton(button) {
        switch (button) {
            case 0: return 'left';
            case 1: return 'middle';
            case 2: return 'right';
            default: return 'left';
        }
    }
    
    toggleFullscreen() {
        if (!document.fullscreenElement) {
            this.elements.desktopViewer.requestFullscreen();
        } else {
            document.exitFullscreen();
        }
    }
    
    takeScreenshot() {
        if (this.elements.desktopScreen.src) {
            const link = document.createElement('a');
            link.download = `screenshot-${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.jpg`;
            link.href = this.elements.desktopScreen.src;
            link.click();
        }
    }
    
    showVirtualKeyboard() {
        const modal = new bootstrap.Modal(document.getElementById('keyboardModal'));
        modal.show();
    }
    
    showNotification(message, type = 'info') {
        // Simple notification - could be enhanced with a proper notification system
        console.log(`${type.toUpperCase()}: ${message}`);
    }
}

// Initialize the remote desktop client when the page loads
document.addEventListener('DOMContentLoaded', function() {
    window.remoteDesktopClient = new RemoteDesktopClient();
});
