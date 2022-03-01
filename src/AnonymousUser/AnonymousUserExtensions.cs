using System;
using Microsoft.AspNetCore.Builder;

namespace InsightArchitectures.AnonymousUser
{
    public static class AnonymousUserExtensions
    {
        public static IApplicationBuilder UseAnonymousUser(this IApplicationBuilder builder, Action<AnonymousUserOptions> configure = null)
        {
            var options = new AnonymousUserOptions();
            
            configure?.Invoke(options);

            _ = options.ClaimType ?? throw new ArgumentNullException(nameof(options.ClaimType));

            return builder.UseMiddleware<AnonymousUserMiddleware>(options);
        }
    }
}