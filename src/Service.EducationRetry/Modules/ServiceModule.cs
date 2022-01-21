using Autofac;
using DotNetCoreDecorators;
using MyServiceBus.TcpClient;
using Service.EducationRetry.Domain.Models;
using Service.EducationRetry.Services;
using Service.ServerKeyValue.Client;

namespace Service.EducationRetry.Modules
{
	public class ServiceModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterKeyValueClient(Program.Settings.ServerKeyValueServiceUrl);

			var tcpServiceBus = new MyServiceBusTcpClient(() => Program.Settings.ServiceBusWriter, "MyJetEducation Service.EducationRetry");
			IPublisher<UpdateRetryUsedCountInfoServiceBusModel> clientRegisterPublisher = new MyServiceBusPublisher(tcpServiceBus);
			builder.Register(context => clientRegisterPublisher);
			tcpServiceBus.Start();
		}
	}
}