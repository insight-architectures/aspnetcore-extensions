using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace InsightArchitectures.AnonymousUser
{
    public class AnonymousUserMiddleware
    {
        private RequestDelegate _nextDelegate;
        private AnonymousUserOptions _options;

        public AnonymousUserMiddleware(RequestDelegate nextDelegate, AnonymousUserOptions options)
        {
            _nextDelegate = nextDelegate ?? throw new ArgumentNullException(nameof(nextDelegate));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private async Task HandleRequestAsync(HttpContext httpContext)
        {
            var cookieEncoder = _options.EncoderService ?? throw new ArgumentNullException(nameof(_options.EncoderService));
            _ = _options.ClaimType ?? throw new ArgumentNullException(nameof(_options.ClaimType));

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                return;
            }

            var encodedValue = httpContext.Request.Cookies[_options.CookieName];

            if (_options.Secure && !httpContext.Request.IsHttps)
            {
                if (!string.IsNullOrWhiteSpace(encodedValue))
                {
                    httpContext.Response.Cookies.Delete(_options.CookieName);
                }

                return;
            }

            var uid = await cookieEncoder.DecodeAsync(encodedValue);

            if (string.IsNullOrWhiteSpace(uid))
            {
                uid = Guid.NewGuid().ToString();
                var encodedUid = await cookieEncoder.EncodeAsync(uid);

                var cookieOptions = new CookieOptions
                {
                    Expires = _options.Expires
                };

                httpContext.Response.Cookies.Append(_options.CookieName, encodedUid, cookieOptions);
            }

            var identity = new ClaimsIdentity(new[] { new Claim(_options.ClaimType, uid) });
            httpContext.User.AddIdentity(identity);
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            await HandleRequestAsync(httpContext);

            await _nextDelegate.Invoke(httpContext);
        }
    }
}