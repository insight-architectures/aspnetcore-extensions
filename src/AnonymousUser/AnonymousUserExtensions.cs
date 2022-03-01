using System;
using Microsoft.AspNetCore.Builder;

namespace InsightArchitectures.AnonymousUser
{
    /// <summary>
    /// Extension methods for the ASP NET Core application builder.
    /// </summary>
    public static class AnonymousUserExtensions
    {
        /// <summary>
        /// Adds the <see cref="AnonymousUserMiddleware" /> to the middleware pipeline.
        /// </summary>
        /// <param name="builder">The application builder object.</param>
        /// <param name="configure">An action to customise the middleware options.</param>
        public static IApplicationBuilder UseAnonymousUser(this IApplicationBuilder builder, Action<AnonymousUserOptions> configure = null)
        {
            var options = new AnonymousUserOptions();

            configure?.Invoke(options);

            _ = options.ClaimType ?? throw new ArgumentNullException(nameof(options.ClaimType));

            return builder.UseMiddleware<AnonymousUserMiddleware>(options);
        }
    }
}