using System.Security.Claims;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using InsightArchitectures.AnonymousUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Moq;

namespace AnonymousUserTests
{
    public class MoqAutoDataAttribute : AutoDataAttribute
    {
        public MoqAutoDataAttribute() : base(() =>
        {
            var fixture = new Fixture();

            fixture.Customize(new AutoMoqCustomization
            {
                ConfigureMembers = true,
                GenerateDelegates = true,
            });

            fixture.Customize<Mock<HttpRequest>>(sb => sb.FromFactory(() => new Mock<HttpRequest>()));

            var requestMock = fixture.Freeze<Mock<HttpRequest>>();
            requestMock.Setup(x => x.Cookies).Returns(new RequestCookieCollection());
            requestMock.Setup(x => x.IsHttps).Returns(false);

            var contextMock = fixture.Freeze<Mock<HttpContext>>();
            contextMock.Setup(x => x.Response).Returns(new DefaultHttpResponse(new DefaultHttpContext()));

            fixture.Customize<AnonymousUserOptions>(sb =>
            {
                return sb.With(x => x.EncoderService, new Base64CookieEncoder())
                        .With(x => x.Secure, false);
            });

            return fixture;
        })
        { }
    }
}