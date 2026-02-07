using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Auth
{
    public class ApplicationUser : IdentityUser
    {
        public string? RegistrationCodeHash { get; set; }
        public DateTime? RegistrationCodeExpiresAt { get; set; }
        public DateTime? RegistrationResendAvailableAt { get; set; }
        public int RegistrationResendCount { get; set; } = 0;
        public bool WantsOrderStatusEmails { get; set; } = false;
        public string? PendingEmail { get; set; }
        public string? EmailChangeCodeHash { get; set; }
        public DateTime? EmailChangeCodeExpiresAt { get; set; }
        public DateTime? EmailChangeResendAvailableAt { get; set; }
        public int EmailChangeResendCount { get; set; } = 0;
    }
}
