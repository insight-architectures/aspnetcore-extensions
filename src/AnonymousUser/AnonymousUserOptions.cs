using System;

namespace InsightArchitectures.AnonymousUser
{
    /// <summary>
    /// Configuration options for the middleware.
    /// </summary>
    public class AnonymousUserOptions
    {
        /// <summary>The name of the cookie.</summary>
        public string CookieName { get; set; } = "tid";

        /// <summary>The expiration date of the cookie. Default set to 10 years.</summary>
        public DateTimeOffset Expires { get; set; } = DateTimeOffset.UtcNow.AddDays(3652);

        /// <summary>The type name of the claim holding the ID.</summary>
        public string ClaimType { get; set; }

        /// <summary>Should the cookie only be allowed on https requests.</summary>
        public bool Secure { get; set; }

        /// <summary>The encoder service to encode/decode the cookie value. Default set to internal base64 encoder.</summary>
        public ICookieEncoder EncoderService { get; set; } = new Base64CookieEncoder();
    }
}