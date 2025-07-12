namespace RemoteDesktopApp.Models
{
    public class RemoteSession
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }
        public bool IsActive { get; set; }
        public string? ClientInfo { get; set; }
        public int ScreenQuality { get; set; } = 85;
        public int FrameRate { get; set; } = 30;
        public bool HasControl { get; set; } = true;
    }
}
