using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Service.EducationRetry.Grpc;
using Service.Grpc;

namespace Service.EducationRetry.Client
{
	[UsedImplicitly]
	public class EducationRetryClientFactory : GrpcClientFactory
	{
		public EducationRetryClientFactory(string grpcServiceUrl, ILogger logger) : base(grpcServiceUrl, logger)
		{
		}

		public IGrpcServiceProxy<IEducationRetryService> GetEducationRetryService() => CreateGrpcService<IEducationRetryService>();
	}
}