using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.EducationRetry.Grpc;

namespace Service.EducationRetry.Client
{
    [UsedImplicitly]
    public class EducationRetryClientFactory : MyGrpcClientFactory
    {
        public EducationRetryClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IEducationRetryService GetEducationRetryService() => CreateGrpcService<IEducationRetryService>();
    }
}
