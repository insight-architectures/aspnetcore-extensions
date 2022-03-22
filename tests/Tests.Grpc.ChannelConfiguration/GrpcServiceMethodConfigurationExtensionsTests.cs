using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.Idioms;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Tests.Grpc.ChannelConfiguration;

[TestFixture]
public class GrpcServiceMethodConfigurationExtensionsTests
{
    [Test]
    [CustomAutoData]
    public void ConfigureServiceMethods_does_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethod(nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethods)));

    [Test, CustomAutoData]
    public void ConfigureServiceMethods_can_customize_all_methods(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode, IHttpClientBuilder builder)
    {
        retryPolicy.RetryableStatusCodes.Add(statusCode);

        var testConfiguration = new
        {
            GRPC = new Dictionary<string, object>
            {
                [GrpcServiceMethodConfigurationExtensions.FallbackConfigurationKeyName] = new 
                { 
                    RetryPolicy = new
                    {
                        retryPolicy.MaxAttempts,
                        retryPolicy.InitialBackoff,
                        retryPolicy.BackoffMultiplier,
                        retryPolicy.RetryableStatusCodes
                    }
                } 
            }
        };

        var configuration = configurationBuilder.AddObject(testConfiguration).Build();

        builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethods(configuration.GetSection("GRPC"));

        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

        Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

        var configurationDelegate = options.ChannelOptionsActions[0];

        var channelOptions = new GrpcChannelOptions();

        configurationDelegate(channelOptions);

        Assert.Multiple(() =>
        {
            Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

            Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

            Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

            Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.Null);

            Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.Null);

            Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

            Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

            Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

            Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

            Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
        });
    }

    [Test]
    [CustomAutoData]
    public void ConfigureServiceMethod_do_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethod)));

    [Test]
    [CustomAutoData]
    public void ConfigureDefaultServiceMethod_do_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.ConfigureDefaultServiceMethod)));
}

public partial class TestGrpcClient : ClientBase<TestGrpcClient>
{
    public TestGrpcClient(ChannelBase channel) : base(channel)
    {
    }

    public TestGrpcClient(CallInvoker callInvoker) : base(callInvoker)
    {
    }

    protected TestGrpcClient(ClientBaseConfiguration configuration) : base(configuration)
    {
    }
    
    protected TestGrpcClient() : base()
    {
    }

    protected override TestGrpcClient NewInstance(ClientBaseConfiguration configuration)
    {
        return new TestGrpcClient(configuration);
    }

    // public AsyncUnaryCall<Response> DoStuff(Request request, CallOptions options)
    // {
    //     var method = new Method<Request, Response>(MethodType.Unary, "TestGrpc", "DoStuff", CreateMarshaller<Request>(), CreateMarshaller<Response>());

    //     return CallInvoker.AsyncUnaryCall(method, null, options, request);
    // }

    // public class Response { }

    // public class Request { }

    // static Marshaller<T> CreateMarshaller<T>() where T : new()
    // {
    //     return new Marshaller<T>(_ => Array.Empty<byte>(), _ => new T());
    // }
}
