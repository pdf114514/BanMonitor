namespace BanMonitor.Shared;

public class NotificationSubscription {
    public int NotificationSubscriptionId { get; set; }
    public string? UserId { get; set; }
    public required string Url { get; set; }
    public required string P256dh { get; set; }
    public required string Auth { get; set; }
}