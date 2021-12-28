using Autofac;
using Service.EducationRetry.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.EducationRetry.Client
{
    public static class AutofacHelper
    {
        public static void RegisterEducationRetryClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new EducationRetryClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetEducationRetryService()).As<IEducationRetryService>().SingleInstance();
        }
    }
}
