using Autofac;
using DotNetCoreDecorators;
using MyServiceBus.TcpClient;
using Service.Core.Domain;
using Service.EducationRetry.Grpc.ServiceBusModel;
using Service.EducationRetry.Services;
using Service.ServerKeyValue.Client;

namespace Service.EducationRetry.Modules
{
	public class ServiceModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterKeyValueClient(Program.Settings.ServerKeyValueServiceUrl);
			builder.RegisterType<SystemClock>().AsImplementedInterfaces().SingleInstance();

			var tcpServiceBus = new MyServiceBusTcpClient(() => Program.Settings.ServiceBusWriter, "MyJetEducation Service.EducationRetry");
			IPublisher<RetryUsedServiceBusModel> clientRegisterPublisher = new MyServiceBusPublisher(tcpServiceBus);
			builder.Register(context => clientRegisterPublisher);
			tcpServiceBus.Start();
		}
	}
}