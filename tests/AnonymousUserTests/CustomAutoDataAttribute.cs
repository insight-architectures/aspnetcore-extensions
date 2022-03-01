using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using InsightArchitectures.Extensions.AspNetCore.AnonymousUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Moq;

namespace AnonymousUserTests
{

    [AttributeUsage(AttributeTargets.Method)]
    public class CustomAutoDataAttribute : AutoDataAttribute
    {
        public CustomAutoDataAttribute() : base(FixtureHelpers.CreateFixture)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CustomInlineAutoDataAttribute : InlineAutoDataAttribute
    {
        public CustomInlineAutoDataAttribute(params object[] args) : base(FixtureHelpers.CreateFixture, args)
        {
        }
    }

    internal static class FixtureHelpers
    {
        public static IFixture CreateFixture()
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
        }
    }
}