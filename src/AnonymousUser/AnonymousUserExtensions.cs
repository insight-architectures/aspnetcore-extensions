using System;
using InsightArchitectures.Extensions.AspNetCore.AnonymousUser;

namespace Microsoft.AspNetCore.Builder
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

            _ = options.ClaimType ?? throw new NullReferenceException($"{nameof(options.ClaimType)} is null. Please provide a claim type name when configuring the middleware.");

            return builder.UseMiddleware<AnonymousUserMiddleware>(options);
        }
    }
}