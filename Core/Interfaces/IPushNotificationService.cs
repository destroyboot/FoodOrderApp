namespace Core.Interfaces
{
    public interface IPushNotificationService
    {
        Task RegisterDeviceAsync(
            string ownerKey,
            string expoPushToken,
            string platform,
            string? deviceName,
            CancellationToken ct = default);

        Task UnregisterDeviceAsync(
            string ownerKey,
            string expoPushToken,
            CancellationToken ct = default);

        Task SendToOwnerAsync(
            string ownerKey,
            string title,
            string body,
            string? payloadJson = null,
            CancellationToken ct = default);
    }
}
