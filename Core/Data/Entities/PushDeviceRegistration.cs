namespace Core.Data.Entities
{
    public class PushDeviceRegistration
    {
        public int Id { get; set; }
        public string OwnerKey { get; set; } = null!;
        public string ExpoPushToken { get; set; } = null!;
        public string Platform { get; set; } = null!;
        public string? DeviceName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime LastRegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSuccessfulPushAt { get; set; }
        public string? LastError { get; set; }
        public DateTime? LastErrorAt { get; set; }
    }
}
