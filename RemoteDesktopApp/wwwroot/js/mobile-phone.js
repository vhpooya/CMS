// Mobile Phone JavaScript

class MobilePhone {
    constructor() {
        this.currentUser = null;
        this.onlineContacts = [];
        this.smsConversations = [];
        this.currentCall = null;
        this.dialedNumber = '';
        this.soundEnabled = true;
        this.notificationSound = new Audio('/sounds/notification.mp3');
        this.ringtoneSound = new Audio('/sounds/ringtone.mp3');
        this.callDurationInterval = null;
        
        this.init();
    }

    async init() {
        this.createPhoneWidget();
        this.bindEvents();
        await this.loadUserData();
        await this.updateNotificationBadges();
        this.startPeriodicUpdates();
    }

    createPhoneWidget() {
        const widget = document.createElement('div');
        widget.className = 'mobile-phone-widget';
        widget.innerHTML = `
            <div class="phone-icon-container" id="phoneIconContainer">
                <div class="phone-icon" id="phoneIcon">
                    <i class="fas fa-mobile-alt"></i>
                </div>
                <div class="notification-badge" id="notificationBadge" style="display: none;">0</div>
                <div class="sound-indicator" id="soundIndicator"></div>
            </div>
        `;
        
        document.body.appendChild(widget);
        this.createPhoneModal();
    }

