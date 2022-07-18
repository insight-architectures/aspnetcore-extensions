using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using InsightArchitectures.Extensions.AspNetCore.AnonymousUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Moq;
using NUnit.Framework;

namespace AnonymousUserTests
{
    public class AnonymousUserMiddlewareTests
    {
        [Test, CustomAutoData]
        public async Task NoCookiesShouldCreateCookie(HttpContext context, [Frozen] AnonymousUserOptions options, AnonymousUserMiddleware sut)
        {
            await sut.InvokeAsync(context);

            var actual = GetCookieValueFromResponse(context.Response, options.CookieName);

            Assert.IsFalse(string.IsNullOrWhiteSpace(actual));
        }

        [Test, CustomAutoData]
        public async Task ExistingCookieShouldNotAddCookieToResponse(HttpContext context, [Frozen] Mock<HttpRequest> httpRequest, [Frozen] AnonymousUserOptions options, AnonymousUserMiddleware sut)
        {
            var cookies = new Dictionary<string, string>
            {
                [options.CookieName] = await options.EncoderService.EncodeAsync("RANDOM")
            };
            httpRequest.Setup(x => x.Cookies).Returns(new RequestCookieCollection(cookies));

            await sut.InvokeAsync(context);

            var actual = GetCookieValueFromResponse(context.Response, options.CookieName);

            Assert.IsTrue(string.IsNullOrWhiteSpace(actual));
        }

        [Test, CustomAutoData]
        public async Task SecureCookieWithHttpShouldExpire(HttpContext context, [Frozen] Mock<HttpRequest> httpRequest, [Frozen] AnonymousUserOptions options, AnonymousUserMiddleware sut)
        {
            var cookies = new Dictionary<string, string>
            {
                [options.CookieName] = await options.EncoderService.EncodeAsync("RANDOM")
            };
            httpRequest.Setup(x => x.Cookies).Returns(new RequestCookieCollection(cookies));

            options.Secure = true;

            await sut.InvokeAsync(context);

            var actual = GetCookieValueFromResponse(context.Response, options.CookieName);

            Assert.IsEmpty(actual);
        }

        [Test, CustomAutoData]
        public async Task AuthenticatedUserShouldSkipMiddleware(HttpContext context, [Frozen] Mock<ClaimsPrincipal> claimsPrincipal, AnonymousUserMiddleware sut)
        {
            var identityMock = new Mock<ClaimsIdentity>(() => new ClaimsIdentity(null, "Test"));
            claimsPrincipal.Setup(x => x.Identity).Returns(identityMock.Object);
            identityMock.Setup(x => x.IsAuthenticated).Returns(true);
            
            claimsPrincipal.Setup(x => x.AddIdentity(It.IsAny<ClaimsIdentity>())).Verifiable();
            identityMock.Setup(x => x.AddClaim(It.IsAny<Claim>())).Verifiable();

            context.User = claimsPrincipal.Object;

            await sut.InvokeAsync(context);

            claimsPrincipal.Verify(x => x.AddIdentity(It.IsAny<ClaimsIdentity>()), Times.Never);
            identityMock.Verify(x => x.AddClaim(It.IsAny<Claim>()), Times.Never);
        }

        private string? GetCookieValueFromResponse(HttpResponse response, string cookieName)
        {
            foreach (var headers in response.Headers.Values)
            {
                foreach (var header in headers)
                {
                    if (header.StartsWith(cookieName))
                    {
                        {
                            var p1 = header.IndexOf('=');
                            var p2 = header.IndexOf(';');
                            return header.Substring(p1 + 1, p2 - p1 - 1);
                        }
                    }
                }
            }

            return null;
        }
    }
}
