using Grpc.Core;

namespace Tests.Grpc.ChannelConfiguration;

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

    protected override TestGrpcClient NewInstance(ClientBaseConfiguration configuration) => new TestGrpcClient(configuration);
}
