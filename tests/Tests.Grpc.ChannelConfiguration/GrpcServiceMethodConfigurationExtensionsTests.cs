using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.Idioms;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Tests.Grpc.ChannelConfiguration;

[TestFixture]
public class GrpcServiceMethodConfigurationExtensionsTests
{
    [Test]
    [CustomAutoData]
    public void ConfigureServiceMethods_does_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethod(nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethods)));

    // [Test]
    // [CustomAutoData]
    // public async Task ConfigureServiceMethods_can_customize_all_methods(ConfigurationBuilder configurationBuilder, ServiceCollection services, RetryPolicy retryPolicy, Uri uri, TestGrpcClient.Request request)
    // {
    //     var testConfiguration = new 
    //     {
    //         GRPC = new 
    //         {
    //             Default = new { RetryPolicy = retryPolicy } 
    //         }
    //     };

    //     var configuration = configurationBuilder.AddObject(testConfiguration).Build();

    //     services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethods(configuration.GetSection("GRPC"));

    //     var provider = services.BuildServiceProvider();

    //     var client = provider.GetRequiredService<TestGrpcClient>();

    //     var options = new CallOptions(new Metadata(), DateTime.MaxValue, default);

    //     var response = await client.DoStuff(request, options);
    // }

    [Test]
    [CustomAutoData]
    public void ConfigureServiceMethod_do_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethod)));

    [Test]
    [CustomAutoData]
    public void ConfigureDefaultServiceMethod_do_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.ConfigureDefaultServiceMethod)));
}

// public partial class TestGrpcClient : ClientBase<TestGrpcClient>
// {
//     public TestGrpcClient(ChannelBase channel) : base(channel)
//     {
//     }

//     public TestGrpcClient(CallInvoker callInvoker) : base(callInvoker)
//     {
//     }

//     protected TestGrpcClient(ClientBaseConfiguration configuration) : base(configuration)
//     {
//     }
    
//     protected TestGrpcClient() : base()
//     {
//     }

//     protected override TestGrpcClient NewInstance(ClientBaseConfiguration configuration)
//     {
//         return new TestGrpcClient(configuration);
//     }

//     public AsyncUnaryCall<Response> DoStuff(Request request, CallOptions options)
//     {
//         var method = new Method<Request, Response>(MethodType.Unary, "TestGrpc", "DoStuff", CreateMarshaller<Request>(), CreateMarshaller<Response>());

//         return CallInvoker.AsyncUnaryCall(method, null, options, request);
//     }

//     public class Response { }

//     public class Request { }

//     static Marshaller<T> CreateMarshaller<T>() where T : new()
//     {
//         return new Marshaller<T>(_ => Array.Empty<byte>(), _ => new T());
//     }
// }
