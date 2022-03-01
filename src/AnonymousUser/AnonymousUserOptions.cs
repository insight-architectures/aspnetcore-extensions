using System;

namespace InsightArchitectures.AnonymousUser
{
    public class AnonymousUserOptions
    {
        public string CookieName { get; set; } = "tid";

        // Default set to approx 10 years
        public DateTimeOffset Expires { get; set; } = DateTimeOffset.UtcNow.AddDays(3652);

        public string ClaimType { get; set; }

        public bool Secure { get; set; } = false;

        public ICookieEncoder EncoderService { get; set; } = new Base64CookieEncoder();
    }
}