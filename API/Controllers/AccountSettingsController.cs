using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Interfaces;

namespace API.Controllers
{
    [ApiController]
    [Route("api/account")]
    [Authorize] // logged-in only
    public class AccountSettingsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IEmailSender _email;

        public AccountSettingsController(UserManager<ApplicationUser> users, IEmailSender email)
        {
            _users = users;
            _email = email;
        }

        public record RequestEmailChangeDto(string NewEmail);
        public record ConfirmEmailChangeDto(string Code);
        public record ResendEmailChangeDto();
        public record StatusEmailPreferenceDto(bool Enabled);

        private string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Missing user id.");

        [HttpPost("email/change-request")]
        public async Task<IActionResult> RequestEmailChange([FromBody] RequestEmailChangeDto dto, CancellationToken ct)
        {
            var newEmail = (dto.NewEmail ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(newEmail)) throw new InvalidOperationException("NewEmail is required.");

            // Ensure new email is not already taken
            var existing = await _users.FindByEmailAsync(newEmail);
            if (existing is not null)
                throw new InvalidOperationException("This email is already in use.");

            var user = await _users.FindByIdAsync(UserId) ?? throw new InvalidOperationException("User not found.");

            var code = Generate6DigitCode();

            user.PendingEmail = newEmail;
            user.EmailChangeCodeHash = HashCode(code);
            user.EmailChangeCodeExpiresAt = DateTime.UtcNow.AddMinutes(20);
            user.EmailChangeResendAvailableAt = DateTime.UtcNow.AddMinutes(1);
            user.EmailChangeResendCount = 0;

            var upd = await _users.UpdateAsync(user);
            if (!upd.Succeeded)
                throw new InvalidOperationException(string.Join("; ", upd.Errors.Select(e => e.Description)));

            await _email.SendAsync(
                toEmail: newEmail,
                subject: "Food Order App – Confirm email change",
                htmlBody: $@"
                <p>Use this code to confirm your email change:</p>
                <h2 style='letter-spacing:2px;'>{code}</h2>
                <p>This code is valid for 20 minutes.</p>",
                ct: ct);

            return Ok(new { message = "Confirmation code sent to new email." });
        }

        [HttpPost("email/change-confirm")]
        public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeDto dto, CancellationToken ct)
        {
            var code = (dto.Code ?? "").Trim();
            var user = await _users.FindByIdAsync(UserId) ?? throw new InvalidOperationException("User not found.");

            if (string.IsNullOrWhiteSpace(user.PendingEmail))
                throw new InvalidOperationException("No email change requested.");

            if (!user.EmailChangeCodeExpiresAt.HasValue || user.EmailChangeCodeExpiresAt.Value <= DateTime.UtcNow)
                throw new InvalidOperationException("Code expired. Request email change again.");

            if (string.IsNullOrWhiteSpace(user.EmailChangeCodeHash))
                throw new InvalidOperationException("No code found. Request email change again.");

            if (!SlowEquals(user.EmailChangeCodeHash, HashCode(code)))
                throw new InvalidOperationException("Invalid code.");

            var newEmail = user.PendingEmail;

            // Update using UserManager methods
            var setEmail = await _users.SetEmailAsync(user, newEmail);
            if (!setEmail.Succeeded)
                throw new InvalidOperationException(string.Join("; ", setEmail.Errors.Select(e => e.Description)));

            // Keep username equal email
            var setUserName = await _users.SetUserNameAsync(user, newEmail);
            if (!setUserName.Succeeded)
                throw new InvalidOperationException(string.Join("; ", setUserName.Errors.Select(e => e.Description)));

            user.EmailConfirmed = true; // keep confirmed after change
            user.PendingEmail = null;
            user.EmailChangeCodeHash = null;
            user.EmailChangeCodeExpiresAt = null;
            user.EmailChangeResendAvailableAt = null;
            user.EmailChangeResendCount = 0;

            var upd = await _users.UpdateAsync(user);
            if (!upd.Succeeded)
                throw new InvalidOperationException(string.Join("; ", upd.Errors.Select(e => e.Description)));

            await _email.SendAsync(
                toEmail: newEmail,
                subject: "Food Order App – Email changed",
                htmlBody: "<p>Your email address has been updated successfully.</p>",
                ct: ct);

            return Ok(new { message = "Email changed." });
        }

        [HttpPost("email/change-resend")]
        public async Task<IActionResult> ResendEmailChangeCode(CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(UserId) ?? throw new InvalidOperationException("User not found.");

            if (string.IsNullOrWhiteSpace(user.PendingEmail))
                throw new InvalidOperationException("No email change requested.");

            if (!user.EmailChangeCodeExpiresAt.HasValue || user.EmailChangeCodeExpiresAt.Value <= DateTime.UtcNow)
                throw new InvalidOperationException("Code expired. Request email change again.");

            if (user.EmailChangeResendCount >= 1)
                throw new InvalidOperationException("Resend limit reached. Request email change again after expiry.");

            if (user.EmailChangeResendAvailableAt.HasValue && DateTime.UtcNow < user.EmailChangeResendAvailableAt.Value)
                throw new InvalidOperationException("You can request resend after 1 minute.");

            var code = Generate6DigitCode();
            user.EmailChangeCodeHash = HashCode(code);
            user.EmailChangeResendCount += 1;
            user.EmailChangeResendAvailableAt = DateTime.UtcNow.AddMinutes(1);

            var upd = await _users.UpdateAsync(user);
            if (!upd.Succeeded)
                throw new InvalidOperationException(string.Join("; ", upd.Errors.Select(e => e.Description)));

            await _email.SendAsync(
                toEmail: user.PendingEmail,
                subject: "Food Order App – Email change code (resend)",
                htmlBody: $@"<p>Your new code is:</p><h2>{code}</h2>",
                ct: ct);

            return Ok(new { message = "Code resent." });
        }

        [HttpGet("preferences/status-emails")]
        public async Task<IActionResult> GetStatusEmailPreference(CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(UserId) ?? throw new InvalidOperationException("User not found.");
            return Ok(new { enabled = user.WantsOrderStatusEmails });
        }

        [HttpPut("preferences/status-emails")]
        public async Task<IActionResult> SetStatusEmailPreference([FromBody] StatusEmailPreferenceDto dto, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(UserId) ?? throw new InvalidOperationException("User not found.");

            user.WantsOrderStatusEmails = dto.Enabled;

            var upd = await _users.UpdateAsync(user);
            if (!upd.Succeeded)
                throw new InvalidOperationException(string.Join("; ", upd.Errors.Select(e => e.Description)));

            return NoContent();
        }

        // helpers
        private static string Generate6DigitCode()
        {
            var bytes = RandomNumberGenerator.GetBytes(4);
            var val = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
            return val.ToString("D6");
        }

        private static string HashCode(string code)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }

        private static bool SlowEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
