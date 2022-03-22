using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tests
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
                GenerateDelegates = true
            });

            fixture.Register<ServiceCollection>(() => new ServiceCollection());

            fixture.Register<ConfigurationBuilder>(() => new ConfigurationBuilder());

            fixture.Customize<TimeSpan>(o => o.FromFactory<int>(i => TimeSpan.FromMilliseconds(i)));

            fixture.Customize<RetryPolicy>(o => o.With(p => p.BackoffMultiplier).With(p => p.InitialBackoff).With(p => p.MaxAttempts).With(p => p.MaxBackoff));

            return fixture;
        }
    }
}
