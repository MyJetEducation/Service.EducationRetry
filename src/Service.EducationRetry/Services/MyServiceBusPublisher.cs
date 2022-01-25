using System.Threading.Tasks;
using DotNetCoreDecorators;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.TcpClient;
using Service.EducationRetry.Grpc.ServiceBusModel;

namespace Service.EducationRetry.Services
{
	public class MyServiceBusPublisher : IPublisher<RetryUsedServiceBusModel>
	{
		private readonly MyServiceBusTcpClient _client;

		public MyServiceBusPublisher(MyServiceBusTcpClient client)
		{
			_client = client;
			_client.CreateTopicIfNotExists(RetryUsedServiceBusModel.TopicName);
		}

		public ValueTask PublishAsync(RetryUsedServiceBusModel valueToPublish)
		{
			byte[] bytesToSend = valueToPublish.ServiceBusContractToByteArray();

			Task task = _client.PublishAsync(RetryUsedServiceBusModel.TopicName, bytesToSend, false);

			return new ValueTask(task);
		}
	}
}