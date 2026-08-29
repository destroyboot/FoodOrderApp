using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Interfaces;
using Infrastructure.Persistence;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly IConfiguration _cfg;
        private readonly IEmailSender _email;
        private readonly AppDbContext _db;

        public AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IConfiguration cfg, IEmailSender email, AppDbContext db)
        {
            _users = users;
            _signIn = signIn;
            _cfg = cfg;
            _email = email;
            _db = db;
        }

        public record RegisterRequest(string Email, string Password);
        public record LoginRequest(string Email, string Password);
        public record AuthResponse(string Token);
        public record ConfirmRegistrationRequest(string Email, string Code);
        public record ResendRegistrationCodeRequest(string Email);
        public record ForgotPasswordRequest(string Email);
        public record ResetPasswordRequest(string Email, string Token, string NewPassword);
        public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Email is required.");
            if (string.IsNullOrWhiteSpace(req.Password)) throw new InvalidOperationException("Password is required.");

            var existing = await _users.FindByEmailAsync(email);

            if (existing is not null)
            {
                if (existing.EmailConfirmed)
                    throw new InvalidOperationException("An account with this email already exists.");

                if (existing.RegistrationCodeExpiresAt.HasValue && existing.RegistrationCodeExpiresAt.Value > DateTime.UtcNow)
                {
                    throw new InvalidOperationException("Registration already started. Please confirm the code or use resend.");
                }

                var del = await _users.DeleteAsync(existing);
                if (!del.Succeeded)
                {
                    var errors = string.Join("; ", del.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Cannot restart registration: {errors}");
                }
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                RegisteredAtUtc = DateTime.UtcNow
            };

            var create = await _users.CreateAsync(user, req.Password);
            if (!create.Succeeded)
            {
                var errors = string.Join("; ", create.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errors);
            }

            var code = Generate6DigitCode();
            user.RegistrationCodeHash = HashCode(code);
            user.RegistrationCodeExpiresAt = DateTime.UtcNow.AddMinutes(20);
            user.RegistrationResendAvailableAt = DateTime.UtcNow.AddMinutes(1);
            user.RegistrationResendCount = 0;

            var upd = await _users.UpdateAsync(user);
            if (!upd.Succeeded)
            {
                var errors = string.Join("; ", upd.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errors);
            }

            await _email.SendAsync(
                toEmail: email,
                subject: "Food Order App – Confirm your account",
                htmlBody: $@"
                <p>Your confirmation code is:</p>
                <h2 style='letter-spacing:2px;'>{code}</h2>
                <p>This code is valid for 20 minutes.</p>
            ",
                ct: ct);

            return Ok(new { message = "Confirmation code sent." });
        }

        [HttpPost("confirm-registration")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmRegistration([FromBody] ConfirmRegistrationRequest req, CancellationToken ct)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var code = (req.Code ?? "").Trim();

            var user = await _users.FindByEmailAsync(email);
            if (user is null) throw new InvalidOperationException("Invalid code or email.");

            if (user.EmailConfirmed) return Ok(new { message = "Account already confirmed." });

            if (!user.RegistrationCodeExpiresAt.HasValue || user.RegistrationCodeExpiresAt.Value <= DateTime.UtcNow)
                throw new InvalidOperationException("Confirmation code expired. Please register again.");

            if (string.IsNullOrWhiteSpace(user.RegistrationCodeHash))
                throw new InvalidOperationException("No confirmation code found. Please register again.");

            if (!SlowEquals(user.RegistrationCodeHash, HashCode(code)))
                throw new InvalidOperationException("Invalid code or email.");

            user.EmailConfirmed = true;
            user.RegistrationCodeHash = null;
            user.RegistrationCodeExpiresAt = null;
            user.RegistrationResendAvailableAt = null;
            user.RegistrationResendCount = 0;

            var upd = await _users.UpdateAsync(user);
            if (!upd.Succeeded)
            {
                var errors = string.Join("; ", upd.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errors);
            }

            await _email.SendAsync(
                toEmail: email,
                subject: "Food Order App – Account activated",
                htmlBody: "<p>Your account is now active. You can log in and start ordering.</p>",
                ct: ct);

            await LinkRestaurantInvitesAsync(user, ct);

            return Ok(new { message = "Account confirmed." });
        }

        [HttpPost("resend-registration-code")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendRegistrationCode([FromBody] ResendRegistrationCodeRequest req, CancellationToken ct)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var user = await _users.FindByEmailAsync(email);

            if (user is null) return Ok(new { message = "If the email exists, a code was sent." });

            if (user.EmailConfirmed) return Ok(new { message = "Account already confirmed." });

            if (!user.RegistrationCodeExpiresAt.HasValue || user.RegistrationCodeExpiresAt.Value <= DateTime.UtcNow)
            {
                return Ok(new { message = "If the email exists, a code was sent." });
            }

            if (user.RegistrationResendCount >= 1)
                throw new InvalidOperationException("Resend limit reached. Please wait until the code expires and register again.");

            if (user.RegistrationResendAvailableAt.HasValue && DateTime.UtcNow < user.RegistrationResendAvailableAt.Value)
                throw new InvalidOperationException("You can request resend after 1 minute.");

            var code = Generate6DigitCode();
            user.RegistrationCodeHash = HashCode(code);
            user.RegistrationResendCount += 1;
            user.RegistrationResendAvailableAt = DateTime.UtcNow.AddMinutes(1);

            var upd = await _users.UpdateAsync(user);
            if (!upd.Succeeded)
            {
                var errors = string.Join("; ", upd.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errors);
            }

            await _email.SendAsync(
                toEmail: email,
                subject: "Food Order App – Your confirmation code (resend)",
                htmlBody: $@"
                <p>Your new confirmation code is:</p>
                <h2 style='letter-spacing:2px;'>{code}</h2>
                <p>This code is valid until: {user.RegistrationCodeExpiresAt:yyyy-MM-dd HH:mm} UTC</p>
            ",
                ct: ct);

            return Ok(new { message = "If the email exists, a code was sent." });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var user = await _users.FindByEmailAsync(email);

            if (user is null || !user.EmailConfirmed)
                return Ok(new { message = "If the email exists, reset instructions were sent." });

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var encodedToken = System.Net.WebUtility.UrlEncode(token);
            var encodedEmail = System.Net.WebUtility.UrlEncode(email);

            var frontendBaseUrl = _cfg["Frontend:BaseUrl"]?.TrimEnd('/')
                ?? "http://localhost:5173";

            var resetLink = $"{frontendBaseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

            await _email.SendAsync(
                toEmail: email,
                subject: "Food Order App – Password reset",
                htmlBody: $@"
                    <p>Click the link below to reset your password:</p>
                    <p><a href=""{resetLink}"">Reset password</a></p>
                    <p>This link is valid for a limited time.</p>
                    <p>If you didn’t request this, ignore this message.</p>
                ",
                ct: ct);

            return Ok(new { message = "If the email exists, reset instructions were sent." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var user = await _users.FindByEmailAsync(email);

            if (user is null || !user.EmailConfirmed)
                return Ok(new { message = "Password reset processed if token was valid." });

            var result = await _users.ResetPasswordAsync(user, req.Token, req.NewPassword);

            if (!result.Succeeded)
                throw new InvalidOperationException("Invalid token or password does not meet requirements.");

            await _email.SendAsync(
                toEmail: email,
                subject: "Food Order App – Password changed",
                htmlBody: "<p>Your password was changed successfully.</p>",
                ct: ct);

            return Ok(new { message = "Password reset processed if token was valid." });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _users.FindByIdAsync(userId);
            if (user is null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(req.CurrentPassword))
                throw new InvalidOperationException("Current password is required.");

            if (string.IsNullOrWhiteSpace(req.NewPassword))
                throw new InvalidOperationException("New password is required.");

            var result = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errors);
            }

            await _email.SendAsync(
                toEmail: user.Email ?? "",
                subject: "Food Order App – Password changed",
                htmlBody: "<p>Your password was changed successfully.</p>",
                ct: ct);

            return Ok(new { message = "Password changed successfully." });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
        {
            var user = await _users.FindByEmailAsync(req.Email);
            if (user is null) return Unauthorized();

            var check = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
            if (!check.Succeeded) return Unauthorized();

            if (user.EmailConfirmed)
            {
                await LinkRestaurantInvitesAsync(user, CancellationToken.None);
            }

            user.LastLoginAtUtc = DateTime.UtcNow;
            user.SuccessfulLoginCount += 1;
            await _users.UpdateAsync(user);

            var roles = await _users.GetRolesAsync(user);

            var token = CreateJwt(user, roles);
            return Ok(new AuthResponse(token));
        }

        private string CreateJwt(ApplicationUser user, IList<string> roles)
        {
            var jwt = _cfg.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Email ?? "")
        };

            foreach (var r in roles)
                claims.Add(new Claim(ClaimTypes.Role, r));

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

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

        private async Task LinkRestaurantInvitesAsync(ApplicationUser user, CancellationToken ct)
        {
            var email = (user.Email ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                return;

            var invites = await _db.RestaurantStaffInvites
                .Where(x => x.Email == email && x.UserId == null && x.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);

            if (invites.Count == 0)
                return;

            foreach (var invite in invites)
            {
                invite.UserId = user.Id;
                invite.AcceptedAt ??= DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
