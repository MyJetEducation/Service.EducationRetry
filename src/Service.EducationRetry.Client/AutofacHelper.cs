using Autofac;
using Microsoft.Extensions.Logging;
using Service.EducationRetry.Grpc;
using Service.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.EducationRetry.Client
{
	public static class AutofacHelper
	{
		public static void RegisterEducationRetryClient(this ContainerBuilder builder, string grpcServiceUrl, ILogger logger)
		{
			var factory = new EducationRetryClientFactory(grpcServiceUrl, logger);

			builder.RegisterInstance(factory.GetEducationRetryService()).As<IGrpcServiceProxy<IEducationRetryService>>().SingleInstance();
		}
	}
}