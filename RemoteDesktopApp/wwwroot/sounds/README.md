# Sound Files for Mobile Phone System

This directory contains audio files for the mobile phone system:

## Required Sound Files:

1. **notification.mp3** - Sound played for SMS notifications and general alerts
2. **ringtone.mp3** - Sound played for incoming calls

## Audio Format Requirements:

- Format: MP3 or WAV
- Duration: 
  - Notification sounds: 1-3 seconds
  - Ringtones: 10-30 seconds (will loop)
- Quality: 44.1kHz, 16-bit minimum
- File size: Keep under 1MB for web performance

## Implementation Notes:

- Sounds are played using HTML5 Audio API
- Ringtones will loop automatically for incoming calls
- Notification sounds play once
- Users can disable sounds in phone settings
- Fallback: System will work without sound files (silent mode)

## Adding Custom Sounds:

1. Place audio files in this directory
2. Update the file paths in `/js/mobile-phone.js`:
   ```javascript
   this.notificationSound = new Audio('/sounds/your-notification.mp3');
   this.ringtoneSound = new Audio('/sounds/your-ringtone.mp3');
   ```

## Default Behavior:

If sound files are not found, the system will:
- Log a console message
- Continue to function normally
- Show visual notifications only
- Allow users to enable sounds when files are added
