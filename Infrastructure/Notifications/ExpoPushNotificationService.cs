using System.Net.Http.Json;
using System.Text.Json;
using Core.Data.Entities;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications
{
    public class ExpoPushNotificationService : IPushNotificationService
    {
        private static readonly HttpClient ExpoPushHttpClient = new()
        {
            BaseAddress = new Uri("https://exp.host")
        };

        private readonly AppDbContext _db;
        private readonly ILogger<ExpoPushNotificationService> _logger;

        public ExpoPushNotificationService(
            AppDbContext db,
            ILogger<ExpoPushNotificationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task RegisterDeviceAsync(
            string ownerKey,
            string expoPushToken,
            string platform,
            string? deviceName,
            CancellationToken ct = default)
        {
            ownerKey = NormalizeRequired(ownerKey, nameof(ownerKey), 200);
            expoPushToken = NormalizeRequired(expoPushToken, nameof(expoPushToken), 300);
            platform = NormalizeRequired(platform, nameof(platform), 40);
            deviceName = TrimOrNull(deviceName, 200);

            var now = DateTime.UtcNow;

            var existing = await _db.PushDeviceRegistrations
                .FirstOrDefaultAsync(
                    x => x.OwnerKey == ownerKey && x.ExpoPushToken == expoPushToken,
                    ct);

            if (existing is null)
            {
                _db.PushDeviceRegistrations.Add(new PushDeviceRegistration
                {
                    OwnerKey = ownerKey,
                    ExpoPushToken = expoPushToken,
                    Platform = platform,
                    DeviceName = deviceName,
                    IsActive = true,
                    LastRegisteredAt = now,
                    LastSeenAt = now
                });
            }
            else
            {
                existing.Platform = platform;
                existing.DeviceName = deviceName;
                existing.IsActive = true;
                existing.LastRegisteredAt = now;
                existing.LastSeenAt = now;
                existing.LastError = null;
                existing.LastErrorAt = null;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task UnregisterDeviceAsync(
            string ownerKey,
            string expoPushToken,
            CancellationToken ct = default)
        {
            ownerKey = NormalizeRequired(ownerKey, nameof(ownerKey), 200);
            expoPushToken = NormalizeRequired(expoPushToken, nameof(expoPushToken), 300);

            var registration = await _db.PushDeviceRegistrations
                .FirstOrDefaultAsync(
                    x => x.OwnerKey == ownerKey && x.ExpoPushToken == expoPushToken,
                    ct);

            if (registration is null)
            {
                return;
            }

            registration.IsActive = false;
            registration.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task SendToOwnerAsync(
            string ownerKey,
            string title,
            string body,
            string? payloadJson = null,
            CancellationToken ct = default)
        {
            ownerKey = NormalizeRequired(ownerKey, nameof(ownerKey), 200);
            title = NormalizeRequired(title, nameof(title), 200);
            body = NormalizeRequired(body, nameof(body), 1000);

            var registrations = await _db.PushDeviceRegistrations
                .Where(x => x.OwnerKey == ownerKey && x.IsActive)
                .ToListAsync(ct);

            if (registrations.Count == 0)
            {
                return;
            }

            object? data = null;
            if (!string.IsNullOrWhiteSpace(payloadJson))
            {
                try
                {
                    data = JsonSerializer.Deserialize<JsonElement>(payloadJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not deserialize push payload for owner {OwnerKey}.", ownerKey);
                }
            }

            var messages = registrations
                .Select(r => new Dictionary<string, object?>
                {
                    ["to"] = r.ExpoPushToken,
                    ["title"] = title,
                    ["body"] = body,
                    ["sound"] = "default",
                    ["channelId"] = "order-status",
                    ["data"] = data
                })
                .ToList();

            try
            {
                using var response = await ExpoPushHttpClient.PostAsJsonAsync("/--/api/v2/push/send", messages, ct);
                var content = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    MarkRegistrationsError(registrations, $"Expo push HTTP {(int)response.StatusCode}: {Trim(content, 500)}");
                    await _db.SaveChangesAsync(ct);
                    _logger.LogWarning("Expo push request failed for owner {OwnerKey}: {StatusCode} {Content}", ownerKey, response.StatusCode, content);
                    return;
                }

                HandleExpoResponse(registrations, content);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                MarkRegistrationsError(registrations, Trim(ex.Message, 500));
                await _db.SaveChangesAsync(ct);
                _logger.LogWarning(ex, "Expo push delivery failed for owner {OwnerKey}.", ownerKey);
            }
        }

        private void HandleExpoResponse(List<PushDeviceRegistration> registrations, string content)
        {
            var now = DateTime.UtcNow;

            try
            {
                using var document = JsonDocument.Parse(content);
                if (!document.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
                {
                    foreach (var registration in registrations)
                    {
                        registration.LastSuccessfulPushAt = now;
                        registration.LastSeenAt = now;
                        registration.LastError = null;
                        registration.LastErrorAt = null;
                    }

                    return;
                }

                for (var i = 0; i < registrations.Count; i++)
                {
                    var registration = registrations[i];
                    registration.LastSeenAt = now;

                    if (i >= dataElement.GetArrayLength())
                    {
                        registration.LastSuccessfulPushAt = now;
                        registration.LastError = null;
                        registration.LastErrorAt = null;
                        continue;
                    }

                    var item = dataElement[i];
                    var status = item.TryGetProperty("status", out var statusElement)
                        ? statusElement.GetString()
                        : null;

                    if (string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase))
                    {
                        registration.LastSuccessfulPushAt = now;
                        registration.LastError = null;
                        registration.LastErrorAt = null;
                        continue;
                    }

                    var errorMessage = item.TryGetProperty("message", out var messageElement)
                        ? messageElement.GetString()
                        : "Expo push delivery error";

                    string? expoError = null;
                    if (item.TryGetProperty("details", out var detailsElement) &&
                        detailsElement.ValueKind == JsonValueKind.Object &&
                        detailsElement.TryGetProperty("error", out var errorElement))
                    {
                        expoError = errorElement.GetString();
                    }

                    registration.LastError = Trim($"{errorMessage} ({expoError})", 500);
                    registration.LastErrorAt = now;

                    if (string.Equals(expoError, "DeviceNotRegistered", StringComparison.OrdinalIgnoreCase))
                    {
                        registration.IsActive = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse Expo push response.");
                MarkRegistrationsError(registrations, "Could not parse Expo push response.");
            }
        }

        private static void MarkRegistrationsError(IEnumerable<PushDeviceRegistration> registrations, string error)
        {
            var now = DateTime.UtcNow;
            foreach (var registration in registrations)
            {
                registration.LastError = error;
                registration.LastErrorAt = now;
                registration.LastSeenAt = now;
            }
        }

        private static string NormalizeRequired(string value, string fieldName, int maxLength)
        {
            var trimmed = TrimOrNull(value, maxLength);
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                throw new InvalidOperationException($"{fieldName} is required.");
            }

            return trimmed;
        }

        private static string? TrimOrNull(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Trim(value.Trim(), maxLength);
        }

        private static string Trim(string value, int maxLength)
            => value.Length <= maxLength ? value : value[..maxLength];
    }
}
