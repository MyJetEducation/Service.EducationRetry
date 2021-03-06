using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.TcpClient;
using Service.Core.Client.Services;
using Service.EducationProgress.Client;
using Service.EducationRetry.Services;
using Service.ServerKeyValue.Client;
using Service.ServiceBus.Models;

namespace Service.EducationRetry.Modules
{
	public class ServiceModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterServerKeyValueClient(Program.Settings.ServerKeyValueServiceUrl, Program.LogFactory.CreateLogger(typeof(ServerKeyValueClientFactory)));
			builder.RegisterEducationProgressClient(Program.Settings.EducationProgressServiceUrl);

			builder.RegisterType<SystemClock>().AsImplementedInterfaces().SingleInstance();
			builder.RegisterType<RetryRepository>().AsImplementedInterfaces().SingleInstance();

			var tcpServiceBus = new MyServiceBusTcpClient(() => Program.Settings.ServiceBusWriter, "MyJetEducation Service.EducationRetry");

			builder
				.Register(context => new MyServiceBusPublisher<RetryUsedServiceBusModel>(tcpServiceBus, RetryUsedServiceBusModel.TopicName, false))
				.As<IServiceBusPublisher<RetryUsedServiceBusModel>>()
				.SingleInstance();

			tcpServiceBus.Start();
		}
	}
}