    createPhoneModal() {
        const modal = document.createElement('div');
        modal.className = 'phone-modal';
        modal.id = 'phoneModal';
        modal.innerHTML = `
            <div class="phone-container">
                <div class="phone-screen">
                    <div class="phone-header">
                        <div class="phone-time" id="phoneTime"></div>
                        <div class="phone-status">
                            <i class="fas fa-signal"></i>
                            <i class="fas fa-wifi"></i>
                            <i class="fas fa-battery-full"></i>
                        </div>
                    </div>
                    <div class="phone-content">
                        <div class="phone-nav">
                            <button class="phone-nav-item active" data-tab="dialer">
                                <i class="fas fa-phone"></i>
                                <span>Dialer</span>
                            </button>
                            <button class="phone-nav-item" data-tab="contacts">
                                <i class="fas fa-address-book"></i>
                                <span>Contacts</span>
                            </button>
                            <button class="phone-nav-item" data-tab="sms">
                                <i class="fas fa-sms"></i>
                                <span>SMS</span>
                            </button>
                            <button class="phone-nav-item" data-tab="history">
                                <i class="fas fa-history"></i>
                                <span>History</span>
                            </button>
                        </div>
                        
                        <!-- Dialer Tab -->
                        <div class="phone-content-area active" id="dialerTab">
                            <div class="dialer-display">
                                <div class="dialer-number" id="dialerNumber"></div>
                            </div>
                            <div class="dialer-keypad">
                                <button class="dialer-key" data-digit="1">1</button>
                                <button class="dialer-key" data-digit="2">2</button>
                                <button class="dialer-key" data-digit="3">3</button>
                                <button class="dialer-key" data-digit="4">4</button>
                                <button class="dialer-key" data-digit="5">5</button>
                                <button class="dialer-key" data-digit="6">6</button>
                                <button class="dialer-key" data-digit="7">7</button>
                                <button class="dialer-key" data-digit="8">8</button>
                                <button class="dialer-key" data-digit="9">9</button>
                                <button class="dialer-key" data-digit="*">*</button>
                                <button class="dialer-key" data-digit="0">0</button>
                                <button class="dialer-key" data-digit="#">#</button>
                            </div>
                            <div class="dialer-actions">
                                <button class="dialer-action call-btn" id="callBtn">
                                    <i class="fas fa-phone"></i>
                                </button>
                                <button class="dialer-action clear-btn" id="clearBtn">
                                    <i class="fas fa-backspace"></i>
                                </button>
                            </div>
                        </div>
                        
                        <!-- Contacts Tab -->
                        <div class="phone-content-area" id="contactsTab">
                            <div class="contacts-search">
                                <input type="text" placeholder="Search contacts..." id="contactsSearch">
                            </div>
                            <div class="contacts-list" id="contactsList">
                                <!-- Contacts will be loaded here -->
                            </div>
                        </div>
                        
                        <!-- SMS Tab -->
                        <div class="phone-content-area" id="smsTab">
                            <div class="sms-conversations" id="smsConversations">
                                <!-- SMS conversations will be loaded here -->
                            </div>
                        </div>
                        
                        <!-- History Tab -->
                        <div class="phone-content-area" id="historyTab">
                            <div class="call-history" id="callHistory">
                                <!-- Call history will be loaded here -->
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        this.createCallScreen();
    }

    createCallScreen() {
        const callScreen = document.createElement('div');
        callScreen.className = 'call-screen';
        callScreen.id = 'callScreen';
        callScreen.innerHTML = `
            <div class="call-avatar" id="callAvatar">
                <i class="fas fa-user"></i>
            </div>
            <div class="call-info">
                <div class="call-name" id="callName">Unknown</div>
                <div class="call-number" id="callNumber">000</div>
                <div class="call-status" id="callStatus">Calling...</div>
                <div class="call-duration" id="callDuration" style="display: none;">00:00</div>
            </div>
            <div class="call-actions" id="callActions">
                <button class="call-action-btn answer-btn" id="answerBtn" style="display: none;">
                    <i class="fas fa-phone"></i>
                </button>
                <button class="call-action-btn decline-btn" id="declineBtn">
                    <i class="fas fa-phone-slash"></i>
                </button>
                <button class="call-action-btn end-call-btn" id="endCallBtn" style="display: none;">
                    <i class="fas fa-phone-slash"></i>
                </button>
            </div>
        `;
        
        document.body.appendChild(callScreen);
    }

    bindEvents() {
        // Phone icon click
        document.getElementById('phoneIconContainer').addEventListener('click', () => {
            this.togglePhoneModal();
        });

        // Close modal when clicking outside
        document.getElementById('phoneModal').addEventListener('click', (e) => {
            if (e.target.id === 'phoneModal') {
                this.closePhoneModal();
            }
        });

        // Tab navigation
        document.querySelectorAll('.phone-nav-item').forEach(tab => {
            tab.addEventListener('click', () => {
                this.switchTab(tab.dataset.tab);
            });
        });

        // Dialer events
        document.querySelectorAll('.dialer-key').forEach(key => {
            key.addEventListener('click', () => {
                this.addDigit(key.dataset.digit);
            });
        });

        document.getElementById('callBtn').addEventListener('click', () => {
            this.makeCall();
        });

        document.getElementById('clearBtn').addEventListener('click', () => {
            this.clearLastDigit();
        });

        // Call screen events
        document.getElementById('answerBtn').addEventListener('click', () => {
            this.answerCall();
        });

        document.getElementById('declineBtn').addEventListener('click', () => {
            this.declineCall();
        });

        document.getElementById('endCallBtn').addEventListener('click', () => {
            this.endCall();
        });

        // Update time
        setInterval(() => {
            this.updateTime();
        }, 1000);
    }

    async loadUserData() {
        try {
            // Load online contacts
            const contactsResponse = await fetch('/api/mobilephone/contacts/online');
            if (contactsResponse.ok) {
                this.onlineContacts = await contactsResponse.json();
                this.renderContacts();
            }

            // Load SMS conversations
            const smsResponse = await fetch('/api/mobilephone/sms/conversations');
            if (smsResponse.ok) {
                this.smsConversations = await smsResponse.json();
                this.renderSmsConversations();
            }

            // Load call history
            await this.loadCallHistory();

        } catch (error) {
            console.error('Error loading user data:', error);
        }
    }

    async updateNotificationBadges() {
        try {
            // Get unread SMS count
            const smsResponse = await fetch('/api/mobilephone/sms/unread-count');
            const smsCount = smsResponse.ok ? await smsResponse.json() : 0;

            // Get missed calls count
            const callsResponse = await fetch('/api/mobilephone/calls/missed-count');
            const callsCount = callsResponse.ok ? await callsResponse.json() : 0;

            const totalCount = smsCount + callsCount;
            const badge = document.getElementById('notificationBadge');
            
            if (totalCount > 0) {
                badge.textContent = totalCount > 99 ? '99+' : totalCount.toString();
                badge.style.display = 'flex';
                badge.classList.add('flashing');
                
                // Play notification sound
                if (this.soundEnabled) {
                    this.playNotificationSound();
                }
            } else {
                badge.style.display = 'none';
                badge.classList.remove('flashing');
            }

        } catch (error) {
            console.error('Error updating notification badges:', error);
        }
    }

    renderContacts() {
        const contactsList = document.getElementById('contactsList');
        contactsList.innerHTML = '';

        this.onlineContacts.forEach(contact => {
            const contactElement = document.createElement('div');
            contactElement.className = 'contact-item';
            contactElement.innerHTML = `
                <div class="contact-avatar">
                    ${contact.profilePicture ? 
                        `<img src="${contact.profilePicture}" alt="${contact.displayName}">` :
                        contact.displayName.charAt(0).toUpperCase()
                    }
                    ${contact.isOnline ? '<div class="online-indicator"></div>' : ''}
                </div>
                <div class="contact-info">
                    <div class="contact-name">${contact.displayName}</div>
                    <div class="contact-phone">${contact.phoneNumber}</div>
                </div>
                <div class="contact-actions">
                    <button class="contact-action call-action" onclick="mobilePhone.callContact('${contact.phoneNumber}')">
                        <i class="fas fa-phone"></i>
                    </button>
                    <button class="contact-action sms-action" onclick="mobilePhone.smsContact('${contact.phoneNumber}')">
                        <i class="fas fa-sms"></i>
                    </button>
                </div>
            `;
            contactsList.appendChild(contactElement);
        });
    }

    renderSmsConversations() {
        const smsConversations = document.getElementById('smsConversations');
        smsConversations.innerHTML = '';

        this.smsConversations.forEach(conversation => {
            const conversationElement = document.createElement('div');
            conversationElement.className = 'sms-conversation';
            conversationElement.innerHTML = `
                <div class="sms-avatar">
                    ${conversation.profilePicture ? 
                        `<img src="${conversation.profilePicture}" alt="${conversation.contactName}">` :
                        conversation.contactName.charAt(0).toUpperCase()
                    }
                    ${conversation.isOnline ? '<div class="online-indicator"></div>' : ''}
                </div>
                <div class="sms-info">
                    <div class="sms-contact-name">${conversation.contactName}</div>
                    <div class="sms-last-message">${conversation.lastMessage || 'No messages'}</div>
                </div>
                <div class="sms-meta">
                    <div class="sms-time">${this.formatTime(conversation.lastMessageTime)}</div>
                    ${conversation.unreadCount > 0 ? 
                        `<div class="sms-unread-badge">${conversation.unreadCount}</div>` : ''
                    }
                </div>
            `;
            
            conversationElement.addEventListener('click', () => {
                this.openSmsConversation(conversation.phoneNumber);
            });
            
            smsConversations.appendChild(conversationElement);
        });
    }

    async loadCallHistory() {
        try {
            const response = await fetch('/api/mobilephone/calls/history?pageSize=20');
            if (response.ok) {
                const calls = await response.json();
                this.renderCallHistory(calls);
            }
        } catch (error) {
            console.error('Error loading call history:', error);
        }
    }

    renderCallHistory(calls) {
        const callHistory = document.getElementById('callHistory');
        callHistory.innerHTML = '';

        calls.forEach(call => {
            const callElement = document.createElement('div');
            callElement.className = 'contact-item';
            
            const isIncoming = call.receiverId === this.currentUser?.id;
            const otherUser = isIncoming ? call.caller : call.receiver;
            const callIcon = this.getCallIcon(call.status, isIncoming);
            
            callElement.innerHTML = `
                <div class="contact-avatar">
                    ${otherUser.profilePicture ? 
                        `<img src="${otherUser.profilePicture}" alt="${otherUser.displayName}">` :
                        otherUser.displayName.charAt(0).toUpperCase()
                    }
                </div>
                <div class="contact-info">
                    <div class="contact-name">${otherUser.displayName}</div>
                    <div class="contact-phone">
                        ${callIcon} ${isIncoming ? call.callerPhoneNumber : call.receiverPhoneNumber}
                        ${call.duration ? ` • ${this.formatDuration(call.duration)}` : ''}
                    </div>
                </div>
                <div class="contact-actions">
                    <button class="contact-action call-action" onclick="mobilePhone.callContact('${isIncoming ? call.callerPhoneNumber : call.receiverPhoneNumber}')">
                        <i class="fas fa-phone"></i>
                    </button>
                </div>
            `;
            callHistory.appendChild(callElement);
        });
    }

    togglePhoneModal() {
        const modal = document.getElementById('phoneModal');
        modal.classList.toggle('show');
        
        if (modal.classList.contains('show')) {
            this.loadUserData();
        }
    }

    closePhoneModal() {
        document.getElementById('phoneModal').classList.remove('show');
    }

    switchTab(tabName) {
        // Update nav items
        document.querySelectorAll('.phone-nav-item').forEach(item => {
            item.classList.remove('active');
        });
        document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');

        // Update content areas
        document.querySelectorAll('.phone-content-area').forEach(area => {
            area.classList.remove('active');
        });
        document.getElementById(`${tabName}Tab`).classList.add('active');

        // Load data for specific tabs
        if (tabName === 'contacts') {
            this.loadUserData();
        } else if (tabName === 'sms') {
            this.loadUserData();
        } else if (tabName === 'history') {
            this.loadCallHistory();
        }
    }

    addDigit(digit) {
        if (this.dialedNumber.length < 3) { // 3-digit limit
            this.dialedNumber += digit;
            document.getElementById('dialerNumber').textContent = this.dialedNumber;
        }
    }

    clearLastDigit() {
        this.dialedNumber = this.dialedNumber.slice(0, -1);
        document.getElementById('dialerNumber').textContent = this.dialedNumber;
    }

    async makeCall() {
        if (this.dialedNumber.length !== 3) {
            this.showToast('Please enter a 3-digit phone number', 'warning');
            return;
        }

        try {
            const response = await fetch('/api/mobilephone/call/initiate', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    phoneNumber: this.dialedNumber,
                    isVideoCall: false
                })
            });

            if (response.ok) {
                this.currentCall = await response.json();
                this.showCallScreen('outgoing');
                this.playRingtone();
            } else {
                const error = await response.json();
                this.showToast(error.message || 'Failed to make call', 'error');
            }
        } catch (error) {
            console.error('Error making call:', error);
            this.showToast('Failed to make call', 'error');
        }
    }

    callContact(phoneNumber) {
        this.dialedNumber = phoneNumber;
        document.getElementById('dialerNumber').textContent = this.dialedNumber;
        this.switchTab('dialer');
        this.makeCall();
    }

    smsContact(phoneNumber) {
        // Open SMS conversation
        this.openSmsConversation(phoneNumber);
    }

    openSmsConversation(phoneNumber) {
        // This would open a detailed SMS conversation view
        // For now, just show a toast
        this.showToast(`Opening SMS conversation with ${phoneNumber}`, 'info');
    }

    showCallScreen(type) {
        const callScreen = document.getElementById('callScreen');
        const answerBtn = document.getElementById('answerBtn');
        const declineBtn = document.getElementById('declineBtn');
        const endCallBtn = document.getElementById('endCallBtn');

        if (type === 'incoming') {
            answerBtn.style.display = 'block';
            declineBtn.style.display = 'block';
            endCallBtn.style.display = 'none';
            document.getElementById('callStatus').textContent = 'Incoming call...';
        } else {
            answerBtn.style.display = 'none';
            declineBtn.style.display = 'none';
            endCallBtn.style.display = 'block';
            document.getElementById('callStatus').textContent = 'Calling...';
        }

        callScreen.classList.add('show');
        this.closePhoneModal();
    }

    hideCallScreen() {
        document.getElementById('callScreen').classList.remove('show');
        this.stopRingtone();
        this.stopCallDuration();
    }

    async answerCall() {
        if (!this.currentCall) return;

        try {
            const response = await fetch(`/api/mobilephone/call/${this.currentCall.id}/answer`, {
                method: 'POST'
            });

            if (response.ok) {
                this.currentCall = await response.json();
                this.startCallDuration();
                document.getElementById('callStatus').style.display = 'none';
                document.getElementById('callDuration').style.display = 'block';
                document.getElementById('answerBtn').style.display = 'none';
                document.getElementById('declineBtn').style.display = 'none';
                document.getElementById('endCallBtn').style.display = 'block';
                this.stopRingtone();
            }
        } catch (error) {
            console.error('Error answering call:', error);
        }
    }

    async declineCall() {
        if (!this.currentCall) return;

        try {
            await fetch(`/api/mobilephone/call/${this.currentCall.id}/decline`, {
                method: 'POST'
            });
        } catch (error) {
            console.error('Error declining call:', error);
        }

        this.hideCallScreen();
        this.currentCall = null;
    }

    async endCall() {
        if (!this.currentCall) return;

        try {
            await fetch(`/api/mobilephone/call/${this.currentCall.id}/end`, {
                method: 'POST'
            });
        } catch (error) {
            console.error('Error ending call:', error);
        }

        this.hideCallScreen();
        this.currentCall = null;
    }

    startCallDuration() {
        let seconds = 0;
        this.callDurationInterval = setInterval(() => {
            seconds++;
            const minutes = Math.floor(seconds / 60);
            const remainingSeconds = seconds % 60;
            document.getElementById('callDuration').textContent = 
                `${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
        }, 1000);
    }

    stopCallDuration() {
        if (this.callDurationInterval) {
            clearInterval(this.callDurationInterval);
            this.callDurationInterval = null;
        }
    }

    playRingtone() {
        if (this.soundEnabled && this.ringtoneSound) {
            this.ringtoneSound.loop = true;
            this.ringtoneSound.play().catch(e => console.log('Could not play ringtone:', e));
            
            // Add ringing animation
            document.getElementById('phoneIcon').classList.add('ringing');
        }
    }

    stopRingtone() {
        if (this.ringtoneSound) {
            this.ringtoneSound.pause();
            this.ringtoneSound.currentTime = 0;
            this.ringtoneSound.loop = false;
        }
        
        // Remove ringing animation
        document.getElementById('phoneIcon').classList.remove('ringing');
    }

    playNotificationSound() {
        if (this.soundEnabled && this.notificationSound) {
            this.notificationSound.play().catch(e => console.log('Could not play notification:', e));
            
            // Show sound indicator
            const indicator = document.getElementById('soundIndicator');
            indicator.classList.add('active');
            setTimeout(() => {
                indicator.classList.remove('active');
            }, 1000);
        }
    }

    updateTime() {
        const now = new Date();
        const timeString = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const timeElement = document.getElementById('phoneTime');
        if (timeElement) {
            timeElement.textContent = timeString;
        }
    }

    formatTime(dateString) {
        if (!dateString) return '';
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'now';
        if (diffMins < 60) return `${diffMins}m`;
        if (diffHours < 24) return `${diffHours}h`;
        if (diffDays < 7) return `${diffDays}d`;
        return date.toLocaleDateString();
    }

    formatDuration(duration) {
        if (!duration) return '';
        const parts = duration.split(':');
        if (parts.length >= 2) {
            return `${parts[0]}:${parts[1]}`;
        }
        return duration;
    }

    getCallIcon(status, isIncoming) {
        switch (status) {
            case 'Missed':
                return '<i class="fas fa-phone-slash text-danger"></i>';
            case 'Declined':
                return '<i class="fas fa-phone-slash text-warning"></i>';
            case 'Ended':
                return isIncoming ? '<i class="fas fa-phone-alt text-success"></i>' : '<i class="fas fa-phone text-primary"></i>';
            default:
                return '<i class="fas fa-phone text-muted"></i>';
        }
    }

    showToast(message, type = 'info') {
        // Create a simple toast notification
        const toast = document.createElement('div');
        toast.className = `alert alert-${type === 'error' ? 'danger' : type} position-fixed`;
        toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        toast.textContent = message;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.remove();
        }, 3000);
    }

    startPeriodicUpdates() {
        // Update notification badges every 30 seconds
        setInterval(() => {
            this.updateNotificationBadges();
        }, 30000);

        // Update online contacts every 60 seconds
        setInterval(() => {
            if (document.getElementById('phoneModal').classList.contains('show')) {
                this.loadUserData();
            }
        }, 60000);
    }
}

// Initialize mobile phone when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.mobilePhone = new MobilePhone();
});

// Handle incoming calls (this would be triggered by SignalR in a real implementation)
function handleIncomingCall(callData) {
    if (window.mobilePhone) {
        window.mobilePhone.currentCall = callData;
        window.mobilePhone.showCallScreen('incoming');
        window.mobilePhone.playRingtone();
        
        // Update call screen with caller info
        document.getElementById('callName').textContent = callData.caller?.displayName || 'Unknown';
        document.getElementById('callNumber').textContent = callData.callerPhoneNumber || '000';
    }
}
